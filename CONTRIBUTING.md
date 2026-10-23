# Padrão de commits — Garimpo da Vez Studio

Todas as alterações futuras devem ter **título em português** e **corpo descritivo em português**. Esta convenção vale para código, documentação, testes, infraestrutura e correções.

## Estrutura obrigatória

```text
Tipo: Descrever a alteração de forma objetiva em português

Alterações:
- Informar quais arquivos, módulos ou comportamentos foram adicionados, alterados ou removidos.
- Destacar decisões relevantes e eventuais limitações.

Motivação:
- Explicar o problema, a necessidade ou o objetivo da mudança.

Validação:
- Informar quais testes, builds ou verificações foram executados e seus resultados.
- Se algo não foi testado, informar o motivo em vez de afirmar que funcionou.

Impactos:
- Opcional: documentar migração, configuração, compatibilidade e riscos.
```

**Tipos permitidos (em português):** `Funcionalidade`, `Correção`, `Documentação`, `Testes`, `Refatoração`, `Manutenção`, `Integração`, `Interface`, `Segurança`, `Desempenho`, `Infraestrutura` e `Reversão`.

Regras:
- O título começa com um dos tipos acima, seguido por `: `, e usa verbo/ação em português.
- Título preferencialmente com até **72 caracteres**.
- Deixar **uma linha em branco** entre título e corpo.
- Não usar títulos vagos como `update`, `fix`, `ajustes` ou `alterações diversas`.
- Cada seção `Alterações:`, `Motivação:` e `Validação:` deve ter conteúdo específico e verificável.
- É permitido indicar `Não executado`, mas acompanhado de **motivo explícito**.
- Não afirmar que uma integração de terceiros está funcional se apenas o build local passou.
- Evitar misturar assuntos não relacionados no mesmo commit.
- Se o GitHub criar commits automáticos de merge com títulos em inglês, preferir **Squash and merge** e preencher manualmente a mensagem em português.

## Exemplo de commit

```text
Integração: Adicionar pesquisa de anúncios do Mercado Livre

Alterações:
- Conectar o WinForms ao adaptador Python existente.
- Exibir categorias, subcategorias e anúncios reais em tabela.
- Armazenar a URL do anúncio separada do link de afiliado.

Motivação:
- Eliminar a necessidade de selecionar anúncios pelo terminal ou por ID manual.

Validação:
- Build do aplicativo Windows executado pelo GitHub Actions: aprovado.
- Testes isolados de respostas JSON da ponte Python: aprovados.
- Consulta real com OAuth do Mercado Livre: pendente de teste no Windows.

Impactos:
- Requer o repositório Python garimpo-da-vez disponível localmente.
```

## Configuração local

Dentro da pasta clonada do projeto:

```powershell
git config --local commit.template .gitmessage
```

O Git utilizará `.gitmessage` como modelo ao executar `git commit` com o editor de mensagens.

Para validar uma mensagem antes de publicar:

```powershell
python tools/git/validar_mensagens.py --arquivo caminho-da-mensagem.txt
```

A automação `.github/workflows/validar-commits.yml` examina os commits novos enviados em `push` e `pull_request`. Ela **não reescreve o histórico**. A verificação checa estrutura e campos mínimos; a qualidade, veracidade e o idioma natural devem continuar sendo revisados pela pessoa responsável.

> Os commits antigos não foram alterados, para evitar mudança de SHAs e perda de referências em GitHub Actions ou cópias locais.
