# IOBusMonitor

IOBusMonitor is a small Windows desktop utility for **querying, logging, and visualising field‑bus data** from Modbus TCP / Modbus RTU devices and Siemens S7 PLCs.  
The application is designed to be compiled and run as a single executable without external services or complex deployment steps.

---

# Home

![Live view](Assets/README/home.png)

# Live Dashboard

![Live view](Assets/README/dashboard.png)

# Dashboard - Graph

![Live view](Assets/README/graph.png)


## 1  Overview

| Topic            | Detail |
|------------------|--------|
| **Target OS**    | Windows 10/11 (x64) |
| **UI Framework** | WPF (.NET Framework 4.8, C# 7.3) |
| **Storage**      | Local SQLite databases (one file per day) |
| **Protocols**    | Modbus TCP, Modbus RTU, Siemens S7 (S7‑200/300/400/1200/1500, LOGO 0BA8) |
| **License**      | MIT |

The executable maintains two working folders next to itself:

```
Settings/Settings.db   # devices, points, application options
Data/Data_YYYYMMDD.db  # daily measurement archive (SQLite)
```

No external installer is required—copy the folder to any workstation, double‑click **IOBusMonitor.exe**, and begin polling.

---

## 2  Features

* **Live dashboard** – realtime list of the most recent values per point.
* **History view** – ad‑hoc line charts from the daily SQLite archives.
* **In‑app administration** – create/edit devices, points, and measurements without editing JSON or XML.
* **Self‑contained data** – all numeric samples are written to SQLite; you can analyse them later with Python, Excel, etc.
* **Protocol plug‑ins** – new buses can be supported by adding one C# reader class that returns a `PointViewModel`.

---

## 3  Getting Started

1. **Clone or download** the repository and open *IOBusMonitor.sln* in Visual Studio 2019 (or newer).
2. Press **F5** to build and launch in Debug mode.
3. On first run the program creates the default `Settings/Settings.db` and an empty `Data` folder.
4. Use the **Administration** menu to add a device → point → measurement chain.
5. Click the green **Start** button (▶︎) to begin polling.

> **Tip** – If you do not have hardware available, call
> `TestDataGenerator.GenerateTestData()` once (e.g. from the Immediate Window).
> The method writes a dummy `Data_YYYYMMDD.db` with random values so you can
> explore the UI.

---

## 4  Project Structure

```
IOBusMonitor.sln         # solution file
├─ IOBusMonitor          # WPF views, windows, XAML pages
├─ IOBusMonitorLib       # protocol readers, view‑models, storage, helpers
└─ ShortcutTool          # cmd helper
```

### Runtime layout

```
IOBusMonitor.exe
Settings/Settings.db      # persistent configuration & definitions
Data/                     # rolling SQLite archives (one per day)
Logs/                     # optional diagnostic output
```

---

## 5  Extending the Application

1. **Define models** (if required) for the new protocol’s devices/points.
2. **Implement a reader** with a single public method that returns `PointViewModel`:
   ```csharp
   public Task<PointViewModel> LoadPointDataAsync(MyPoint point) { /* … */ }
   ```
3. **Register the reader** in `TimerService` so it is called during the scan loop.
4. Duplicate the CRUD XAML pages for devices/points/measurements and adjust the SQL queries.

All other layers—storage, live dashboard, history charts—are protocol‑agnostic and work automatically once you populate `PointViewModel` instances.

---

## 6  Building a Release

```powershell
# from repository root
msbuild IOBusMonitor.sln -p:Configuration=Release
```

Copy the resulting `IOBusMonitor\bin\Release` folder to the target machine. No installer or registry keys are required.

---

## 7  Contributing

Pull requests are welcome.  
Before submitting, please ensure:

* The solution builds with **C# 7.3 / .NET Framework 4.8**.
* New code uses the existing logging (`LogService`) instead of `Console.WriteLine`.
* UI strings are written in English; localisation can be discussed separately.

Areas that could use help:

* Additional protocol readers (OPC UA, BACnet, MQTT ⟶ SQL bridge).
* UX refinements, dark theme, high‑DPI tweaks.
* Performance profiling when polling thousands of points.

---

## 8  License

This project is licensed under the MIT License – see **LICENSE** for details.

