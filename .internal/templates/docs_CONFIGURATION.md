# Configuration guide draft

## Device → point → measurement model

IOBusMonitor should be documented around this hierarchy:

```text
Device
└── Point
    └── Measurement
```

### Device

Connection endpoint:

- Modbus TCP: IP/host + port.
- Modbus RTU: COM port + baud + parity + slave id.
- Siemens S7: IP/host + rack + slot + CPU type.

### Point

A logical group under a device. It should represent a subsystem, station, block, or group of related measurements.

### Measurement

One value read from a register/address, with:

- name,
- unit,
- address/register,
- data type/quantity,
- rounding,
- optional formula/condition.

## Validation rules to document

- Names should be unique inside their parent scope.
- Register/address must be valid for selected protocol.
- Formula must be tested before saving.
- Disabled devices/measurements are not polled.
