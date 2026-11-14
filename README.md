# Mossbell Vale

Mossbell Vale is a top-down exploration RPG for Nintendo 3DS with original Game Boy Color-style pixel art. Follow the road from Harbor Town through Bracken Route, Ember Cave and Slate Summit. Collect four glowbells across the region and ring the arch in Summit Shrine. The Windows editor opens the whole connected project.

## Play on Nintendo 3DS

Download `MossbellVale.cia` from [Releases](https://github.com/usa521071-droid/mossbell-vale/releases/latest). Install it with a CIA installer on a 3DS set up to run homebrew titles. The release also includes `MossbellVale.3dsx` for the Homebrew Launcher. Put that version in `/3ds/MossbellVale/` on your SD card.

- **D-pad or Circle Pad** moves. Hold **B** to run.
- **A** talks to people, reads signs and rings the arch when you face it. Walk onto a glowbell to collect it.
- **X** toggles the map zoom.
- **Start** opens the trail menu. View connected maps, save your progress, change clothes or check for updates. Walk through roads, doors and cave exits to travel.
- **Select** opens the wardrobe directly. Pick a feature with Up/Down and change its color with Left/Right.

## Build a world on Windows

Open `MossbellValeEditor.exe` from the same release. The project tree groups settlements, routes and interiors. Select a map to edit it.

1. Pick a ground or object tile and paint on the 16×16-pixel map grid. Each map cell is made of four 8×8 pixel tiles. Mountain rock, cliff edges, corners and stairs are in **Ground**. Roof and façade pieces join to form buildings in **Objects**.
2. Use **Movement** to see collision and override whether the player can walk on a cell. Use **Events** to place a warp, a sign or an NPC. A warp needs a walkable destination in another map or a different location in the same map. Place a second warp for the return trip.
3. Use **Header** to name or group the map and select the project's start map. Use **Set player start** on a walkable tile. Set the starting character's outfit in the editor.
4. Open **Sprite Studio** (**Ctrl+E**) to draw tiles or 16×16 sprite frames with 8×8 guides. Use the shirt swatch to recolor the stock hero's shared accent. Custom sprites can use all four outfit swatch groups. **Save PNG** and **Import PNG** work with one tile or frame at a time. To put the new artwork into an older project, use **Reset art**. This replaces its art sheets and can be undone in Sprite Studio.
5. Save the editable project. Keep the `.rpgp` project and its `.rpga` art sidecar together using the same filename stem. You can also open an older single-map `.rpgw` as a one-map project.
6. To test on the base game, choose **Export to SD**, select the root of your 3DS SD card and restart. It reads `world.rpgp` and `art.rpga` in `/3ds/MossbellVale/`.

Place glowbells on walkable terrain and leave room beside trail signs, NPCs and stone arches. To return to the built-in project after SD testing, move the exported project file out of the game's folder.

To shape a mountain quickly, select **Mountain plateau** from the region brush menu and drag a rectangle. Use the stairs and ladder ground stamps to cut routes through cliffs. East and west stairs run across the grid. Ladders run up and down. In **Events**, place a **Creature** on a walkable tile and optionally add a message. The pink blob roams within two tiles of that spot. Edit the blob's three 16×16 frames in **Sprite Studio → Pink blob**.

## Playtest in the editor

Press **F5** or click **Playtest** to start on the selected map. **Shift+F5** starts at the project's configured entry point. Move with **arrows** or **WASD**, hold **Shift** to run, and use **Z** or **Enter** to interact. Test your current maps, warps, collision, and art before publishing. Restart or exit without changing the project.

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

The build writes the CIA, 3DSX and editor EXE to `dist`. The 3DS tool installer uses a portable directory. Pass `-ToolRoot` to both 3DS scripts to choose a specific location. To regenerate the graphics and demo world, run `py tools/art/make_assets.py` with Pillow installed. Add `--overrides PATH` to use Sprite Studio PNG exports. The generator accepts numbered tile PNGs such as `23.png`, the whole `player.png` sheet, and individual frames named `player-0.png` through `player-11.png`, `blob-0.png` through `blob-2.png`, or `npc-0.png` through `npc-7.png`.

The RPGW v2 map starts with four ASCII bytes (`RPGW`), followed by five little-endian 16-bit values (format version, width, height, spawn X, spawn Y), four palette bytes (shirt, pants, hair, skin), then two row-major byte arrays for ground and objects. Dimensions are 8–128 tiles per side. Loading a v1 map migrates its existing mountain ledges and stairs to the ground layer.

The RPGA v4 art sidecar starts with the bytes `52 50 47 41 04 00 00 00`, followed by the 128×288 tile sheet, 48×64 player sheet, 48×16 creature sheet and 64×32 townsfolk sheet in scanline RGBA order. Each frame is 16×16. The player sheet has rows for down, left, right and up. The townsfolk sheet has four variants in columns and two poses in rows. Versions 1–3 still load. Their 16×24 player frames are fitted individually to 16×16, and stock art fills sheets or tiles absent from the older format.

RPGP v2 stores up to 16 named maps. Each entry contains an RPGW grid, one movement byte per cell, and up to 64 warp, sign, NPC or creature events. The map IDs are stable within a project, so renaming a map preserves its links. RPGP v1 projects can still open.

The official updater reads `updates/project-catalog-v2.txt`. The catalog is `MVB1 <build> <world-revision> <cia-sha256>`, where build is `major*10000 + minor*100 + patch`. Publish `MossbellVale.cia` on the matching `vMAJOR.MINOR.PATCH` GitHub release and put the official project at `updates/project-<revision>.rpgp`. For a CIA built in the editor, the release's `catalog.txt` is used instead.

The 3DS build uses [devkitARM and libctru](https://github.com/devkitPro/libctru), [libcurl](https://curl.se/), [Mbed TLS](https://github.com/Mbed-TLS/mbedtls), and [zlib](https://zlib.net/). The included CA bundle is [curl's Mozilla CA extract](https://curl.se/docs/caextract.html), distributed under MPL 2.0.
