# Garimpo da Vez Studio

Aplicativo desktop **Windows Forms (C# / .NET 10)** para garimpo de anúncios oficiais do Mercado Livre, curadoria de produtos afiliados, preparação de imagens com IA assistida e rascunhos de Pins para Pinterest.

## Funcionalidades

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
