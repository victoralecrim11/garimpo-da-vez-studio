#!/usr/bin/env python3
"""Baixa catálogo HISTÓRICO de títulos MeLi 2019, sem confundir com anúncios.

Fonte de publicação: Mateo-Rua/titulos-productos-ecommerce-es (Apache-2.0,
subconjunto processado de títulos do desafio MeLi 2019).
Referência oficial: https://huggingface.co/datasets/Mateo-Rua/titulos-productos-ecommerce-es
Os dados originais não representam itens ativos, preços, links ou comissão.
"""
from __future__ import annotations

import argparse
import csv
import json
import os
from pathlib import Path
import tempfile
import time
from concurrent.futures import ThreadPoolExecutor, as_completed
from urllib.error import HTTPError, URLError
from urllib.parse import urlencode
from urllib.request import Request, urlopen

DATASET = "Mateo-Rua/titulos-productos-ecommerce-es"
API = "https://datasets-server.huggingface.co/rows"
SOURCE = "https://huggingface.co/datasets/" + DATASET
STEP = 100


def get_page(offset: int, total: int) -> tuple[int, list[dict]]:
    qs = urlencode({
        "dataset": DATASET, "config": "default", "split": "train",
        "offset": offset, "length": min(STEP, total - offset)
    })
    for attempt in range(5):
        try:
            request = Request(API + "?" + qs, headers={
                "User-Agent": "GarimpoDaVezStudio/1.0 (dataset publico, uso local)",
                "Accept": "application/json",
            })
            with urlopen(request, timeout=25) as response:
                if response.status != 200:
                    raise RuntimeError(f"Status HTTP {response.status}")
                body = json.load(response)
            rows = body.get("rows", [])
            if not isinstance(rows, list) or len(rows) != min(STEP, total - offset):
                raise ValueError(f"Lote incompleto na posição {offset}")
            return offset, rows
        except (HTTPError, URLError, TimeoutError, ValueError, OSError):
            if attempt == 4:
                raise
            time.sleep(min(2 ** attempt, 12))
    raise AssertionError("Tentativas esgotadas")


def download(output: Path, quantity: int, workers: int) -> tuple[int, int]:
    if quantity < 100 or quantity > 25000:
        raise ValueError("Quantidade deve ser entre 100 e 25.000.")
    offsets = list(range(0, quantity, STEP))
    pages: dict[int, list[dict]] = {}
    with ThreadPoolExecutor(max_workers=min(max(workers, 1), 6)) as pool:
        futures = [pool.submit(get_page, offset, quantity) for offset in offsets]
        for future in as_completed(futures):
            offset, rows = future.result()
            pages[offset] = rows

    output.parent.mkdir(parents=True, exist_ok=True)
    seen: set[tuple[str, str]] = set()
    count = 0
    categories: set[str] = set()
    fd, tmp = tempfile.mkstemp(suffix=".csv", prefix=".meli-", dir=output.parent)
    try:
        with os.fdopen(fd, "w", encoding="utf-8-sig", newline="") as handle:
            writer = csv.writer(handle, delimiter=";", quoting=csv.QUOTE_MINIMAL)
            writer.writerow(["Nome", "Categoria"])
            for offset in offsets:
                for entry in pages[offset]:
                    row = entry.get("row", {})
                    title = str(row.get("title") or "").strip()
                    category = str(row.get("category") or "").strip()
                    if not (4 <= len(title) <= 450 and 2 <= len(category) <= 90):
                        continue
                    if any(ch in title for ch in "\r\n\x00"):
                        title = " ".join(title.split())
                    label = "MeLi 2019 / " + category
                    key = (title.casefold(), label)
                    if key in seen:
                        continue
                    seen.add(key)
                    writer.writerow([title, label])
                    categories.add(category)
                    count += 1
        if count < quantity * 0.7:
            raise RuntimeError(f"Base incompleta: {count} de {quantity} entradas.")
        os.replace(tmp, output)
    finally:
        if os.path.exists(tmp):
            os.unlink(tmp)
    return count, len(categories)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--quantidade", type=int, default=12000)
    parser.add_argument("--trabalhadores", type=int, default=4)
    parser.add_argument("--saida", type=Path, required=True)
    args = parser.parse_args()
    count, categories = download(args.saida, args.quantidade, args.trabalhadores)
    print(json.dumps({
        "ok": True, "arquivo": str(args.saida.resolve()),
        "registros": count, "categorias_historicas": categories,
        "origem": SOURCE, "idioma": "espanhol",
        "ano": 2019, "dados_atuais": False,
    }, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
