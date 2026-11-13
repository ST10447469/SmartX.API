# Smart-X IoT Mesh Gateway POE

Smart-X is an ASP.NET Core application that models the software side of an IoT mesh gateway. Simulated ESP32 sensors, smart plugs and actuators send telemetry to a Web API. The API processes it in real time, and a web dashboard lets an operator watch the network, send manual overrides, and inspect how data travels through the mesh.

The suite has three modules, all reachable from one menu bar:

| Module | Page | What it does |
|---|---|---|
| 1. Sensor telemetry | `/Home/Dashboard` | Register sensors, attach device logs, live telemetry feed with a gateway health score, deployment path validation, meter aggregation |
| 2. Command stream | `/command-stream` | Real-time queues, critical alerts, manual overrides with Undo, device search and filters, suggested actions, built-in device simulator |
| 3. Network topology | `/topology` | Mesh graph, route tracking by MAC address, failure simulation and rerouting, minimum spanning tree, historical telemetry range queries |

Swagger is available at `/swagger` in the Development environment and lists every endpoint.

---


### Using the application

### Module 1: Sensor telemetry (`/Home/Dashboard`)
- **Register a sensor** with a MAC or device ID, a zone, and a category.
- **Attach a device log or config file** to a registered device.
- **Live telemetry feed** shows readings as they arrive, with anomalies highlighted, and a gateway health score.
- **Validate deployment path** resolves a zone path against a sample tree using recursion.
- **Aggregate two smart meters** combines two readings with an overloaded `+` operator.

The Part 1 API endpoints are listed in Swagger at `/swagger`.

### Module 2: Command stream (`/command-stream`)
1. Click **Start devices** in the *Device simulator* panel. Four devices appear and keep sending a reading every 5 seconds.
2. Click **Moisture drop**, **Power spike** or **Overheat** to send a critical reading. The device row pulses red and the alert appears under *Active alerts*.
3. Click **On** or **Off** next to Pump A or Smart Relay 1 to send a manual override. **Undo last** reverses the most recent one.
4. Use the search box and the category and state dropdowns to filter devices.
5. Click a device row to draw its timeline.
6. *Suggested actions and insights* shows rule-based hints immediately and learned patterns after you repeat a routine.

### Module 3: Network topology (`/topology`)
1. The graph shows the nine-node demonstration mesh. Thick teal links are the **minimum spanning tree**. The number on each link is its power cost.
2. Click a node, or type its MAC address and click **Track**. The page shows its route to the gateway two ways: fewest hops (BFS) and lowest power (MST).
3. Click **Simulate failure** to take the selected node down. Links to it turn dashed red, and *Failures and rerouting* lists each affected node with its old path and its new hop path, or reports it as **cut off**. Click **Restore node** to bring it back, or **Reset mesh** to restore everything.
4. *Connected to the gateway (BFS)* lists every device that can still reach the gateway and how many hops it takes.
5. *Telemetry history (AVL tree)* queries the selected device by time range and metric, and shows the tree size, its height, and how many nodes the query visited.
   Devices only have history after they have sent telemetry, so run the Module 2 simulator first.

## Project structure

```
SmartX.API.slnx
SmartX.API/                     the web project
  Program.cs                    service registration and middleware
  Controllers/                  API controllers (Part 1, StreamController, MeshController, HistoryController)
                                and StreamPagesController, which serves the Part 2 and Part 3 pages
  Services/                     Part 1: TelemetryStore, DeploymentValidator
  Part 2/                       DeviceRegistry, IngestionPipeline, CommandHistory, PredictionEngine, StreamWorker
  Part 3/                       AvlTree, TelemetryHistory, MeshGraph, MeshModels
  Views/                        Razor views and the shared _Nav menu partial
  wwwroot/css/site.css          the one shared stylesheet
  simulator/simulate.ps1        command-line device simulator
```



## 5. Data structures and algorithms

### Part 2

