using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using GarimpoDaVezStudio.Data;
using Microsoft.VisualBasic.FileIO;

namespace GarimpoDaVezStudio.Services;

public static class CatalogFilesService
{
    private static readonly string[] Headers = {
        "Tipo","ID oficial","Nome","Categoria","ID categoria","ID subcategoria",
        "Descrição","Preço informado","Moeda","URL do anúncio","URL da imagem",
        "Link de afiliado","Link confirmado","Origem","Fonte do arquivo",
        "Última verificação UTC","Importado UTC","Atualizado UTC","Status"
    };

    private static string NormalizeHeader(string raw)
    {
        var text = (raw ?? "").Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var letters = new string(text.Where(c =>
            CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        return Regex.Replace(letters, @"[^a-z0-9]+", "");
    }

    private static string Value(IReadOnlyList<string> fields, Dictionary<string,int> columns,
        params string[] aliases)
    {
        foreach (var alias in aliases)
            if (columns.TryGetValue(NormalizeHeader(alias), out var index) &&
                index >= 0 && index < fields.Count)
                return fields[index].Trim();
        return "";
    }

    private static CatalogEntry? ConvertRow(IReadOnlyList<string> fields,
        Dictionary<string,int> cols, string origin, string sourceName)
    {
        var name = Value(fields,cols,"nome","nome do produto","produto","title","name","titulo","product_title");
        if (string.IsNullOrWhiteSpace(name)) return null;
        var id = Value(fields,cols,"id oficial","item_id","id anuncio","id do anuncio",
            "external_id","id externo","mlb","product_id");
        var category = Value(fields,cols,"categoria","category","category_name");
        var catid = Value(fields,cols,"id categoria","category_id","categoria_id");
        var subid = Value(fields,cols,"id subcategoria","subcategory_id");
        var url = Value(fields,cols,"url do anuncio","url anuncio","url original",
            "listing_url","permalink","url");
        if (!ProductInputValidator.IsMercadoLivreListing(url))
            url = ""; // URL inválida jamais será convertida em link verificado.
        var image = Value(fields,cols,"imagem","url imagem","url da imagem","thumbnail","image_url");
        if (Uri.TryCreate(image,UriKind.Absolute,out var imgUrl))
        {
            var host = imgUrl.IdnHost;
            if (imgUrl.Scheme != "https" || imgUrl.UserInfo.Length > 0 || !imgUrl.IsDefaultPort ||
                !(host.Equals("mlstatic.com",StringComparison.OrdinalIgnoreCase) ||
                  host.EndsWith(".mlstatic.com",StringComparison.OrdinalIgnoreCase)))
                image = "";
        }
        else image = "";

        var number = Value(fields,cols,"preco","preço","preco informado","price");
        var price = ReadPrice(number);
        var kind = id.StartsWith("MLBU",StringComparison.OrdinalIgnoreCase) ? "catalog" :
            Regex.IsMatch(id,@"\AMLB\d{6,18}\z",RegexOptions.IgnoreCase) && url.Length > 0
                ? "listing" : "offline";
        return new CatalogEntry {
            Kind=kind,Origin=origin,ExternalId=id[..Math.Min(id.Length,40)],
            Name=name[..Math.Min(name.Length,500)],
            Category=category[..Math.Min(category.Length,250)],
            CategoryId=catid[..Math.Min(catid.Length,35)],
            SubcategoryId=subid[..Math.Min(subid.Length,35)],
            Description=Value(fields,cols,"descricao","descrição","description")[..Math.Min(
                Value(fields,cols,"descricao","descrição","description").Length,3000)],
            ListingUrl=url,
            ImageUrl=image,Price=price,
            Currency=Value(fields,cols,"moeda","currency","currency_id")[..Math.Min(
                Value(fields,cols,"moeda","currency","currency_id").Length,8)],
            SourceFile=sourceName
            // Nunca importa status ou comissão como confirmados.
        };
    }

    private static decimal? ReadPrice(string value)
    {
        if (string.IsNullOrWhiteSpace(value))return null;
        value=value.Replace("R$","",StringComparison.OrdinalIgnoreCase)
                   .Replace("BRL","",StringComparison.OrdinalIgnoreCase)
                   .Replace(" ","").Replace("\u00A0","");
        // O último separador seguido de 1 ou 2 dígitos define a parte decimal.
        // Evita interpretar "49.99" como "4.999" em culturas brasileiras.
        var lastComma=value.LastIndexOf(',');
        var lastPoint=value.LastIndexOf('.');
        int separator=Math.Max(lastComma,lastPoint);
        string normalized;
        if(separator>=0 && value.Length-separator-1 is 1 or 2)
        {
            var integer=value[..separator].Replace(",","").Replace(".","");
            normalized=integer+"."+value[(separator+1)..];
        }
        else normalized=value.Replace(",","").Replace(".","");
        if (decimal.TryParse(normalized,NumberStyles.AllowDecimalPoint|NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,out var amount) &&
            amount>=0 && amount<=10_000_000)
            return amount;
        return null;
    }

    private static IEnumerable<CatalogEntry> CsvRows(string file, string origin)
    {
        using var parser = new TextFieldParser(file,Encoding.UTF8,detectEncoding:true);
        using var input = new StreamReader(file,Encoding.UTF8,detectEncodingFromByteOrderMarks:true);
        var firstLine = input.ReadLine() ?? throw new InvalidDataException("CSV vazio.");
        char separator = DetectSeparator(firstLine);
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(separator.ToString());
        parser.HasFieldsEnclosedInQuotes = true;
        parser.TrimWhiteSpace = false;
        var rawHeaders = parser.ReadFields() ?? throw new InvalidDataException("CSV sem cabeçalho.");
        var cols = BuildColumns(rawHeaders);
        var count=0;
        while (!parser.EndOfData)
        {
            if (++count > 150000) throw new InvalidDataException("Máximo de 150 mil linhas por importação.");
            string[]? values;
            try { values = parser.ReadFields(); }
            catch (MalformedLineException ex) { throw new InvalidDataException(
                "CSV inválido na linha "+parser.ErrorLineNumber,ex); }
            if (values is null) continue;
            var row = ConvertRow(values,cols,origin,Path.GetFileName(file));
            if (row is not null) yield return row;
        }
    }

    private static char DetectSeparator(string header)
    {
        // Não conta separadores dentro de campos entre aspas.
        var counts = new Dictionary<char,int> {{';',0},{',',0},{'\t',0}};
        var quoted = false;
        foreach (var chr in header)
        {
            if (chr == '"') quoted=!quoted;
            else if (!quoted && counts.ContainsKey(chr)) counts[chr]++;
        }
        return counts.OrderByDescending(c=>c.Value).First().Key;
    }

    private static Dictionary<string,int> BuildColumns(IEnumerable<string> headers)
    {
        var cols = headers.Select((s,i)=>(name:NormalizeHeader(s), index:i))
            .Where(x=>x.name.Length>0).GroupBy(x=>x.name)
            .ToDictionary(x=>x.Key,x=>x.First().index);
        if (!new[] {"nome","nomedoproduto","produto","title","name","titulo","producttitle"}
            .Any(cols.ContainsKey))
            throw new InvalidDataException("Planilha deve conter uma coluna Nome/Produto/Title.");
        return cols;
    }

    private static IEnumerable<CatalogEntry> XlsxRows(string file)
    {
        using var workbook = new XLWorkbook(file);
        var sheet = workbook.Worksheets.First();
        var rows = sheet.RowsUsed().Take(150002).ToList();
        if (rows.Count == 0) throw new InvalidDataException("Planilha vazia.");
        static string Text(IXLCell cell)
        {
            // Nunca executa fórmulas de arquivos externos como fontes de dados.
            if (cell.HasFormula) return "";
            return cell.GetFormattedString();
        }
        var lastColumn = Math.Min(rows[0].LastCellUsed()?.Address.ColumnNumber ?? 0,100);
        var cols = BuildColumns(Enumerable.Range(1,lastColumn)
            .Select(i=>Text(rows[0].Cell(i))));
        foreach (var row in rows.Skip(1))
        {
            if (row.RowNumber()>150001) throw new InvalidDataException("Máximo de 150 mil linhas.");
            var fields = Enumerable.Range(1,lastColumn)
                .Select(i=>Text(row.Cell(i))).ToArray();
            var entry = ConvertRow(fields,cols,"xlsx",Path.GetFileName(file));
            if (entry is not null) yield return entry;
        }
    }

    public static int Import(CatalogRepository catalog,string file)
    {
        if (!File.Exists(file)) throw new FileNotFoundException("Arquivo não encontrado.",file);
        var type = Path.GetExtension(file).ToLowerInvariant();
        if (type is not (".csv" or ".xlsx"))
            throw new InvalidDataException("Somente CSV e XLSX são aceitos.");
        var size= new FileInfo(file).Length;
        if (size==0 || size>60_000_000)
            throw new InvalidDataException("Tamanho inválido: limite de 60 MB por arquivo.");
        return catalog.UpsertBatch(type==".csv" ? CsvRows(file,"csv") : XlsxRows(file),
            "Importação "+Path.GetFileName(file));
    }

    private static string SafeExcelText(string? text)
    {
        var v = text ?? "";
        // Proteção de injeção de fórmula ao exportar dados externos.
        return v.Length>0 && "=+-@\t\r".Contains(v[0]) ? "'"+v : v;
    }

    public static string AutomaticPath => Path.Combine(AppPaths.Root,"relatorios","catalogo-atualizado.xlsx");

    public static void Export(CatalogRepository repo, string path)
    {
        var rows = repo.All();
        var folder = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(folder);
        var temp = Path.Combine(folder,".catalogo-"+Guid.NewGuid().ToString("N")+".xlsx");
        try
        {
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.Worksheets.Add("Catálogo");
                for (int i=0;i<Headers.Length;i++) sheet.Cell(1,i+1).SetValue(Headers[i]);
                var row=2;
                foreach (var entry in rows)
                {
                    var columns = new[] {
                        entry.KindLabel,entry.ExternalId,entry.Name,entry.Category,entry.CategoryId,
                        entry.SubcategoryId,entry.Description,entry.Price?.ToString(CultureInfo.InvariantCulture)??"",
                        entry.Currency,entry.ListingUrl,entry.ImageUrl,entry.AffiliateUrl,
                        entry.AffiliateConfirmed?"Sim":"Não",entry.Origin,entry.SourceFile,
                        entry.LastVerifiedUtc,entry.ImportedUtc,entry.UpdatedUtc,entry.Status
                    };
                    for(int i=0;i<columns.Length;i++)
                    {
                        if(i==7 && entry.Price.HasValue)
                            sheet.Cell(row,i+1).SetValue((double)entry.Price.Value);
                        else sheet.Cell(row,i+1).SetValue(SafeExcelText(columns[i]));
                    }
                    row++;
                }
                var header=sheet.Range(1,1,1,Headers.Length);
                header.Style.Fill.BackgroundColor=XLColor.FromHtml("#21334E");
                header.Style.Font.FontColor=XLColor.White;
                header.Style.Font.Bold=true;
                sheet.SheetView.FreezeRows(1);
                if (row>2) sheet.Range(1,1,row-1,Headers.Length).SetAutoFilter();
                for (int i=1;i<=Headers.Length;i++)
                    sheet.Column(i).Width=i is 3 or 7 or 10 or 11 or 12 ? 40 : 22;
                sheet.Column(8).Style.NumberFormat.Format="#,##0.00";
                sheet.Column(8).Style.Alignment.Horizontal=XLAlignmentHorizontalValues.Right;
                workbook.SaveAs(temp);
            }
            File.Move(temp,path,true); // Arquivo antigo intacto até o novo estar completo.
        }
        finally { if(File.Exists(temp))File.Delete(temp); }
    }
}
