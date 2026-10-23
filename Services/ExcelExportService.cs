using GarimpoDaVezStudio.Data;
using ClosedXML.Excel;

namespace GarimpoDaVezStudio.Services;

public static class ExcelExportService
{
    public static void Export(StudioRepository repo, string path)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Produtos");
        var columns = new[] { "Produto", "Fonte", "ID externo (opcional)", "Categoria",
            "Nicho", "Descrição", "Link afiliado", "Imagem original", "Imagem aprovada",
            "Status Pin", "Atualizado (UTC)" };
        for (var i = 0; i < columns.Length; i++) sheet.Cell(1, i + 1).Value = columns[i];
        var row = 2;
        foreach (var product in repo.GetProducts())
        {
            var approved = repo.GetImages(product.Id).FirstOrDefault(i => i.IsApproved);
            var draft = repo.GetPinDraft(product.Id);
            var values = new[] { product.Name, product.Source, product.ExternalId,
                product.Category, product.Niche, product.Description, product.AffiliateUrl,
                product.OriginalImagePath, approved?.FilePath ?? "", draft?.Status ?? "Sem rascunho",
                product.UpdatedAt };
            for (var i = 0; i < values.Length; i++)
                sheet.Cell(row, i + 1).Value = values[i];
            row++;
        }
        var header = sheet.Range(1, 1, 1, columns.Length);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#21334E");
        header.Style.Font.FontColor = XLColor.White;
        sheet.SheetView.FreezeRows(1);
        sheet.Range(1, 1, Math.Max(row - 1, 1), columns.Length).SetAutoFilter();
        for (var c = 1; c <= columns.Length; c++)
            sheet.Column(c).Width = c is 6 or 7 or 8 or 9 ? 48 : 24;
        workbook.SaveAs(path);
    }
}
