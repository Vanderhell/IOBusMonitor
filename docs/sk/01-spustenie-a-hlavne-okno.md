# 01 — Spustenie aplikácie a hlavné okno

Táto časť sleduje, čo sa stane od dvojitého kliknutia na `IOBusMonitor.exe` po zobrazenie hlavného okna.

Súbory:

- `IOBusMonitor/App.xaml`, `App.xaml.cs` — štart aplikácie.
- `IOBusMonitor/InstanceManager.cs` — správanie pri opätovnom spustení.
- `IOBusMonitor/MainWindow.xaml`, `MainWindow.xaml.cs` — hlavné okno a systémová lišta.
- `IOBusMonitor/ViewModels/MainViewModel.cs` — navigácia a príkazy.
- `IOBusMonitor/ViewModels/ViewModelBase.cs` — spoločný základ pre dáta obrazovky.
- `IOBusMonitorLib/HelpClass/RelayCommand.cs` — prepojenie akcií s tlačidlami.
- `IOBusMonitor/AppVersionProvider.cs` — zobrazenie verzie.
- `IOBusMonitor/SplashScreen.xaml`, `.xaml.cs` — úvodná obrazovka.

## 1. `App.xaml`: zdroje a prvé okno

WPF aplikácia začína súborom `App.xaml`. Určuje globálne zdroje a štartovacie okno. **XAML** je textový zápis rozloženia používateľského rozhrania, teda okien, tlačidiel a ich vlastností.

Jeho sprievodný súbor `App.xaml.cs` obsahuje triedu `App`, odvodenú od WPF triedy `Application`. Metóda `OnStartup()` sa zavolá pri spustení programu. Je to podobné ako `main()` v jednoduchšom programe: tu sa pripraví databáza a až potom sa ukáže okno.

## 2. Čo robí `OnStartup()`

Poradie štartu v aktuálnom kóde:

1. Nastaví kultúru používateľského rozhrania na `en-US`.
2. Zavolá `InstanceManager.EnsureSingleInstanceByClosingPrevious()`.
3. Vytvorí priečinok `Settings` a databázovú schému.
4. Ak sú prítomné špeciálne argumenty na snímku obrazovky, spustí automatizačný režim bez splash screenu.
5. Inak zobrazí splash screen a po krátkom čakaní otvorí hlavné okno.

**Kultúra** je sada pravidiel pre jazyk a formátovanie, napríklad dátumov a desatinných čísel. Toto nastavenie je pre používateľské rozhranie; neprekladá samo osebe texty aplikácie.

### Priečinok a databáza

`EnsureDatabaseFolderExists()` zostaví cestu z priečinka, z ktorého beží program, a názvu `Settings`. Ak priečinok neexistuje, vytvorí ho.

`CreateSchema()` vytvorí súbor `Settings/Settings.db` a tabuľky, ak ešte neexistujú. **Schéma** znamená plán databázy: aké tabuľky a stĺpce v nej budú.

Jedna tabuľka napríklad ukladá nastavenia aplikácie. Ďalšie tabuľky ukladajú zariadenia, body a merania pre Modbus RTU, Modbus TCP a Siemens S7.

SQL príkaz `CREATE TABLE IF NOT EXISTS` znamená: vytvor tabuľku len vtedy, keď ešte neexistuje. Príznak `AUTOINCREMENT` dovolí databáze automaticky prideliť nové jedinečné ID.

### Ošetrenie chyby štartu

Blok `try/catch` skúša pripraviť priečinok a databázu. Ak nastane výnimka, program uloží jej text do `init_error.log`, zobrazí chybové okno a ukončí sa.

**Výnimka** je objekt opisujúci neočakávaný problém. `catch` je miesto, ktoré problém zachytí, aby aplikácia mohla zareagovať namiesto nekontrolovaného pádu.

### Automatizačný režim snímky obrazovky

`ScreenshotCapture.TryParseArgs(...)` kontroluje, či program dostal argumenty na zachytenie UI. Ak áno, hlavné okno sa zobrazí bez úvodného čakania a po zachytení sa aplikácia ukončí.

Toto je pomocník pre automatické vytváranie obrázkov používateľského rozhrania. Podrobnosti sú v lekcii o testovaní a balení.

## 3. `InstanceManager.cs`: iba jeden proces

**Proces** je bežiaci program. `EnsureSingleInstanceByClosingPrevious()` vyhľadá iný proces s rovnakým názvom.

Najprv sa pokúsi staršie okno slušne zatvoriť správou Windows `WM_CLOSE`. Ak proces po dvoch sekundách stále beží, zavolá `Kill()`, čo ho násilne ukončí.

**Win32 API** je sada nízkoúrovňových funkcií operačného systému Windows. Atribút `DllImport("user32.dll")` dovolí C# zavolať funkciu `PostMessage` z tejto knižnice. Toto je technickejšia časť programu; jej úlohou je správa opakovaných spustení, nie čítanie PLC.

