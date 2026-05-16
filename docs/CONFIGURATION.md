# Configuration Guide

## Configuration Model

IOBusMonitor is configured around this hierarchy:

```text
Device
└── Point
    └── Measurement
```

## Device

A device defines the communication endpoint.

Protocol-specific fields currently visible in the codebase:

- Modbus TCP: device name, IP/host, port, active state
- Modbus RTU: device name, serial port, baud rate, parity, slave ID, active state
- Siemens S7: device name, IP/host, port, rack, slot, CPU type, active state

Validation behavior currently implemented:

- device name is required,
- endpoint fields must be valid,
- names must be unique,
- protocol endpoint combinations must be unique within the same protocol table.

## Point

A point is a logical group under one device. Typical examples:

- station,
- skid,
- controller section,
- process area.

Validation behavior currently implemented:

- point name is required,
- referenced device must exist,
- point names must be unique per device.

## Measurement

A measurement defines one value source under a point.

Current measurement fields depend on protocol but generally include:

- name,
- engineering unit,
- register or address,
- quantity or data width,
- rounding,
- condition/formula,
- active state.

Validation behavior currently implemented:

- measurement name is required,
- referenced point must exist,
- Modbus register must be zero or greater,
- Modbus quantity must be `1`, `2`, or `4`,
- rounding must be between `0` and `6`,
- Siemens addresses must match formats such as `DB1.DBX0.0`, `DB1.DBB0`, `DB1.DBW2`, `DB1.DBD4`, `DB1.DBL8`,
- names must be unique per point,
- register/address combinations must be unique per point.

## Condition / Formula Field

The current admin validation requires a condition string for each measurement.

For a pass-through measurement, use:

```text
value
```

If the expression is invalid, the admin workflow rejects the row before save.

## Application Settings

The app settings page currently persists:

- read interval in milliseconds,
- auto start,
- demo mode enabled/disabled,
- data path for daily SQLite archives.

Defaults currently documented from code:

- read interval: `1000 ms`
- auto start: `false`
- default data path: `<app folder>\Data`

## Data Location

The settings database is created under:

```text
Settings/Settings.db
```

Daily measurement archives are written under:

```text
Data/Data_yyyyMMdd.db
```

If `PathData` is changed in app settings, daily archives are written to that configured folder instead of the default local `Data` folder.
