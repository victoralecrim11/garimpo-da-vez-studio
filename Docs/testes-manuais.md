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


## Integração Mercado Livre / Python

- [ ] Selecione a raiz real `garimpo-da-vez`; selecione outra pasta e verifique validação.
- [ ] Com Python e OAuth autorizados, carregue categorias da API/cache oficial; verifique procedência.
- [ ] Selecione uma categoria raiz e carregue subcategorias; filtre a busca e confira o `MLB...` e permalink oficial.
- [ ] Pesquise um termo inexistente e confirme lista vazia (sem dados fictícios).
- [ ] Provoque erro HTTP 401/403 (permissão ausente); verifique mensagem e ausência de fallback.
- [ ] Importe anúncio e confira o `listing_url` no SQLite, sem preencher `affiliate_url`.
- [ ] Importe o mesmo MLB novamente; confira que não há duplicata e campos já editados são preservados.
- [ ] Marque sincronização Python e verifique entrada na fila `pins.json` e planilha `produtos.xlsx` (ou alerta de falha).
- [ ] Desmarque sincronização Python; verifique que apenas o SQLite Studio muda.
- [ ] Se o `ingest-url` falhar, confirme que o Studio mantém produto importado e relata erro parcial.
- [ ] Execute com banco SQLite anterior à atualização; verifique migração não destrutiva de `listing_url`.
- [ ] Verifique que a exportação Excel Studio traz coluna URL anúncio independente do link afiliado.
- [ ] Teste com URL inválida/ID ML incorreto e confirme recusa.

## Categorias, nichos, busca bloqueada e prévias (2026-10)

- [ ] Cadastro de produto: clicar **Carregar categorias oficiais** e confirmar as opções em cada seletor.
- [ ] Trocar categoria e confirmar carregamento das subcategorias e sugestões de nichos.
- [ ] Digitar um nicho personalizado que não existe na lista, salvar e recarregar o produto.
- [ ] Atualizar banco SQLite legado sem perder categoria, nicho, foto e link de afiliado.
- [ ] Pesquisar anúncio autorizado: selecionar linha e ver miniatura `mlstatic.com` à direita, sem salvar no disco.
- [ ] Forçar um resultado sem thumbnail e confirmar que o espaço fica vazio (nunca aparece imagem fictícia).
- [ ] Receber HTTP 401/403 e confirmar mensagem indicando importação pela URL real.
- [ ] Importar URL real do anúncio pelo campo alternativo e revisar título, ID, categoria e ausência de direitos confirmados.
- [ ] Tentar importar URL externa (example.com) e confirmar rejeição.
- [ ] Clicar no retângulo da imagem original, selecionar um arquivo válido no diálogo e conferir caminho e prévia.
- [ ] Cancelar o diálogo sem alterar a imagem previamente salva.
- [ ] Importar anúncio cujo título/thumbnail não é acessível e confirmar que o sistema NÃO inventa dados.

## Tipagem, URL editável e autenticação OAuth (2026-10)

- [ ] Aba Produto: digitar URL HTTPS oficial no campo `URL do anúncio original`, salvar, reiniciar Studio e conferir persistência no SQLite.
- [ ] Colar `https://meli.la/...` no campo **Link oficial do afiliado** e confirmar aceitação após revisão da relação com o anúncio.
- [ ] Tentar inserir `meli.la` como URL **original** e confirmar recusa.
- [ ] Rejeitar `http://...`, domínio falso, usuário/senha na URL e porta customizada.
- [ ] Conferir que `MLB1234567890` e `RS1033-VHDC` são IDs válidos; espaços e símbolos fora do padrão devem ser rejeitados.
- [ ] Confirmar limites de texto (nome, descrição, nicho, título/descrição do Pin).
- [ ] Aba Garimpo: informar App ID somente numérico e rejeitar letras/valores incorretos.
- [ ] Acionar **Verificar sessão OAuth** sem cofre; deve informar ausência de sessão sem expor tokens.
- [ ] Com cofre já autenticado, acionar **Verificar sessão OAuth**; observar validade sem exibir credenciais.
- [ ] Acionar **Iniciar login OAuth** no Windows: PowerShell deve abrir, solicitar Client Secret oculto se necessário e abrir o navegador.
- [ ] Confirmar o URI cadastrado no DevCenter: `https://victoralecrim11.github.io/garimpo-da-vez/oauth/callback/`.
- [ ] Com terminal ainda aberto, completar autorização; callback deve encaminhar ao servidor local `127.0.0.1:8765`.
- [ ] Após login, revisar status no Studio. Repetir em caso de token revogado/vencido.
- [ ] Mesmo autorizado, testar endpoint bloqueado (403), garantindo que a interface mostre o erro e ofereça entrada por URL.
- [ ] Nunca inserir Client Secret, refresh token ou código de callback em arquivos de testes, commits ou screenshots.
