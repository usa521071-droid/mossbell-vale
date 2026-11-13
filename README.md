# Mossbell Vale

Mossbell Vale is a short top-down exploration RPG for Nintendo 3DS with original GBA-inspired pixel art. Follow the trail, collect three glowbells, and ring the stone arch. The Windows editor lets you build a new world or start from the included demo.

## Play on Nintendo 3DS

Download `MossbellVale.cia` from [Releases](https://github.com/usa521071-droid/mossbell-vale/releases/latest). Install it with a CIA installer on a 3DS set up to run homebrew titles. The release also includes `MossbellVale.3dsx` for the Homebrew Launcher. Put that version in `/3ds/MossbellVale/` on your SD card.

- **D-pad or Circle Pad** moves. Hold **B** to run.
- **A** reads signs and rings the arch when you face it. Walk onto a glowbell to collect it.
- **Start** opens the trail menu. Save your progress there, open the wardrobe, or check for updates.
- **Select** opens the wardrobe directly. Pick a feature with Up/Down and change its color with Left/Right.

## Build a world on Windows

Open `MossbellValeEditor.exe` from the same release. It opens the sample world with all assets embedded.

1. Pick a ground or object tile and paint on the map. Ground includes meadow, trails, water and boardwalk. Objects include trees, mountains, rocks, cottages and quest markers.
2. Use **Spawn** to set the starting position on a walkable tile. Set the character's shirt, pants, hair and skin colors in the outfit panel.
3. Save an editable `.rpgw` file. Choose **Export to SD**, select the root of your 3DS SD card, and restart the game. It reads `/3ds/MossbellVale/world.rpgw` automatically.

Place glowbells on walkable terrain and leave room beside trail signs and stone arches for the player to interact. To return to the official world, move your custom `world.rpgw` file out of the game's SD folder.

## Update from the game

Open **Start → Updates → Check for updates** with Wi-Fi connected. The first official world update adds a ruin and fourth glowbell to the eastern trail. **Download official world** applies it in the game. An SD card world exported by the editor stays active if one is present.

**Download game CIA** appears available when a newer game release is listed. It verifies the release file against the catalog's SHA-256 hash and saves it to `/3ds/MossbellVale/MossbellVale-update.cia`. Install that file from your CIA installer. Updates to the map and wardrobe can be exported from the editor without rebuilding or reinstalling the game.

## Build from source

From PowerShell on Windows:

```powershell
powershell -ExecutionPolicy Bypass -File .\build\Install-3DSTools.ps1
powershell -ExecutionPolicy Bypass -File .\build\Build-3DS.ps1
powershell -ExecutionPolicy Bypass -File .\editor\build.ps1
```

The build writes the CIA, 3DSX and editor EXE to `dist`. The 3DS tool installer uses a portable directory. Pass `-ToolRoot` to both 3DS scripts to choose a specific location. To regenerate the graphics and demo world, run `py tools/art/make_assets.py` with Pillow installed.

The RPGW v1 map starts with four ASCII bytes (`RPGW`), followed by five little-endian 16-bit values (format version, width, height, spawn X, spawn Y), four palette bytes (shirt, pants, hair, skin), then two row-major byte arrays for ground and objects. Dimensions are 8–128 tiles per side.

The updater reads `updates/catalog.txt`. Each catalog row is `MVB1 <build> <world-revision> <cia-sha256>`, where build is `major*10000 + minor*100 + patch`. Publish `MossbellVale.cia` on the matching `vMAJOR.MINOR.PATCH` GitHub release and put the official map at `updates/world-<revision>.rpgw` before increasing those numbers. For a fork, change the GitHub URLs in `game/source/update.c`.

The 3DS build uses [devkitARM and libctru](https://github.com/devkitPro/libctru), [libcurl](https://curl.se/), [Mbed TLS](https://github.com/Mbed-TLS/mbedtls), and [zlib](https://zlib.net/). The included CA bundle is [curl's Mozilla CA extract](https://curl.se/docs/caextract.html), distributed under MPL 2.0.
