"""Testes isolados do contrato JSON (fixtures locais; nenhuma chamada à API real)."""
from __future__ import annotations

import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

BRIDGE = Path(__file__).resolve().parents[1] / "ml_bridge.py"


class BridgeTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        automation = self.root / "automation"
        automation.mkdir()
        (automation / "ml_discovery.py").write_text(
            """
def read_token():
    return "fixture-private-token"

def root_categories(*, token):
    assert token == "fixture-private-token"
    return [{"id":"MLB123", "name":"Casa"}], "api"

def children_of(category_id, *, token):
    return [{"id":"MLB456","name":"Decoração"}], {"id":category_id}

class DiscoveryError(ValueError):
    pass

def _api_get(path, params=None, token=""):
    if path == "/categories/MLB123":
        return {"path_from_root": [
            {"id":"MLB456","name":"Casa"},
            {"id":"MLB123","name":"Quadros decorativos"}]}
    if path == "/items/MLB123456789":
        return {"id":"MLB123456789", "category_id":"MLB123",
                "title":"Abajur LED oficial", "status":"active",
                "price":88.70, "currency_id":"BRL",
                "permalink":"https://www.mercadolivre.com.br/item/MLB123456789"}
    if path == "/items/MLB123456789/description":
        return {"plain_text":"Luminária LED com estrutura metálica, conforme anúncio."}
    if path == "/items/MLB987654321":
        return {"id":"MLB987654321", "category_id":"MLB123"}
    if path == "/items/MLB987654321/description":
        raise DiscoveryError("HTTP 403")
    if path == "/products/MLBU300001":
        return {"id":"MLBU300001", "name":"Luminária de catálogo", "category_id":"MLB123",
                "short_description":{"type":"text","content":"Luminária de catálogo com iluminação ajustável."}}
    if path == "/products/MLBU300002":
        return {"id":"MLBU300002", "attributes":[{"name":"Cor","value_name":"Branco"},
                            {"name":"Material","value_name":"Metal"}]}
    if path == "/products/MLBU300003":
        return {"id":"MLBU300003", "attributes":[]}
    if path == "/products/MLBU300004":
        raise DiscoveryError("HTTP 403")
    if path == "/highlights/MLB/category/MLB123":
        return {"content":[{"id":"MLB123456789","type":"ITEM","position":1},
                           {"id":"MLBU300001","type":"PRODUCT","position":2},
                           {"id":"MLB000000001","type":"UNKNOWN","position":3}]}
    if path == "/highlights/MLB/category/MLB999":
        raise DiscoveryError("HTTP 403")
    if path == "/sites/MLB/domain_discovery/search":
        return [{"category_id":"MLB123", "category_name":"Quadros decorativos"}]
    assert path == "/products/search"
    if params["q"] == "ambos403":
        raise DiscoveryError("HTTP 403")
    return {"results": [{
        "id":"MLBU300001", "name":"Produto de referência catalogado",
        "pictures":[{"secure_url":"https://http2.mlstatic.com/catalog.jpg"}]
    }]}

def search_listings(query, *, category_id, limit, token):
    if query == "erro403":
        raise ValueError("HTTP 403")
    if query in ("busca403", "ambos403"):
        raise DiscoveryError("Busca oficial bloqueada (HTTP 403)")
    return [{"id":"MLB123456789","title":"Produto de teste",
             "category_id":"MLB123","permalink":"https://www.mercadolivre.com.br/item/MLB123456789",
             "thumbnail":"https://http2.mlstatic.com/miniatura.jpg",
             "price":123.45,"currency":"BRL"}]
""".strip(), encoding="utf-8"
        )
        (automation / "ml_ingest.py").write_text(
            """
from urllib.parse import urlparse
def validate_url(url, *, image=False):
    if urlparse(url).hostname != ("http2.mlstatic.com" if image else "www.mercadolivre.com.br"):
        raise ValueError("URL inválida")
def extract_item_id(url):
    return url.rsplit("/", 1)[-1]
def get_product_metadata(url, *, token=None):
    validate_url(url)
    return {"title":"Produto extraído da URL", "item_id":"MLB123456789",
            "summary":"Descrição de origem pública, revisão necessária.",
            "source_url":url, "resolved_url":url,
            "ml_category_id":"MLB123", "image_url":"https://http2.mlstatic.com/fixture.jpg",
            "metadata_verified":False, "metadata_source":"public_page_unverified"}
""".strip(), encoding="utf-8"
        )
        (automation / "ml_oauth.py").write_text(
            """
import time
EXPIRY_SKEW_SECONDS = 120
def read_credentials():
    return {"expires_at": int(time.time()) + 3600,
            "access_token": "secret-must-not-appear",
            "client_secret": "another-hidden-secret",
            "refresh_token": "refresh-secret"}
""".strip(), encoding="utf-8"
        )
        (automation / "garimpo.py").write_text(
            """
from pathlib import Path
import json
import sys

DEFAULT_STORE = Path(__file__).parent / "data" / "pins.json"

def load_store(path):
    return json.loads(path.read_text(encoding="utf-8"))

if __name__ == "__main__":
    if sys.argv[1] != "ingest-url":
        sys.exit(2)
    url = sys.argv[2]
    Path(__file__).parent.joinpath("imported.txt").write_text(url, encoding="utf-8")
    DEFAULT_STORE.parent.mkdir(exist_ok=True)
    DEFAULT_STORE.write_text(json.dumps({
        "pins": {"fixture": {
            "source_url": url, "resolved_url": url,
            "item_id": "MLB123456789", "title": "Produto de teste",
            "ml_category_id": "MLB123", "source_image_url": "https://http2.mlstatic.com/fixture.jpg"
        }}
    }), encoding="utf-8")
""".strip(), encoding="utf-8"
        )

    def run_bridge(self, action, *args):
        result = subprocess.run(
            [sys.executable, str(BRIDGE), "--root", str(self.root), "--action", action, *args],
            stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding="utf-8",
            timeout=10
        )
        return result.returncode, json.loads(result.stdout)

    def test_oauth_status_does_not_export_tokens(self):
        code, obj = self.run_bridge("oauth_status")
        self.assertEqual(code, 0)
        self.assertTrue(obj["authenticated"])
        self.assertTrue(obj["token_valid"])
        self.assertEqual(set(obj.keys()), {"ok", "authenticated", "token_valid", "origin"})
        self.assertNotIn("secret", json.dumps(obj))

    def test_oauth_status_without_login(self):
        oauth = self.root / "automation" / "ml_oauth.py"
        oauth.write_text(
            "EXPIRY_SKEW_SECONDS = 120\ndef read_credentials():\n    return None\n",
            encoding="utf-8",
        )
        code, obj = self.run_bridge("oauth_status")
        self.assertEqual(code, 0)
        self.assertFalse(obj["authenticated"])
        self.assertFalse(obj["token_valid"])

    def test_suggest_real_listing_preserves_url_and_image(self):
        code, obj = self.run_bridge("suggest", "--query", "varal", "--category", "")
        self.assertEqual(code, 0)
        self.assertEqual(obj["origin"], "anuncios_oficiais")
        self.assertEqual(obj["suggestions"][0]["kind"], "listing")
        self.assertEqual(obj["suggestions"][0]["id"], "MLB123456789")
        self.assertTrue(obj["suggestions"][0]["permalink"].startswith("https://www.mercadolivre"))
        self.assertIn("mlstatic.com", obj["suggestions"][0]["thumbnail"])

    def test_catalog_suggestions_do_not_claim_to_be_listings(self):
        code, obj = self.run_bridge("suggest", "--query", "busca403", "--category", "")
        self.assertEqual(code, 0)
        self.assertEqual(obj["origin"], "catalogo_referencia")
        self.assertEqual(obj["suggestions"][0]["kind"], "catalog")
        self.assertEqual(obj["suggestions"][0]["id"], "MLBU300001")
        self.assertEqual(obj["suggestions"][0]["permalink"], "")
        self.assertIn("não anúncios", obj["warning"])

    def test_denied_search_and_catalog_returns_empty_not_invented(self):
        code, obj = self.run_bridge("suggest", "--query", "ambos403", "--category", "")
        self.assertEqual(code, 0)
        self.assertEqual(obj["origin"], "indisponivel")
        self.assertEqual(obj["suggestions"], [])

    def test_category_path_returns_official_hierarchy(self):
        code, obj = self.run_bridge("category_path", "--category", "MLB123")
        self.assertEqual(code, 0)
        self.assertEqual(obj["origin"], "api")
        self.assertEqual(obj["categories"][0]["name"], "Casa")
        self.assertEqual(obj["categories"][-1]["id"], "MLB123")

    def test_category_path_refuses_invalid_identifier(self):
        code, obj = self.run_bridge("category_path", "--category", "TESTE123")
        self.assertNotEqual(code, 0)
        self.assertFalse(obj["ok"])

    def test_highlights_mixed_types_are_not_all_listings(self):
        code, obj = self.run_bridge("highlights", "--category", "MLB123")
        self.assertEqual(code, 0)
        self.assertEqual(obj["origin"], "highlights_oficiais")
        self.assertEqual(len(obj["suggestions"]), 2)
        listing, product = obj["suggestions"]
        self.assertEqual(listing["kind"], "listing")
        self.assertEqual(listing["price"], 88.7)
        self.assertTrue(listing["permalink"].startswith("https://www.mercadolivre"))
        self.assertEqual(product["kind"], "catalog")
        self.assertEqual(product["permalink"], "")
        self.assertIn("1 resultados não exibidos", obj["warning"])

    def test_highlights_403_does_not_fabricate_products(self):
        code, obj = self.run_bridge("highlights", "--category", "MLB999")
        self.assertNotEqual(code, 0)
        self.assertFalse(obj["ok"])

    def test_highlights_rejects_invalid_category(self):
        code, obj = self.run_bridge("highlights", "--category", "CASA")
        self.assertNotEqual(code, 0)
        self.assertFalse(obj["ok"])

    def test_enrich_listing_gets_official_description_and_category(self):
        code, obj = self.run_bridge(
            "enrich", "--kind", "listing", "--item-id", "MLB123456789",
            "--query", "Luminária", "--category", "")
        self.assertEqual(code, 0)
        row = obj["suggestions"][0]
        self.assertEqual(row["description_source"], "descricao_do_anuncio")
        self.assertIn("estrutura metálica", row["description"])
        self.assertEqual(row["category_source"], "anuncio")
        self.assertEqual(obj["categories"][-1]["id"], "MLB123")
        self.assertFalse((self.root / "automation" / "imported.txt").exists())

    def test_enrich_catalog_gets_real_text_without_listing(self):
        code, obj = self.run_bridge(
            "enrich", "--kind", "catalog", "--item-id", "MLBU300001",
            "--query", "Luminária", "--category", "")
        self.assertEqual(code, 0)
        row = obj["suggestions"][0]
        self.assertEqual(row["kind"], "catalog")
        self.assertEqual(row["description_source"], "resumo_catalogo")
        self.assertEqual(row["description"], "Luminária de catálogo com iluminação ajustável.")
        self.assertEqual(obj["categories"][0]["name"], "Casa")
        self.assertNotIn("permalink", row)

    def test_enrich_catalog_ficha_tecnica_uses_only_api_attributes(self):
        code, obj = self.run_bridge(
            "enrich", "--kind", "catalog", "--item-id", "MLBU300002",
            "--query", "Luminária", "--category", "")
        self.assertEqual(code, 0)
        row = obj["suggestions"][0]
        self.assertEqual(row["description_source"], "ficha_tecnica_catalogo")
        self.assertIn("Cor: Branco", row["description"])
        self.assertIn("Material: Metal", row["description"])
        self.assertEqual(row["category_source"], "predicao")

    def test_enrich_catalog_without_description_does_not_fabricate(self):
        code, obj = self.run_bridge(
            "enrich", "--kind", "catalog", "--item-id", "MLBU300003",
            "--query", "Luminária", "--category", "")
        self.assertEqual(code, 0)
        self.assertEqual(obj["suggestions"][0]["description"], "")
        self.assertIn("não forneceu descrição", obj["warning"])

    def test_enrich_listing_forbidden_description_remains_empty(self):
        code, obj = self.run_bridge(
            "enrich", "--kind", "listing", "--item-id", "MLB987654321",
            "--query", "Luminária", "--category", "")
        self.assertEqual(code, 0)
        self.assertEqual(obj["suggestions"][0]["description"], "")
        self.assertIn("indisponível", obj["warning"])

    def test_enrich_rejects_invalid_suggestion_type(self):
        code, obj = self.run_bridge(
            "enrich", "--kind", "local", "--item-id", "MLBU300001",
            "--query", "Luminária", "--category", "")
        self.assertNotEqual(code, 0)
        self.assertFalse(obj["ok"])

    def test_preview_by_url_does_not_write_to_python_queue(self):
        url = "https://www.mercadolivre.com.br/item/MLB123456789"
        code, obj = self.run_bridge("preview_url", "--url", url)
        self.assertEqual(code, 0)
        self.assertEqual(obj["suggestions"][0]["name"], "Produto extraído da URL")
        self.assertFalse(obj["suggestions"][0]["verified"])
        self.assertFalse((self.root / "automation" / "imported.txt").exists())

    def test_preview_url_denies_foreign_host(self):
        code, obj = self.run_bridge("preview_url", "--url", "https://example.com/MLB123456789")
        self.assertNotEqual(code, 0)
        self.assertFalse(obj["ok"])

    def test_categories_and_children(self):
        code, obj = self.run_bridge("categories")
        self.assertEqual(code, 0)
        self.assertEqual(obj["origin"], "api")
        self.assertEqual(obj["categories"][0]["id"], "MLB123")
        code, obj = self.run_bridge("children", "--category", "MLB123")
        self.assertEqual(code, 0)
        self.assertEqual(obj["categories"][0]["id"], "MLB456")

    def test_search_preserves_listing_permalink(self):
        code, obj = self.run_bridge("search", "--query", "organizador", "--category", "")
        self.assertEqual(code, 0)
        self.assertEqual(obj["items"][0]["id"], "MLB123456789")
        self.assertEqual(obj["items"][0]["currency"], "BRL")
        self.assertNotIn("fixture-private-token", json.dumps(obj))

    def test_denied_access_is_not_turned_into_fake_result(self):
        code, obj = self.run_bridge("search", "--query", "erro403")
        self.assertNotEqual(code, 0)
        self.assertFalse(obj["ok"])
        self.assertIn("HTTP 403", obj["error"])
        self.assertIn("permissão da aplicação", obj["error"])
        self.assertIn("URL real", obj["error"])
        self.assertNotIn("items", obj)

    def test_manual_url_import_returns_verified_queue_fields(self):
        url = "https://www.mercadolivre.com.br/item/MLB123456789"
        code, obj = self.run_bridge("manual", "--url", url)
        self.assertEqual(code, 0)
        self.assertTrue(obj["ok"])
        self.assertEqual(obj["items"][0]["id"], "MLB123456789")
        self.assertEqual(obj["items"][0]["thumbnail"], "https://http2.mlstatic.com/fixture.jpg")
        self.assertEqual(obj["origin"], "url_manual")

    def test_manual_url_blocks_external_domain(self):
        code, obj = self.run_bridge("manual", "--url", "https://example.com/item/MLB123456789")
        self.assertNotEqual(code, 0)
        self.assertFalse(obj["ok"])

    def test_ingest_checks_matching_listing_url(self):
        code, obj = self.run_bridge(
            "ingest", "--url", "https://www.mercadolivre.com.br/item/MLB123456789",
            "--item-id", "MLB123456789"
        )
        self.assertEqual(code, 0)
        self.assertTrue(obj["ok"])
        self.assertTrue((self.root / "automation" / "imported.txt").exists())
        code, obj = self.run_bridge(
            "ingest", "--url", "https://www.mercadolivre.com.br/item/MLB123456789",
            "--item-id", "MLB987654321"
        )
        self.assertNotEqual(code, 0)
        self.assertFalse(obj["ok"])


if __name__ == "__main__":
    unittest.main()
