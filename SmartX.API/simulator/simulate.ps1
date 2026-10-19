<#
 Smart-X device simulator. Every reading is POSTed to the API exactly like a real ESP32 would.
 Usage (PowerShell 7+), pass your https address with -Base:
   ./simulate.ps1 -Base https://localhost:7000 -Mode normal
   ./simulate.ps1 -Base https://localhost:7000 -Mode drop       # moisture crashes to 12%   -> critical alert
   ./simulate.ps1 -Base https://localhost:7000 -Mode spike      # smart relay power spike   -> critical alert
   ./simulate.ps1 -Base https://localhost:7000 -Mode burst      # 40 standard packets then 1 critical (pause worker first)
   ./simulate.ps1 -Base https://localhost:7000 -Mode offline    # one heartbeat per device, then silence -> disconnect alerts
   ./simulate.ps1 -Base https://localhost:7000 -Mode duplicate  # 10 identical critical packets -> only 1 alert
#>
param(
  [string]$Base = "https://localhost:7000",
  [ValidateSet("normal","drop","spike","burst","offline","duplicate")][string]$Mode = "normal",
  [int]$Count = 20
)
$devices = @(
  @{ mac="24:6F:28:AA:00:01"; name="Moisture Sensor B"; category="Hydroponics"; type="sensor";   metric="moisture";    value=55 },
  @{ mac="24:6F:28:AA:00:02"; name="Pump A";            category="Hydroponics"; type="actuator"; metric="flow";        value=3  },
  @{ mac="24:6F:28:AA:00:03"; name="Smart Relay 1";     category="Power";       type="actuator"; metric="power";       value=800 },
  @{ mac="24:6F:28:AA:00:04"; name="Greenhouse Temp";   category="Environment"; type="sensor";   metric="temperature"; value=24 }
)
function Send($d, $value) {
  $body = @{ mac=$d.mac; name=$d.name; category=$d.category; deviceType=$d.type; metric=$d.metric; value=$value; timestamp=(Get-Date).ToUniversalTime().ToString("o") } | ConvertTo-Json
  $r = Invoke-RestMethod -Method Post -Uri "$Base/api/stream/ingest" -ContentType "application/json" -Body $body -SkipCertificateCheck
  Write-Host ("{0,-18} {1,-12} {2,6}  -> {3}" -f $d.name, $d.metric, $value, $r.lane)
}
$moist = $devices[0]; $relay = $devices[2]
switch ($Mode) {
  "normal"    { 1..$Count | % { foreach ($d in $devices) { Send $d ($d.value + (Get-Random -Min -3 -Max 4)) }; Start-Sleep -Milliseconds 700 } }
  "drop"      { foreach ($d in $devices) { Send $d $d.value }; Send $moist 12 }
  "spike"     { foreach ($d in $devices) { Send $d $d.value }; Send $relay 4200 }
  "burst"     { 1..40 | % { Send $devices[3] (20 + $_ % 5) }; Send $relay 4200 }
  "offline"   { foreach ($d in $devices) { Send $d $d.value }; Write-Host "Now wait ~25 s: the dashboard will flag every device as disconnected." }
  "duplicate" { Send $moist 55; 1..10 | % { Send $moist 12 } }
}