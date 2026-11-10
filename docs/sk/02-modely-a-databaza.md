# 02 — Zariadenia, body, merania a databáza nastavení

V tejto lekcii rozoberieme údaje, ktoré používateľ pridá v aplikácii. Najskôr si zopakujme hierarchiu:

```text
Modbus TCP zariadenie
└── Modbus TCP bod
    └── TCP meranie

Modbus RTU zariadenie
└── Modbus RTU bod
    └── RTU meranie

Siemens S7 zariadenie
└── S7 bod
    └── S7 meranie
```

**Záznam** je jeden uložený riadok v databáze. **ID** je jedinečné číslo, ktorým program záznam rozpozná. Ak má bod hodnotu `ModbusTCPDeviceId = 4`, znamená to, že patrí k zariadeniu s ID 4.

## 1. Databáza `Settings/Settings.db`

Pri štarte ju pripraví `IOBusMonitor/App.xaml.cs`. Súbor je SQLite databáza, teda celá lokálna databáza v jednom súbore.

Aplikácia vytvára tieto skupiny tabuliek:

| Skupina | Tabuľky | Čo opisujú |
|---|---|---|
| Nastavenia | `AppSettings` | interval, autostart, cesta k archívu, demo režim |
| Modbus TCP | `ModbusTCPDevice`, `ModbusTCPPoint`, `TCPMeasurement` | sieťové zariadenie a jeho merania |
| Modbus RTU | `ModbusRTUDevice`, `ModbusRTUPoint`, `RTUMeasurement` | sériové zariadenie a jeho merania |
| Siemens S7 | `SimensDevice`, `SimensPoint`, `SimensMeasurement` | PLC, jeho skupiny a adresované hodnoty |

Tabuľka `AppSettings` zvyčajne obsahuje jediný riadok:

- `ReadIntervalMs` — čas medzi meracími cyklami v milisekundách.
- `AutoStart` — či sa monitoring spustí po otvorení programu.
- `PathData` — priečinok denných databáz meraní.
- `DemoModeEnabled` — či sa majú generovať umelé hodnoty namiesto čítania hardvéru.

**Milisekunda** je tisícina sekundy; `1000 ms = 1 s`.

## 2. `SettingsService.cs`: načítanie a uloženie nastavení

Táto služba vyhľadá `Settings.db` vedľa programu a podľa potreby vytvorí priečinok, databázu a tabuľku `AppSettings`.

- `LoadSettings()` načíta prvý riadok tabuľky a vytvorí objekt `AppSettings`.
- `SaveSettings(settings)` skontroluje, či záznam existuje; potom ho aktualizuje alebo vloží.
- `EnsureColumnExists()` skontroluje štruktúru tabuľky a pridá stĺpec, ak v staršej databáze chýba.
- `HasColumn()` zisťuje, či aktuálna odpoveď z databázy obsahuje určitý stĺpec.

**SQL** je jazyk príkazov pre databázy. `SELECT` číta údaje, `INSERT` vkladá nový záznam, `UPDATE` upravuje existujúci a `ALTER TABLE` mení štruktúru tabuľky.

Nastavenia, ktoré služba nevie prečítať, nahradí predvolenými hodnotami. Výnimky zapisuje cez `LogService`.

## 3. Triedy zariadení

Každá trieda zariadenia obsahuje jedinečné `Id`, názov a príznak `Active`.

- `DBModel/TCP/ModbusTCPDevice.cs`: `IPAddress` a `Port`.
- `DBModel/RTU/ModbusRTUDevice .cs`: sériový port, baud rate, parita a Slave ID.
- `DBModel/S7/SimensDevice.cs`: IP adresa, port, rack, slot a typ CPU.
- `DBModel/AppSettings.cs`: globálne nastavenia aplikácie.

Súbory sú v priečinku `IOBusMonitorLib/DBModel`. **Model** je trieda, ktorá drží údaje jednej veci. Napríklad `ModbusTCPDevice` je opis jedného sieťového zariadenia.

### Čo znamenajú sieťové a sériové údaje

- **IP adresa** označuje zariadenie v počítačovej sieti, napríklad `192.168.1.20`.
- **Port** je číslovaný komunikačný vstup na zariadení, napríklad Modbus TCP bežne používa 502.
- **COM port** označuje sériové rozhranie Windows, napríklad COM3.
- **Baud rate** vyjadruje rýchlosť sériového prenosu. Hodnota 9600 znamená približne 9600 prenosových symbolov za sekundu.
- **Parita** pridáva do sériového prenosu kontrolný bit: bez parity, párna alebo nepárna.
- **Slave ID** rozlišuje zariadenie na zdieľanej Modbus linke.
- **Rack** a **slot** určujú pozíciu CPU v konfigurácii S7.
- **CPU type** označuje rodinu riadiacej jednotky, napríklad S7-1200.

## 4. Triedy bodov a meraní

Bod má svoje `Id`, názov, ID rodičovského zariadenia a kolekciu meraní:

- `ModbusTCPPoint.cs`
- `ModbusRTUPoint.cs`
- `SimensPoint.cs`

