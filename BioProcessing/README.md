# BioProcessing

Biological processing system for Techtonica. Grow organic materials and convert them into valuable biofuel and biogas.

## Features

### Facilities

#### Algae Vat
- Produces raw algae from water and light
- Continuous production while conditions are met
- Visual feedback shows fill level

#### Mushroom Farm
- Grows mushrooms in dark conditions
- Requires fertilizer input
- Produces mushrooms and spores

#### Bio-Reactor
- Converts organic matter into biofuel and biogas
- Accepts algae, mushrooms, and organic waste
- 30% conversion to biofuel, 50% to biogas

#### Composter
- Converts organic waste into fertilizer
- 50% conversion efficiency
- Feeds mushroom farms

### Bio-Remediation
- Clean up hazardous zones using biological processes
- Accelerates radiation decay from AtlantumEnrichment

## Integration

- **Recycler**: Organic waste from recycling feeds composters
- **DroneLogistics**: Biofuel powers drone operations
- **AtlantumEnrichment**: Bio-remediation cleans radiation zones

## Resource Flow

```
Water + Light -> Algae Vat -> Raw Algae
                                  |
Fertilizer -> Mushroom Farm -> Mushrooms
     ^                            |
     |                            v
Composter <- Organic Waste <- Bio-Reactor -> Biofuel + Biogas
```

## Requirements

- BepInEx 5.4.2100+
- EquinoxsModUtils 6.1.3+
- EMUAdditions 2.0.0+

## Installation

1. Install BepInEx for Techtonica
2. Install EquinoxsModUtils and EMUAdditions
3. Place BioProcessing.dll in your BepInEx/plugins folder

## Configuration

Growth rates and conversion efficiencies can be configured in the BepInEx configuration file.

## Changelog

### [1.0.0] - 2025-01-05
- Initial release
- Algae Vat for continuous algae production
- Mushroom Farm with fertilizer system
- Bio-Reactor for biofuel/biogas conversion
- Composter for fertilizer production
- Bio-remediation system for hazard cleanup
