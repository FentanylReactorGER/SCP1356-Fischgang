const express = require('express');
const fs = require('fs');
const path = require('path');

const app = express();
const PORT = 3001;

const dataPaths = {
  lcz: path.join(__dirname, 'map-lcz.json'),
  surface: path.join(__dirname, 'map-surface.json'), // HCZ + EZ together
};

app.use(express.json({ limit: '2mb' }));

function writeSnapshot(filePath, body) {
  fs.writeFileSync(filePath, JSON.stringify(body, null, 2), 'utf8');
}

function normalizeRoom(room) {
  return {
    name: room.name ?? room.Name ?? 'Unnamed',
    type: room.type ?? room.Type ?? 'Unknown',
    label: room.label ?? room.Label ?? 'Unknown',
    zone: room.zone ?? room.Zone ?? 'Unknown',
    worldX: Number(room.worldX ?? room.WorldX ?? 0),
    worldY: Number(room.worldY ?? room.WorldY ?? 0),
    worldZ: Number(room.worldZ ?? room.WorldZ ?? 0),
    mapX: Number(room.mapX ?? room.MapX ?? 0),
    mapY: Number(room.mapY ?? room.MapY ?? 0),
  };
}

function normalizeDuck(duck, fallbackZone) {
  return {
    name: duck.name ?? duck.Name ?? 'Duck',
    image: duck.image ?? duck.Image ?? 'duck.png',
    zone: duck.zone ?? duck.Zone ?? fallbackZone,
    worldX: Number(duck.worldX ?? duck.WorldX ?? 0),
    worldY: Number(duck.worldY ?? duck.WorldY ?? 0),
    worldZ: Number(duck.worldZ ?? duck.WorldZ ?? 0),
    mapX: Number(duck.mapX ?? duck.MapX ?? 0),
    mapY: Number(duck.mapY ?? duck.MapY ?? 0),
    speed: Number(duck.speed ?? duck.Speed ?? 1),
  };
}

function readSnapshot(filePath, fallbackZone) {
  if (!fs.existsSync(filePath)) {
    return {
      generatedAt: null,
      zone: fallbackZone,
      rooms: [],
      ducks: [],
    };
  }

  const raw = fs.readFileSync(filePath, 'utf8');
  const data = JSON.parse(raw);

  const rooms = Array.isArray(data.rooms)
    ? data.rooms
    : Array.isArray(data.Rooms)
      ? data.Rooms
      : [];

  const ducks = Array.isArray(data.ducks)
    ? data.ducks
    : Array.isArray(data.Ducks)
      ? data.Ducks
      : [];

  return {
    generatedAt: data.generatedAt ?? data.GeneratedAt ?? null,
    zone: data.zone ?? data.Zone ?? fallbackZone,
    rooms: rooms.map(normalizeRoom),
    ducks: ducks.map((duck) => normalizeDuck(duck, fallbackZone)),
  };
}

app.post('/api/map/lcz', (req, res) => {
  try {
    writeSnapshot(dataPaths.lcz, req.body);
    console.log(
      `[POST] /api/map/lcz rooms=${req.body?.Rooms?.length ?? req.body?.rooms?.length ?? 0} ducks=${req.body?.Ducks?.length ?? req.body?.ducks?.length ?? 0}`
    );
    res.status(200).json({ ok: true });
  } catch (err) {
    console.error(err);
    res.status(500).json({ ok: false, error: 'write_failed' });
  }
});

app.get('/api/map/lcz', (req, res) => {
  try {
    res.status(200).json(readSnapshot(dataPaths.lcz, 'LightContainment'));
  } catch (err) {
    console.error(err);
    res.status(500).json({ ok: false, error: 'read_failed' });
  }
});

app.post('/api/map/surface', (req, res) => {
  try {
    writeSnapshot(dataPaths.surface, req.body);
    console.log(
      `[POST] /api/map/surface rooms=${req.body?.Rooms?.length ?? req.body?.rooms?.length ?? 0} ducks=${req.body?.Ducks?.length ?? req.body?.ducks?.length ?? 0}`
    );
    res.status(200).json({ ok: true });
  } catch (err) {
    console.error(err);
    res.status(500).json({ ok: false, error: 'write_failed' });
  }
});

app.get('/api/map/surface', (req, res) => {
  try {
    res.status(200).json(readSnapshot(dataPaths.surface, 'HeavyContainment+Entrance'));
  } catch (err) {
    console.error(err);
    res.status(500).json({ ok: false, error: 'read_failed' });
  }
});

app.use(express.static(path.join(__dirname, 'public')));

app.listen(PORT, '0.0.0.0', () => {
  console.log(`Map site running on http://0.0.0.0:${PORT}`);
});