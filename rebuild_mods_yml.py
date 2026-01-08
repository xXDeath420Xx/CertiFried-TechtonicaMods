#!/usr/bin/env python3
"""Rebuild mods.yml for r2modman with correct CertiFried mod entries."""

import os
import json
import base64
import time
import shutil
from pathlib import Path

try:
    import yaml
except ImportError:
    print("Installing PyYAML...")
    import subprocess
    subprocess.check_call(['pip', 'install', 'pyyaml'])
    import yaml

# Paths
PROFILE_PATH = Path(r"C:\Users\crawf\AppData\Roaming\r2modmanPlus-local\Techtonica\profiles\not default")
MODS_YML_PATH = PROFILE_PATH / "mods.yml"
SOURCE_ROOT = Path(r"C:\Users\crawf\TechtonicaMods\NewMods")

# CertiFried mod mappings (source folder -> r2modman name)
CERTIFRIED_MODS = {
    "AdvancedMachines": "CertiFried-AdvancedMachines",
    "AmbientLife": "CertiFried-AmbientLife",
    "AtlantumEnrichment": "CertiFried-AtlantumEnrichment",
    "AtlantumReactor": "CertiFried-AtlantumReactor_Updated",
    "BaseBuilding": "CertiFried-BaseBuilding",
    "BeltImmunity": "CertiFried-BeltImmunity_Updated",
    "BioProcessing": "CertiFried-BioProcessing",
    "ChainExplosives": "CertiFried-ChainExplosives_Updated",
    "CoreComposerPlus": "CertiFried-CoreComposerPlus",
    "DevTools": "CertiFried-DevTools",
    "DroneLogistics": "CertiFried-DroneLogistics",
    "EnhancedLogistics": "CertiFried-EnhancedLogistics",
    "HazardousWorld": "CertiFried-HazardousWorld",
    "MechSuit": "CertiFried-MechSuit",
    "MobilityPlus": "CertiFried-MobilityPlus",
    "MorePlanters": "CertiFried-MorePlanters_Updated",
    "MultiplayerFixes": "CertiFried-MultiplayerFixes",
    "NarrativeExpansion": "CertiFried-NarrativeExpansion",
    "OmniseekerPlus": "CertiFried-OmniseekerPlus",
    "PetsCompanions": "CertiFried-PetsCompanions",
    "PlanterCoreClusters": "CertiFried-PlanterCoreClusters_Updated",
    "Recycler": "CertiFried-Recycler",
    "Restock": "CertiFried-Restock_Updated",
    "SeamlessWorld": "CertiFried-SeamlessWorld",
    "SurvivalElements": "CertiFried-SurvivalElements",
    "TechtonicaFramework": "CertiFried-TechtonicaFramework",
    "TurretDefense": "CertiFried-TurretDefense",
    "WormholeChests": "CertiFried-WormholeChests_Updated",
}

# Mods to remove (conflicting with CertiFried versions)
MODS_TO_REMOVE = [
    # Direct conflicts - Equinox originals replaced by CertiFried updates
    "Equinox-AtlantumReactor",
    "Equinox-MorePlanters",
    "Equinox-PlanterCoreClusters",
    "Equinox-WormholeChests",
    "Equinox-EarlyBaseBuilding",  # conflicts with CertiFried-BaseBuilding
]

def get_image_base64(image_path):
    """Convert image file to base64 data URI."""
    if image_path.exists():
        with open(image_path, 'rb') as f:
            data = base64.b64encode(f.read()).decode('utf-8')
        return f"data:image/png;base64,{data}"
    return None

