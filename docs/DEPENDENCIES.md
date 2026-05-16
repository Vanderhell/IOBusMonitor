# Dependencies

This table records package usage as found in project files and `packages.config` on 2026-05-15. Decisions are fact-based and conservative because the required Windows restore/build verification could not be completed in the current WSL environment.

| Package | Purpose | Project | Keep/Remove Decision |
|---|---|---|---|
| `EasyModbusTCP` | Modbus TCP and Modbus RTU protocol access via `EasyModbus.dll` | `IOBusMonitor`, `IOBusMonitorLib` | Keep. Referenced in both `.csproj` files and used by Modbus reader services. |
| `FontAwesome.WPF` | WPF icon support | `IOBusMonitor` | Keep. Referenced by `IOBusMonitor.csproj`. Runtime/UI usage not fully re-verified in build. |
| `HandyControl` | WPF controls, theme resources, and growl notifications | `IOBusMonitor`, `IOBusMonitorLib` | Keep. Referenced by both `.csproj` files; used in `App.xaml`, `MainWindow.xaml.cs`, and comments indicate UI integration. |
| `HandyControl.Lang.en` | HandyControl English language resources | `IOBusMonitor` | Keep for now. Present in `packages.config`; not directly proven unused without a verified build/runtime check. |
| `HandyControls` | Additional UI package with similar naming to `HandyControl` | `IOBusMonitor` | Keep for now. Present in `packages.config`, but no direct `.csproj` reference was verified. Not removed because task only allows removal if build proves it is unused. |
| `Microsoft.CodeAnalysis.Analyzers` | Roslyn analyzers | `IOBusMonitor` | Keep. Analyzer entries exist in `IOBusMonitor.csproj`. |
| `Microsoft.CodeAnalysis.Common` | Roslyn core assemblies | `IOBusMonitor` | Keep. Referenced by `IOBusMonitor.csproj`. |
| `Microsoft.CodeAnalysis.CSharp` | Roslyn C# compiler services | `IOBusMonitor` | Keep. Referenced by `IOBusMonitor.csproj`. |
| `Microsoft.CodeAnalysis.CSharp.Scripting` | Roslyn C# scripting support | `IOBusMonitor` | Keep. Referenced by `IOBusMonitor.csproj`. |
| `Microsoft.CodeAnalysis.Scripting.Common` | Roslyn scripting abstractions | `IOBusMonitor` | Keep. Referenced by `IOBusMonitor.csproj`. |
| `Microsoft.CSharp` | Framework support assembly | `IOBusMonitor` | Keep. Present in `packages.config`; direct need was not revalidated independently. |
| `Microsoft.NETFramework.ReferenceAssemblies` | Build-time framework reference assemblies | `IOBusMonitor` | Keep. Development dependency for Windows build tooling. |
| `Microsoft.NETFramework.ReferenceAssemblies.net472` | Build-time `.NET Framework 4.7.2` references | `IOBusMonitor` | Keep. Matches current target framework metadata. |
| `Newtonsoft.Json` | JSON serialization/deserialization | `IOBusMonitor`, `IOBusMonitorLib` | Keep. Referenced by both `.csproj` files. |
| `OxyPlot.Core` | Charting core library | `IOBusMonitor` | Keep. Referenced by `IOBusMonitor.csproj` and used in graph/history view models. |
| `OxyPlot.Wpf` | WPF chart rendering | `IOBusMonitor` | Keep. Referenced by `IOBusMonitor.csproj` and used in graph/history view models. |
| `OxyPlot.Wpf.Shared` | Shared OxyPlot WPF support assembly | `IOBusMonitor` | Keep for now. Referenced by `IOBusMonitor.csproj`; not removed without build proof. |
| `S7netplus` | Siemens S7 PLC communication via `S7.Net.dll` | `IOBusMonitor`, `IOBusMonitorLib` | Keep. Referenced by both `.csproj` files and used by Siemens read service. |
| `Stub.System.Data.SQLite.Core.NetFramework` | SQLite .NET Framework targets/imports | `IOBusMonitor`, `IOBusMonitorLib` | Keep. Both `.csproj` files import targets from this package and fail build if it is missing. |
| `System.Buffers` | Support dependency for newer libraries | `IOBusMonitor`, `IOBusMonitorLib` | Keep. Referenced by both `.csproj` files. |
| `System.Collections.Immutable` | Support dependency for Roslyn assemblies | `IOBusMonitor` | Keep. Referenced by `IOBusMonitor.csproj`. |
| `System.Data.SQLite.Core` | SQLite runtime/provider package | `IOBusMonitor`, `IOBusMonitorLib` | Keep. SQLite is used throughout storage, settings, and history loading code. |
| `System.Linq.Dynamic.Core` | Dynamic expression evaluation | `IOBusMonitor`, `IOBusMonitorLib` | Keep. Used in `ConditionEvaluator` and `SimensReadService`. |
| `System.Linq.Expressions` | Expression support dependency | `IOBusMonitor` | Keep. Referenced by `IOBusMonitor.csproj`. |
| `System.Memory` | Support dependency for newer libraries | `IOBusMonitor`, `IOBusMonitorLib` | Keep. Referenced by both `.csproj` files. |
| `System.Numerics.Vectors` | Numeric/vector dependency | `IOBusMonitor`, `IOBusMonitorLib` | Keep. Referenced by both `.csproj` files. |
| `System.Reflection.Metadata` | Roslyn metadata dependency | `IOBusMonitor` | Keep. Referenced by `IOBusMonitor.csproj`. |
| `System.Runtime.CompilerServices.Unsafe` | Support dependency for newer libraries | `IOBusMonitor`, `IOBusMonitorLib` | Keep. Referenced by both `.csproj` files. |
| `System.Text.Encoding.CodePages` | Text encoding provider dependency | `IOBusMonitor` | Keep. Referenced by `IOBusMonitor.csproj`. |
| `System.Threading.Tasks.Extensions` | Task support dependency | `IOBusMonitor` | Keep. Referenced by `IOBusMonitor.csproj`. |

## Notes

- `IOBusMonitor` and `IOBusMonitorLib` both target `.NET Framework 4.7.2` in their project files.
- `ShortcutTool` targets `.NET Framework 4.8` but does not contribute to the README contradiction about the WPF application framework.
- No package was removed in this task because the required Windows build verification was not available in the current environment.
