# Checklist de homologação no Windows

Execute depois de `dotnet restore` e `dotnet build` com o SDK .NET 10 instalado.

- [ ] Compilar sem erros: `dotnet build -c Release`
- [ ] Abrir programa sem produtos fictícios; entrar na tela 01.
- [ ] Criar produto com nome, nicho e descrição; salvar; fechar/reabrir e conferir persistência.
- [ ] Selecionar foto PNG/JPG e ver prévia sem arquivo permanecer bloqueado.
- [ ] Gerar prompt com estilo/cenário; conferir substituições no texto completo.
- [ ] Copiar prompt e colar manualmente em bloco de notas.
- [ ] Com ChatGPT ativo no Edge comum, clicar em ativar aba (sem CDP).
- [ ] Com Edge CDP opcional na porta 9333, trazer para frente guia ChatGPT inativa.
- [ ] Salvar imagem gerada manualmente e importar como v001, depois v002.
- [ ] Aprovar v002 e confirmar que somente ela aparece aprovada no produto.
- [ ] Montar rascunho de Pin e verificar que nenhum Pin é publicado pelo app.
- [ ] Confirmar publicação manual e verificar status + histórico.
- [ ] Exportar .xlsx, abrir no Excel e conferir dados e versão aprovada.
- [ ] Fechar e abrir novamente; conferir cadastro, imagens, versões, pin e logs.
- [ ] Importar imagem inexistente/corrompida e confirmar mensagem de erro amigável.
- [ ] Com Edge/CDP indisponível, confirmar fallback/aviso sem nova janela aberta.

**Não validado nesta entrega:** build real .NET no Windows, integração CLI do `garimpo.py`,
endpoints de marketplace, API do Pinterest e automação da interface do ChatGPT.