| Structure | File | Purpose |
|---|---|---|
| `Queue<StreamPacket>` | `Part 2/IngestionPipeline.cs` | FIFO lane for normal telemetry |
| `PriorityQueue<StreamPacket,(int,long)>` | `Part 2/IngestionPipeline.cs` | Critical lane, always drained first. The priority is (severity, sequence number) so equal alerts stay in arrival order |
| `Stack<DeviceCommand>` | `Part 2/CommandHistory.cs` | Manual override history with Undo |
| `Dictionary<string,StreamDevice>` | `Part 2/DeviceRegistry.cs` | O(1) device lookup by MAC address |
| `SortedDictionary<DateTime,double>` | `Part 2/DeviceRegistry.cs` | Per-device history in time order, for the timeline chart |
| `HashSet<string>` | `Part 2/DeviceRegistry.cs` | Unique active alerts and disconnected nodes, so a repeat is ignored |
| Transition dictionary | `Part 2/PredictionEngine.cs` | Learns "event A is usually followed by event B" for Suggested Actions |

Critical thresholds (`Part 2/Models.cs`): moisture below 20, power above 3000, temperature above 45. A device silent for 20 seconds is flagged as disconnected.

### Part 3: the network topology feature

#### AVL tree: historical telemetry (`Part 3/AvlTree.cs`, `Part 3/TelemetryHistory.cs`)

**Role.** Each device has its own AVL tree keyed by timestamp. It answers a question such as "all moisture readings for this node between 14:00 and 16:00" without scanning the full history.

**How it works.** An AVL tree is a binary search tree that rebalances itself with rotations so the two subtrees of every node differ in height by at most one. The height therefore stays close to log2(n), even when readings arrive in perfect time order (which would turn a plain binary search tree into a linked list). Insert and lookup are O(log n).

**Key design.** The key is a `HistoryKey` (timestamp plus a sequence number), so two readings with the same timestamp never overwrite each other. Each tree is capped at 2,000 readings: when it is full, `RemoveMin` drops the oldest, so memory use is bounded.

**Range query.** It descends only into subtrees that can contain keys inside the interval, so it touches about the tree height plus the matching readings:

**Example.** `GET /api/history/24:6F:28:AA:00:01?from=2026-11-13T12:00:00Z&to=2026-11-13T14:00:00Z&metric=moisture` returns the readings in that range plus `treeSize`, `treeHeight`, `nodesVisited` and `matched`. The topology page shows the same numbers.

#### Graph: the mesh model (`Part 3/MeshGraph.cs`)

**Role.** Vertices are the ESP32 nodes and edges are wireless links. The graph is stored as adjacency lists (a dictionary from each node to its neighbours and link costs), which suits a mesh because each node links to only a few others. Memory is O(V + E). A link's weight is a power cost.

The demonstration mesh has 9 nodes (the gateway, two repeaters and six devices) and 16 links. Each node has a `Down` flag so a hardware or power failure can be simulated.

#### Breadth-first search: connected, failed and rerouted nodes

**Role.** BFS starts at the gateway and explores one hop at a time using a queue, so it finds a **fewest-hops** path to every node. It never enters a node that is down, so the nodes it cannot reach are exactly the ones that have lost their route. It runs in O(V + E).

**Where to hop when a node is down.** The reroute report runs BFS twice, once over the healthy network and once over the current one. A node whose healthy path contained the failed node is affected, and its new path comes from the second search. If the second search cannot reach it, it is reported as cut off.

#### Prim's algorithm: minimum spanning tree

**Role.** The minimum spanning tree (MST) is the cheapest set of links that connects every live node to the gateway with no cycles. It shows the most power-efficient way for telemetry to hop through the mesh. Prim's algorithm grows the tree from the gateway. At each step it takes the cheapest link to a node not yet in the tree, using a priority queue so the cheapest candidate is always first. It runs in O(E log V).

