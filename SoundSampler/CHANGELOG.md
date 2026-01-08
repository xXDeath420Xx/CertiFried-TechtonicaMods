# SoundSampler_Patched Changelog

## About This Fork
This is a patched version of **SoundSampler** originally created by **Equinox**.
- Original mod: https://thunderstore.io/c/techtonica/p/Equinox/SoundSampler/
- Original license: Not specified

## v1.1.0-patched (CertiFried)
**Reason for patch:** Original mod uses deprecated EMU API (LoadTexture2DFromFile) causing game crash on load.

### Changes from original:
- Removed dependency on deprecated `ModUtils.LoadTexture2DFromFile`
- Reimplemented sound discovery using reflection
- Added search filter functionality
- Added category grouping for sounds

### Functionality preserved:
- Press F12 to open sound sampler GUI
- Click any sound to play it
- Copy button copies sound event name to clipboard
- Configurable hotkey
