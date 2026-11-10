# IOBusMonitor — sprievodca pre začiatočníka

> **Čo je tento projekt, ako súvisia jeho časti a v akom poradí sa ich oplatí študovať.**  
> Vysvetlenie vychádza zo zdrojového kódu a verejnej dokumentácie v repozitári.

## Čo program robí

**IOBusMonitor** je desktopový diagnostický nástroj pre Windows. Pripojí sa k priemyselným zariadeniam, číta ich hodnoty, zobrazuje posledné merania a ukladá históriu do lokálnej SQLite databázy.

Podporuje tri spôsoby komunikácie:

- **Modbus TCP** — Modbus správy sa prenášajú po počítačovej sieti.
- **Modbus RTU** — Modbus správy sa prenášajú cez sériovú linku, často po RS485.
- **Siemens S7** — čítanie dát z PLC Siemens cez S7 protokol.

Program je určený na čítanie a monitorovanie. README výslovne neuvádza ovládanie výstupov PLC ako podporovanú funkciu.

**PLC** je programovateľný priemyselný riadiaci automat. **Monitorovanie** znamená opakované čítanie hodnôt a ich zobrazenie; samo osebe nemení stav zariadenia.

## Hierarchia údajov: zariadenie → bod → meranie

V aplikácii sú údaje usporiadané takto:

```text
Zariadenie (Device)
└── Bod (Point)
    └── Meranie (Measurement)
```

Príklad:

```text
Zariadenie: Boiler TCP, adresa 192.168.1.20
└── Bod: Boiler Registers
    ├── Meranie: Boiler Temperature, register 1, °C
    └── Meranie: Line Pressure, register 2, bar
```

- **Zariadenie** hovorí, kam sa pripojiť: IP adresa, port alebo sériový port.
- **Bod** je pomenovaná skupina súvisiacich hodnôt, napríklad jedna stanica alebo časť stroja.
- **Meranie** opisuje konkrétnu hodnotu: adresu registra, dátový typ, jednotku, zaokrúhlenie a prípadný výpočtový výraz.

Register je pomenované/číslované miesto, ktoré zariadenie poskytuje na čítanie. **Jednotka** dáva číslu fyzikálny význam: napríklad 25.4 °C alebo 3.2 bar.

## Ako putujú dáta

```text
nastavenia v Settings.db
          │ zariadenia, body, merania
          ▼
TimerService — v intervale načíta aktívne body
          │
          ├── Modbus TCP reader
          ├── Modbus RTU reader
          └── Siemens S7 reader
          │ surová hodnota → podmienka/výpočet → zaokrúhlená hodnota
          ▼
PointRead udalosť ───────────────► Dashboard
          │
          ▼
DataStorageService ──────────────► denné SQLite archívy
                                         │
                                         ▼
                               DataLoaderService
                                         │
                                  History / graf
```

**Surová hodnota** je to, čo zariadenie vráti pred úpravou. Napríklad merač môže vrátiť celé číslo `253`, ktoré znamená `25.3 °C`. Výraz `value / 10` potom vykoná delenie a zmení ho na `25.3`.

**SQLite** je databáza uložená v jednom súbore. Program používa jednu databázu nastavení a samostatné databázy s meraniami podľa dňa.

## Čo znamená matematický výraz

Aplikácia má pri meraní pole `Condition`. V jednoduchom prípade je v ňom len:

```text
value
```

To znamená: použi prečítanú hodnotu tak, ako je. Výraz s operáciou, napríklad `value / 10`, vykoná aritmetický prepočet.

- `+` sčíta: `2 + 3 = 5`
- `-` odčíta: `7 - 2 = 5`
- `*` násobí: `4 * 3 = 12`
- `/` delí: `10 / 2 = 5`
- `(…)` zátvorky určujú, čo vypočítať skôr: `(2 + 3) * 4 = 20`

Zápis `* 0.1` znamená násobenie desatinou, teda posun desatinnej čiarky o jedno miesto doľava. Presné pravidlá vyhodnocovania, prioritu operácií a podporované funkcie vysvetlíme v osobitnej matematickej lekcii podľa kódu `ConditionEvaluator`.

## Z čoho sa skladá repozitár

- `IOBusMonitor/` — WPF aplikácia pre Windows: okná, stránky, navigácia a ViewModel triedy.
- `IOBusMonitorLib/` — zdieľaná knižnica: modely zariadení, protokolové čítače, výrazy, časovač, nastavenia, archív a demo režim.
- `IOBusMonitorLib.Tests/` — automatizované testy niektorých základných funkcií.
- `ShortcutTool/` — pomocný program pre Windows skratky.
- `docs/` — existujúce návody, závislosti, troubleshootovanie a obrázky.
- `.github/workflows/` — CI/release automatizácie spúšťané cez GitHub Actions.

**Knižnica** je oddeliteľný balík kódu, ktorý môže používať iný program. **WPF** je technológia Microsoftu na tvorbu okien pre Windows. **ViewModel** je trieda, ktorá pripravuje dáta a príkazy pre obrazovku.

## Dôležité názvy v kóde

V repozitári sú historické preklepy `Simens` a `Simense` v názvoch tried, priečinkov a databázových tabuliek. Správny názov výrobcu/protokolu je **Siemens**. V príručke budeme písať Siemens, ale názvy zdrojových súborov uvedieme presne tak, ako sú v repozitári, aby sa dali nájsť.

Slovo **point** v tomto programe znamená logickú skupinu meraní. Nemusí ísť o jeden elektrický bod. **Measurement** je konkrétna hodnota pod týmto bodom.

## Poradie lekcií

Projekt je väčší, preto ho rozoberáme po menších tematických častiach. Každý dokončený dokument dostane vlastný commit a push.

1. **00 — Obsah a architektúra** (tento dokument)
2. **01 — Spustenie, databáza nastavení a hlavné okno**
3. **02 — Model zariadenie/bod/meranie a SQLite tabuľky**
4. **03 — Modbus RTU a Modbus TCP: registre, typy a prevody**
5. **04 — Siemens S7: adresy DBX/DBB/DBW/DBD a čítanie**
6. **05 — Matematické výrazy a dátové typy**
7. **06 — TimerService: polling, chyby, demo režim a live udalosti**
8. **07 — SQLite archív, načítanie histórie a zmenšovanie grafu**
9. **08 — Dashboard, história a MVVM/WPF**
10. **09 — Nastavenia, správa zariadení a validácia**
11. **10 — Testy, zostavenie, balenie a zvyšné súbory**

Poradie ide od toho, ako sa program spustí, k tomu, ako číta a zobrazuje dáta.

## Poznámka o rozsahu

Tento projekt má veľa súborov a niektoré sú rozsiahle. Výklad preto vysvetlí každú skupinu súborov a jej dôležité funkcie v samostatných lekciách. Pri každej matematickej alebo priemyselnej skratke uvedieme jednoduchý význam a príklad.

