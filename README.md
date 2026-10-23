# Garimpo da Vez Studio

Aplicativo desktop **Windows Forms (C# / .NET 10)** para garimpo de anúncios oficiais do Mercado Livre, curadoria de produtos afiliados, preparação de imagens com IA assistida e rascunhos de Pins para Pinterest.

## Funcionalidades

- **Garimpo Mercado Livre:** seleciona pasta do projeto Python `garimpo-da-vez`, consulta categorias/subcategorias e pesquisa anúncios reais via **`automation/ml_discovery.py`** (API oficial). Busca por texto, categoria, seleção visual, preço indicativo e URL oficial.
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
4. Digite um termo (ex.: `luminária LED`) e clique **Pesquisar anúncios**. A lista só é preenchida com resultados oficiais validados pelo módulo Python; preços são momentâneos.
5. Selecione um anúncio e clique **Importar anúncio selecionado**. O cadastro passa à guia **Produto**; o campo **Link oficial do afiliado** permanece vazio até você informar e conferir o link correto da Central de Afiliados.
6. Se **Também registrar na fila Python** estiver marcado, a ponte executará `ingest-url` e informará sucesso ou erro de sincronização. O arquivo `produtos.xlsx` da automação depende das regras e permissões já implementadas no Python.
7. Adicione uma foto que possua autorização para uso comercial, gere o prompt no **Estúdio IA**, salve a versão aprovada e prepare o Pin.

A configuração do caminho da pasta Python é salva em `%LOCALAPPDATA%\GarimpoDaVezStudio\python-repository.txt` (somente o caminho, **nunca tokens**).

### Limites importantes

- A API de consulta **não** gera links de comissão; as funções do Mercado Livre para catálogo/produtos são distintas dos anúncios e da Central de Afiliados. Confira o link individual e a elegibilidade na Central.
- A busca pela API pode ser bloqueada: se houver **401/403**, utilize o fluxo `ingest-url URL_REAL` diretamente no projeto Python. O app mostra o erro; não apresenta resultados falsos.
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
Services/MercadoLivreBridgeService.cs # inicializa Python, JSON, valida pasta e timeout
integration/ml_bridge.py             # importa ml_discovery.py e chama garimpo.py
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
