# Mapa de telas e arquitetura — Garimpo da Vez Studio — MVP

## Navegação

```mermaid
flowchart LR
  A[Lista de produtos] --> B[01 Produto]
  B --> C[02 Estúdio IA]
  C --> D[ChatGPT no Edge - envio manual]
  D --> E[03 Importar versões]
  E --> F{Imagem aprovada?}
  F -- não --> D
  F -- sim --> G[04 Rascunho Pin]
  G --> H[Pinterest - publicação manual]
  H --> I[Marcar status manualmente]
  B --> J[05 Histórico]
  E --> J
  I --> J
  J --> K[Exportar Excel XLSX]
```

## Estrutura de classes

```mermaid
classDiagram
  class MainForm {
    +BuildInterface()
    +SaveCurrent()
    +GeneratePrompt()
    +ImportGenerated()
    +SaveDraft()
  }
  class StudioRepository {
    +GetProducts()
    +SaveProduct()
    +GetImages()
    +SaveImage()
    +ApproveImage()
    +SavePin()
    +AddLog()
  }
  class Product { +string Id; +string Name; +string AffiliateUrl }
  class ImageVersion { +string ProductId; +int Version; +bool IsApproved }
  class PinDraft { +string ProductId; +string Status }
  class PromptService { +Build(Product,style,scene) string }
  class FileStudioService { +CopyOriginal(); +CopyGenerated(); +ShowImage() }
  class EdgeTabService { +ActivateExistingChatGptTabAsync() Task~bool~ }
  class EdgeWindowService { +ActivateVisibleChatGptWindow() bool }
  class ExcelExportService { +Export() }
  MainForm --> StudioRepository
  MainForm --> PromptService
  MainForm --> FileStudioService
  MainForm --> EdgeTabService
  MainForm --> EdgeWindowService
  MainForm --> ExcelExportService
  StudioRepository --> Product
  StudioRepository --> ImageVersion
  StudioRepository --> PinDraft
```

## SQLite

Tabelas: `products`, `image_versions`, `pin_drafts`, `studio_logs`.
Chaves estrangeiras habilitadas com `PRAGMA foreign_keys=ON`, comandos parametrizados,
versões únicas por produto, e no máximo uma versão aprovada por operação lógica.
Arquivo do banco: `%LOCALAPPDATA%\GarimpoDaVezStudio\studio.sqlite`.

## Integração planejada com garimpo.py (não implementada no MVP)

Adicionar uma fronteira `IProductProvider`, com `SearchAsync(query, category, niche)` e
`ImportAsync(selectedProduct)`. O adaptador Python deve produzir JSON com título,
identificador externo, categoria, descrição e caminho de foto; **não presumir** que
os parâmetros do CLI atual já atendem a esse contrato. Vincular produto à fonte
por `Source` + `ExternalId` para evitar duplicados. Não gerar links de afiliado a
partir de IDs simples sem mecanismo oficialmente disponibilizado.

## Decisões de limites

- Nenhuma API paga de imagens.
- O aplicativo **não** automatiza o envio no ChatGPT, o download de respostas ou a publicação no Pinterest.
- Apenas traz ao primeiro plano uma aba existente: via CDP se configurado ou via
  título de janela do Edge se o ChatGPT estiver na guia ativa.
- Os status de publicação são declarações manuais, sem validação remota.
- Não há analytics de Pinterest, monetização nem descoberta automática de produtos neste MVP.
