using GarimpoDaVezStudio.Models;
using GarimpoDaVezStudio.Services;
using Microsoft.Data.Sqlite;

namespace GarimpoDaVezStudio.Data;

public sealed class StudioRepository
{
    private readonly string _connectionString;

    public StudioRepository(string databasePath)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;";
        command.ExecuteNonQuery();
        return connection;
    }

    public void Initialize()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS products (
              id TEXT PRIMARY KEY, source TEXT NOT NULL DEFAULT 'Mercado Livre',
              external_id TEXT NOT NULL DEFAULT '', name TEXT NOT NULL,
              category TEXT NOT NULL DEFAULT '', niche TEXT NOT NULL DEFAULT '',
              description TEXT NOT NULL DEFAULT '', affiliate_url TEXT NOT NULL DEFAULT '',
              original_image_path TEXT NOT NULL DEFAULT '', created_at TEXT NOT NULL,
              updated_at TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_products_name ON products(name COLLATE NOCASE);
            CREATE TABLE IF NOT EXISTS image_versions (
              id TEXT PRIMARY KEY,
              product_id TEXT NOT NULL REFERENCES products(id) ON DELETE CASCADE,
              version INTEGER NOT NULL, file_path TEXT NOT NULL, prompt TEXT NOT NULL,
              style TEXT NOT NULL, is_approved INTEGER NOT NULL DEFAULT 0,
              created_at TEXT NOT NULL, UNIQUE(product_id, version)
            );
            CREATE INDEX IF NOT EXISTS ix_image_versions_product ON image_versions(product_id);
            CREATE TABLE IF NOT EXISTS pin_drafts (
              id TEXT PRIMARY KEY,
              product_id TEXT NOT NULL UNIQUE REFERENCES products(id) ON DELETE CASCADE,
              title TEXT NOT NULL DEFAULT '', description TEXT NOT NULL DEFAULT '',
              board TEXT NOT NULL DEFAULT '', affiliate_url TEXT NOT NULL DEFAULT '',
              image_version_id TEXT REFERENCES image_versions(id) ON DELETE SET NULL,
              status TEXT NOT NULL DEFAULT 'Rascunho', updated_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS studio_logs (
              id INTEGER PRIMARY KEY AUTOINCREMENT,
              product_id TEXT NOT NULL REFERENCES products(id) ON DELETE CASCADE,
              occurred_at TEXT NOT NULL, action TEXT NOT NULL, message TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_logs_product ON studio_logs(product_id, occurred_at DESC);
            """;
        command.ExecuteNonQuery();
    }

    public List<Product> GetProducts()
    {
        var results = new List<Product>();
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT id,source,external_id,name,category,niche,description,affiliate_url,
                   original_image_path,created_at,updated_at
            FROM products ORDER BY updated_at DESC, name COLLATE NOCASE
            """;
        using var r = cmd.ExecuteReader();
        while (r.Read())
            results.Add(new Product
            {
                Id = r.GetString(0), Source = r.GetString(1), ExternalId = r.GetString(2),
                Name = r.GetString(3), Category = r.GetString(4), Niche = r.GetString(5),
                Description = r.GetString(6), AffiliateUrl = r.GetString(7),
                OriginalImagePath = r.GetString(8), CreatedAt = r.GetString(9),
                UpdatedAt = r.GetString(10)
            });
        return results;
    }

    public void SaveProduct(Product product)
    {
        var now = AppPaths.UtcNow();
        if (string.IsNullOrWhiteSpace(product.CreatedAt)) product.CreatedAt = now;
        product.UpdatedAt = now;
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO products(id,source,external_id,name,category,niche,description,affiliate_url,
                                 original_image_path,created_at,updated_at)
            VALUES($id,$source,$external,$name,$category,$niche,$description,$url,$image,$created,$updated)
            ON CONFLICT(id) DO UPDATE SET
              source=excluded.source, external_id=excluded.external_id,
              name=excluded.name, category=excluded.category,niche=excluded.niche,
              description=excluded.description, affiliate_url=excluded.affiliate_url,
              original_image_path=excluded.original_image_path, updated_at=excluded.updated_at
            """;
        Add(cmd, "$id", product.Id); Add(cmd, "$source", product.Source);
        Add(cmd, "$external", product.ExternalId); Add(cmd, "$name", product.Name);
        Add(cmd, "$category", product.Category); Add(cmd, "$niche", product.Niche);
        Add(cmd, "$description", product.Description); Add(cmd, "$url", product.AffiliateUrl);
        Add(cmd, "$image", product.OriginalImagePath); Add(cmd, "$created", product.CreatedAt);
        Add(cmd, "$updated", product.UpdatedAt);
        cmd.ExecuteNonQuery();
    }

    public void DeleteProduct(string productId)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM products WHERE id=$id";
        Add(cmd, "$id", productId);
        cmd.ExecuteNonQuery();
    }

    public List<ImageVersion> GetImages(string productId)
    {
        var results = new List<ImageVersion>();
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT id,product_id,version,file_path,prompt,style,is_approved,created_at
            FROM image_versions WHERE product_id=$product ORDER BY version DESC
            """;
        Add(cmd, "$product", productId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            results.Add(new ImageVersion
            {
                Id = r.GetString(0), ProductId = r.GetString(1), Version = r.GetInt32(2),
                FilePath = r.GetString(3), Prompt = r.GetString(4), Style = r.GetString(5),
                IsApproved = r.GetInt32(6) == 1, CreatedAt = r.GetString(7)
            });
        return results;
    }

    public int NextImageVersion(string productId)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(MAX(version),0)+1 FROM image_versions WHERE product_id=$id";
        Add(cmd, "$id", productId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void SaveImage(ImageVersion image)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO image_versions(id,product_id,version,file_path,prompt,style,is_approved,created_at)
            VALUES($id,$product,$version,$file,$prompt,$style,$approved,$date)
            """;
        Add(cmd, "$id", image.Id); Add(cmd, "$product", image.ProductId);
        Add(cmd, "$version", image.Version); Add(cmd, "$file", image.FilePath);
        Add(cmd, "$prompt", image.Prompt); Add(cmd, "$style", image.Style);
        Add(cmd, "$approved", image.IsApproved ? 1 : 0); Add(cmd, "$date", image.CreatedAt);
        cmd.ExecuteNonQuery();
    }

    public void ApproveImage(string productId, string imageId)
    {
        using var connection = Open();
        using var tx = connection.BeginTransaction();
        using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "UPDATE image_versions SET is_approved=0 WHERE product_id=$product";
        Add(cmd, "$product", productId);
        cmd.ExecuteNonQuery();
        cmd.Parameters.Clear();
        cmd.CommandText = "UPDATE image_versions SET is_approved=1 WHERE id=$image AND product_id=$product";
        Add(cmd, "$image", imageId); Add(cmd, "$product", productId);
        if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Versão não encontrada.");
        tx.Commit();
    }

    public PinDraft? GetPinDraft(string productId)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT id,product_id,title,description,board,affiliate_url,image_version_id,status,updated_at
            FROM pin_drafts WHERE product_id=$id LIMIT 1
            """;
        Add(cmd, "$id", productId);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return new PinDraft
        {
            Id = r.GetString(0), ProductId = r.GetString(1), Title = r.GetString(2),
            Description = r.GetString(3), Board = r.GetString(4),
            AffiliateUrl = r.GetString(5), ImageVersionId = r.IsDBNull(6) ? "" : r.GetString(6),
            Status = r.GetString(7), UpdatedAt = r.GetString(8)
        };
    }

    public void SavePin(PinDraft draft)
    {
        draft.UpdatedAt = AppPaths.UtcNow();
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO pin_drafts(id,product_id,title,description,board,affiliate_url,
                                   image_version_id,status,updated_at)
            VALUES($id,$product,$title,$description,$board,$url,$image,$status,$updated)
            ON CONFLICT(product_id) DO UPDATE SET
              title=excluded.title,description=excluded.description,board=excluded.board,
              affiliate_url=excluded.affiliate_url,image_version_id=excluded.image_version_id,
              status=excluded.status,updated_at=excluded.updated_at
            """;
        Add(cmd, "$id", draft.Id); Add(cmd, "$product", draft.ProductId);
        Add(cmd, "$title", draft.Title); Add(cmd, "$description", draft.Description);
        Add(cmd, "$board", draft.Board); Add(cmd, "$url", draft.AffiliateUrl);
        Add(cmd, "$image", string.IsNullOrWhiteSpace(draft.ImageVersionId)
            ? DBNull.Value : draft.ImageVersionId);
        Add(cmd, "$status", draft.Status); Add(cmd, "$updated", draft.UpdatedAt);
        cmd.ExecuteNonQuery();
    }

    public void AddLog(string productId, string action, string message)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO studio_logs(product_id,occurred_at,action,message)
            VALUES($product,$date,$action,$message)
            """;
        Add(cmd, "$product", productId); Add(cmd, "$date", AppPaths.UtcNow());
        Add(cmd, "$action", action); Add(cmd, "$message", message);
        cmd.ExecuteNonQuery();
    }

    public List<StudioLog> GetLogs(string productId)
    {
        var results = new List<StudioLog>();
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT occurred_at,action,message FROM studio_logs
            WHERE product_id=$id ORDER BY id DESC LIMIT 200
            """;
        Add(cmd, "$id", productId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            results.Add(new StudioLog
            {
                OccurredAt = r.GetString(0), Action = r.GetString(1), Message = r.GetString(2)
            });
        return results;
    }

    private static void Add(SqliteCommand cmd, string name, object value) =>
        cmd.Parameters.AddWithValue(name, value);
}
