// All calls are relative 

async function registerSensor() {
    const sensor = {
        deviceId: document.getElementById('deviceId').value,
        location: document.getElementById('location').value,
        category: document.getElementById('category').value
    };

    const res = await fetch('/api/sensors', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(sensor)
    });

    document.getElementById('registerStatus').textContent = res.ok
        ? `Registered ${sensor.deviceId} at ${sensor.location}.`
        : 'Failed to register sensor.';
}

async function uploadAttachment() {
    const deviceId = document.getElementById('attachDeviceId').value;
    const fileInput = document.getElementById('attachFile');
    const status = document.getElementById('uploadStatus');

    if (!deviceId) { status.textContent = 'Enter a Device ID first.'; return; }
    if (!fileInput.files.length) { status.textContent = 'Choose a file first.'; return; }

    const formData = new FormData();
    formData.append('file', fileInput.files[0]);

    const res = await fetch(`/api/sensors/${encodeURIComponent(deviceId)}/attachments`, {
        method: 'POST',
        body: formData
    });

    status.textContent = res.ok
        ? `Uploaded ${fileInput.files[0].name} for ${deviceId}.`
        : 'Upload failed.';
}

function startFeedPolling() {
    refreshFeed();
    setInterval(refreshFeed, 4000);
}

async function refreshFeed() {
    try {
        const res = await fetch('/api/telemetry/feed');
        if (!res.ok) return;
        const feed = await res.json();

        const list = document.getElementById('feedList');
        list.innerHTML = '';

        let anomalyCount = 0;
        feed.forEach(item => {
            if (item.isAnomaly) anomalyCount++;
            const li = document.createElement('li');
            li.className = 'feed-item' + (item.isAnomaly ? ' anomaly' : '');
            li.innerHTML = `
                <span>${item.sensorId}</span>
                <span>${item.value.toFixed(1)}</span>
                <span>${item.isAnomaly ? 'anomaly' : 'ok'}</span>
                <span>${new Date(item.timestamp).toLocaleTimeString()}</span>
            `;
            list.appendChild(li);
        });

        const score = feed.length === 0 ? 100 : Math.round(100 - (100 * anomalyCount / feed.length));
        const scoreEl = document.getElementById('healthScore');
        scoreEl.textContent = `${score}%`;
        scoreEl.className = 'health-score ' + (score >= 80 ? 'good' : score >= 50 ? 'warn' : 'bad');
    } catch {
        // API might still be starting up — ignore and retry next tick.
    }
}

async function validateSamplePath() {
    const tree = {
        name: 'Facility A',
        children: [
            {
                name: 'Zone 1',
                children: [
                    { name: 'Sub-Zone B', children: [] },
                    { name: 'Sub-Zone A', children: [] }
                ]
            },
            { name: 'Zone 2', children: [] }
        ]
    };

    const request = {
        root: tree,
        pathSegments: ['Facility A', 'Zone 1', 'Sub-Zone B']
    };

    const res = await fetch('/api/sensors/validate-path', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request)
    });

    const result = await res.json();
    document.getElementById('pathValidationResult').textContent =
        result.valid ? ' Valid path' : ' Invalid path';
}

async function aggregateMeters() {
    const request = {
        a: { meterId: 'Meter1', wattageLoad: 120 },
        b: { meterId: 'Meter2', wattageLoad: 95 }
    };

    const res = await fetch('/api/meters/aggregate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request)
    });

    if (res.ok) {
        const combined = await res.json();
        document.getElementById('aggregateResult').textContent =
            `${combined.meterId}: ${combined.wattageLoad.toFixed(1)} W`;
    }
}