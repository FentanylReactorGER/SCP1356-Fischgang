const express = require("express");
const path = require("path");

const app = express();

const API_KEY = "SECRET_KEY";
const PORT = 3000;
const MAX_LOGS = 50;
const BREACH_DURATION_MS = 15000;

app.use(express.json({ limit: "1mb" }));

app.use((req, res, next) => {
    res.setHeader("Content-Type", "text/html; charset=utf-8");
    next();
});

app.use(express.static(path.join(__dirname, "public"), {
    setHeaders: (res, filePath) => {
        if (filePath.endsWith(".html")) {
            res.setHeader("Content-Type", "text/html; charset=utf-8");
        } else if (filePath.endsWith(".js")) {
            res.setHeader("Content-Type", "application/javascript; charset=utf-8");
        } else if (filePath.endsWith(".css")) {
            res.setHeader("Content-Type", "text/css; charset=utf-8");
        } else if (filePath.endsWith(".json")) {
            res.setHeader("Content-Type", "application/json; charset=utf-8");
        }
    }
}));

let scpData = {
    health: 0,
    maxHealth: 2500,
    location: "UNKNOWN",
    previousLocation: "UNKNOWN",
    currentZone: "UNKNOWN",
    lastUpdate: null,

    breach: false,
    breachUntil: 0,
    lastBreachFrom: "UNKNOWN",
    lastBreachTo: "UNKNOWN",

    logs: [
        {
            time: new Date().toISOString(),
            type: "system",
            text: "‹berwachungsknoten initialisiert."
        }
    ]
};

function addLog(type, text) {
    scpData.logs.unshift({
        time: new Date().toISOString(),
        type,
        text
    });

    if (scpData.logs.length > MAX_LOGS) {
        scpData.logs.length = MAX_LOGS;
    }
}

function refreshBreachState() {
    if (scpData.breach && Date.now() > scpData.breachUntil) {
        scpData.breach = false;
    }
}

app.post("/update", (req, res) => {
    if (req.headers.authorization !== API_KEY) {
        return res.sendStatus(403);
    }

    const newHealth = Number.isFinite(req.body.health) ? req.body.health : 0;
    const newMaxHealth = Number.isFinite(req.body.maxHealth) ? req.body.maxHealth : scpData.maxHealth;

    const newLocation =
        typeof req.body.location === "string" && req.body.location.trim().length > 0
            ? req.body.location.trim()
            : "UNKNOWN";

    const newPreviousLocation =
        typeof req.body.previousLocation === "string" && req.body.previousLocation.trim().length > 0
            ? req.body.previousLocation.trim()
            : scpData.previousLocation;

    const newZone =
        typeof req.body.currentZone === "string" && req.body.currentZone.trim().length > 0
            ? req.body.currentZone.trim()
            : "UNKNOWN";

    const oldHealth = scpData.health;
    const oldLocation = scpData.location;

    scpData.health = newHealth;
    scpData.maxHealth = newMaxHealth;
    scpData.location = newLocation;
    scpData.previousLocation = newPreviousLocation;
    scpData.currentZone = newZone;
    scpData.lastUpdate = new Date().toISOString();

    if (oldLocation !== "UNKNOWN" && oldLocation !== newLocation) {
        scpData.breach = true;
        scpData.breachUntil = Date.now() + BREACH_DURATION_MS;
        scpData.lastBreachFrom = oldLocation;
        scpData.lastBreachTo = newLocation;

        addLog("movement", `SCP-1356 wechselte von ${oldLocation} nach ${newLocation}.`);
        addLog("breach", `Eind‰mmungsverstoﬂ erkannt: Raumwechsel von ${oldLocation} nach ${newLocation}.`);
    } else {
        refreshBreachState();
    }

    if (oldHealth !== newHealth) {
        const delta = newHealth - oldHealth;

        if (delta < 0) {
            addLog("damage", `Vitalwerte sanken um ${Math.abs(delta)}. Aktuelle Gesundheit: ${newHealth}.`);
        } else if (delta > 0) {
            addLog("heal", `Vitalwerte stiegen um ${delta}. Aktuelle Gesundheit: ${newHealth}.`);
        }
    }

    res.setHeader("Content-Type", "application/json; charset=utf-8");
    res.status(200).json({ ok: true });
});

app.get("/data", (req, res) => {
    refreshBreachState();
    res.setHeader("Content-Type", "application/json; charset=utf-8");
    res.json(scpData);
});

app.listen(PORT, () => {
    console.log(`SCP-1356 ‹berwachungsserver l‰uft auf Port ${PORT}`);
});