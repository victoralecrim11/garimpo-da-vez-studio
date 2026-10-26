using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace GarimpoDaVezStudio.Data;

public sealed class CatalogEntry
{
    public string Key { get; set; } = "";
    public string Kind { get; set; } = "offline"; // offline, listing, catalog
    public string Origin { get; set; } = "csv"; // csv, xlsx, api, url, studio
    public string ExternalId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string CategoryId { get; set; } = "";
    public string SubcategoryId { get; set; } = "";
    public string Description { get; set; } = "";
    public string ListingUrl { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public decimal? Price { get; set; }
    public string Currency { get; set; } = "";
    public string AffiliateUrl { get; set; } = "";
    public bool AffiliateConfirmed { get; set; }
    public string LastVerifiedUtc { get; set; } = "";
    public string ImportedUtc { get; set; } = "";
    public string UpdatedUtc { get; set; } = "";
    public string SourceFile { get; set; } = "";

    public string Status => AffiliateConfirmed ? "Link confirmado"
        : !string.IsNullOrWhiteSpace(LastVerifiedUtc) ? "Validado"
        : Kind == "offline" ? "Referência offline" : "Encontrado";
    public string KindLabel => Kind switch { "listing" => "Anúncio", "catalog" => "Catálogo", _ => "Referência" };

    public static string Identity(CatalogEntry x)
    {
        var id = x.ExternalId.Trim().ToUpperInvariant();
        if (Regex.IsMatch(id, @"\A(?:MLB\d{6,18}|MLB[A-Z0-9]{3,25})\z"))
            return x.Kind + ":" + id;
        if (Uri.TryCreate(x.ListingUrl, UriKind.Absolute, out var url) &&
            url.Scheme == "https" && url.Host.Contains("mercadolivre", StringComparison.OrdinalIgnoreCase))
            return "url:" + Sha256(url.GetLeftPart(UriPartial.Path).ToLowerInvariant());
        var normalized = Regex.Replace(x.Name.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormKC), @"\s+", " ");
        return "offline:" + Sha256(normalized + "|" + x.CategoryId + "|" + x.Category.ToLowerInvariant());
    }
    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..32];
}

