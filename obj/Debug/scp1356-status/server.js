const express = require("express");
const path = require("path");
const fs = require("fs");

const app = express();
const PORT = Number(process.env.PORT) || 3000;
const API_TOKEN = process.env.API_TOKEN || "SUPER_SECRET_TOKEN_HERE";

app.use(express.json());

const PUBLIC_DIR = path.join(__dirname, "public");
app.use(express.static(PUBLIC_DIR));

const DATA_DIR = path.join(__dirname, "data");
const LOG_FILE = path.join(DATA_DIR, "remarks.json");

if (!fs.existsSync(DATA_DIR)) {
  fs.mkdirSync(DATA_DIR, { recursive: true });
}

function loadRemarks() {
  try {
    if (!fs.existsSync(LOG_FILE)) return [];
    const raw = fs.readFileSync(LOG_FILE, "utf8");
    const parsed = JSON.parse(raw);
    return Array.isArray(parsed) ? parsed : [];
  } catch (error) {
    console.error("Fehler beim Laden der Bemerkungen:", error);
    return [];
  }
}

const state = {
  hp: 2500,
  breachStatus: "CONTAINED",
  currentRoom: "Sektor Unbekannt",
  previousRoom: "Sektor Unbekannt",
  activityTimeline: Array.from({ length: 60 }, () => 0),
  currentActivity: 0,
  activityUntil: 0,
  lastUpdated: new Date().toISOString(),
  remarks: loadRemarks(),
};

function saveRemarks() {
  try {
    fs.writeFileSync(LOG_FILE, JSON.stringify(state.remarks, null, 2), "utf8");
  } catch (error) {
    console.error("Fehler beim Speichern der Bemerkungen:", error);
  }
}

function clamp(value, min, max) {
  return Math.max(min, Math.min(max, value));
}

function addRemark(message) {
  const entry = {
    timestamp: new Date().toISOString(),
    message: String(message || "").trim(),
  };

  if (!entry.message) return;

  state.remarks.unshift(entry);

  if (state.remarks.length > 200) {
    state.remarks = state.remarks.slice(0, 200);
  }

  saveRemarks();
}

function auth(req, res, next) {
  const token = req.headers["x-api-token"];

  if (token !== API_TOKEN) {
    return res.status(401).json({
      ok: false,
      error: "Nicht autorisiert",
    });
  }

  next();
}

function normalizeBreachStatus(value) {
  const raw = String(value || "").trim().toUpperCase();

  if (!raw) return "UNBEKANNT";
  if (["CONTAINED", "CONTAINMENT STABLE", "STABLE"].includes(raw)) return "EINGEDÄMMT";
  if (["BREACH", "ACTIVE BREACH", "ESCAPED"].includes(raw)) return "SICHERHEITSBRUCH";
  if (["LOCKDOWN"].includes(raw)) return "LOCKDOWN";
  if (["CRITICAL", "CRITICAL BREACH"].includes(raw)) return "KRITISCH";

  return raw;
}

app.get("/api/status", (req, res) => {
  res.json({
    ok: true,
    data: state,
  });
});

app.post("/api/set/hp", auth, (req, res) => {
  state.hp = Math.max(0, Math.floor(Number(req.body.hp) || 0));
  state.lastUpdated = new Date().toISOString();

  addRemark(`Vitalwert aktualisiert: ${state.hp} HP`);

  res.json({
    ok: true,
    hp: state.hp,
  });
});

app.post("/api/set/breach-status", auth, (req, res) => {
  state.breachStatus = normalizeBreachStatus(req.body.breachStatus);
  state.lastUpdated = new Date().toISOString();

  addRemark(`Sicherheitsstatus geändert: ${state.breachStatus}`);

  res.json({
    ok: true,
    breachStatus: state.breachStatus,
  });
});

app.post("/api/set/rooms", auth, (req, res) => {
  state.currentRoom = String(req.body.currentRoom || "Sektor Unbekannt").trim();
  state.previousRoom = String(req.body.previousRoom || "Sektor Unbekannt").trim();
  state.lastUpdated = new Date().toISOString();

  addRemark(`Raumwechsel: ${state.previousRoom} → ${state.currentRoom}`);

  res.json({
    ok: true,
    currentRoom: state.currentRoom,
    previousRoom: state.previousRoom,
  });
});

app.post("/api/set/activity", auth, (req, res) => {
  const activity = clamp(Number(req.body.activity) || 0, 0, 10);
  const duration = clamp(Math.floor(Number(req.body.duration) || 0), 0, 60);

  state.currentActivity = activity;
  state.activityUntil = Date.now() + duration * 1000;
  state.lastUpdated = new Date().toISOString();

  addRemark(`Aktivitätsindex geändert: ${activity} für ${duration}s`);

  res.json({
    ok: true,
    activity,
    duration,
    activityUntil: state.activityUntil,
  });
});

app.post("/api/add-remark", auth, (req, res) => {
  const message = String(req.body.message || "").trim();

  if (!message) {
    return res.status(400).json({
      ok: false,
      error: "Nachricht ist leer",
    });
  }

  addRemark(message);

  res.json({
    ok: true,
  });
});

setInterval(() => {
  const now = Date.now();

  if (now < state.activityUntil) {
    state.activityTimeline.shift();
    state.activityTimeline.push(state.currentActivity);
  } else {
    state.currentActivity = 0;
    state.activityTimeline.shift();
    state.activityTimeline.push(0);
  }

  state.lastUpdated = new Date().toISOString();
}, 1000);

app.get(/.*/, (req, res) => {
  res.sendFile(path.join(PUBLIC_DIR, "index.html"));
});

app.listen(PORT, () => {
  console.log(`SCP-1356 Überwachungsoberfläche läuft auf Port ${PORT}`);
});