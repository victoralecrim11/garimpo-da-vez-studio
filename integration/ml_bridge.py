"""Adaptador JSON do Garimpo da Vez: reutiliza os módulos do repositório Python original."""
from __future__ import annotations

import argparse
import json
import time
from pathlib import Path
import re
import subprocess
import sys
from urllib.parse import urlsplit


def emit(data: dict) -> int:
    print(json.dumps(data, ensure_ascii=False, separators=(",", ":")))
    return 0



def safe_picture(value: object) -> str:
    """Aceita apenas miniaturas HTTPS oficiais, sem baixar conteúdo."""
    if not isinstance(value, str) or len(value) > 2048:
        return ""
    parts = urlsplit(value)
    host = (parts.hostname or "").lower()
    return value if (parts.scheme == "https" and not parts.username and not parts.password
                     and parts.port in (None, 443)
                     and (host == "mlstatic.com" or host.endswith(".mlstatic.com"))) else ""


def official_suggestions(query: str, token: str, category: str) -> dict:
    """Anúncios individuais primeiro; catálogo de referência apenas se busca for bloqueada."""
    from automation.ml_discovery import search_listings, _api_get, DiscoveryError
    if len(query.strip()) < 3 or len(query) > 100:
        raise ValueError("Informe pelo menos três caracteres no nome do produto.")
    try:
        listings = search_listings(query, category_id=category, limit=12, token=token)
        return {"ok": True, "origin": "anuncios_oficiais", "suggestions": [
            {"id": row["id"], "name": row["title"], "kind": "listing",
             "category_id": row.get("category_id") or "", "permalink": row.get("permalink") or "",
             "thumbnail": safe_picture(row.get("thumbnail") or ""),
             "description": "", "verified": True}
            for row in listings
        ]}
    except DiscoveryError as exc:
        if "HTTP 401" not in str(exc) and "HTTP 403" not in str(exc):
            raise
        # Busca por anúncios pode ser negada; catálogo é recurso distinto, não contorno.
        # Um produto de catálogo NÃO é anúncio individual com link de comissão.
        try:
            raw = _api_get("/products/search", {
                "site_id": "MLB", "status": "active", "q": query, "limit": 12
            }, token=token)
        except DiscoveryError as catalog_error:
            return {"ok": True, "origin": "indisponivel", "suggestions": [],
                    "warning": "Busca de anúncios indisponível: " + str(exc)[:200]
                               + " Catálogo também indisponível: " + str(catalog_error)[:160]}
        rows = raw.get("results", []) if isinstance(raw, dict) else []
        results = []
        for row in rows[:12]:
            if not isinstance(row, dict):
                continue
            product_id, name = row.get("id"), row.get("name")
            if not isinstance(product_id, str) or not isinstance(name, str) or not name.strip():
                continue
            images = row.get("pictures") or []
            image = images[0].get("secure_url") if images and isinstance(images[0], dict) else ""
            results.append({
                "id": product_id, "name": name.strip()[:180], "kind": "catalog",
                "category_id": "", "permalink": "",
                "thumbnail": safe_picture(image), "description": "", "verified": True
            })
        return {"ok": True, "origin": "catalogo_referencia", "suggestions": results,
                "warning": "Busca de anúncios bloqueada (401/403). Resultados são produtos do catálogo, não anúncios ou links comissionados."}


