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

## Cadastro pelo campo Nome do produto (2026-10)

- [ ] Digitar 3+ caracteres e verificar sugestões com rótulos Meu cadastro / Anúncio / Referência de catálogo.
- [ ] Selecionar cadastro local e confirmar que links, imagens e edições existentes foram preservados.
- [ ] Selecionar anúncio real e confirmar nome, ID MLB, URL do anúncio e classificação quando a API disponibilizar.
- [ ] Selecionar produto de catálogo e confirmar que o título aparece, mas nenhum ID de anúncio ou link comissionado é inventado.
- [ ] Testar busca 403: mostrar aviso, recorrer somente à consulta oficial de catálogo se autorizada; se também negar, mostrar só cadastros locais.
- [ ] Mudar de produto enquanto uma consulta está em andamento e verificar que o resultado antigo não sobrescreve o produto atual.
- [ ] Colar URL HTTPS oficial no cadastro e usar Preencher pela URL; confirmar que não criou entrada na fila Python.
- [ ] Consultar URL com metadados parciais e verificar aviso de revisão; conferir categoria e preço antes de publicar.
- [ ] Confirmar que miniatura exibida na interface não é gravada como imagem licenciada para divulgação.
- [ ] Clicar Salvar produto após a seleção e recarregar o registro no SQLite.

## Preenchimento automático da classificação e descrição (2026-10)

- [ ] Selecionar um anúncio real e confirmar categoria raiz, caminho completo da subcategoria e descrição do vendedor retornada pela API, quando acessível.
- [ ] Selecionar um produto de catálogo sem anúncio e confirmar categoria oficial ou categoria predita identificada como sugestão.
- [ ] Se houver texto de short_description no catálogo, mostrar o texto sem atribuí-lo a um vendedor.
- [ ] Quando só houver atributos oficiais, apresentar ficha técnica com os valores existentes, sem inventar informações.
- [ ] Se nem descrição nem atributos forem devolvidos, deixar a descrição vazia e mostrar aviso.
- [ ] Se o endpoint de descrições responder 401/403, manter nome e categoria disponíveis sem fabricar conteúdo.
- [ ] Editar a descrição manualmente durante uma consulta e confirmar que o resultado não sobrescreve a edição.
- [ ] Escolher outro produto enquanto aguarda resposta e confirmar que a categoria e a descrição antigas não são aplicadas.
- [ ] Salvar categoria raiz e folha no SQLite, fechar e reabrir o programa sem carregar a API: os IDs salvos devem continuar selecionáveis.
- [ ] Confirmar manualmente qualquer categoria sugerida pelo preditor e revisar os dados antes de publicar.

## Busca lateral de produtos (2026-10)

- [ ] Com SQLite vazio, observar mensagem "Nenhum produto salvo" em vez de um painel silenciosamente vazio.
- [ ] Digitar "Abajur" no campo lateral; após breve pausa, receber anúncios oficiais quando disponíveis ou referências de catálogo identificadas.
- [ ] Simular HTTP 401/403 nos serviços oficiais: apresentar orientação de importação por URL sem inventar produtos.
- [ ] Sem configurar a pasta do projeto Python, apresentar orientação para a aba 02, mantendo a busca local operacional.
- [ ] Clicar num anúncio da lateral: abrir aba Produto com nome, ID e URL original; verificar que ele NÃO foi salvo automaticamente no SQLite.
- [ ] Clicar em referência de catálogo: nome e atributos disponíveis, sem criar ID de anúncio nem link de afiliado.
- [ ] Salvar o produto revisado e localizar o resultado pelo rótulo Salvo na lateral.
- [ ] Alterar o filtro enquanto a API está respondendo; descartar a resposta antiga e não substituir o formulário selecionado.
- [ ] Digitar duas letras: filtrar localmente sem chamar a busca remota. Apagar o filtro para listar todos os salvos.
- [ ] Confirmar que produtos locais, anúncios e referências de catálogo são objetos de tipos diferentes na seleção.

## Título resumido do produto (2026-10)