## 4. Hlavné okno a systémová lišta

`MainWindow.xaml` definuje vzhľad hlavného shellu: navigáciu, panel a miesto, kam sa načítajú stránky.

**Shell** tu znamená spoločný rám aplikácie — navigáciu, titulok a systémové menu, ktoré zostáva okolo vymieňaných stránok.

V `MainWindow.xaml.cs` konštruktor:

1. zavolá `InitializeComponent()`, čím vytvorí prvky z XAML,
2. vytvorí `MainViewModel`,
3. pripojí zmeny stavu,
4. nastaví správanie systémovej lišty,
5. po načítaní okna načíta nastavenia a rozhodne, či zobrazí Dashboard alebo Home.

**Systémová lišta** je oblasť pri hodinách Windows. Ak používateľ zavrie okno bežným krížikom, `OnClosing()` môže zatvorenie zrušiť a okno skryť, aby monitorovanie mohlo pokračovať na pozadí.

Kliknutia na položky lišty zobrazia okno, spustia/zastavia monitoring alebo ukončia program. Pri ukončení sa zobrazí potvrdzovacie okno.

## 5. `MainViewModel`: navigácia a príkazy

**ViewModel** je trieda, ktorá poskytuje používateľskému rozhraniu hodnoty a akcie. `MainViewModel` vlastní:

- `TimerService` — službu, ktorá vykonáva opakované čítanie,
- zoznam posledných bodov,
- stav, či monitoring beží,
- stav demo režimu,
- navigačné príkazy,
- titulok a popis aktuálnej stránky.

V konštruktore vytvorí `TimerService` a prihlási sa na jeho udalosť `PointRead`. **Udalosť** je upozornenie: keď služba prečíta bod, zavolá pripojenú obsluhu `OnPointRead`.

### Príkazy

Vlastnosť typu `ICommand` predstavuje akciu, ktorú možno naviazať na tlačidlo. Napríklad `StartCommand` zavolá `StartMonitoring()`, keď používateľ stlačí tlačidlo Start.

`RelayCommand` je jednoduchý adaptér: prijme funkciu `Action` a odovzdá ju WPF ako príkaz.

- `Execute()` vykoná akciu.
- `CanExecute()` odpovie, či sa akcia v danom stave smie spustiť.

Príklad: príkaz Stop možno povoliť len vtedy, keď `TimerService.IsRunning` je pravda.

### Navigácia

`NavigateTo(page, ...)` zmení aktívnu stránku vo WPF `Frame`. **Frame** je kontajner, ktorý vie zobrazovať rôzne stránky na jednom mieste.

Príkazy ako `ShowDashboardCommand` vytvoria stránku Dashboard a zavolajú `NavigateTo()`. Metóda `UpdatePageContext()` zmení názov, popis a navigačnú cestu pre aktuálnu stránku.

### Spustiť a zastaviť monitoring

- `StartMonitoring()` znova načíta nastavenia, spustí časovač a obnoví stav shellu.
- `StopMonitoring()` zastaví časovač a nastaví stav na vypnutý.
- `EnableDemoMode()` pripraví ukážkové zariadenia, zapne ich a zapne demo režim v nastaveniach.

Podrobnosti o tom, čo časovač robí pri každom meracom cykle, budú v osobitnej lekcii.

## 6. `ViewModelBase.cs`: oznamovanie zmien

WPF často potrebuje vedieť, že hodnota v C# sa zmenila, aby mohol obnoviť text alebo stav tlačidla. `ViewModelBase` implementuje rozhranie `INotifyPropertyChanged`.

- `OnPropertyChanged()` vyšle upozornenie pre pomenovanú vlastnosť.
- `SetProperty(ref field, value)` porovná starú a novú hodnotu, uloží zmenu a vyšle upozornenie iba vtedy, keď sa hodnota naozaj zmenila.

Zápis `[CallerMemberName]` umožní kompilátoru doplniť názov vlastnosti automaticky. Nemusíme teda ručne písať napríklad reťazec `"CurrentPageTitle"`.

## 7. Verzia a úvodná obrazovka

- `AppVersionProvider.cs` najprv skúsi načítať informačnú verziu z metadát programu. Ak chýba, poskladá verziu z čísel Major.Minor.Build.
- `SplashScreen.xaml` definuje úvodnú obrazovku, ktorá sa ukazuje počas krátkeho čakania.
- `SplashScreen.xaml.cs` je jej sprievodná C# trieda. Je malá, pretože väčšina vzhľadu je zapísaná v XAML.

## Jednoduchý prehľad

```text
IOBusMonitor.exe
  → App.OnStartup()
  → Settings/Settings.db a tabuľky
  → splash screen
  → MainWindow
  → MainViewModel + navigácia
  → používateľ dá Start
  → TimerService začne čítať hodnoty
```

V ďalšej lekcii sa pozrieme na to, aké tabuľky aplikácia vytvára a ako sa zariadenie, bod a meranie ukladajú do databázy.