```csharp
var pq = new PriorityQueue<(string From, string To, double Cost), double>();
foreach (var (to, cost) in _adj[GatewayId])
    if (!_nodes[to].Down) pq.Enqueue((GatewayId, to, cost), cost);

while (pq.TryDequeue(out var e, out _))
{
    if (inTree.Contains(e.To)) continue;                         // would form a cycle
    inTree.Add(e.To); parent[e.To] = e.From;
    edges.Add(new MeshLink(e.From, e.To, e.Cost)); total += e.Cost;
    foreach (var (next, nextCost) in _adj[e.To])
        if (!inTree.Contains(next) && !_nodes[next].Down)
            pq.Enqueue((e.To, next, nextCost), nextCost);
}
```

For the demonstration mesh the MST has 8 links and a total cost of **22**. It is recomputed whenever a node fails or is restored.

**BFS and the MST answer different questions.** For the Tank Level Sensor, BFS gives the direct link to the gateway (1 hop, cost 4). The MST route goes through Repeater West (2 hops, cost 5). BFS minimises hops, which suits latency. The MST minimises the total cost of the whole network, which suits power use. The topology page shows both.

| Operation | Structure | Cost |
|---|---|---|
| Store a reading | AVL insert | O(log n) |
| Readings between two times | AVL range query | O(log n + k) |
| Find a device by MAC | Dictionary | O(1) average |
| Connected, down and cut-off nodes | BFS | O(V + E) |
| Minimum spanning tree | Prim with a priority queue | O(E log V) |

---

##  How to test

 There are two ways to simulate devices, and both send real HTTP requests to `POST /api/stream/ingest`:

- **In the page:** the *Device simulator* panel on `/command-stream`.
- **Command line (PowerShell 7):** `./SmartX.API/simulator/simulate.ps1 -Base https://localhost:44366 -Mode normal`. Modes: `normal`, `drop`, `spike`, `burst`, `offline`, `duplicate`.

| # | Do this | Expect |
|---|---|---|
| 1 | Command stream: **Start devices** | Four devices appear and stay online |
| 2 | **Moisture drop** or **Power spike** | The response says `lane: critical`, the row turns red, the alert is listed |
| 3 | **Priority queue demo** | The CRITICAL packet has the lowest number in *Processing order*, although it was sent last |
| 4 | **Duplicate alerts** | One alert is raised and *duplicate alerts blocked* rises by 9 |
| 5 | **Stop devices**, wait about 25 seconds | One "Disconnected" alert per device |
| 6 | **On**, **Off**, then **Undo last** on Pump A | Pump A returns to ON |
| 7 | Type `pump` in the search box | Only Pump A is listed |
| 8 | Topology: click **Tank Level Sensor** | BFS route 1 hop, cost 4; MST route cost 5 |
| 9 | Click **Repeater West**, then **Simulate failure** | Moisture Sensor B and Pump A are rerouted via the Tank Level and Light sensors |
| 10 | Also fail Tank Level Sensor and Light Sensor | Moisture Sensor B and Pump A are reported cut off |
| 11 | **Reset mesh** | All nodes up, MST cost 22 |
| 12 | Start devices, wait a minute, open `/topology`, select Moisture Sensor B, run a range query | Readings in the range, plus tree size, height and nodes visited |

Raw ingest request (a high-priority alert):

```bash
curl -k -X POST https://localhost:44366/api/stream/ingest -H "Content-Type: application/json" \
  -d '{"mac":"24:6F:28:AA:00:03","name":"Smart Relay 1","category":"Power","deviceType":"actuator","metric":"power","value":4200}'
```

##  API summary

**Part 2:** `POST /api/stream/ingest` · `GET /api/stream/devices?category=&alertState=&search=` · `GET /api/stream/devices/{mac}/history` · `GET /api/stream/alerts` · `GET /api/stream/queues` · `GET /api/stream/insights` · `GET|POST /api/stream/commands` · `POST /api/stream/commands/undo` · `POST /api/stream/worker/pause` and `/resume`

**Part 3:** `GET /api/mesh/topology` · `GET /api/mesh/reachable?from=` · `GET /api/mesh/route/{id}` · `POST /api/mesh/nodes/{id}/down` and `/up` · `POST /api/mesh/reset` · `GET /api/history` · `GET /api/history/{mac}?from=&to=&metric=&take=` · `GET /api/history/{mac}/latest?take=`