- [ ] Pesquisar `Lumi` e selecionar um catálogo cujo nome oficial contenha várias expressões separadas por vírgulas: o campo deve receber uma sugestão curta, não a lista inteira de palavras-chave.
- [ ] Conferir o botão **Ver título original recebido na pesquisa** e revisar o título oficial antes de salvar.
- [ ] Verificar que o título inicial do Pin usa o mesmo nome curto e continua editável.
- [ ] Selecionar um título normal com características relevantes e verificar que ele não é indevidamente resumido.
- [ ] Selecionar um cadastro já salvo e verificar que o nome antigo não é alterado automaticamente.
- [ ] Selecionar uma URL oficial e repetir o teste de nome curto e de enriquecimento de categoria/descrição.
- [ ] Digitar manualmente outro nome durante a consulta e confirmar que a resposta assíncrona não modifica o novo cadastro.
- [ ] Alterar o nome proposto quando o trecho resumido omitir modelo ou característica essencial; a confirmação final continua humana.

## Catálogo híbrido — homologação no Windows (2026-10)

- [ ] Abrir a aba 07 com banco SQLite anterior; confirmar preservação de produtos, imagens e Pins.
- [ ] Importar CSV `Nome;Categoria;Preço` e XLSX com campos opcionais, inclusive acentos.
- [ ] Confirmar índices FTS5 e pesquisa local por termos com/sem acento.
- [ ] Reimportar o mesmo CSV e confirmar ausência de duplicatas.
- [ ] Verificar erro para cabeçalho ausente, URL externa inválida e XLSX com fórmula na coluna de nome.
- [ ] Pesquisar referência importada na lateral e no campo Nome do produto, mesmo com Python/OAuth desconectados.
- [ ] Promover referência ao cadastro; a ação não deve salvar automaticamente nem criar link comissionado.
- [ ] Clicar Reunir salvos e confirmar que produtos já cadastrados entram no índice híbrido.
- [ ] Buscar anúncios com a API autorizada e verificar entrada incremental no catálogo sem inventar IDs.
- [ ] Escolher categoria folha na aba 02, coletar mais vendidos na aba 07; checar tipos ITEM/PRODUCT, ausências e avisos.
- [ ] Simular 403 no ranking e conferir que não surgem produtos falsos ou status Validado.
- [ ] Atualizar anúncio existente e confirmar data de verificação somente com metadados reais.
- [ ] Confirmar link de afiliado de anúncio, confirmar status e reimportar planilha antiga: o link confirmado não deve ser apagado.
- [ ] Exportar Excel e conferir colunas de origem, último verificado, status e link.
- [ ] Deixar Excel automático ativo, importar arquivo e aguardar sincronização em %LOCALAPPDATA%.
- [ ] Abrir Excel automático para bloquear gravação e repetir atualização: SQLite deve permanecer intacto.
- [ ] Verificar que arquivo do Python `automation/data/produtos.xlsx` não foi tratado como base transacional do Studio.

## Pesquisa brasileira, cartões responsivos e fotos oficiais (30/10/2026)

- [ ] Arrastar a divisória lateral, alterar tamanho da janela e conferir títulos completos em duas linhas e tooltip.
- [ ] Pesquisar "perf" e conferir origem de resultados brasileiros; ativar a opção histórica ES e verificar que os nomes importados de 2019 continuam identificados sem tradução.
- [ ] Usar pesquisa na aba 07 e confirmar que o campo ocupa a largura disponível, com ação clara de limpar a busca.
- [ ] Selecionar anúncio com imagem oficial: a aba 07 mostra prévia e a aba 01 pode mostrar a mesma imagem sem criar arquivo local automaticamente.
- [ ] Selecionar referência histórica sem ID: nenhum anúncio, URL ou fotografia é fabricado.
- [ ] Selecionar produto catálogo oficial MLBU com `pictures`: exibir imagem mas NÃO afirmar que a URL é de anúncio comissionado.
- [ ] Quando a API retornar 401/403 ou não enviar imagem, exibir mensagem de indisponibilidade.
- [ ] Alterar rapidamente a seleção de anúncios: foto atrasada não deve substituir a prévia do novo produto.
- [ ] Abrir a aba 03, clicar no quadro vazio e verificar o Explorer; cancelar sem criar produto.
- [ ] Escolher imagem JPG/PNG válida: exibição atualizada nas abas 01 e 03; verificar se o arquivo foi salvo só após validação.
- [ ] Verificar que um `permalink` oficial aparece no campo URL original, nunca é classificado como link de afiliado.