public sealed class CatalogRepository
{
    private readonly string _connection;
    public CatalogRepository(string path) =>
        _connection = new SqliteConnectionStringBuilder {
            DataSource = path, Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connection);
        conn.Open();
        using var pragma = conn.CreateCommand();
        pragma.CommandText = "PRAGMA busy_timeout=5000; PRAGMA foreign_keys=ON;";
        pragma.ExecuteNonQuery();
        return conn;
    }

    public void Initialize()
    {
        using var db = Open();
        using var sql = db.CreateCommand();
        sql.CommandText = """
            CREATE TABLE IF NOT EXISTS catalog_entries(
                key TEXT PRIMARY KEY, kind TEXT NOT NULL, origin TEXT NOT NULL,
                external_id TEXT NOT NULL DEFAULT '', name TEXT NOT NULL,
                category TEXT NOT NULL DEFAULT '', category_id TEXT NOT NULL DEFAULT '',
                subcategory_id TEXT NOT NULL DEFAULT '', description TEXT NOT NULL DEFAULT '',
                listing_url TEXT NOT NULL DEFAULT '', image_url TEXT NOT NULL DEFAULT '',
                price REAL, currency TEXT NOT NULL DEFAULT '',
                affiliate_url TEXT NOT NULL DEFAULT '', affiliate_confirmed INTEGER NOT NULL DEFAULT 0,
                last_verified_utc TEXT NOT NULL DEFAULT '', imported_utc TEXT NOT NULL,
                updated_utc TEXT NOT NULL, source_file TEXT NOT NULL DEFAULT ''
            );
            CREATE INDEX IF NOT EXISTS ix_catalog_kind ON catalog_entries(kind);
            CREATE INDEX IF NOT EXISTS ix_catalog_category ON catalog_entries(category_id);
            CREATE INDEX IF NOT EXISTS ix_catalog_name ON catalog_entries(name COLLATE NOCASE);
            CREATE TABLE IF NOT EXISTS catalog_events(
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                occurred_utc TEXT NOT NULL, action TEXT NOT NULL,
                details TEXT NOT NULL, count INTEGER NOT NULL DEFAULT 0
            );
            """;
        sql.ExecuteNonQuery();
        // FTS5 é acelerador de pesquisa, nunca condição para inicializar o catálogo.
        try
        {
            using var chk = db.CreateCommand();
            chk.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='catalog_fts'";
            bool newIndex = Convert.ToInt32(chk.ExecuteScalar()) == 0;
            using var fts = db.CreateCommand();
            fts.CommandText = """
                CREATE VIRTUAL TABLE IF NOT EXISTS catalog_fts USING fts5(
                    name,category,description,content='catalog_entries',content_rowid='rowid',
                    tokenize='unicode61 remove_diacritics 2'
                );
                CREATE TRIGGER IF NOT EXISTS catalog_ai AFTER INSERT ON catalog_entries BEGIN
                  INSERT INTO catalog_fts(rowid,name,category,description)
                  VALUES(new.rowid,new.name,new.category,new.description);
                END;
                CREATE TRIGGER IF NOT EXISTS catalog_ad AFTER DELETE ON catalog_entries BEGIN
                  INSERT INTO catalog_fts(catalog_fts,rowid,name,category,description)
                  VALUES('delete',old.rowid,old.name,old.category,old.description);
                END;
                CREATE TRIGGER IF NOT EXISTS catalog_au AFTER UPDATE ON catalog_entries BEGIN
                  INSERT INTO catalog_fts(catalog_fts,rowid,name,category,description)
                  VALUES('delete',old.rowid,old.name,old.category,old.description);
                  INSERT INTO catalog_fts(rowid,name,category,description)
                  VALUES(new.rowid,new.name,new.category,new.description);
                END;
                """;
            fts.ExecuteNonQuery();
            if (newIndex)
            {
                using var rebuild = db.CreateCommand();
                rebuild.CommandText = "INSERT INTO catalog_fts(catalog_fts) VALUES('rebuild')";
                rebuild.ExecuteNonQuery();
            }
        }
        catch (SqliteException) { /* Busca LIKE continua funcionando. */ }
    }

    private const string SelectColumns = """
        key,kind,origin,external_id,name,category,category_id,subcategory_id,
        description,listing_url,image_url,price,currency,affiliate_url,
        affiliate_confirmed,last_verified_utc,imported_utc,updated_utc,source_file
        """;

    public int Count()
    {
        using var db = Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT count(*) FROM catalog_entries";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public List<CatalogEntry> Search(string query, int limit = 200)
    {
        limit = Math.Clamp(limit, 1, 2000);
        using var db = Open();
        using var cmd = db.CreateCommand();
        var words = Regex.Matches(query ?? "", @"[\p{L}\p{N}]+")
            .Select(m => m.Value).Where(s => s.Length >= 2).Take(5).ToArray();
        if (words.Length == 0)
        {
            cmd.CommandText = $"SELECT {SelectColumns} FROM catalog_entries ORDER BY updated_utc DESC LIMIT $lim";
        }
        else
        {
            cmd.CommandText = $"""
                SELECT {SelectColumns} FROM catalog_entries
                WHERE rowid IN (SELECT rowid FROM catalog_fts WHERE catalog_fts MATCH $q)
                ORDER BY updated_utc DESC LIMIT $lim
                """;
            cmd.Parameters.AddWithValue("$q", string.Join(" AND ", words.Select(w => "\"" + w.Replace("\"", "") + "\"*")));
        }
        cmd.Parameters.AddWithValue("$lim", limit);
        try { return ReadRows(cmd); }
        catch (SqliteException)
        {
            cmd.Parameters.Clear();
            cmd.CommandText = $"""
                SELECT {SelectColumns} FROM catalog_entries
                WHERE name LIKE $q OR category LIKE $q OR external_id LIKE $q
                ORDER BY updated_utc DESC LIMIT $lim
                """;
            cmd.Parameters.AddWithValue("$q", "%" + (query ?? "").Trim()[..Math.Min((query ?? "").Trim().Length, 100)] + "%");
            cmd.Parameters.AddWithValue("$lim", limit);
            return ReadRows(cmd);
        }
    }

    public List<CatalogEntry> All()
    {
        using var db = Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = $"SELECT {SelectColumns} FROM catalog_entries ORDER BY updated_utc DESC LIMIT 150000";
        return ReadRows(cmd);
    }

    public CatalogEntry? Find(string key)
    {
        using var db = Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = $"SELECT {SelectColumns} FROM catalog_entries WHERE key=$key";
        cmd.Parameters.AddWithValue("$key", key);
        return ReadRows(cmd).FirstOrDefault();
    }

    private static List<CatalogEntry> ReadRows(SqliteCommand cmd)
    {
        var rows = new List<CatalogEntry>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            rows.Add(new CatalogEntry {
                Key = reader.GetString(0), Kind = reader.GetString(1),
                Origin = reader.GetString(2), ExternalId = reader.GetString(3),
                Name = reader.GetString(4), Category = reader.GetString(5),
                CategoryId = reader.GetString(6), SubcategoryId = reader.GetString(7),
                Description = reader.GetString(8), ListingUrl = reader.GetString(9),
                ImageUrl = reader.GetString(10),
                Price = reader.IsDBNull(11) ? null : reader.GetDecimal(11),
                Currency = reader.GetString(12), AffiliateUrl = reader.GetString(13),
                AffiliateConfirmed = reader.GetInt32(14) == 1,
                LastVerifiedUtc = reader.GetString(15),
                ImportedUtc = reader.GetString(16), UpdatedUtc = reader.GetString(17),
                SourceFile = reader.GetString(18)
            });
        }
        return rows;
    }

    public int UpsertBatch(IEnumerable<CatalogEntry> entries, string action = "importação")
    {
        using var db = Open();
        using var tx = db.BeginTransaction();
        int count = 0;
        var now = DateTime.UtcNow.ToString("O");
        using var cmd = db.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO catalog_entries(
                key,kind,origin,external_id,name,category,category_id,subcategory_id,
                description,listing_url,image_url,price,currency,affiliate_url,
                affiliate_confirmed,last_verified_utc,imported_utc,updated_utc,source_file)
            VALUES($key,$kind,$origin,$id,$name,$category,$catid,$subid,$description,
                $url,$image,$price,$currency,'',0,$verified,$now,$now,$source)
            ON CONFLICT(key) DO UPDATE SET
                kind=CASE WHEN excluded.kind='offline' THEN catalog_entries.kind ELSE excluded.kind END,
                origin=CASE WHEN excluded.origin IN ('api','url') THEN excluded.origin ELSE catalog_entries.origin END,
                external_id=CASE WHEN excluded.external_id!='' THEN excluded.external_id ELSE catalog_entries.external_id END,
                name=CASE WHEN excluded.name!='' AND excluded.origin IN ('api','url') THEN excluded.name ELSE catalog_entries.name END,
                category=CASE WHEN excluded.category!='' THEN excluded.category ELSE catalog_entries.category END,
                category_id=CASE WHEN excluded.category_id!='' THEN excluded.category_id ELSE catalog_entries.category_id END,
                subcategory_id=CASE WHEN excluded.subcategory_id!='' THEN excluded.subcategory_id ELSE catalog_entries.subcategory_id END,
                description=CASE WHEN excluded.description!='' AND catalog_entries.description='' THEN excluded.description ELSE catalog_entries.description END,
                listing_url=CASE WHEN excluded.listing_url!='' AND (catalog_entries.listing_url='' OR excluded.origin IN ('api','url'))
                    THEN excluded.listing_url ELSE catalog_entries.listing_url END,
                image_url=CASE WHEN excluded.image_url!='' AND (catalog_entries.image_url='' OR excluded.origin IN ('api','url'))
                    THEN excluded.image_url ELSE catalog_entries.image_url END,
                price=CASE WHEN excluded.origin IN ('api','url') OR catalog_entries.price IS NULL
                    THEN COALESCE(excluded.price,catalog_entries.price) ELSE catalog_entries.price END,
                currency=CASE WHEN excluded.currency!='' THEN excluded.currency ELSE catalog_entries.currency END,
                last_verified_utc=CASE WHEN excluded.last_verified_utc!='' THEN excluded.last_verified_utc ELSE catalog_entries.last_verified_utc END,
                updated_utc=excluded.updated_utc,
                source_file=CASE WHEN excluded.source_file!='' THEN excluded.source_file ELSE catalog_entries.source_file END
            """;
        var names = new[] { "$key","$kind","$origin","$id","$name","$category",
            "$catid","$subid","$description","$url","$image","$price","$currency",
            "$verified","$now","$source" };
        foreach (var name in names) cmd.Parameters.Add(new SqliteParameter(name, DBNull.Value));
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name)) continue;
            entry.Kind = entry.Kind is "offline" or "listing" or "catalog" ? entry.Kind : "offline";
            if (entry.Kind == "offline" && entry.Origin is not ("csv" or "xlsx" or "studio"))
                entry.Origin = "csv";
            entry.Name = entry.Name.Trim()[..Math.Min(entry.Name.Trim().Length, 500)];
            entry.Key = CatalogEntry.Identity(entry);
            var values = new object?[] {
                entry.Key,entry.Kind,entry.Origin,entry.ExternalId,entry.Name,entry.Category,
                entry.CategoryId,entry.SubcategoryId,entry.Description,entry.ListingUrl,
                entry.ImageUrl,entry.Price,entry.Currency,entry.LastVerifiedUtc,now,entry.SourceFile
            };
            for (int i = 0; i < names.Length; i++)
                cmd.Parameters[names[i]].Value = values[i] ?? DBNull.Value;
            cmd.ExecuteNonQuery();
            count++;
            if (count > 150000) throw new InvalidOperationException("Lote excede 150 mil registros.");
        }
        using var log = db.CreateCommand();
        log.Transaction = tx;
        log.CommandText = "INSERT INTO catalog_events(occurred_utc,action,details,count) VALUES($date,$action,$detail,$count)";
        log.Parameters.AddWithValue("$date", now);
        log.Parameters.AddWithValue("$action", action[..Math.Min(action.Length, 100)]);
        log.Parameters.AddWithValue("$detail", "Catálogo SQLite atualizado por origem declarada.");
        log.Parameters.AddWithValue("$count", count);
        log.ExecuteNonQuery();
        tx.Commit();
        return count;
    }

    public void ConfirmAffiliate(string key, string url)
    {
        if (!GarimpoDaVezStudio.Services.ProductInputValidator.IsMercadoLivreAffiliate(url, true))
            throw new ArgumentException("Link oficial de afiliado inválido.");
        using var db = Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            UPDATE catalog_entries SET affiliate_url=$url,affiliate_confirmed=1,
                updated_utc=$date WHERE key=$key AND kind='listing'
            """;
        cmd.Parameters.AddWithValue("$url", url.Trim());
        cmd.Parameters.AddWithValue("$date", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$key", key);
        if (cmd.ExecuteNonQuery() == 0)
            throw new InvalidOperationException("Confirmação exige um anúncio individual existente.");
    }
}
