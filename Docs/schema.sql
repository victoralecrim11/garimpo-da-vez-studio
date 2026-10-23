PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS products (
  id TEXT PRIMARY KEY,
  source TEXT NOT NULL DEFAULT 'Mercado Livre',
  external_id TEXT NOT NULL DEFAULT '',
  name TEXT NOT NULL,
  category TEXT NOT NULL DEFAULT '',
  niche TEXT NOT NULL DEFAULT '',
  description TEXT NOT NULL DEFAULT '',
  affiliate_url TEXT NOT NULL DEFAULT '',
  original_image_path TEXT NOT NULL DEFAULT '',
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_products_name ON products(name COLLATE NOCASE);

CREATE TABLE IF NOT EXISTS image_versions (
  id TEXT PRIMARY KEY,
  product_id TEXT NOT NULL REFERENCES products(id) ON DELETE CASCADE,
  version INTEGER NOT NULL,
  file_path TEXT NOT NULL,
  prompt TEXT NOT NULL,
  style TEXT NOT NULL,
  is_approved INTEGER NOT NULL DEFAULT 0,
  created_at TEXT NOT NULL,
  UNIQUE(product_id, version)
);
CREATE INDEX IF NOT EXISTS ix_image_versions_product ON image_versions(product_id);

CREATE TABLE IF NOT EXISTS pin_drafts (
  id TEXT PRIMARY KEY,
  product_id TEXT NOT NULL UNIQUE REFERENCES products(id) ON DELETE CASCADE,
  title TEXT NOT NULL DEFAULT '',
  description TEXT NOT NULL DEFAULT '',
  board TEXT NOT NULL DEFAULT '',
  affiliate_url TEXT NOT NULL DEFAULT '',
  image_version_id TEXT REFERENCES image_versions(id) ON DELETE SET NULL,
  status TEXT NOT NULL DEFAULT 'Rascunho',
  updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS studio_logs (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  product_id TEXT NOT NULL REFERENCES products(id) ON DELETE CASCADE,
  occurred_at TEXT NOT NULL,
  action TEXT NOT NULL,
  message TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_logs_product ON studio_logs(product_id, occurred_at DESC);
