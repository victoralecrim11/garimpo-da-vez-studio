"""Testes offline do conversor de catálogo histórico, sem rede nem dados pessoais."""
import csv
import importlib.util
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

SCRIPT = Path(__file__).with_name("baixar_amostra_meli.py")
spec = importlib.util.spec_from_file_location("catalogo_meli", SCRIPT)
meli = importlib.util.module_from_spec(spec)
assert spec.loader is not None
spec.loader.exec_module(meli)


class DownloadTests(unittest.TestCase):
    def test_csv_has_only_historical_references(self):
        def mocked_page(offset, total):
            return offset, [
                {
                    "row": {
                        "title": "Abajur de mesa" if offset == 100 and i == 0
                            else f"Produto real de referência {offset + i}",
                        "category": "LIGHTING" if i % 2 == 0 else "HOME_DECOR",
                    }
                }
                for i in range(100)
            ]

        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "historico.csv"
            with patch.object(meli, "get_page", side_effect=mocked_page):
                count, categories = meli.download(path, 200, 2)
            self.assertEqual(count, 200)
            self.assertEqual(categories, 2)
            with path.open(encoding="utf-8-sig", newline="") as handle:
                rows = list(csv.DictReader(handle, delimiter=";"))
            self.assertEqual(len(rows), 200)
            self.assertEqual(list(rows[0]), ["Nome", "Categoria"])
            self.assertIn("MeLi 2019 / ", rows[0]["Categoria"])
            self.assertNotIn("URL do anúncio", rows[0])
            self.assertNotIn("Preço", rows[0])
            self.assertNotIn("Link afiliado", rows[0])

    def test_repeated_titles_are_deduplicated(self):
        def same_rows(offset, total):
            return offset, [{"row": {
                "title": f"Abajur de referência {i}",
                "category": "LIGHTING"
            }} for i in range(100)]

        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "base.csv"
            with patch.object(meli, "get_page", side_effect=same_rows):
                with self.assertRaisesRegex(RuntimeError, "Base incompleta"):
                    meli.download(path, 200, 2)
            self.assertFalse(path.exists())  # Importação incompleta jamais publicada.

    def test_rejects_too_large_or_small_batches(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "base.csv"
            for quantity in (0, 80, 25001):
                with self.assertRaises(ValueError):
                    meli.download(path, quantity, 1)


if __name__ == "__main__":
    unittest.main()
