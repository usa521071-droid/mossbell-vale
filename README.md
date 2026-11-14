# Mossbell Vale

Mossbell Vale is a top-down exploration RPG for Nintendo 3DS with original 16-bit pixel art. Follow the road from Harbor Town through Bracken Route, Ember Cave and Slate Summit. Collect four glowbells across the region and ring the arch in Summit Shrine. The Windows editor opens the whole connected project.

## Play on Nintendo 3DS

Download `MossbellVale.cia` from [Releases](https://github.com/usa521071-droid/mossbell-vale/releases/latest). Install it with a CIA installer on a 3DS set up to run homebrew titles. The release also includes `MossbellVale.3dsx` for the Homebrew Launcher. Put that version in `/3ds/MossbellVale/` on your SD card.

- **D-pad or Circle Pad** moves. Hold **B** to run.
- **A** talks to people, reads signs and rings the arch when you face it. Walk onto a glowbell to collect it.
- **X** toggles the map zoom.
- **Start** opens the trail menu. View connected maps, save your progress, change clothes or check for updates. Walk through roads, doors and cave exits to travel.
- **Select** opens the wardrobe directly. Pick a feature with Up/Down and change its color with Left/Right.

## Build a world on Windows

Open `MossbellValeEditor.exe` from the same release. The project tree groups settlements, routes and interiors. Select a map to edit it.

1. Pick a ground or object tile and paint on the 16×16-pixel tile grid. Mountain rock, cliff edges, corners and stairs are in **Ground**. Roof and facade pieces join to form buildings in **Objects**.
2. Use **Movement** to see collision and override whether the player can walk on a cell. Use **Events** to place a warp, a sign or an NPC. A warp needs a walkable destination in another map or a different location in the same map. Place a second warp for the return trip.
3. Use **Header** to name or group the map and select the project's start map. Use **Set player start** on a walkable tile. Set the starting character's outfit in the editor.
4. Open **Sprite Studio** (**Ctrl+E**) to draw tiles or character frames pixel by pixel. Use the outfit swatches to keep in-game color changes working. **Save PNG** and **Import PNG** work with one tile or frame at a time.
5. Save the editable project. Keep the `.rpgp` project and its `.rpga` art sidecar together using the same filename stem. You can also open an older single-map `.rpgw` as a one-map project.
6. To test on the base game, choose **Export to SD**, select the root of your 3DS SD card and restart. It reads `world.rpgp` and `art.rpga` in `/3ds/MossbellVale/`.

Place glowbells on walkable terrain and leave room beside trail signs, NPCs and stone arches. To return to the built-in project after SD testing, move the exported project file out of the game's folder.

To shape a mountain quickly, select **Mountain plateau** from the region brush menu and drag a rectangle. Use the **Stairs** ground stamp to cut a walkable route through a cliff.

## Publish a CIA from the editor

Choose **Publish CIA**, set the title, version and output directory, then build. The editor embeds all maps and custom art in the CIA. The first build can download the portable 3DS toolchain. Keep the same unique title ID for later versions of your game.

To distribute through GitHub, enter your `owner/repository` and use **Upload to GitHub** after a successful build. This uses the GitHub CLI (`gh`) and an existing signed-in account with access to the repository. The editor uploads the CIA and its checksum catalog for future in-game updates.

## Update from the game

Open **Start → Updates → Check for updates** with Wi-Fi connected. **Download official world** downloads the connected region. The original project and updated project keep separate saves.

**Download game CIA** appears available when a newer game release is listed. It verifies the release file against the catalog's SHA-256 hash and saves it to `/3ds/MossbellVale/MossbellVale-update.cia`. Install that file from your CIA installer. Updates to the map and wardrobe can be exported from the editor without rebuilding or reinstalling the game.

## Build from source

From PowerShell on Windows:

```powershell
powershell -ExecutionPolicy Bypass -File .\build\Install-3DSTools.ps1
powershell -ExecutionPolicy Bypass -File .\build\Build-3DS.ps1
powershell -ExecutionPolicy Bypass -File .\editor\build.ps1
```

The build writes the CIA, 3DSX and editor EXE to `dist`. The 3DS tool installer uses a portable directory. Pass `-ToolRoot` to both 3DS scripts to choose a specific location. To regenerate the graphics and demo world, run `py tools/art/make_assets.py` with Pillow installed. To use Sprite Studio exports, add `--overrides PATH` and name the PNGs by tile ID (for example, `23.png`), player frame (`player-0.png` through `player-11.png`), or `player.png` for the whole sprite sheet.

The RPGW v2 map starts with four ASCII bytes (`RPGW`), followed by five little-endian 16-bit values (format version, width, height, spawn X, spawn Y), four palette bytes (shirt, pants, hair, skin), then two row-major byte arrays for ground and objects. Dimensions are 8–128 tiles per side. Loading a v1 map migrates its existing mountain ledges and stairs to the ground layer.

The RPGA v2 art sidecar starts with the bytes `52 50 47 41 02 00 00 00`, followed by the 128×256 tile sheet and the 48×96 player sheet in scanline RGBA order. Each tile is 16×16. The player sheet uses 16×24 frames with rows for down, left, right and up. Loading v1 art keeps compatible custom sprites and updates the tile slots whose layer or collision changed.

RPGP v1 stores up to 16 named maps. Each entry contains an RPGW grid, one movement byte per cell, and up to 64 warp, sign or NPC events. The map IDs are stable within a project, so renaming a map preserves its links.

The official updater reads `updates/project-catalog.txt`. The catalog is `MVB1 <build> <world-revision> <cia-sha256>`, where build is `major*10000 + minor*100 + patch`. Publish `MossbellVale.cia` on the matching `vMAJOR.MINOR.PATCH` GitHub release and put the official project at `updates/project-<revision>.rpgp`. For a CIA built in the editor, the release's `catalog.txt` is used instead.

The 3DS build uses [devkitARM and libctru](https://github.com/devkitPro/libctru), [libcurl](https://curl.se/), [Mbed TLS](https://github.com/Mbed-TLS/mbedtls), and [zlib](https://zlib.net/). The included CA bundle is [curl's Mozilla CA extract](https://curl.se/docs/caextract.html), distributed under MPL 2.0.
