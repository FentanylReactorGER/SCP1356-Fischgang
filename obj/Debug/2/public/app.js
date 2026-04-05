const hpValue = document.getElementById("hpValue");
const breachStatus = document.getElementById("breachStatus");
const breachBanner = document.getElementById("breachBanner");
const currentRoom = document.getElementById("currentRoom");
const previousRoom = document.getElementById("previousRoom");
const liveActivity = document.getElementById("liveActivity");
const alertBar = document.getElementById("alertBar");
const terminalLog = document.getElementById("terminalLog");

const labels = Array.from({ length: 60 }, (_, i) => `${59 - i}s`);

const ctx = document.getElementById("activityChart").getContext("2d");
const activityChart = new Chart(ctx, {
  type: "line",
  data: {
    labels,
    datasets: [
      {
        label: "Activity",
        data: Array.from({ length: 60 }, () => 0),
        borderColor: "#83ffd8",
        backgroundColor: "rgba(131,255,216,0.12)",
        fill: true,
        borderWidth: 2,
        tension: 0.25,
        pointRadius: 0,
      },
    ],
  },
  options: {
    responsive: true,
    maintainAspectRatio: false,
    animation: false,
    plugins: {
      legend: {
        labels: {
          color: "#dff7ef",
        },
      },
    },
    scales: {
      x: {
        ticks: { color: "#86a79f", maxTicksLimit: 10 },
        grid: { color: "rgba(131,255,216,0.08)" },
      },
      y: {
        min: 0,
        max: 10,
        ticks: { stepSize: 1, color: "#86a79f" },
        grid: { color: "rgba(131,255,216,0.08)" },
      },
    },
  },
});

function applyStatus(data) {
  hpValue.textContent = data.hp ?? 0;
  breachStatus.textContent = data.breachStatus ?? "UNKNOWN";
  breachBanner.textContent = `BREACH STATUS // ${data.breachStatus ?? "UNKNOWN"}`;
  currentRoom.textContent = data.currentRoom ?? "Unknown";
  previousRoom.textContent = data.previousRoom ?? "Unknown";

  const currentAct = data.activityTimeline?.[data.activityTimeline.length - 1] ?? 0;
  liveActivity.textContent = currentAct;

  activityChart.data.datasets[0].data = data.activityTimeline || Array(60).fill(0);
  activityChart.update();

  const normalized = String(data.breachStatus || "").toUpperCase();
  alertBar.classList.remove("alert-contained", "alert-breach");

  if (normalized.includes("CONTAIN")) {
    alertBar.classList.add("alert-contained");
  } else {
    alertBar.classList.add("alert-breach");
  }

  renderRemarks(data.remarks || []);
}

function renderRemarks(remarks) {
  terminalLog.innerHTML = "";

  for (const entry of remarks) {
    const div = document.createElement("div");
    const time = new Date(entry.timestamp).toLocaleTimeString();
    div.textContent = `> ${time} // ${entry.message}`;
    terminalLog.appendChild(div);
  }
}

async function poll() {
  try {
    const res = await fetch("/api/status");
    const json = await res.json();
    if (!json.ok) return;

    applyStatus(json.data);
  } catch (err) {
    terminalLog.innerHTML = "<div>> Telemetry request failed.</div>";
  }
}

poll();
setInterval(poll, 1000);