# Garimpo da Vez Studio — C# WinForms + SQLite

> **Projeto:** Garimpo da Vez Studio (`garimpo-da-vez-studio`)  
> **Descrição GitHub:** Plataforma desktop em C#/.NET para gerenciar produtos afiliados, preparar imagens com IA e organizar publicações no Pinterest, com SQLite, Excel e integração planejada com Python.  
> **Estado:** MVP inicial, não validado por build Windows nem integrado ao `garimpo.py`.


Projeto-fonte demonstrativo em **C# / .NET 10 Windows Forms** para organizar imagens de
produtos de afiliados e preparar Pins do Pinterest, **sem API do ChatGPT ou Pinterest**.

## Funcionalidades já codificadas

- Cadastro local de produtos por nome, fonte, categoria, nicho, descrição e link oficial.
- ID interno gerado automaticamente (o identificador externo é opcional).
- Importar foto de referência e criar diretório por produto.
- Gerar e copiar um prompt editável para melhoria de foto via ChatGPT.
  Template externo editável em `%LOCALAPPDATA%\GarimpoDaVezStudio\prompts\pinterest-template.txt`,
  com campos `{{NOME}}`, `{{DESCRICAO}}`, `{{CATEGORIA}}`, `{{NICHO}}`, `{{ESTILO}}`, `{{CENARIO}}`.
- Encontrar e trazer ao primeiro plano o ChatGPT **já aberto** no Microsoft Edge:
  por CDP (`127.0.0.1:9333`) mesmo quando a guia não está selecionada, ou por
  Windows API se o ChatGPT já for a **aba ativa** de uma janela do Edge normal.
  Não abre outra janela nem controla mensagens.
- Importar imagens finais editadas manualmente; versões e aprovação.
- Preparar rascunho de Pin, registrar publicação confirmada manualmente e consultar histórico.
- Exportar produtos e estados para uma planilha **Excel .xlsx** usando ClosedXML.
- Persistência local via Microsoft.Data.Sqlite, sem credenciais ou banco remoto.

## O que NÃO faz

- Não acessa catálogo do Mercado Livre automaticamente, não roda `garimpo.py`, não
  gera links comissionáveis a partir de um ID qualquer. Ponto de integração futuro em
  `Docs/arquitetura.md`.
- Não faz upload, clique em enviar ou download automatizado na interface do ChatGPT.
- Não publica nem agenda Pins no Pinterest.
- Não altera fotos usando um modelo local; o usuário usa o ChatGPT na aba autenticada.

## Como executar no Windows

1. Instale o **SDK .NET 10** (Windows) em https://dotnet.microsoft.com/download/dotnet/10.0
   e, de preferência, o Visual Studio com a carga de trabalho **Desenvolvimento para desktop com .NET**.
2. Extraia este ZIP e abra `GarimpoDaVezStudio.csproj` no Visual Studio, ou abra o
   PowerShell no diretório onde está o arquivo `.csproj`.
3. Execute:

```powershell
dotnet restore
dotnet run
```

Na primeira execução, o app cria `%LOCALAPPDATA%\GarimpoDaVezStudio\studio.sqlite`
e o diretório `%LOCALAPPDATA%\GarimpoDaVezStudio\produtos`.

### Geração de executável independente do SDK

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

O executável gerado estará em `bin\Release\net10.0-windows\win-x64\publish`.
Distribua os arquivos exigidos na pasta de publicação. O publish precisa restaurar
os pacotes NuGet na primeira execução.

## Como conectar à MESMA janela autenticada do Edge

**Modo simples — sem CDP e sem abrir outra janela:** deixe a aba do ChatGPT
selecionada em seu Edge comum e clique **Ativar aba do ChatGPT**. O programa
localiza a janela pela identificação do processo/título e a traz para frente.
Ele não consegue localizar uma guia em segundo plano nesse modo.

**Modo avançado — com CDP:** para selecionar uma guia do ChatGPT que **não está
ativa**, não é possível ativar o CDP retroativamente em uma janela iniciada sem
`--remote-debugging-port`. Use uma sessão Edge que já estava habilitada para
depuração. Recomendamos um **perfil separado dedicado**.

1. Feche somente as janelas do perfil dedicado, se estiverem em execução.
2. Execute `iniciar-edge-cdp.cmd` incluído no projeto.
3. Nessa janela do Edge, abra `https://chatgpt.com` e autentique-se uma vez.
   A sessão persistirá nesse perfil local se não for limpa.
4. Confirme que `http://127.0.0.1:9333/json/list` responde e contém sua aba.
5. No app, abra **Estúdio IA > Ativar aba do ChatGPT**.

Se você só precisa trazer a janela **já ativa no ChatGPT** para a frente,
**não precisa executar esses cinco passos do modo avançado**.

**Segurança:** a porta 9333 deve permanecer restrita a `127.0.0.1`.
Não exponha a depuração na rede e não use o perfil pessoal principal caso ele
contenha contas e dados sensíveis. Feche o navegador depurável ao terminar.

## Fluxo sugerido para um produto

1. Clique em **Novo**; cadastre nome, categoria, nicho, link e importe a foto.
2. No **Estúdio IA**, escolha o estilo, gere e copie o prompt.
3. Ative a aba do ChatGPT, anexe manualmente a foto e envie o texto.
4. Baixe a imagem no ChatGPT usando os controles normais do site.
5. Vá a **Versões**, importe a foto editada e marque a versão aprovada.
6. Em **Rascunho Pin**, prepare título, descrição, board e link. Salve.
7. Publique manualmente no Pinterest. Só depois marque que publicou.
8. Use **Exportar Excel (.xlsx)** quando quiser uma cópia do catálogo atualizado.

## Organização

```text
GarimpoDaVezStudio/
├── GarimpoDaVezStudio.csproj
├── Program.cs
├── prompts/pinterest-template.txt
├── Models/Models.cs
├── Data/StudioRepository.cs
├── Services/
│   ├── AppPaths.cs
│   ├── PromptService.cs
│   ├── FileStudioService.cs
│   ├── EdgeTabService.cs
│   ├── EdgeWindowService.cs
│   └── ExcelExportService.cs
├── UI/MainForm.cs
├── Docs/arquitetura.md
├── Docs/schema.sql
└── iniciar-edge-cdp.cmd
```

## Limitações e testes

Código gerado e revisado estaticamente. O ambiente em que foi preparado é Linux e
**não tem SDK .NET**, por isso este arquivo **não foi compilado nem executado em
Windows**. Faça `dotnet restore` e `dotnet build` no seu PC; se aparecer erro de
compilação, mande o log para correção. Não é um instalador ou `.exe` já pronto.

Observação: descrições e links de afiliados devem ser fiéis às ofertas e respeitar
as regras da plataforma. O prompt fornecido é original e reproduz os objetivos
funcionais da estratégia, sem copiar o texto inteiro de um material de terceiros.

## Repositório e evolução

Este repositório contém o MVP inicial do Garimpo da Vez Studio. O código ainda precisa passar por build e testes manuais no Windows.

Os scripts Python de garimpo e a publicação automatizada no Pinterest não estão integrados. Perfis de navegador, dados locais SQLite, planilhas geradas e segredos não devem ser versionados.
