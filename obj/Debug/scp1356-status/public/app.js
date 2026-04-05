const hpValue = document.getElementById("hpValue");
const breachStatus = document.getElementById("breachStatus");
const breachBanner = document.getElementById("breachBanner");
const currentRoom = document.getElementById("currentRoom");
const previousRoom = document.getElementById("previousRoom");
const liveActivity = document.getElementById("liveActivity");
const alertBar = document.getElementById("alertBar");
const terminalLog = document.getElementById("terminalLog");
const lastUpdatedText = document.getElementById("lastUpdatedText");

const labels = Array.from({ length: 60 }, (_, index) => `${59 - index}s`);

const chartCanvas = document.getElementById("activityChart");
const chartContext = chartCanvas.getContext("2d");

const activityChart = new Chart(chartContext, {
  type: "line",
  data: {
    labels,
    datasets: [
      {
        label: "Aktivität",
        data: Array.from({ length: 60 }, () => 0),
        borderColor: "#e3e3dd",
        backgroundColor: "rgba(255,255,255,0.08)",
        fill: true,
        borderWidth: 2,
        tension: 0.24,
        pointRadius: 0,
      },
    ],
  },
  options: {
    responsive: true,
    maintainAspectRatio: false,
    animation: false,
    interaction: {
      intersect: false,
      mode: "index",
    },
    plugins: {
      legend: {
        labels: {
          color: "#d7d7d1",
        },
      },
      tooltip: {
        backgroundColor: "rgba(20, 20, 22, 0.96)",
        borderColor: "rgba(255,255,255,0.12)",
        borderWidth: 1,
        titleColor: "#ffffff",
        bodyColor: "#d8d8d2",
      },
    },
    scales: {
      x: {
        ticks: {
          color: "#8f9298",
          maxTicksLimit: 10,
        },
        grid: {
          color: "rgba(255,255,255,0.06)",
        },
      },
      y: {
        min: 0,
        max: 10,
        ticks: {
          stepSize: 1,
          color: "#8f9298",
        },
        grid: {
          color: "rgba(255,255,255,0.06)",
        },
      },
    },
  },
});

function formatGermanTime(timestamp) {
  const date = new Date(timestamp);

  if (Number.isNaN(date.getTime())) {
    return "—";
  }

  return new Intl.DateTimeFormat("de-DE", {
    dateStyle: "short",
    timeStyle: "medium",
  }).format(date);
}

function normalizeStatus(value) {
  const raw = String(value || "").trim().toUpperCase();

  if (!raw) return "UNBEKANNT";
  if (raw.includes("CONTAIN") || raw === "EINGEDÄMMT") return "EINGEDÄMMT";
  if (raw.includes("LOCKDOWN")) return "LOCKDOWN";
  if (raw.includes("KRITISCH") || raw.includes("CRITICAL")) return "KRITISCH";
  if (raw.includes("BREACH") || raw.includes("SICHERHEITSBRUCH")) return "SICHERHEITSBRUCH";

  return raw;
}

function setAlertVisualState(statusText) {
  alertBar.classList.remove("status-contained", "status-breach", "status-critical");

  if (statusText === "EINGEDÄMMT") {
    alertBar.classList.add("status-contained");
    return;
  }

  if (statusText === "KRITISCH") {
    alertBar.classList.add("status-critical");
    return;
  }

  alertBar.classList.add("status-breach");
}

function renderRemarks(remarks) {
  terminalLog.innerHTML = "";

  if (!Array.isArray(remarks) || remarks.length === 0) {
    terminalLog.innerHTML = "<div>&gt; Keine Protokolleinträge vorhanden.</div>";
    return;
  }

  for (const entry of remarks) {
    const row = document.createElement("div");
    const time = formatGermanTime(entry.timestamp);
    const message = String(entry.message || "").trim() || "Leerer Eintrag";

    row.textContent = `> ${time} // ${message}`;
    terminalLog.appendChild(row);
  }
}

function applyStatus(data) {
  const safeData = data || {};
  const statusText = normalizeStatus(safeData.breachStatus);

  hpValue.textContent = Number.isFinite(Number(safeData.hp))
    ? Math.max(0, Math.floor(Number(safeData.hp)))
    : 0;

  breachStatus.textContent = statusText;
  breachBanner.textContent = `SICHERHEITSSTATUS // ${statusText}`;
  currentRoom.textContent = safeData.currentRoom || "Sektor Unbekannt";
  previousRoom.textContent = safeData.previousRoom || "Sektor Unbekannt";

  const timeline = Array.isArray(safeData.activityTimeline) && safeData.activityTimeline.length
    ? safeData.activityTimeline.map((value) => {
        const numeric = Number(value);
        if (!Number.isFinite(numeric)) return 0;
        return Math.min(10, Math.max(0, numeric));
      })
    : Array.from({ length: 60 }, () => 0);

  const currentActivityValue = timeline[timeline.length - 1] ?? 0;
  liveActivity.textContent = String(currentActivityValue);

  activityChart.data.datasets[0].data = timeline;
  activityChart.update();

  setAlertVisualState(statusText);

  lastUpdatedText.textContent = `Letzte Aktualisierung: ${formatGermanTime(safeData.lastUpdated)}`;

  renderRemarks(safeData.remarks);
}

async function poll() {
  try {
    const response = await fetch("/api/status", {
      headers: {
        Accept: "application/json",
      },
      cache: "no-store",
    });

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`);
    }

    const payload = await response.json();

    if (!payload.ok) {
      throw new Error("API-Antwort nicht erfolgreich");
    }

    applyStatus(payload.data);
  } catch (error) {
    terminalLog.innerHTML = `<div>> Telemetrie-Anfrage fehlgeschlagen: ${error.message}</div>`;
    alertBar.classList.remove("status-contained", "status-breach", "status-critical");
    alertBar.classList.add("status-critical");
    breachBanner.textContent = "SICHERHEITSSTATUS // VERBINDUNG UNTERBROCHEN";
    lastUpdatedText.textContent = "Letzte Aktualisierung: Verbindung verloren";
  }
}

poll();
setInterval(poll, 1000);