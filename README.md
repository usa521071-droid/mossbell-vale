# Mossbell Vale

Mossbell Vale is a top-down exploration RPG for Nintendo 3DS with original 16-bit pixel art. Explore Bellwick Town, Lantern Cavern and Highstone Pass, collect the glowbells and ring each region's stone arch. The Windows editor includes the three maps, a tile painter and a pixel editor for tiles and animated characters.

## Play on Nintendo 3DS

Download `MossbellVale.cia` from [Releases](https://github.com/usa521071-droid/mossbell-vale/releases/latest). Install it with a CIA installer on a 3DS set up to run homebrew titles. The release also includes `MossbellVale.3dsx` for the Homebrew Launcher. Put that version in `/3ds/MossbellVale/` on your SD card.

- **D-pad or Circle Pad** moves. Hold **B** to run.
- **A** reads signs and rings the arch when you face it. Walk onto a glowbell to collect it.
- **X** toggles the map zoom.
- **Start** opens the trail menu. Choose a region, save your progress, change clothes or check for updates. Each region has its own save.
- **Select** opens the wardrobe directly. Pick a feature with Up/Down and change its color with Left/Right.

## Build a world on Windows

Open `MossbellValeEditor.exe` from the same release. It opens the town with all assets embedded. Load the cavern or mountain preset to start with a different layout.

1. Pick a ground or object tile and paint on the 16×16-pixel tile grid. Mountain rock, cliff edges, corners and stairs are in **Ground**. Roof and facade pieces join to form buildings in **Objects**.
2. Use **Spawn** to set the starting position on a walkable tile. Set the character's shirt, pants, hair and skin colors in the outfit panel.
3. Open **Sprite Studio** (**Ctrl+E**) to draw tiles or character frames pixel by pixel. Each walking direction has an idle frame and two step frames. Use the designated outfit swatches to keep in-game color changes working on your custom character art. **Save PNG** exports the selected tile or frame and **Import PNG** accepts an image of the same size.
4. Save the editable world. Keep the `.rpgw` map and its `.rpga` art sidecar together using the same filename stem.
5. Choose **Export to SD**, select the root of your 3DS SD card, and restart the game. It reads the exported `world.rpgw` and `art.rpga` from `/3ds/MossbellVale/` automatically. You can revisit the export from **Start → Choose a region → Custom World**.

Place glowbells on walkable terrain and leave room beside trail signs and stone arches for the player to interact. Select Bellwick Town, Lantern Cavern or Highstone Pass from the region menu to return to the built-in maps.

To shape a mountain quickly, select **Mountain plateau** from the region brush menu and drag a rectangle. Use the **Stairs** ground stamp to cut a walkable route through a cliff.

## Update from the game

Open **Start → Updates → Check for updates** with Wi-Fi connected. **Download official world** updates Bellwick Town. You can visit it from the region menu.

**Download game CIA** appears available when a newer game release is listed. It verifies the release file against the catalog's SHA-256 hash and saves it to `/3ds/MossbellVale/MossbellVale-update.cia`. Install that file from your CIA installer. Updates to the map and wardrobe can be exported from the editor without rebuilding or reinstalling the game.

## Build from source

From PowerShell on Windows:

```powershell
powershell -ExecutionPolicy Bypass -File .\build\Install-3DSTools.ps1
powershell -ExecutionPolicy Bypass -File .\build\Build-3DS.ps1
powershell -ExecutionPolicy Bypass -File .\editor\build.ps1
```

The build writes the CIA, 3DSX and editor EXE to `dist`. The 3DS tool installer uses a portable directory. Pass `-ToolRoot` to both 3DS scripts to choose a specific location. To regenerate the graphics and demo world, run `py tools/art/make_assets.py` with Pillow installed.

The RPGW v2 map starts with four ASCII bytes (`RPGW`), followed by five little-endian 16-bit values (format version, width, height, spawn X, spawn Y), four palette bytes (shirt, pants, hair, skin), then two row-major byte arrays for ground and objects. Dimensions are 8–128 tiles per side. Loading a v1 map migrates its existing mountain ledges and stairs to the ground layer.

The RPGA v2 art sidecar starts with the bytes `52 50 47 41 02 00 00 00`, followed by the 128×256 tile sheet and the 48×96 player sheet in scanline RGBA order. Each tile is 16×16. The player sheet uses 16×24 frames with rows for down, left, right and up. Loading v1 art keeps compatible custom sprites and updates the tile slots whose layer or collision changed.

The updater reads `updates/catalog.txt`. Each catalog row is `MVB1 <build> <world-revision> <cia-sha256>`, where build is `major*10000 + minor*100 + patch`. Publish `MossbellVale.cia` on the matching `vMAJOR.MINOR.PATCH` GitHub release and put the official map at `updates/world-<revision>.rpgw` before increasing those numbers. For a fork, change the GitHub URLs in `game/source/update.c`.

The 3DS build uses [devkitARM and libctru](https://github.com/devkitPro/libctru), [libcurl](https://curl.se/), [Mbed TLS](https://github.com/Mbed-TLS/mbedtls), and [zlib](https://zlib.net/). The included CA bundle is [curl's Mozilla CA extract](https://curl.se/docs/caextract.html), distributed under MPL 2.0.