Kolekcia je zoznam objektov. V triede bodu tak môže byť viacero meraní.

Meranie obsahuje spravidla:

- `Id` a ID nadradeného bodu,
- `Name` — čitateľný názov,
- `Unit` — jednotku,
- `Round` — počet desatinných miest,
- `Condition` — matematický výraz na úpravu hodnoty,
- `Active` — či sa má čítať,
- Modbus: `Register`, `Quantity`, `BitOrder`,
- Siemens: `Address`.

Súbory:

- `TCP/TCPMeasurement.cs`
- `RTU/RTUMeasurement.cs`
- `S7/SimensMeasurement.cs`

### Príklad merania

```text
Name      = Teplota kotla
Unit      = °C
Register  = 1
Quantity  = 2
Condition = value / 10
Round     = 1
```

Ak zariadenie vráti `253`, výraz `value / 10` urobí `253 ÷ 10 = 25.3`. `Round = 1` hovorí, že sa výsledok zobrazí na jedno desatinné miesto. Presné pravidlá výrazu sú v lekcii 05.

## 5. Atribúty v C# modeloch

Niektoré triedy majú zápisy ako `[Table("TCPMeasurement")]`, `[Key]`, `[StringLength(50)]` alebo `[NotMapped]`.

- **Atribút** je doplnková informácia zapísaná nad triedou alebo vlastnosťou.
- `Table` uvádza názov tabuľky.
- `Key` označuje primárny kľúč, teda hlavné ID riadka.
- `StringLength` vyjadruje zamýšľanú maximálnu dĺžku textu.
- `NotMapped` označuje pomocnú vlastnosť, ktorá nemá byť uložená ako stĺpec.

V týchto modeloch sú napríklad `ValidationError` a `LastTestResult` iba dočasné informácie pre obrazovku. Databázové príkazy vytvárajú tabuľky a čítajú ich cez SQLite priamo; samotný atribút v C# nezaručuje, že databázový stĺpec existuje.

V štartovacej SQL schéme nie sú deklarované explicitné obmedzenia `FOREIGN KEY`. Väzby zariadenie–bod–meranie sú uložené pomocou ID stĺpcov a spájajú sa v SQL dotazoch.

## 6. Číselníky: uzavreté sady možností

**Enum** (číselník) je typ, ktorý môže nadobúdať len vopred vymenované hodnoty. Namiesto voľného textu `"online"` program používa napríklad `PointStatus.Online`. Pomáha to predísť preklepom v kóde.

- `Enums/DeviceType.cs` — ModbusTCP, ModbusRTU alebo S7.
- `Enums/PointType.cs` — typ protokolu bodu.
- `Enums/PointStatus.cs` — Unknown, Connecting, Online, Timeout, ReadError, Disabled, Offline.
- `Enums/DataType.cs` — Bit, Int, Word, Real, Double, Byte alebo Unknown.
- `Enums/BaudRate.cs` — bežné rýchlosti sériovej linky.
- `Enums/SerialParity.cs` — None, Even, Odd, Mark, Space.
- `Enums/SerialPortName.cs` — COM1 až COM50.
- `Enums/CpuType.cs` — modely Siemens CPU.
- `BitOrder.cs` — Normal alebo Swapped poradie slov pri skladaní dlhšej hodnoty.

### Rozdiel medzi bitom, bajtom a slovom

- **Bit** je jedna dvojstavová informácia (0 alebo 1).
- **Bajt** je skupina 8 bitov.
- **Word** v tomto kóde znamená 16-bitové celé číslo.
- **Int** je znamienkové 16-bitové celé číslo.
- **Real** je 32-bitové desatinné číslo podľa štandardu IEEE-754.
- **Double** je 64-bitové desatinné číslo.

Slovo `BitOrder` môže miasť: jeho komentár hovorí o poradí 16-bitových slov, nie o poradí jednotlivých bitov. Dva registre môžu byť napríklad uložené ako [horná časť, dolná časť] alebo opačne. Pri spojení rozhoduje `Normal` alebo `Swapped`.

## 7. Objekty pre používateľské rozhranie

Databázový model a model na obrazovku nie sú vždy to isté:

- `Models/PointViewModel.cs` — bod spolu s poslednými hodnotami, časom, stavom komunikácie a chybou.
- `Models/MeasurementViewModel.cs` — jedna hodnota pripravená na zobrazenie, vrátane textu `ValueStr` a viditeľnosti grafu.
- `DBModelData/MeasurementData.cs` — jeden uložený záznam z archívu: timestamp, ID, názvy, číslo, jednotka a protokol.

`PointViewModel` a `MeasurementViewModel` implementujú `INotifyPropertyChanged`, aby sa WPF obrazovka obnovila, keď sa hodnota zmení. Podrobnejší pojem MVVM je vysvetlený v lekcii 01.

## 8. Historické preklepy

Kód a tabuľky používajú názvy `Simens`, `SimenseDeviceId` a `SimensMeasurement`. Ide o historické preklepy v identifikátoroch. Pri hľadaní súborov alebo databázových stĺpcov použi presný názov; pri vysvetľovaní budeme hovoriť o Siemens S7.

