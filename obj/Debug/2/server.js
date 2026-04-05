const express = require("express");
const path = require("path");
const fs = require("fs");

const app = express();
const PORT = process.env.PORT || 3000;
const API_TOKEN = "SUPER_SECRET_TOKEN_HERE";

app.use(express.json());
app.use(express.static(path.join(__dirname, "public")));

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
  } catch (err) {
    console.error("Failed to load remarks:", err);
    return [];
  }
}

function saveRemarks() {
  try {
    fs.writeFileSync(LOG_FILE, JSON.stringify(state.remarks, null, 2), "utf8");
  } catch (err) {
    console.error("Failed to save remarks:", err);
  }
}

const state = {
  hp: 2500,
  breachStatus: "CONTAINED",
  currentRoom: "Unknown",
  previousRoom: "Unknown",
  activityTimeline: Array.from({ length: 60 }, () => 0),
  currentActivity: 0,
  activityUntil: 0,
  lastUpdated: new Date().toISOString(),
  remarks: loadRemarks(),
};

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
    return res.status(401).json({ ok: false, error: "Unauthorized" });
  }
  next();
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
  addRemark(`Vitalwertänderung:  ${state.hp}`);
  res.json({ ok: true, hp: state.hp });
});

app.post("/api/set/breach-status", auth, (req, res) => {
  state.breachStatus = String(req.body.breachStatus || "UNKNOWN");
  state.lastUpdated = new Date().toISOString();
  addRemark(`Breach status changed to ${state.breachStatus}`);
  res.json({ ok: true, breachStatus: state.breachStatus });
});

app.post("/api/set/rooms", auth, (req, res) => {
  state.currentRoom = String(req.body.currentRoom || "Unknown");
  state.previousRoom = String(req.body.previousRoom || "Unknown");
  state.lastUpdated = new Date().toISOString();
  addRemark(`Raum änderung: ${state.previousRoom} -> ${state.currentRoom}`);
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

  addRemark(`Aktivitätsänderung: ${activity}`);

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
    return res.status(400).json({ ok: false, error: "Message is empty" });
  }

  addRemark(message);
  res.json({ ok: true });
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

app.listen(PORT, () => {
  console.log(`SCP-1356 status page listening on port ${PORT}`);
});