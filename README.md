 Smart-X IoT Mesh Ecosystem

 Overview

Smart-X is an ASP.NET Core Web API designed to manage telemetry from a large number of simulated IoT sensors. The system is aimed at use cases such as hydroponic farming, smart property monitoring and smart grid installations.

Features

* Sensor registration and validation
* Telemetry packet processing
* Mock sensor telemetry
* Sensor status and offline detection
* Anomaly detection
* Historical telemetry
* Telemetry dashboard with filtering
* Deployment validation
* Sensor file attachments


Technologies

* C#
* ASP.NET Core Web API
* .NET
* HTML, CSS and JavaScript
* Docker
* Git and GitHub

Project Structure

Controller  contains the API endpoints.

Models contains the sensor, telemetry and deployment models.

Services contains the validation and telemetry processing logic.

`Views` and `wwwroot` contain the dashboard interface.

Running the Project

Open the solution in Visual Studio, build the project and run it using the HTTPS profile. The API can be tested through Swagger, while the dashboard provides a visual view of the sensor and telemetry data.

 Purpose

The project demonstrates how an IoT gateway can receive, validate and monitor telemetry from many simulated devices while identifying abnormal or offline sensors.


## Part 2: Real-time command stream

Open `/command-stream` (menu: **Command stream**). Devices send telemetry to `POST /api/stream/ingest`.
Packets are queued, processed by a background worker, and shown live on the dashboard, which refreshes every 1.5 seconds.
The menu bar links all three modules: Sensor telemetry (Part 1), Command stream (Part 2) and Network topology (a placeholder for the final PoE).

### Where each data structure lives

| Structure | File | Purpose |
|---|---|---|
| `Queue<StreamPacket>` | `Part 2/IngestionPipeline.cs` | FIFO lane for normal telemetry |
| `PriorityQueue<StreamPacket,(int,long)>` | `Part 2/IngestionPipeline.cs` | Critical alerts. A power spike outranks other alerts, and the critical lane is always drained first |
| `Stack<DeviceCommand>` | `Part 2/CommandHistory.cs` | Manual override history with Undo |
| `Dictionary<string,StreamDevice>` | `Part 2/DeviceRegistry.cs` | O(1) device lookup by MAC address |
| `SortedDictionary<DateTime,double>` | `Part 2/DeviceRegistry.cs` | Per-device history in timestamp order for the timeline chart |
| `HashSet<string>` | `Part 2/DeviceRegistry.cs` | Unique active alerts and disconnected nodes, so duplicates are ignored |
| Transition dictionary | `Part 2/PredictionEngine.cs` | Learns "event A is usually followed by event B" for Suggested Actions |

Critical thresholds are in `Part 2/Models.cs` (`AlertRules`): moisture below 20, power above 3000, temperature above 45.
A device that sends nothing for 20 seconds is flagged as disconnected.

### How to test

There are two ways to simulate devices. Both send real HTTP requests to `POST /api/stream/ingest`, exactly as a device would.

- **In the page:** the *Device simulator* panel on `/command-stream`.
- **Command line (PowerShell 7):** `./SmartX.API/simulator/simulate.ps1 -Base https://localhost:44366 -Mode normal`.
  Modes: `normal`, `drop`, `spike`, `burst`, `offline`, `duplicate`.

1. **Live devices (Dictionary):** click *Start devices*. Four devices appear and update by MAC address.
2. **High-priority alert:** click *Power spike* or *Moisture drop*. The packet response says `lane: critical`, the device row turns red, and the alert appears under *Active alerts*.
   Raw request:
```
   curl -k -X POST https://localhost:44366/api/stream/ingest -H "Content-Type: application/json" -d "{\"mac\":\"24:6F:28:AA:00:03\",\"name\":\"Smart Relay 1\",\"category\":\"Power\",\"deviceType\":\"actuator\",\"metric\":\"power\",\"value\":4200}"
```
3. **Priority queue bypasses FIFO:** click *Priority queue demo*. It pauses the worker, queues 20 standard packets and then 1 power spike, and releases them after 4 seconds.
   In *Processing order* the CRITICAL packet has the lowest number even though it was sent last.
4. **HashSet de-duplication:** click *Duplicate alerts*. Ten identical critical packets raise one alert, and *duplicate alerts blocked* rises by 9.
   Click *Stop devices* and wait about 25 seconds: each device gets exactly one "Disconnected" alert, although the sweep runs every 5 seconds.
5. **Stack and Undo:** click **On** then **Off** on Pump A, then **Undo last**. Pump A returns to ON. Each Undo pops one command, newest first.
6. **SortedDictionary timeline:** click a device row. Its readings are drawn in timestamp order.
7. **Search and filter:** search for "pump", filter by category, or filter by *Alerting* or *Disconnected*.
8. **Predictions:** click *Moisture drop*. A rule-based suggestion ("Turn on Pump A") appears immediately.
   To train the pattern, repeat 3 times: moisture drop, type `pump a` in the search box and press Enter, click **On** on Pump A, then *Back to normal*.
   On the next moisture drop the panel suggests *Search 'pump a'* and then *ON Pump A*, with confidence taken from the observed counts.

### API summary

`POST /api/stream/ingest` · `GET /api/stream/devices?category=&alertState=&search=` · `GET /api/stream/devices/{mac}/history`
· `GET /api/stream/alerts` · `GET /api/stream/queues` · `GET /api/stream/insights`
· `GET|POST /api/stream/commands` · `POST /api/stream/commands/undo` · `POST /api/stream/worker/pause` and `/resume`
