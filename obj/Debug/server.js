const express = require("express");
const session = require("express-session");
const bodyParser = require("body-parser");
const bcrypt = require("bcryptjs");
const Database = require("better-sqlite3");
const dns = require("dns").promises;
const crypto = require("crypto");
const net = require("net");

const app = express();
const db = new Database("license.db");

const PORT = 4000;
const DEFAULT_USERNAME = "Tristan";
const DEFAULT_PASSWORD = "2009";
const PLUGIN_SHARED_SECRET = "Public-Key-AjHs)2aPPsa3Kan";
const SERVER_DOMAIN = "m26g23tvsv4vpvyy.myfritz.net";
const ONLINE_THRESHOLD_SECONDS = 15;

app.use(bodyParser.urlencoded({ extended: true }));
app.use(bodyParser.json());

app.use(session({
  secret: "very-long-random-session-secret-change-me",
  resave: false,
  saveUninitialized: false,
  cookie: {
    httpOnly: true,
    secure: false
  }
}));

function tableExists(name) {
  const row = db.prepare(`
    SELECT name FROM sqlite_master
    WHERE type='table' AND name=?
  `).get(name);
  return !!row;
}

function columnExists(table, column) {
  try {
    const cols = db.prepare(`PRAGMA table_info(${table})`).all();
    return cols.some(c => c.name === column);
  } catch {
    return false;
  }
}

function ensureSchema() {
  db.exec(`
    CREATE TABLE IF NOT EXISTS admin_users (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      username TEXT NOT NULL UNIQUE,
      password_hash TEXT NOT NULL
    );

    CREATE TABLE IF NOT EXISTS licenses (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      license_key TEXT NOT NULL UNIQUE,
      label TEXT DEFAULT '',
      created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
    );

    CREATE TABLE IF NOT EXISTS whitelisted_entries (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      license_id INTEGER NOT NULL,
      value TEXT NOT NULL,
      type TEXT NOT NULL,
      note TEXT DEFAULT '',
      created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
      UNIQUE(license_id, value),
      FOREIGN KEY (license_id) REFERENCES licenses(id) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS plugin_logs (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      license_key TEXT,
      identity TEXT,
      plugin_name TEXT,
      plugin_version TEXT,
      result TEXT,
      message TEXT,
      created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
    );

    CREATE TABLE IF NOT EXISTS pending_messages (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      license_id INTEGER NOT NULL,
      message TEXT NOT NULL,
      created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
      FOREIGN KEY (license_id) REFERENCES licenses(id) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS server_statuses (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      license_id INTEGER NOT NULL,
      entry_value TEXT NOT NULL,
      reported_identity TEXT NOT NULL,
      server_name TEXT DEFAULT '',
      players INTEGER DEFAULT 0,
      game_version TEXT DEFAULT '',
      plugin_version TEXT DEFAULT '',
      last_seen TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
      UNIQUE(license_id, entry_value),
      FOREIGN KEY (license_id) REFERENCES licenses(id) ON DELETE CASCADE
    );
  `);

  if (tableExists("whitelisted_ips")) {
    const oldRows = db.prepare("SELECT * FROM whitelisted_ips").all();

    for (const row of oldRows) {
      try {
        db.prepare(`
          INSERT OR IGNORE INTO whitelisted_entries (license_id, value, type, note, created_at)
          VALUES (?, ?, 'ip', ?, ?)
        `).run(
          row.license_id,
          String(row.ip || "").trim().toLowerCase(),
          row.note || "",
          row.created_at || new Date().toISOString()
        );
      } catch {}
    }

    db.prepare("DROP TABLE whitelisted_ips").run();
  }

  if (!columnExists("plugin_logs", "identity")) {
    try {
      db.prepare("ALTER TABLE plugin_logs ADD COLUMN identity TEXT").run();
    } catch {}
  }

  if (columnExists("plugin_logs", "ip")) {
    try {
      db.prepare(`
        UPDATE plugin_logs
        SET identity = ip
        WHERE (identity IS NULL OR identity = '') AND ip IS NOT NULL
      `).run();
    } catch {}
  }
}

ensureSchema();

function ensureDefaultAdmin() {
  const existing = db.prepare("SELECT id FROM admin_users WHERE username = ?").get(DEFAULT_USERNAME);
  if (!existing) {
    const hash = bcrypt.hashSync(DEFAULT_PASSWORD, 10);
    db.prepare("INSERT INTO admin_users (username, password_hash) VALUES (?, ?)")
      .run(DEFAULT_USERNAME, hash);
  }
}