def official_preview(url: str, token: str) -> dict:
    """Pré-preenchimento sem ingestão, sem gravação da fila e sem download de imagens."""
    from automation.ml_ingest import get_product_metadata
    info = get_product_metadata(url, token=token or None)
    name = str(info.get("title") or "").strip()[:180]
    if not name:
        raise ValueError("Nenhum título disponível para esta URL; informe os dados manualmente.")
    return {"ok": True, "origin": str(info.get("metadata_source") or "indisponivel"),
            "suggestions": [{
                "id": str(info.get("item_id") or ""), "name": name, "kind": "listing",
                "category_id": str(info.get("ml_category_id") or ""),
                "permalink": str(info.get("resolved_url") or url),
                "thumbnail": safe_picture(info.get("image_url")),
                "description": str(info.get("summary") or "")[:400],
                "verified": bool(info.get("metadata_verified"))
            }],
            "warning": "Dados podem ser parciais; confira os detalhes e o link original."}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", required=True)
    parser.add_argument("--action", choices=["categories", "children", "search", "ingest", "manual", "oauth_status", "suggest", "preview_url", "category_path"], required=True)
    parser.add_argument("--category", default="")
    parser.add_argument("--query", default="")
    parser.add_argument("--url", default="")
    parser.add_argument("--item-id", default="")
    args = parser.parse_args()

    root = Path(args.root).resolve()
    if not (root / "automation" / "ml_discovery.py").is_file():
        raise ValueError("Pasta Python inválida: automation/ml_discovery.py ausente")
    sys.path.insert(0, str(root))
    if args.action == "oauth_status":
        # Nunca encaminhar token, client_secret, código nem refresh_token ao WinForms.
        from automation.ml_oauth import read_credentials, EXPIRY_SKEW_SECONDS
        data = read_credentials()
        if not data:
            return emit({"ok": True, "authenticated": False,
                         "token_valid": False, "origin": "cofre_sistema"})
        expires_at = int(data.get("expires_at") or 0)
        return emit({"ok": True, "authenticated": True,
                     "token_valid": expires_at > time.time() + EXPIRY_SKEW_SECONDS,
                     "origin": "cofre_sistema"})
    from automation.ml_discovery import (
        children_of, read_token, root_categories, search_listings
    )

    token = read_token()
    if args.action == "category_path":
        from automation.ml_discovery import _api_get
        if not re.fullmatch(r"MLB[0-9]{2,12}", args.category):
            raise ValueError("Identificador oficial de categoria inválido.")
        obj = _api_get("/categories/" + args.category, token=token)
        if not isinstance(obj, dict):
            raise ValueError("A API não retornou detalhes da categoria.")
        path = obj.get("path_from_root") or []
        categories = [
            {"id": row["id"], "name": row["name"]}
            for row in path if isinstance(row, dict)
            and re.fullmatch(r"MLB[0-9]{2,12}", str(row.get("id") or ""))
            and isinstance(row.get("name"), str) and row["name"].strip()
        ]
        return emit({"ok": True, "origin": "api", "categories": categories})
    if args.action == "suggest":
        return emit(official_suggestions(args.query.strip(), token, args.category))
    if args.action == "preview_url":
        return emit(official_preview(args.url, token))
    if args.action == "categories":
        rows, origin = root_categories(token=token)
        return emit({"ok": True, "categories": rows, "origin": origin})
    if args.action == "children":
        rows, _ = children_of(args.category, token=token)
        return emit({"ok": True, "categories": rows, "origin": "api"})
    if args.action == "search":
        rows = search_listings(args.query, category_id=args.category,
                               limit=25, token=token)
        return emit({"ok": True, "items": rows, "origin": "mercadolivre_official_search"})
    from automation.ml_ingest import validate_url, extract_item_id
    validate_url(args.url)
    if args.action == "ingest":
        if not re.fullmatch(r"MLB[0-9]{6,18}", args.item_id):
            raise ValueError("ID do anúncio inválido")
        if extract_item_id(args.url) != args.item_id:
            raise ValueError("A URL do anúncio não corresponde ao ID selecionado")
    # O CLI de origem mantém pins.json e sincroniza produtos.xlsx. Sem confirmar link ou direitos.
    result = subprocess.run(
        [sys.executable, str(root / "automation" / "garimpo.py"), "ingest-url", args.url],
        cwd=str(root), capture_output=True, text=True, encoding="utf-8",
        timeout=50, check=False
    )
    if result.returncode != 0:
        raise ValueError("Falha ao sincronizar com a fila Python. Rode 'garimpo.py ingest-url' no terminal para diagnosticar.")
    if args.action == "manual":
        from automation.garimpo import load_store, DEFAULT_STORE
        from urllib.parse import urlsplit
        pin = next((x for x in load_store(DEFAULT_STORE).get("pins", {}).values()
                    if x.get("source_url") == args.url), None)
        if not pin:
            raise ValueError("Produto importado, mas não localizado na fila Python")
        image = pin.get("source_image_url") or ""
        if image:
            from automation.ml_ingest import validate_url as check_url
            try:
                check_url(image, image=True)
            except ValueError:
                image = ""
        url = pin.get("resolved_url") or args.url
        try:
            validate_url(url)
        except ValueError:
            url = args.url
        item = {
            "id": pin.get("item_id") or "",
            "title": pin.get("title") or "",
            "category_id": pin.get("ml_category_id") or "",
            "permalink": url,
            "thumbnail": image,
            "currency": "",
            "price": None,
        }
        return emit({"ok": True, "origin": "url_manual", "items": [item]})
    return emit({"ok": True, "origin": "python_queue"})


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as exc:
        message = str(exc)
        match = re.search(r"HTTP\s*(401|403)\b", message)
        if match:
            code = match.group(1)
            reason = ("verifique a validade da sessão OAuth" if code == "401"
                      else "verifique a permissão da aplicação para o endpoint de busca")
            message = ("Mercado Livre respondeu HTTP " + code + ": " + reason +
                       ". Você pode cadastrar o anúncio pela URL real na interface; "
                       "não é possível listar resultados de uma busca negada.")
        if len(message) > 450:
            message = message[:450]
        emit({"ok": False, "error": message})
        sys.exit(1)