def create_mod_entry(source_name, r2_name):
    """Create a mod entry from manifest.json and icon."""
    manifest_path = SOURCE_ROOT / source_name / "manifest.json"
    icon_path = SOURCE_ROOT / source_name / "icon.png"

    if not manifest_path.exists():
        return None

    with open(manifest_path, 'r') as f:
        manifest = json.load(f)

    # Parse version
    version_parts = manifest.get('version_number', '1.0.0').split('.')
    major = int(version_parts[0]) if len(version_parts) > 0 else 1
    minor = int(version_parts[1]) if len(version_parts) > 1 else 0
    patch = int(version_parts[2]) if len(version_parts) > 2 else 0

    # Get icon
    icon_base64 = get_image_base64(icon_path)
    if not icon_base64:
        # Default 1x1 transparent PNG
        icon_base64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="

    # Display name (remove prefix and suffix)
    display_name = r2_name.replace("CertiFried-", "").replace("_Updated", "")

    return {
        'manifestVersion': 1,
        'name': r2_name,
        'authorName': 'CertiFried',
        'websiteUrl': f'https://thunderstore.io/c/techtonica/p/CertiFried/{display_name}/',
        'displayName': display_name,
        'description': manifest.get('description', ''),
        'gameVersion': '0',
        'networkMode': 'both',
        'packageType': 'other',
        'installMode': 'managed',
        'installedAtTime': int(time.time() * 1000),
        'loaders': [],
        'dependencies': manifest.get('dependencies', []),
        'incompatibilities': [],
        'optionalDependencies': [],
        'versionNumber': {
            'major': major,
            'minor': minor,
            'patch': patch,
        },
        'enabled': True,
        'icon': icon_base64,
    }

def main():
    print("Rebuilding mods.yml...")

    # Backup
    backup_path = MODS_YML_PATH.with_suffix(f'.yml.backup_py_{int(time.time())}')
    shutil.copy(MODS_YML_PATH, backup_path)
    print(f"  Backed up to: {backup_path.name}")

    # Read current mods.yml
    print("  Parsing mods.yml...")
    with open(MODS_YML_PATH, 'r', encoding='utf-8') as f:
        mods = yaml.safe_load(f)

    if mods is None:
        mods = []

    print(f"  Found {len(mods)} mod entries")

    # Filter out CertiFried mods and mods to remove
    filtered_mods = []
    removed_count = 0

    for mod in mods:
        name = mod.get('name', '')
        keep = True

        # Remove CertiFried mods (we'll recreate)
        if name.startswith('CertiFried-'):
            keep = False
            removed_count += 1

        # Remove specific duplicates/conflicts
        if name in MODS_TO_REMOVE:
            keep = False
            removed_count += 1
            # Also remove the plugin folder
            plugin_folder = PROFILE_PATH / "BepInEx" / "plugins" / name
            if plugin_folder.exists():
                shutil.rmtree(plugin_folder)
                print(f"  [REMOVED PLUGIN] {name}")

        if keep:
            filtered_mods.append(mod)

    print(f"  Removed {removed_count} old CertiFried/duplicate entries")
    print(f"  Keeping {len(filtered_mods)} dependency/other mods")

    # Create new CertiFried entries
    new_mods = []
    for source_name, r2_name in CERTIFRIED_MODS.items():
        entry = create_mod_entry(source_name, r2_name)
        if entry:
            version = entry['versionNumber']
            print(f"  [OK] {source_name} v{version['major']}.{version['minor']}.{version['patch']}")
            new_mods.append(entry)
        else:
            print(f"  [SKIP] {source_name} - no manifest")

    # Combine
    all_mods = filtered_mods + new_mods

    # Write back
    print("  Writing new mods.yml...")
    with open(MODS_YML_PATH, 'w', encoding='utf-8') as f:
        yaml.dump(all_mods, f, default_flow_style=False, allow_unicode=True, sort_keys=False)

    print()
    print("mods.yml rebuilt!")
    print(f"  Total entries: {len(all_mods)}")
    print(f"  Dependencies/other: {len(filtered_mods)}")
    print(f"  CertiFried mods: {len(new_mods)}")
    print()
    print("Restart r2modman to see changes.")

if __name__ == '__main__':
    main()
