# CoreComposerPlus

Enhances the Memory Tree (Core Composer) with batch processing and speed options.

## Features

- **Batch Processing**: Process multiple research cores per cycle instead of one at a time
- **Speed Multiplier**: Increase processing speed (2x, 5x, 10x faster)
- **Instant Mode**: Process all queued cores immediately
- **Large Queue Capacity**: Queue up to 8000 cores at once

## Configuration

Edit the config file at `BepInEx/config/com.certifired.CoreComposerPlus.cfg`:

| Setting | Default | Description |
|---------|---------|-------------|
| Batch Size | 1 | Number of cores to process per cycle (1-100) |
| Speed Multiplier | 1.0 | Processing speed multiplier (0.1-10.0) |
| Instant Mode | false | Process all queued cores instantly |
| Debug Mode | false | Enable verbose debug logging |

## How It Works

The vanilla Memory Tree processes 1 core every 5 seconds. With CoreComposerPlus:

- **Batch Size = 10**: Process 10 cores every 5 seconds
- **Speed Multiplier = 2**: Process every 2.5 seconds instead of 5
- **Both combined**: Process 10 cores every 2.5 seconds (4x throughput)
- **Instant Mode**: All cores in the queue are processed immediately

## Tips

1. Use inserters or conveyors to feed cores into the Memory Tree's input
2. The queue can hold up to 8000 cores - perfect for automation
3. Combine with faster conveyor mods for maximum throughput

## Changelog

### v1.0.0
- Initial release
- Batch processing support (1-100 cores per cycle)
- Speed multiplier (0.1x to 10x)
- Instant mode option
- Enhanced queue capacity

## License

GPL 3.0

## Credits

- CertiFried - Author
- Equinox - EquinoxsModUtils
