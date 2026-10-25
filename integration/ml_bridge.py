"""Adaptador JSON do Garimpo da Vez: reutiliza os módulos do repositório Python original."""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import re
import subprocess
import sys


def emit(data: dict) -> int:
    print(json.dumps(data, ensure_ascii=False, separators=(",", ":")))
    return 0


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", required=True)
    parser.add_argument("--action", choices=["categories", "children", "search", "ingest", "manual"], required=True)
    parser.add_argument("--category", default="")
    parser.add_argument("--query", default="")
    parser.add_argument("--url", default="")
    parser.add_argument("--item-id", default="")
    args = parser.parse_args()

    root = Path(args.root).resolve()
    if not (root / "automation" / "ml_discovery.py").is_file():
        raise ValueError("Pasta Python inválida: automation/ml_discovery.py ausente")
    sys.path.insert(0, str(root))
    from automation.ml_discovery import (
        children_of, read_token, root_categories, search_listings
    )

    token = read_token()
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
        match = re.search(r"HTTP\\s*(401|403)\\b", message)
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
