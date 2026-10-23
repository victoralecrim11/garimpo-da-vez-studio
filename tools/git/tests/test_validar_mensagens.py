"""Testes unitários do padrão de mensagens em português."""
from __future__ import annotations

import sys
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools" / "git"))
from validar_mensagens import validar  # noqa: E402


VALIDA = """Integração: Reutilizar o motor Python para encontrar anúncios

Alterações:
- Adicionar seletor de categoria na interface e persistir a URL original do anúncio.
- Vincular o resultado com a fila Python sem preencher links de comissão.

Motivação:
- Evitar o preenchimento manual de identificadores e preservar a origem dos produtos.

Validação:
- Compilação Windows via GitHub Actions concluída com sucesso.
- Consulta real ao Mercado Livre pendente de teste com OAuth no Windows.

Impactos:
- O módulo depende de Python 3.11 ou superior.
"""


class MensagensDeCommitTests(unittest.TestCase):
    def test_mensagem_completa_em_portugues_e_aceita(self):
        self.assertEqual(validar(VALIDA), [])

    def test_titulo_ingles_e_rejeitado(self):
        self.assertTrue(validar(VALIDA.replace(
            "Integração: Reutilizar o motor Python para encontrar anúncios",
            "feat: integrate Python catalog discovery"
        )))

    def test_corpo_sem_secoes_e_rejeitado(self):
        self.assertTrue(validar("Correção: Atualizar o fluxo de importação"))

    def test_campos_modelo_sao_rejeitados(self):
        mensagem = VALIDA.replace(
            "Evitar o preenchimento manual de identificadores e preservar a origem dos produtos.",
            "Explicar a necessidade e o problema resolvido."
        )
        self.assertTrue(validar(mensagem))

    def test_descricao_de_validacao_pode_apontar_limitacao(self):
        texto = VALIDA.replace(
            "Compilação Windows via GitHub Actions concluída com sucesso.",
            "Teste de integração não executado: credencial de serviço não disponível."
        )
        self.assertEqual(validar(texto), [])


if __name__ == "__main__":
    unittest.main()
