using GarimpoDaVezStudio.Data;
using GarimpoDaVezStudio.Services;
using ClosedXML.Excel;
using Microsoft.Data.Sqlite;

var temp = Path.Combine(Path.GetTempPath(), "gdv-catalog-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temp);
int checks=0;
void Check(bool ok,string label)
{
    checks++;
    if(!ok)throw new Exception("Falhou: "+label);
}
try
{
    var catalog=new CatalogRepository(Path.Combine(temp,"catalog.sqlite"));
    catalog.Initialize();
    Check(catalog.Count()==0,"migração inicial");
    var csv=Path.Combine(temp,"dados.csv");
    File.WriteAllText(csv,
        "Nome;Categoria;Preço;ID oficial;URL do anúncio;Link de afiliado;Status\n"+
        "\"Abajur de mesa; LED\";Iluminação;129,90;;;"+"https://meli.la/FAKE;Link confirmado\n"+
        "Quadro decorativo;Decoração;59,00;;; \n",System.Text.Encoding.UTF8);
    Check(CatalogFilesService.Import(catalog,csv)==2,"importar CSV com aspas");
    Check(catalog.Count()==2,"índice sem duplicatas CSV");
    var abajur=catalog.Search("abajur").Single();
    Check(abajur.Kind=="offline" && abajur.Status=="Referência offline","CSV não é anúncio validado");
    Check(!abajur.AffiliateConfirmed && abajur.AffiliateUrl=="","link CSV não é confirmado");
    Check(abajur.Price==129.90m,"preço pt-BR");
    Check(catalog.Search("iluminacao").Any(),"FTS5 sem acentos ou fallback");
    Check(CatalogFilesService.Import(catalog,csv)==2 && catalog.Count()==2,"deduplicação idempotente");

    var item=new CatalogEntry{
        Kind="listing",Origin="api",ExternalId="MLB123456789",Name="Abajur LED oficial",
        Category="Casa",ListingUrl="https://www.mercadolivre.com.br/item/MLB123456789",
        Price=88.70m,Currency="BRL",LastVerifiedUtc=DateTime.UtcNow.ToString("O")
    };
    Check(catalog.UpsertBatch(new[]{item})==1,"inserir anúncio real");
    var saved=catalog.Find(CatalogEntry.Identity(item))!;
    Check(saved.Price==88.70m && saved.Status=="Validado","status verificado");
    catalog.ConfirmAffiliate(saved.Key,"https://meli.la/Exemplo123");
    saved=catalog.Find(saved.Key)!;
    Check(saved.AffiliateConfirmed && saved.Status=="Link confirmado","confirmação explícita");
    var bad=false;
    try{catalog.ConfirmAffiliate(abajur.Key,"https://meli.la/Exemplo123");}
    catch(InvalidOperationException){bad=true;}
    Check(bad,"referência não recebe status confirmado");

    var updated=new CatalogEntry{
        Kind="listing",Origin="api",ExternalId="MLB123456789",
        Name="Abajur LED verificado novamente",
        Price=90.00m,Currency="BRL",ListingUrl="https://www.mercadolivre.com.br/item/MLB123456789",
        LastVerifiedUtc=DateTime.UtcNow.ToString("O")
    };
    catalog.UpsertBatch(new[]{updated});
    saved=catalog.Find(saved.Key)!;
    Check(saved.AffiliateConfirmed && saved.AffiliateUrl=="https://meli.la/Exemplo123",
        "atualização não sobrescreve comissão");
    Check(saved.Price==90.00m,"preço atualizado pela API");
    Check(catalog.Count()==3,"mesmo MLB não duplica");
    Check(catalog.Search("MLB123456789").Count==1,"busca pelo ID oficial indexado");

    var xlsx=Path.Combine(temp,"planilha.xlsx");
    using(var wb=new XLWorkbook())
    {
        var sheet=wb.Worksheets.Add("Itens");
        sheet.Cell("A1").SetValue("Produto");
        sheet.Cell("B1").SetValue("Categoria");
        sheet.Cell("C1").SetValue("Preço");
        sheet.Cell("A2").SetValue("Varal de chão");
        sheet.Cell("B2").SetValue("Lavanderia");
        sheet.Cell("C2").SetValue(49.99);
        sheet.Cell("A3").FormulaA1="=1+1"; // Não importa fórmulas externas como nomes.
        wb.SaveAs(xlsx);
    }
    Check(CatalogFilesService.Import(catalog,xlsx)==1,"importar XLSX com cabeçalho");
    Check(catalog.Search("Varal").Count==1,"buscar XLSX offline");
    Check(catalog.Search("Varal")[0].Price==49.99m,"preço XLSX com ponto decimal");
    var outXlsx=Path.Combine(temp,"export.xlsx");
    CatalogFilesService.Export(catalog,outXlsx);
    using(var report=new XLWorkbook(outXlsx))
    {
        var sheet=report.Worksheet("Catálogo");
        Check(sheet.Cell("A1").GetString()=="Tipo","cabeçalho exportado");
        Check(sheet.RangeUsed()!.RowCount()>=5,"todas as linhas exportadas");
        Check(sheet.RowsUsed().Any(r=>r.CellsUsed().Any(c=>c.GetString()=="Link confirmado")),
            "exporta status de comissão confirmado");
        var priceRow=sheet.RowsUsed().FirstOrDefault(r=>r.Cell(3).GetString()=="Abajur LED verificado novamente");
        Check(priceRow is not null && priceRow.Cell(8).DataType==XLDataType.Number &&
            priceRow.Cell(8).GetDouble()==90.0,"preço Excel exportado como número");
    }
    // O arquivo anterior não deve sumir por ter sido substituído.
    CatalogFilesService.Export(catalog,outXlsx);
    Check(new FileInfo(outXlsx).Length>1000,"exportação repetida atômica");

    File.WriteAllText(Path.Combine(temp,"sem-nome.csv"),"Preco;Categoria\n1;Casa\n");
    bad=false;
    try{CatalogFilesService.Import(catalog,Path.Combine(temp,"sem-nome.csv"));}
    catch(InvalidDataException){bad=true;}
    Check(bad,"planilha sem nome é recusada");
    Check(catalog.Count()==4,"erro mantém dados anteriores");

    Console.WriteLine($"Testes do catálogo híbrido: {checks} verificações aprovadas.");
    return 0;
}
finally
{
    try{Directory.Delete(temp,true);}catch(IOException){}
}
