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

def search_listings(query, *, category_id, limit, token):
    if query == "erro403":
        raise ValueError("HTTP 403")
    return [{"id":"MLB123456789","title":"Produto de teste",
             "category_id":"MLB123","permalink":"https://www.mercadolivre.com.br/item/MLB123456789",
             "price":123.45,"currency":"BRL"}]
""".strip(), encoding="utf-8"
        )
        (automation / "ml_ingest.py").write_text(
            """
from urllib.parse import urlparse
def validate_url(url):
    if urlparse(url).hostname != "www.mercadolivre.com.br":
        raise ValueError("URL inválida")
def extract_item_id(url):
    return url.rsplit("/", 1)[-1]
""".strip(), encoding="utf-8"
        )
        (automation / "garimpo.py").write_text(
            """
from pathlib import Path
import sys
if sys.argv[1] != "ingest-url":
    sys.exit(2)
Path(__file__).parent.joinpath("imported.txt").write_text(sys.argv[2], encoding="utf-8")
""".strip(), encoding="utf-8"
        )

    def run_bridge(self, action, *args):
        result = subprocess.run(
            [sys.executable, str(BRIDGE), "--root", str(self.root), "--action", action, *args],
            stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding="utf-8",
            timeout=10
        )
        return result.returncode, json.loads(result.stdout)

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
        self.assertEqual(obj["error"], "API oficial Mercado Livre retornou acesso negado (401/403); não há contorno.")
        self.assertNotIn("items", obj)

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
