
## Catálogo público histórico — MeLi Data Challenge 2019 (amostra em espanhol)

**Fonte**: https://huggingface.co/datasets/Mateo-Rua/titulos-productos-ecommerce-es

Conjunto público transformado de títulos e classificações históricas do MeLi Data
Challenge 2019, publicado por Mateo Rúa sob declaração de licença **Apache 2.0**
na página do dataset. O material original do desafio tem termos próprios:
verifique permissões antes de publicar ou redistribuir as entradas.

**Este não é o catálogo oficial atual do Mercado Livre.** Não contém preços,
URLs de anúncios atuais, disponibilidade, elegibilidade de afiliação nem links
de comissão. O idioma é espanhol, e as categorias do dataset (por exemplo
`LASER_LEVELS`) **não são IDs oficiais MLB da API**.

### Operação aplicada em 26/10/2026

No computador conectado `SA5S306624`, foi baixada uma amostra de 12.000
registros da partição `train`. Após validação/deduplicação, resultaram:

- **11.998 títulos e classificações históricos**;
- **730 códigos de classificação históricos diferentes**;
- banco local:
  `%LOCALAPPDATA%\GarimpoDaVezStudio\studio.sqlite`;
- CSV local:
  `%LOCALAPPDATA%\GarimpoDaVezStudio\importacoes\meli_2019_es_12000.csv`;
- Excel exportado:
  `%LOCALAPPDATA%\GarimpoDaVezStudio\relatorios\catalogo-atualizado.xlsx`.

O computador `ASUS-VIVOBOOK16` estava **offline** durante a importação.
Para ter os mesmos dados naquela máquina, execute as instruções abaixo.
O projeto no GitHub contém as ferramentas de importação, **não uma
cópia integral da base de terceiros**.

### Reproduzir em outro Windows com o repositório atualizado

No PowerShell, na raiz do `garimpo-da-vez-studio`:

```powershell
git pull origin main
$arquivo = Join-Path $env:LOCALAPPDATA 'GarimpoDaVezStudio\importacoes\meli_2019_es_12000.csv'
python tools/catalogo/baixar_amostra_meli.py --quantidade 12000 --saida $arquivo
dotnet run --project tools/CatalogImport/CatalogImport.csproj -c Release -- $arquivo
```

O script usa **apenas a biblioteca padrão Python** para consultar a
API pública de visualização do dataset (não usa a API do Mercado Livre nem
OAuth). O segundo comando chama o importador C# real do Studio e atualiza o
SQLite e o Excel com transações e proteção de links confirmados.

A operação pode ser repetida; a deduplicação impede recriar a mesma
referência. Para importar apenas um arquivo sem exportar o Excel
imediatamente, acrescente `--sem-excel`. Para testar com uma base
temporária, use `--banco C:\Temp\catalogo-teste.sqlite`.

### Conferência

Abra a aba **07 · Catálogo híbrido** e pesquise nomes em espanhol, por exemplo
`Nivel Laser`, `Motosierra` ou `Bateria`. A lista lateral do Studio
também consulta o índice offline. Registros importados continuam como
**Referência offline**, mesmo que tenham origem MeLi 2019. Para divulgar,
localize um anúncio vigente e confirme o link oficial de afiliado.

O CSV é guardado localmente porque sua cópia integral não foi
redistribuída no repositório público sem uma revisão adicional dos direitos
da fonte original. O script e o registro de procedência são versionados.