ensureDefaultAdmin();

function requireAuth(req, res, next) {
  if (!req.session.user) return res.redirect("/login");
  next();
}

function escapeHtml(str = "") {
  return String(str)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function generateSecureKey(length = 32) {
  const chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789-_@#!%";
  const bytes = crypto.randomBytes(length);
  let out = "";

  for (let i = 0; i < length; i++) {
    out += chars[bytes[i] % chars.length];
  }

  return out;
}

function isIp(value) {
  return net.isIP(value) !== 0;
}

function isDomain(value) {
  return /^[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/.test(value);
}

async function resolveToIpv4(value) {
  const input = String(value || "").trim().toLowerCase();

  if (!input) return null;
  if (isIp(input)) return input;

  if (isDomain(input)) {
    try {
      const resolved = await dns.lookup(input, { family: 4 });
      return String(resolved.address || "").trim().toLowerCase();
    } catch {
      return null;
    }
  }

  return null;
}

function logPluginCheck(licenseKey, identity, pluginName, pluginVersion, result, message) {
  db.prepare(`
    INSERT INTO plugin_logs (license_key, identity, plugin_name, plugin_version, result, message)
    VALUES (?, ?, ?, ?, ?, ?)
  `).run(
    licenseKey || "",
    identity || "",
    pluginName || "",
    pluginVersion || "",
    result || "",
    message || ""
  );
}

async function findMatchingEntry(licenseId, identity) {
  const entries = db.prepare(`
    SELECT * FROM whitelisted_entries
    WHERE license_id = ?
  `).all(licenseId);

  const normalizedIdentity = String(identity || "").trim().toLowerCase();
  const identityIp = await resolveToIpv4(normalizedIdentity);

  for (const entry of entries) {
    const entryValue = String(entry.value || "").trim().toLowerCase();

    if (normalizedIdentity === entryValue)
      return entry;

    const entryIp = await resolveToIpv4(entryValue);
    if (identityIp && entryIp && identityIp === entryIp)
      return entry;
  }

  return null;
}

function updateServerStatus(licenseId, entryValue, reportedIdentity, serverName, players, gameVersion, pluginVersion) {
  db.prepare(`
    INSERT INTO server_statuses (
      license_id,
      entry_value,
      reported_identity,
      server_name,
      players,
      game_version,
      plugin_version,
      last_seen
    ) VALUES (?, ?, ?, ?, ?, ?, ?, CURRENT_TIMESTAMP)
    ON CONFLICT(license_id, entry_value)
    DO UPDATE SET
      reported_identity = excluded.reported_identity,
      server_name = excluded.server_name,
      players = excluded.players,
      game_version = excluded.game_version,
      plugin_version = excluded.plugin_version,
      last_seen = CURRENT_TIMESTAMP
  `).run(
    licenseId,
    String(entryValue || "").trim().toLowerCase(),
    String(reportedIdentity || "").trim().toLowerCase(),
    String(serverName || "").trim(),
    Number(players || 0),
    String(gameVersion || "").trim(),
    String(pluginVersion || "").trim()
  );
}

function getStatusForEntry(licenseId, entryValue) {
  return db.prepare(`
    SELECT *
    FROM server_statuses
    WHERE license_id = ? AND entry_value = ?
  `).get(licenseId, String(entryValue || "").trim().toLowerCase());
}

function isOnline(lastSeen) {
  if (!lastSeen) return false;
  const last = new Date(lastSeen).getTime();
  if (Number.isNaN(last)) return false;
  const diffSeconds = (Date.now() - last) / 1000;
  return diffSeconds <= ONLINE_THRESHOLD_SECONDS;
}

function renderLayout(title, content) {
  return `
  <html>
    <head>
      <meta charset="utf-8" />
      <meta name="viewport" content="width=device-width, initial-scale=1.0" />
      <title>${escapeHtml(title)}</title>
      <style>
        :root{
          --bg:#0b1020;
          --card:#111827;
          --text:#f8fafc;
          --muted:#94a3b8;
          --line:#253047;
          --primary:#3b82f6;
          --danger:#dc2626;
          --success:#16a34a;
          --warning:#f59e0b;
        }
        *{box-sizing:border-box}
        body{
          margin:0;
          font-family:Arial,sans-serif;
          color:var(--text);
          background:
            radial-gradient(circle at top left, rgba(59,130,246,.18), transparent 25%),
            radial-gradient(circle at top right, rgba(168,85,247,.14), transparent 20%),
            linear-gradient(135deg, var(--bg), #0f172a 55%, #0a1120 100%);
        }
        .wrap{max-width:1300px;margin:0 auto;padding:24px}
        .card{
          background:rgba(17,24,39,.92);
          border:1px solid var(--line);
          border-radius:16px;
          padding:20px;
          margin-bottom:18px;
          box-shadow:0 10px 30px rgba(0,0,0,.25);
        }
        .row{display:flex;gap:10px;flex-wrap:wrap}
        .grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(360px,1fr));gap:16px}
        input,button,textarea{
          padding:12px;border-radius:10px;border:none;font:inherit
        }
        input,textarea{
          background:#1f2937;color:#fff;flex:1;min-width:180px;border:1px solid #334155
        }
        textarea{width:100%;min-height:90px;resize:vertical}
        button{background:var(--primary);color:#fff;cursor:pointer}
        .danger{background:var(--danger)}
        .muted{color:var(--muted)}
        .mono{font-family:Consolas,monospace;word-break:break-all}
        a{color:#93c5fd}
        .pill{
          display:inline-block;padding:6px 10px;border-radius:999px;
          background:#1e293b;color:#cbd5e1;font-size:12px
        }
        .status-online{
          background:rgba(22,163,74,.18);
          color:#86efac;
        }
        .status-offline{
          background:rgba(220,38,38,.18);
          color:#fca5a5;
        }
        .entry-box{
          border:1px solid #334155;
          border-radius:14px;
          padding:12px;
          margin-top:10px;
        }
        .entry-status{
          margin-top:10px;
          padding:12px;
          border-radius:12px;
          background:#0f172a;
          border:1px solid #1e293b;
        }
        .login-box{max-width:420px;margin:80px auto 0}
      </style>
    </head>
    <body>${content}</body>
  </html>
  `;
}

app.get("/login", (req, res) => {
  res.send(renderLayout("Login", `
    <div class="wrap">
      <div class="login-box card">
        <h1>Login</h1>
        <p class="muted">Melde dich an, um Lizenzen zu verwalten.</p>
        <form method="post" action="/login">
          <div class="row" style="flex-direction:column">
            <input name="username" placeholder="Username" required />
            <input name="password" placeholder="Password" type="password" required />
            <button type="submit">Einloggen</button>
          </div>
        </form>
      </div>
    </div>
  `));
});

app.post("/login", (req, res) => {
  const { username, password } = req.body;
  const user = db.prepare("SELECT * FROM admin_users WHERE username = ?").get(username);

  if (!user || !bcrypt.compareSync(password, user.password_hash)) {
    return res.status(401).send("Falsche Login-Daten.");
  }

  req.session.user = { id: user.id, username: user.username };
  res.redirect("/");
});

app.get("/logout", (req, res) => {
  req.session.destroy(() => res.redirect("/login"));
});

app.get("/", requireAuth, async (req, res) => {
  const licenses = db.prepare("SELECT * FROM licenses ORDER BY id DESC").all();
  const logs = db.prepare(`
    SELECT * FROM plugin_logs
    ORDER BY id DESC
    LIMIT 15
  `).all();

  const generatedLength = Math.max(16, Math.min(128, parseInt(req.query.keyLength, 10) || 32));
  const generatedKey = generateSecureKey(generatedLength);

  let licensesHtml = "";

  for (const l of licenses) {
    const entries = db.prepare(`
      SELECT * FROM whitelisted_entries
      WHERE license_id = ?
      ORDER BY id DESC
    `).all(l.id);

    let list = `<div class="muted">Keine IPs/Domains whitelisted.</div>`;

    if (entries.length) {
      const parts = [];

      for (const e of entries) {
        const status = getStatusForEntry(l.id, e.value);
        const online = status ? isOnline(status.last_seen) : false;

        parts.push(`
          <div class="entry-box">
            <div><span class="pill">${escapeHtml(e.type.toUpperCase())}</span></div>
            <div class="mono" style="margin-top:8px">${escapeHtml(e.value)}</div>
            <div class="muted">${escapeHtml(e.note || "Keine Notiz")}</div>

            <div class="entry-status">
              <div>
                <span class="pill ${online ? "status-online" : "status-offline"}">
                  ${online ? "ONLINE" : "OFFLINE"}
                </span>
              </div>
              <div style="margin-top:8px"><strong>Spieler:</strong> ${status ? escapeHtml(String(status.players)) : "0"}</div>
              <div><strong>Spiel-Version:</strong> ${status ? escapeHtml(status.game_version || "Unknown") : "Unknown"}</div>
              <div><strong>Plugin-Version:</strong> ${status ? escapeHtml(status.plugin_version || "Unknown") : "Unknown"}</div>
              <div><strong>Servername:</strong> ${status ? escapeHtml(status.server_name || "Unknown") : "Unknown"}</div>
              <div><strong>Zuletzt gesehen:</strong> ${status ? escapeHtml(status.last_seen || "Nie") : "Nie"}</div>
              <div><strong>Gemeldete Identity:</strong> ${status ? escapeHtml(status.reported_identity || "Unknown") : "Unknown"}</div>
            </div>

            <form method="post" action="/entry/delete" style="margin-top:10px">
              <input type="hidden" name="entryId" value="${e.id}" />
              <button class="danger" type="submit">Entfernen</button>
            </form>
          </div>
        `);
      }

      list = parts.join("");
    }

    licensesHtml += `
      <div class="card">
        <h3>${escapeHtml(l.label || "Ohne Label")}</h3>
        <div class="mono">${escapeHtml(l.license_key)}</div>

        <form method="post" action="/entry/add" style="margin-top:14px">
          <input type="hidden" name="licenseId" value="${l.id}" />
          <div class="row">
            <input name="value" placeholder="IP oder Domain hinzufügen" required />
            <input name="note" placeholder="Notiz (optional)" />
            <button type="submit">Whitelisten</button>
          </div>
        </form>

        <div style="margin-top:12px">${list}</div>

        <form method="post" action="/license/send-message" style="margin-top:14px">
          <input type="hidden" name="licenseId" value="${l.id}" />
          <textarea name="message" placeholder="String an genau diesen Server senden"></textarea>
          <div class="row" style="margin-top:10px;">
            <button type="submit">Senden</button>
          </div>
        </form>

        <form method="post" action="/license/delete" style="margin-top:14px">
          <input type="hidden" name="licenseId" value="${l.id}" />
          <button class="danger" type="submit">Lizenz löschen</button>
        </form>
      </div>
    `;
  }

  const logsHtml = logs.length
    ? logs.map(log => `
      <div class="card" style="padding:12px">
        <div><strong>${escapeHtml(log.result)}</strong></div>
        <div class="mono">${escapeHtml(log.identity || "")}</div>
        <div class="muted">${escapeHtml(log.message || "")}</div>
        <div class="muted">${escapeHtml(log.plugin_name || "")} ${escapeHtml(log.plugin_version || "")} · ${escapeHtml(log.created_at || "")}</div>
      </div>
    `).join("")
    : `<div class="muted">Keine Logs vorhanden.</div>`;

  res.send(renderLayout("Lizenzpanel", `
    <div class="wrap">
      <div class="card">
        <h1>Lizenzpanel</h1>
        <div class="muted">Eingeloggt als ${escapeHtml(req.session.user.username)} - <a href="/logout">Logout</a></div>
      </div>

      <div class="grid">
        <div class="card">
          <h2>Neue Lizenz</h2>
          <form method="post" action="/license/create">
            <div class="row">
              <input name="licenseKey" placeholder="License Key" required />
              <input name="label" placeholder="Label / Kunde / Servername" />
              <button type="submit">Erstellen</button>
            </div>
          </form>
        </div>

        <div class="card">
          <h2>Key Generator</h2>
          <form method="get" action="/">
            <div class="row">
              <input type="number" name="keyLength" min="16" max="128" value="${generatedLength}" />
              <button type="submit">Generieren</button>
            </div>
          </form>
          <div class="card" style="margin-top:12px">
            <div id="generatedKey" class="mono">${escapeHtml(generatedKey)}</div>
            <button style="margin-top:10px" type="button" onclick="copyGeneratedKey()">Kopieren</button>
          </div>
        </div>
      </div>

      <div class="card">
        <h2>Lizenzen & Serverstatus</h2>
        <div class="grid">
          ${licensesHtml || `<div class="muted">Noch keine Lizenzen vorhanden.</div>`}
        </div>
      </div>

      <div class="card">
        <h2>Letzte Logs</h2>
        ${logsHtml}
      </div>
    </div>

    <script>
      async function copyGeneratedKey() {
        const value = document.getElementById("generatedKey").innerText;
        try {
          await navigator.clipboard.writeText(value);
          alert("Kopiert!");
        } catch {
          prompt("Kopiere diesen Text:", value);
        }
      }
    </script>
  `));
});

app.post("/license/create", requireAuth, (req, res) => {
  const { licenseKey, label } = req.body;
  try {
    db.prepare("INSERT INTO licenses (license_key, label) VALUES (?, ?)")
      .run(String(licenseKey).trim(), String(label || "").trim());
    res.redirect("/");
  } catch {
    res.status(400).send("Lizenz konnte nicht erstellt werden. Vielleicht existiert sie schon.");
  }
});

app.post("/license/send-message", requireAuth, (req, res) => {
  try {
    const { licenseId, message } = req.body;
    const msg = String(message || "").trim();
    const id = parseInt(licenseId, 10);

    if (!id || !msg)
      return res.redirect("/");

    db.prepare(`
      INSERT INTO pending_messages (license_id, message)
      VALUES (?, ?)
    `).run(id, msg);

    res.redirect("/");
  } catch {
    res.redirect("/");
  }
});

app.post("/license/delete", requireAuth, (req, res) => {
  const { licenseId } = req.body;
  db.prepare("DELETE FROM pending_messages WHERE license_id = ?").run(licenseId);
  db.prepare("DELETE FROM server_statuses WHERE license_id = ?").run(licenseId);
  db.prepare("DELETE FROM whitelisted_entries WHERE license_id = ?").run(licenseId);
  db.prepare("DELETE FROM licenses WHERE id = ?").run(licenseId);
  res.redirect("/");
});

app.post("/entry/add", requireAuth, (req, res) => {
  const { licenseId, value, note } = req.body;
  const raw = String(value || "").trim().toLowerCase();

  if (!raw) return res.status(400).send("Wert fehlt.");

  let type = "unknown";
  if (isIp(raw)) type = "ip";
  else if (isDomain(raw)) type = "domain";
  else return res.status(400).send("Nur IP oder Domain erlaubt.");

  try {
    db.prepare(`
      INSERT INTO whitelisted_entries (license_id, value, type, note)
      VALUES (?, ?, ?, ?)
    `).run(licenseId, raw, type, String(note || "").trim());

    res.redirect("/");
  } catch {
    res.status(400).send("Eintrag konnte nicht hinzugefügt werden. Vielleicht existiert er schon.");
  }
});

app.post("/entry/delete", requireAuth, (req, res) => {
  const { entryId } = req.body;
  const entry = db.prepare("SELECT * FROM whitelisted_entries WHERE id = ?").get(entryId);

  if (entry) {
    db.prepare(`
      DELETE FROM server_statuses
      WHERE license_id = ? AND entry_value = ?
    `).run(entry.license_id, entry.value);
  }

  db.prepare("DELETE FROM whitelisted_entries WHERE id = ?").run(entryId);
  res.redirect("/");
});

app.post("/api/plugin/check", async (req, res) => {
  try {
    const sharedSecret = req.header("X-Plugin-Secret");

    if (sharedSecret !== PLUGIN_SHARED_SECRET) {
      return res.status(403).json({
        ok: false,
        isWhitelisted: false,
        message: "Ungültiges Shared Secret"
      });
    }

    const { licenseKey, serverIdentity, pluginName, pluginVersion } = req.body || {};
    const identity = String(serverIdentity || "").trim().toLowerCase();
    const key = String(licenseKey || "").trim();

    if (!identity) {
      return res.status(400).json({
        ok: false,
        isWhitelisted: false,
        message: "serverIdentity fehlt"
      });
    }

    let ownServerIp = null;
    try {
      const resolved = await dns.lookup(SERVER_DOMAIN, { family: 4 });
      ownServerIp = String(resolved.address || "").trim().toLowerCase();
    } catch {}

    if (identity === SERVER_DOMAIN.toLowerCase() || (ownServerIp && identity === ownServerIp)) {
      logPluginCheck(key, identity, pluginName, pluginVersion, "ALLOWED", "Eigener Server automatisch erlaubt");
      return res.json({
        ok: true,
        isWhitelisted: true,
        message: "Eigener Server automatisch erlaubt"
      });
    }

    if (!key) {
      return res.status(400).json({
        ok: false,
        isWhitelisted: false,
        message: "licenseKey fehlt"
      });
    }

    const license = db.prepare("SELECT * FROM licenses WHERE license_key = ?").get(key);

    if (!license) {
      logPluginCheck(key, identity, pluginName, pluginVersion, "DENIED", "Lizenzkey nicht gefunden");
      return res.json({
        ok: true,
        isWhitelisted: false,
        message: "Lizenzkey nicht gefunden"
      });
    }

    const matchedEntry = await findMatchingEntry(license.id, identity);
    const allowed = !!matchedEntry;

    logPluginCheck(
      key,
      identity,
      pluginName,
      pluginVersion,
      allowed ? "ALLOWED" : "DENIED",
      allowed ? "Identity ist freigeschaltet" : "Identity ist nicht freigeschaltet"
    );

    return res.json({
      ok: true,
      isWhitelisted: allowed,
      message: allowed ? "Identity ist freigeschaltet" : "Identity ist nicht freigeschaltet"
    });
  } catch (err) {
    console.error("[CHECK] Fatal error:", err);
    return res.status(500).json({
      ok: false,
      isWhitelisted: false,
      message: "Interner Serverfehler"
    });
  }
});

app.post("/api/plugin/poll-message", (req, res) => {
  try {
    const sharedSecret = req.header("X-Plugin-Secret");

    if (sharedSecret !== PLUGIN_SHARED_SECRET) {
      return res.status(403).json({
        ok: false,
        hasMessage: false,
        message: ""
      });
    }

    const { licenseKey } = req.body || {};
    const key = String(licenseKey || "").trim();

    if (!key) {
      return res.status(400).json({
        ok: false,
        hasMessage: false,
        message: ""
      });
    }

    const license = db.prepare("SELECT * FROM licenses WHERE license_key = ?").get(key);

    if (!license) {
      return res.json({
        ok: true,
        hasMessage: false,
        message: ""
      });
    }

    const pending = db.prepare(`
      SELECT id, message
      FROM pending_messages
      WHERE license_id = ?
      ORDER BY id ASC
      LIMIT 1
    `).get(license.id);

    if (!pending) {
      return res.json({
        ok: true,
        hasMessage: false,
        message: ""
      });
    }

    db.prepare("DELETE FROM pending_messages WHERE id = ?").run(pending.id);

    return res.json({
      ok: true,
      hasMessage: true,
      message: String(pending.message || "")
    });
  } catch (err) {
    console.error("[POLL MESSAGE] Fatal error:", err);
    return res.status(500).json({
      ok: false,
      hasMessage: false,
      message: ""
    });
  }
});

app.post("/api/plugin/heartbeat", async (req, res) => {
  try {
    const sharedSecret = req.header("X-Plugin-Secret");

    if (sharedSecret !== PLUGIN_SHARED_SECRET) {
      return res.status(403).json({ ok: false, message: "Ungültiges Shared Secret" });
    }

    const {
      licenseKey,
      serverIdentity,
      serverName,
      players,
      gameVersion,
      pluginVersion
    } = req.body || {};

    const key = String(licenseKey || "").trim();
    const identity = String(serverIdentity || "").trim().toLowerCase();

    if (!key || !identity) {
      return res.status(400).json({ ok: false, message: "licenseKey oder serverIdentity fehlt" });
    }

    const license = db.prepare("SELECT * FROM licenses WHERE license_key = ?").get(key);
    if (!license) {
      return res.json({ ok: false, message: "Lizenz nicht gefunden" });
    }

    const matchedEntry = await findMatchingEntry(license.id, identity);
    if (!matchedEntry) {
      return res.json({ ok: false, message: "Server nicht whitelisted" });
    }

    updateServerStatus(
      license.id,
      matchedEntry.value,
      identity,
      serverName,
      players,
      gameVersion,
      pluginVersion
    );

    return res.json({ ok: true, message: "Heartbeat gespeichert" });
  } catch (err) {
    console.error("[HEARTBEAT] Fatal error:", err);
    return res.status(500).json({ ok: false, message: "Interner Serverfehler" });
  }
});

app.listen(PORT, "0.0.0.0", () => {
  console.log(`License panel running on http://0.0.0.0:${PORT}`);
});