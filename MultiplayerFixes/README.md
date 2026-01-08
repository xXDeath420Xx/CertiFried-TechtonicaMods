# MultiplayerFixes

Fixes and improvements for multiplayer gameplay.

## Features

- **Sync Improvements** - Better synchronization of game state between players
- **Desync Prevention** - Fixes for common desync issues
- **Network Optimization** - Reduced bandwidth usage
- **Error Recovery** - Graceful handling of connection issues
- **Host Migration** - Better support for host changes

## Known Issues Fixed

- Machine state synchronization bugs
- Inventory desync on high latency connections
- Player position rubber-banding
- Recipe unlock synchronization

## Configuration

Settings can be adjusted in the config file:
- Network tick rate
- Buffer sizes
- Debug logging

## Installation

1. Install BepInEx
2. Place MultiplayerFixes.dll in BepInEx/plugins

## Requirements

- BepInEx 5.4.21+

## Note

This mod should be installed on all connected players for best results.
