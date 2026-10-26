using System.Text;
using GarimpoDaVezStudio.Data;
using GarimpoDaVezStudio.Services;

Console.OutputEncoding = Encoding.UTF8;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    Console.WriteLine("Uso: dotnet run --project Tools/CatalogImport -- <arquivo.csv ou .xlsx> [--banco <arquivo.sqlite>] [--sem-excel]");
    return args.Length == 0 ? 2 : 0;
}

var file = Path.GetFullPath(args[0]);
var sqlitePath = AppPaths.DatabaseFile;
var exportExcel = true;
for (int i = 1; i < args.Length; i++)
{
    if (args[i] == "--sem-excel")
        exportExcel = false;
    else if (args[i] == "--banco" && i + 1 < args.Length)
        sqlitePath = Path.GetFullPath(args[++i]);
    else
    {
        Console.Error.WriteLine("Argumento desconhecido: " + args[i]);
        return 2;
    }
}

try
{
    Directory.CreateDirectory(Path.GetDirectoryName(sqlitePath)!);
    var catalog = new CatalogRepository(sqlitePath);
    catalog.Initialize();
    var before = catalog.Count();
    var processed = CatalogFilesService.Import(catalog, file);
    var after = catalog.Count();
    Console.WriteLine($"Arquivo: {file}");
    Console.WriteLine($"Banco SQLite: {sqlitePath}");
    Console.WriteLine($"Linhas válidas processadas: {processed:N0}");
    Console.WriteLine($"Novos registros únicos: {after - before:N0}");
    Console.WriteLine($"Total indexado: {after:N0}");
    if (exportExcel)
    {
        // Para banco personalizado, não escrever na pasta global do aplicativo.
        var excel = sqlitePath == AppPaths.DatabaseFile
            ? CatalogFilesService.AutomaticPath
            : Path.Combine(Path.GetDirectoryName(sqlitePath)!, "catalogo-importado.xlsx");
        CatalogFilesService.Export(catalog, excel);
        Console.WriteLine("Excel atualizado: " + excel);
    }
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("Importação falhou: " + ex.Message);
    return 1;
}
