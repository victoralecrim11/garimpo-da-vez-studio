# Garimpo da Vez Studio

Aplicativo desktop **Windows Forms (C# / .NET 10)** para garimpo de anúncios oficiais do Mercado Livre, curadoria de produtos afiliados, preparação de imagens com IA assistida e rascunhos de Pins para Pinterest.

## Funcionalidades

- **Busca lateral PRODUTOS:** filtra imediatamente os cadastros salvos no SQLite por nome, nicho, categoria ou ID externo e, após uma pausa de 900 ms com três ou mais caracteres, consulta sugestões oficiais do Mercado Livre pelo mesmo adaptador Python da aba Produto. Cada resultado é identificado como **Salvo**, **Anúncio** ou **Referência de catálogo**. Selecionar um resultado oficial abre a aba 01 e preenche o cadastro para revisão; **não salva automaticamente**. A lateral informa quando não existem cadastros locais, quando a pasta Python ainda não foi configurada ou quando a API nega acesso (401/403). Caso a busca de anúncios seja negada, o motor tenta o recurso oficial de catálogo, que não equivale a um anúncio comissionado. Nenhuma sugestão fictícia é apresentada.

- **Nome do produto pesquisável:** na aba **01 · Produto**, digite 3 ou mais caracteres para ver cadastros locais e sugestões oficiais, ou clique em **Buscar sugestões** / pressione Enter. Títulos extensos de anúncios e catálogos com listas de sinônimos e palavras-chave recebem uma **sugestão resumida, editável**. A lista exibe essa versão curta e o campo Nome do produto também utiliza a sugestão, sem copiar indiscriminadamente o título SEO inteiro para o título inicial do Pin. Quando ocorre abreviação, o link **Ver título original recebido na pesquisa** permite conferir o texto retornado pela API antes de salvar. Cadastros locais existentes permanecem intactos. Ao escolher um anúncio real, o aplicativo preenche nome, ID e URL original; tenta selecionar categoria e subcategoria e exibe miniatura quando disponível. Os dados são editáveis e não são salvos antes de clicar em **Salvar produto**.
  - As sugestões são identificadas como **Meu cadastro**, **Anúncio** ou **Referência de catálogo**. O catálogo oficial `/products/search` é consultado **somente quando a busca de anúncios for bloqueada** e não fornece ID de anúncio do vendedor nem link comissionado.
  - Use **Preencher pela URL** depois de informar um anúncio no campo **URL do anúncio original** para recuperar os metadados disponíveis sem registrar o produto automaticamente na fila Python.
  - Se ambos os endpoints forem negados, o sistema mantém apenas correspondências do SQLite e a entrada manual. Há uma pausa entre buscas e suspensão temporária após indisponibilidade para evitar consultas repetitivas.
  - Ao selecionar uma sugestão de **anúncio**, o Studio consulta o item real pela API, recupera a hierarquia oficial da categoria e solicita a descrição do vendedor em `/items/{id}/description`. O texto é preenchido **somente se existir** e se você ainda não tiver digitado uma descrição.
  - Ao selecionar uma **referência de catálogo**, consulta `/products/{id}` e, se faltar categoria, utiliza `/sites/MLB/domain_discovery/search` como preditor. A categoria predita é marcada como sugestão e deve ser confirmada antes de salvar.
  - Para produtos de catálogo, o campo de descrição recebe texto oficial disponível ou uma **ficha técnica montada exclusivamente com atributos reais da API** (claramente identificada como ficha técnica, nunca descrição de vendedor). Se a API não fornecer conteúdo, permanece vazio.
  - A subcategoria apresenta o caminho hierárquico, mantendo os IDs oficiais. Consultas negadas (401/403) não são contornadas, e a seleção não cria links comissionados.
  - Miniaturas são apenas prévias remotas; o arquivo original para Pins continua dependendo de autorização e seleção explícita do usuário.
- **Garimpo Mercado Livre:** consulta categorias e subcategorias oficiais e, quando o acesso autoriza, pesquisa anúncios reais por termo na API com **`automation/ml_discovery.py`**. Exibe miniatura oficial do anúncio à direita da lista, se disponível; a prévia não concede licença de uso da foto.
- **Alternativa a 401/403:** quando o endpoint de busca negar acesso, o usuário cola a URL real do produto na guia Garimpo e usa **Importar diretamente pela URL**. O motor Python existente usa os metadados permitidos para criar rascunho, sem inventar itens ou contornar a API.
- **Cadastro por categorias oficiais:** seletores de categoria e subcategoria no formulário Produto com IDs persistidos no SQLite; os nomes antigos são preservados até o usuário escolher a classificação oficial.
- **Nichos editoriais:** lista local de sugestões de marketing (ex.: decoração religiosa, home office, moda e organização), editável e separada dos domínios/categorias oficiais do Mercado Livre.
- **Imagem original:** clique na área de prévia vazia ou no caminho exibido para abrir o seletor de arquivos JPG/PNG/BMP; o caminho salvo e a prévia atualizam após a importação.
- **Importação:** traz o anúncio escolhido ao SQLite usando o ID **do anúncio MLB** como identificador externo. Evita importações duplicadas no Studio, preserva dados já editados e registra a URL **não comissionada** separadamente.
- **Sincronização opcional com Python:** caixa marcada na tela pode executar o comando já existente `garimpo.py ingest-url URL`, que mantém a fila `automation/data/pins.json` e tenta atualizar `automation/data/produtos.xlsx`. Falha nessa etapa não desfaz o cadastro no SQLite: o programa informa a situação.
- **Estúdio IA assistido:** prepara prompt e ativa a aba autenticada já aberta do ChatGPT no Edge; o usuário envia e salva a imagem manualmente.
- **Curadoria de imagens e Pins:** versões, aprovação, rascunhos e marcação manual de publicações.
- **Excel:** exportação local do Studio em XLSX, independente do Excel automático gerado pelo projeto Python.
- **Persistência:** SQLite em `%LOCALAPPDATA%\GarimpoDaVezStudio\studio.sqlite`, sem armazenar token de API no Studio.

## Dependências

- Windows 10/11 e **SDK .NET 10** ([instalação](https://dotnet.microsoft.com/download/dotnet/10.0)).
- **Python 3.11+** e cópia atualizada de [garimpo-da-vez](https://github.com/victoralecrim11/garimpo-da-vez).
- Para busca com autenticação, utilize o OAuth e dependências **já configurados no projeto Python**. O WinForms reaproveita o ambiente `.venv\Scripts\python.exe` quando presente; senão usa `python` disponível no PATH.
- A API do Mercado Livre pode devolver **HTTP 401/403** para buscas/itens, mesmo com OAuth; nenhum contorno, scraping ou dado inventado é implementado.

### Começar

No PowerShell:

```powershell
git clone https://github.com/victoralecrim11/garimpo-da-vez.git
git clone https://github.com/victoralecrim11/garimpo-da-vez-studio.git

cd .\garimpo-da-vez
py -3 -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r automation\requirements.txt

cd ..\garimpo-da-vez-studio
dotnet restore
dotnet run
```

No app:

**Caminho rápido pelo campo Nome:** abra a aba **01 · Produto**, digite o nome do item, escolha uma sugestão e confira os campos preenchidos. Para anúncios já conhecidos, cole a URL original e clique **Preencher pela URL**. O link de afiliado precisa ser informado separadamente com a Barra/Central oficial.

**Caminho avançado pela busca de anúncios:**

1. Abra a guia **02 · Garimpo Mercado Livre**.
2. Em **Selecionar pasta Python**, escolha a raiz `garimpo-da-vez` (a pasta que contém `automation`). Se os repositórios forem irmãos, a descoberta pode ser automática.
3. Clique **Carregar categorias** (API oficial; a raiz pode usar cache oficial identificado). Escolha uma categoria e, quando disponível, uma subcategoria.
4. Digite um termo (ex.: `luminária LED`) e clique **Pesquisar anúncios**. A lista só é preenchida com resultados oficiais validados pelo módulo Python; preços são momentâneos. A miniatura aparece ao lado se o endpoint retornar uma URL `mlstatic.com` válida.
   - **Se der HTTP 401/403:** abra o produto real no Mercado Livre, copie a URL da barra de endereços e cole no campo **Busca bloqueada? Cole a URL real**; clique **Importar diretamente pela URL**. Os metadados podem ser parciais e exigem revisão.
   - A URL manual já passa pela fila Python durante a importação; não haverá duplicação dessa etapa ao concluir o cadastro no Studio.
5. Selecione um anúncio e clique **Importar anúncio selecionado**. O cadastro passa à guia **Produto**; o campo **Link oficial do afiliado** permanece vazio até você informar e conferir o link correto da Central de Afiliados.
6. Se **Também registrar na fila Python** estiver marcado, a ponte executará `ingest-url` e informará sucesso ou erro de sincronização. O arquivo `produtos.xlsx` da automação depende das regras e permissões já implementadas no Python.
7. Na aba **01 · Produto**, escolha a categoria/subcategoria da API e um nicho editorial (ou escreva o seu). Clique na imagem original vazia ou no campo de caminho e escolha um arquivo de imagem local autorizado para divulgação comercial.
8. Gere o prompt no **Estúdio IA**, importe a versão editada aprovada e prepare o Pin.

A configuração do caminho da pasta Python é salva em `%LOCALAPPDATA%\GarimpoDaVezStudio\python-repository.txt` (somente o caminho, **nunca tokens**).

### Campos do produto: validação por tipo

- **Nome, fonte, nicho, título, pasta e descrição:** texto livre com limites de caracteres apropriados (o nome pode conter números de modelo).
- **ID externo/SKU:** campo *alfanumérico*, pois IDs oficiais de anúncios usam o prefixo `MLB` com números e SKUs podem conter hífens. Não use apenas um controle numérico.
- **URL do anúncio original:** campo **editável**, salvo no SQLite junto com o produto. Para produtos do Mercado Livre, aceita somente HTTPS dos domínios oficiais `mercadolivre.com.br` / `mercadolibre.com.br`. **Não cole `meli.la` neste campo**, pois isso pode ser link comissionado.
- **Link de afiliado:** campo separado, aceita somente links HTTPS dos domínios oficiais do Mercado Livre e `meli.la` quando a fonte for Mercado Livre. Formato e domínio não comprovam comissão, anúncio correto ou elegibilidade; confirme no portal.
- **App ID do OAuth:** somente 5 a 30 dígitos. Não é o Client Secret e não permite obter tokens sozinho.
- Todos os campos são validados novamente ao clicar **Salvar produto**. Valores já armazenados no SQLite antes da atualização não são apagados ou reescritos automaticamente.

### Login OAuth no WinForms (reutiliza Python)

1. Na guia **02 · Garimpo Mercado Livre**, selecione a pasta local do repositório Python `garimpo-da-vez`.
2. Clique **Verificar sessão OAuth**. O Studio pede ao Python apenas os indicadores de sessão existente e token dentro do prazo; **não recebe nem guarda tokens**.
3. Se não houver sessão, informe o **App ID numérico** da aplicação do Mercado Livre e clique **Iniciar login OAuth**. O botão inicia o comando `automation/ml_oauth.py login` em um terminal PowerShell visível e com interação humana. O Client Secret é solicitado de forma oculta pelo Python se não estiver disponível no ambiente.
4. Autorize a aplicação no navegador. O endereço de início esperado é `https://auth.mercadolivre.com.br/authorization`. Cadastre exatamente no DevCenter a URI de callback `https://victoralecrim11.github.io/garimpo-da-vez/oauth/callback/`.
5. O callback hospedado no GitHub Pages encaminha o código temporário e o `state` para `http://127.0.0.1:8765/callback`, atendido **somente pelo processo Python de login ativo**. Não feche o terminal antes da confirmação.
6. De volta ao WinForms, clique **Verificar sessão OAuth**. Mesmo com login válido, o endpoint de busca pode retornar **HTTP 403** por falta de permissão; o login não resolve sozinho uma restrição de API.

O Login usa **Authorization Code + PKCE S256, proteção `state`, troca de token e keyring** já presentes no projeto Python. O Studio não cria outro redirect nem tenta interceptar códigos do navegador. Tokens e Client Secret permanecem no cofre do sistema operacional administrado pelo Python. O Studio guarda localmente somente o App ID público para preencher o campo na próxima execução.

**Segurança:** nunca envie a URL final do callback com `code`, nem o Client Secret, access token ou refresh token em chats, capturas, issues ou repositórios. O encaminhamento pelo GitHub Pages foi projetado no repositório Python para uso pessoal; multiusuário requer backend HTTPS dedicado e controles adicionais.

### Limites importantes

- A API de consulta **não** gera links de comissão; as funções do Mercado Livre para catálogo/produtos são distintas dos anúncios e da Central de Afiliados. Confira o link individual e a elegibilidade na Central.
- A busca pela API pode ser bloqueada: se houver **401/403**, utilize a importação por URL da própria interface (ou o comando Python `ingest-url URL_REAL`). Isso **não restaura a pesquisa/listagem bloqueada** e não apresenta resultados falsos. O 401/403 depende das permissões/regras de acesso da API.
- Miniaturas são apenas prévias remotas sem gravação automática; ao preparar Pins, importe manualmente um arquivo que você tem direito de usar. O mesmo vale quando a API não fornecer uma imagem.
- Categorias vêm da API oficial ou cache oficial identificado, mas nichos são curadoria local e **não provam participação no programa de afiliados**.
- Importar anúncio não concede direitos sobre fotografia, não confirma estoque/comissão e não aprova publicação.
- O WinForms **não automatiza postagem no Pinterest nem envio/download no ChatGPT web**.
- O Studio mantém seu **próprio SQLite**; o JSON/XLSX são da **fila Python**. A sincronização opcional é unidirecional Studio → Python para a seleção importada; edições posteriores no Studio não atualizam automaticamente a fila Python.
- O projeto não copia nem publica credenciais, tokens OAuth, catálogo XLSX real, fotos de terceiros ou pastas do navegador.

### Build

```powershell
dotnet build GarimpoDaVezStudio.csproj -c Release
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

O executável publicado fica em `bin\Release\net10.0-windows\win-x64\publish`.

### Aba ChatGPT no Edge

O botão **Ativar aba do ChatGPT** foca a janela Edge que já está com ChatGPT ativo, ou, quando houver sessão CDP disponível em `127.0.0.1:9333`, permite localizar a aba em segundo plano. A janela não é criada automaticamente. Se utilizar CDP, mantenha a porta acessível apenas localmente, preferencialmente em perfil dedicado.

## Estrutura

```text
UI/MainForm.cs                       # interface, garimpo, estúdio e rascunhos
Services/MercadoLivreBridgeService.cs # inicia Python, recebe anúncios/miniaturas e importa URL manual
integration/ml_bridge.py             # reusa ml_discovery.py e suporta ingest-url sem scraping
Services/NicheCatalog.cs             # nichos editoriais locais editáveis
Models/Models.cs                     # produto, imagem, Pin e log
Data/StudioRepository.cs             # SQLite, incluindo migração não destrutiva
Services/ExcelExportService.cs       # Excel de produtos do Studio
Docs/arquitetura.md                  # arquitetura e limites de integração
Docs/testes-manuais.md               # checklist de homologação
```

Para verificar a integração isoladamente, sem abrir o WinForms:

```powershell
python integration\ml_bridge.py --root "..\garimpo-da-vez" --action categories
python integration\ml_bridge.py --root "..\garimpo-da-vez" --action search --query "organizador" --category ""
```

O primeiro comando imprime JSON com categorias. O segundo solicita a busca real, sujeita ao acesso concedido pelo Mercado Livre. A ponte não exibe os tokens.

**Repositórios:** [Motor Python](https://github.com/victoralecrim11/garimpo-da-vez) · [Studio desktop](https://github.com/victoralecrim11/garimpo-da-vez-studio).


## Padrão de commits e contribuições

Os próximos commits deste repositório devem ter **título em português** e **descrição detalhada em português**, com as seções `Alterações:`, `Motivação:` e `Validação:`. Escreva o que mudou, por que mudou e como verificou; não declare testes não realizados como aprovados.

- Consulte [CONTRIBUTING.md](CONTRIBUTING.md) para a convenção completa e um exemplo de mensagem.
- Configure o modelo local com `git config --local commit.template .gitmessage`.
- O workflow [Validar mensagens de commits em português](.github/workflows/validar-commits.yml) verifica os commits novos. O verificador confere a estrutura do texto, mas não garante sozinho que todas as palavras estejam em português.
- O histórico anterior permanece inalterado para preservar os SHAs e não interromper cópias locais ou integrações.

## Catálogo híbrido: CSV, Excel, API e SQLite

A aba **07 · Catálogo híbrido** utiliza as tabelas `catalog_entries` e `catalog_events` no mesmo
SQLite do Studio. Ela não substitui nem apaga os produtos existentes. O índice FTS5 acelera
buscas locais e há uma consulta `LIKE` de segurança quando FTS não está disponível.

### Como importar uma planilha

1. Entre em **07 · Catálogo híbrido** e clique **Importar CSV/XLSX**.
2. Selecione um arquivo `.csv` (separador `;`, `,` ou tabulação) ou `.xlsx`.
3. A planilha precisa ter uma coluna `Nome`, `Produto`, `Nome do produto`, `Title` ou `Name`.
4. Colunas opcionais: `Categoria`, `ID categoria`, `ID subcategoria`, `Descrição`,
   `Preço`, `Moeda`, `ID oficial`, `URL do anúncio` e `URL da imagem`.
5. Busque o produto na nova aba, na lista lateral ou no campo **Nome do produto**. Clique
   **Usar no Studio** e revise as informações antes de clicar **Salvar produto**.

Os arquivos são lidos em lotes com transação SQL; o limite é **60 MB e 150 mil linhas** por arquivo.
Arquivos sem cabeçalho de nome são rejeitados. Registros com IDs oficiais conhecidos
são deduplicados por tipo e ID; os demais usam título e categoria para uma chave local.
Pode reimportar uma planilha sem duplicar a mesma referência. Preços importados
são **históricos/informados**, jamais preços confirmados como atuais. Fórmulas não são
avaliadas durante a importação e URLs de imagens ficam restritas a HTTPS `mlstatic.com`.

### Descobertas online e mais vendidos

- Consultas de nome na aba 01, buscas laterais e busca de anúncios na aba 02 podem
  alimentar automaticamente o catálogo quando retornarem dados reais.
- Para obter **mais vendidos por categoria**, selecione uma categoria/subcategoria
  oficial na aba **02 · Garimpo Mercado Livre**, depois clique
  **Mais vendidos · categoria** na aba 07. O serviço consulta
  `/highlights/MLB/category/{id}` e os detalhes oficiais de até 15 resultados.
  Pode misturar anúncios individuais e produtos do catálogo, além de omitir itens
  indisponíveis. **Não comprova elegibilidade à comissão ou estoque atual**.
- **Atualizar anúncio** consulta metadados oficiais do ID selecionado. Sem
  permissão ou sem ID oficial, o registro permanece como referência local.
  Restrições HTTP 401/403 não são contornadas.
- **Reunir salvos** indexa produtos já cadastrados no Studio sem alterar
  ou marcar links de afiliado como confirmados.
- **Confirmar link** exige um anúncio real e a confirmação humana de um
  endereço obtido na Barra/Central oficial de Afiliados. Importações nunca
  confirmam comissão, mesmo que o arquivo afirme isso.

### Excel incremental

Quando a opção **Atualizar catálogo Excel automaticamente após alterações** está
ativada (padrão), o aplicativo gera em poucos segundos o arquivo:

`%LOCALAPPDATA%\GarimpoDaVezStudio\relatorios\catalogo-atualizado.xlsx`

O SQLite continua sendo a fonte principal. A exportação é preparada em um
arquivo temporário e substitui a versão anterior somente após terminar.
Se o Excel estiver aberto e bloquear a substituição, o cadastro permanece salvo
no SQLite; feche o Excel e tente exportar novamente. O botão **Exportar Excel**
permite salvar uma cópia onde você preferir.

O Excel mantém procedência, data de verificação, status e link de afiliado
independentemente. O `automation/data/produtos.xlsx` do motor Python continua
sendo uma saída separada: a fila Python não é alterada em massa pela importação
do catálogo, e somente o fluxo de importação pela aba Garimpo sincroniza essa fila
quando a opção correspondente estiver ativa.

**Aviso:** o Studio não inclui uma lista pré-carregada de milhares de
produtos. Você precisa importar seu arquivo CSV/XLSX ou coletar dados de
endpoints oficiais autorizados. Não importa automaticamente datasets de
terceiros sem verificar licenças e condições de uso.

### Importar catálogo histórico real (MeLi 2019)

Acesse **[Como baixar e importar a amostra MeLi 2019](Docs/catalogo-publico-meli-2019.md)**. A ferramenta `tools/catalogo/baixar_amostra_meli.py` obtém nomes e classificações históricos reais e gera um CSV para importação pelo `Tools/CatalogImport` ou pela aba 07. Não contém preços atuais, anúncios válidos ou links de afiliado. O dataset não é republicado no GitHub; a importação é local.

## Busca brasileira, fotos oficiais e layout responsivo (30/10/2026)

- A lateral **PRODUTOS** usa `SplitContainer`: você pode arrastar a divisória para ampliar a área de resultados. Os itens aparecem em cartões com a origem separada do título, texto em até duas linhas e tooltip com o nome completo.
- A busca da lateral e a busca da aba **07 · Catálogo híbrido** priorizam registros brasileiros e locais. A base histórica **MeLi 2019 em espanhol** fica **oculta por padrão**, mas pode ser exibida pelas opções **Exibir referências históricas em espanhol (2019)** ou **Incluir base histórica em espanhol (2019)**. Os registros originais NÃO são alterados ou traduzidos.
- As consultas oficiais existentes já usam o site **MLB (Brasil)**: `/sites/MLB/search`, `/products/search?site_id=MLB`, `/categories/...` e `/sites/MLB/domain_discovery/search`. Não existe promessa de nomes em português para datasets históricos importados de outros países. Restrições 401/403 continuam sendo respeitadas.
- A aba **07 · Catálogo híbrido** tem uma barra de busca com largura adaptativa, botão para limpar o texto e um painel de prévia. Ao selecionar um anúncio (ID `MLB...`) ou referência oficial de produto, o Studio tenta carregar imagem `pictures`/`thumbnail` disponível pela API. Nenhum arquivo é salvo automaticamente para divulgação. Referências históricas sem identificação oficial não têm imagem associada.
- Ao selecionar um anúncio oficial, os detalhes podem disponibilizar o campo `permalink`: a **URL pública do anúncio brasileiro**, não um link de afiliado. A Central/Barra de Afiliados continua responsável por gerar o link comissionado para confirmação humana.
- Na aba **03 · Estúdio IA**, clique na fotografia de referência ou em **Escolher foto de referência...** para abrir o Explorer. O seletor é aberto **antes** de validar/salvar o cadastro; após escolher uma imagem, é necessário ter um produto válido. As prévias das abas 01 e 03 são atualizadas. Formatos suportados neste momento: JPG, JPEG, PNG e BMP.
- A prévia de imagens remotas vem apenas de HTTPS `*.mlstatic.com`, com limites de tamanho e verificação de validade de conteúdo. Quando não houver URL de imagem ou a API negar acesso, a tela informa que a imagem não está disponível em vez de inventar uma miniatura.

**Verificação:** além dos testes do adaptador Python e do banco SQLite, há um smoke test que abre a janela WinForms, verifica o divisor responsivo, as abas e a foto clicável. O uso real com OAuth e os arquivos locais da conta deve ser homologado na máquina Windows antes de publicação comercial.
