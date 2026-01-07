# AtlantumEnrichment

Dangerous Atlantum enrichment system for Techtonica. Process raw Atlantum into highly valuable enriched Atlantum - but beware of explosions!

## WARNING

Atlantum enrichment is DANGEROUS! Facilities can explode, creating radiation zones that damage everything nearby. Use safety modules!

## Features

### Facilities

#### Centrifuge
- First stage: separates Atlantum isotopes
- Input: Raw Atlantum
- Output: Concentrated Atlantum (40%) + Waste (50%)
- Risk: LOW (0.5x base risk)
- Max 2 safety modules

#### Enrichment Chamber
- Main enrichment process
- Input: Concentrated Atlantum
- Output: Enriched Atlantum (20%) + Waste (70%)
- Risk: HIGH (1.5x base risk)
- Containment system that degrades over time
- Max 4 safety modules

#### Waste Processor
- Handles radioactive waste safely
- Input: Atlantum Waste
- Output: Depleted Atlantum (80%, safe) + Recovered Material (5%)

### Safety System

- **Safety Modules**: Reduce explosion risk by 10% each
- **Containment Integrity**: Enrichment chambers have containment that degrades
- **Containment Breach**: Low containment causes radiation leaks

### Radiation Zones

- Created by explosions and containment breaches
- Damages nearby facilities and players
- Decays over time (configurable)
- Can be cleaned by BioProcessing bio-remediation

## Risk Management

| Facility | Base Risk | With 4 Safety Modules |
|----------|-----------|----------------------|
| Centrifuge | 12.5% | 2.5% |
| Enrichment Chamber | 37.5% | 7.5% |

## Integration

- **BioProcessing**: Bio-remediation accelerates radiation cleanup
- **Waste to BioProcessing**: Depleted waste can be safely composted

## Requirements

- BepInEx 5.4.2100+
- EquinoxsModUtils 6.1.3+
- EMUAdditions 2.0.0+

## Installation

1. Install BepInEx for Techtonica
2. Install EquinoxsModUtils and EMUAdditions
3. Place AtlantumEnrichment.dll in your BepInEx/plugins folder

## Configuration

Risk levels, radiation damage, and decay times can be configured in the BepInEx configuration file.

## Changelog

### [1.0.0] - 2025-01-05
- Initial release
- Centrifuge for isotope separation
- Enrichment Chamber with containment system
- Waste Processor for safe disposal
- Radiation zones with decay
- Safety module system
- BioProcessing integration for bio-remediation
