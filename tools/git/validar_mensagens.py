#!/usr/bin/env python3
"""Verifica títulos e descrições de commits futuros, sem reescrever histórico."""
from __future__ import annotations

import argparse
from pathlib import Path
import re
import subprocess
import sys

TIPOS = (
    "Funcionalidade", "Correção", "Documentação", "Testes",
    "Refatoração", "Manutenção", "Integração", "Interface",
    "Segurança", "Desempenho", "Infraestrutura", "Reversão",
)
SECOES = ("Alterações", "Motivação", "Validação")
TITULO = re.compile(r"^(?:" + "|".join(map(re.escape, TIPOS)) + r"): [^\n]{12,}$")
PLACEHOLDERS = (
    "descrever uma alteração", "informar quais funcionalidades",
    "explicar a necessidade", "descrever testes executados",
    "substituir pelas informações", "escrever aqui", "preencher aqui",
)


def validar(mensagem: str) -> list[str]:
    """Retorna lista de erros. Não tenta deduzir automaticamente o idioma natural."""
    linhas = [linha.rstrip() for linha in mensagem.splitlines()
              if not linha.lstrip().startswith("#")]
    while linhas and not linhas[0].strip():
        linhas.pop(0)

    if not linhas:
        return ["A mensagem do commit está vazia."]

    erros: list[str] = []
    titulo = linhas[0].strip()
    if titulo == "Tipo: Descrever uma alteração específica em português":
        erros.append("Substitua o título-modelo pelo título real do commit.")
    elif not TITULO.fullmatch(titulo):
        erros.append("Título inválido: use 'Tipo: Descrição em português' com tipo permitido e descrição específica.")
    if len(titulo) > 88:
        erros.append("O título ultrapassa 88 caracteres; prefira 72.")

    if len(linhas) < 2 or linhas[1].strip():
        erros.append("Insira uma linha em branco após o título.")

    titulos: list[int] = []
    for secao in SECOES:
        indices = [indice for indice, linha in enumerate(linhas)
                   if linha.strip() == f"{secao}:"]
        if len(indices) != 1:
            erros.append(f"A seção '{secao}:' é obrigatória e deve aparecer uma vez.")
        else:
            titulos.append(indices[0])

    if len(titulos) == len(SECOES):
        if titulos != sorted(titulos):
            erros.append("As seções devem seguir a ordem: Alterações, Motivação, Validação.")
        for indice, secao in enumerate(SECOES):
            inicio = titulos[indice] + 1
            prox = [pos for pos in titulos if pos > titulos[indice]]
            impacto = next((pos for pos, linha in enumerate(linhas)
                            if linha.strip() == "Impactos:" and pos > titulos[indice]), None)
            if impacto is not None:
                prox.append(impacto)
            fim = min(prox) if prox else len(linhas)
            texto = " ".join(linha.strip().removeprefix("-").strip()
                             for linha in linhas[inicio:fim] if linha.strip())
            if len(texto) < 20:
                erros.append(f"Detalhe melhor a seção '{secao}:' (mínimo de 20 caracteres).")
            if any(frase in texto.casefold() for frase in PLACEHOLDERS):
                erros.append(f"Substitua o texto-modelo da seção '{secao}:'.")
    return erros


def commits_no_intervalo(intervalo: str) -> list[tuple[str, str]]:
    if not re.fullmatch(r"[a-fA-F0-9]{40}(?:\.\.[a-fA-F0-9]{40})?", intervalo):
        raise ValueError("O intervalo deve conter um SHA completo ou dois SHAs separados por '..'.")
    shas = subprocess.check_output(
        ["git", "rev-list", intervalo], text=True, encoding="utf-8"
    ).splitlines()
    resultado: list[tuple[str, str]] = []
    for sha in shas:
        mensagem = subprocess.check_output(
            ["git", "show", "--no-patch", "--format=%B", sha],
            text=True, encoding="utf-8"
        )
        resultado.append((sha, mensagem))
    return resultado


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--arquivo", type=Path, help="Caminho de uma mensagem para validar")
    group.add_argument("--intervalo", help="SHA ou SHA_ANTERIOR..SHA_ATUAL")
    args = parser.parse_args()

    try:
        registros = (
            [(args.arquivo.name, args.arquivo.read_text(encoding="utf-8"))]
            if args.arquivo else commits_no_intervalo(args.intervalo)
        )
    except (OSError, subprocess.CalledProcessError, ValueError) as exc:
        print(f"Falha ao ler mensagens de commit: {exc}", file=sys.stderr)
        return 2

    if not registros:
        print("Nenhum commit novo para validar.")
        return 0

    falhas = 0
    for identificador, mensagem in registros:
        problemas = validar(mensagem)
        if problemas:
            falhas += 1
            print(f"ERRO {identificador[:12]}:")
            for problema in problemas:
                print(f"  - {problema}")
        else:
            print(f"OK {identificador[:12]}: título e seções do commit validados.")

    if falhas:
        print(f"{falhas} de {len(registros)} commit(s) fora do padrão.")
        return 1
    print(f"{len(registros)} commit(s) dentro do padrão.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
