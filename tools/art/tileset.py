"""Assemble the tile atlas in the order shared by the editor and game."""

from PIL import Image

from silver_buildings import building_tiles
from silver_terrain import terrain_tiles
from silver_vegetation import GRASS, vegetation
from vertical_route import vertical_route


def make_atlas():
    terrain = terrain_tiles(GRASS)
    terrain.update(building_tiles())
    vegetation(terrain)
    vertical_route(terrain)
    expected = set(range(144))
    if terrain.keys() != expected:
        raise ValueError(f'Missing or unexpected tile IDs: {sorted(terrain.keys() ^ expected)}')
    tiles = [terrain[index] for index in range(144)]
    atlas = Image.new('RGBA', (128, 288))
    for index, image in enumerate(tiles):
        if image.size != (16, 16) or image.mode != 'RGBA':
            raise ValueError(f'Tile {index} must be 16 by 16 RGBA')
        if (index < 16 or 32 <= index < 64 or 108 <= index <= 122 or index >= 128) and image.getchannel('A').getextrema() != (255, 255):
            raise ValueError(f'Ground tile {index} must be opaque')
        for y in (0, 8):
            for x in (0, 8):
                colors = image.crop((x, y, x + 8, y + 8)).getcolors(maxcolors=4)
                if colors is None or any(color[3] not in (0, 255) for _, color in colors):
                    raise ValueError(f'Tile {index} exceeds the 8×8 palette at ({x}, {y})')
        atlas.paste(image, (index % 8 * 16, index // 8 * 16))
    return atlas, tiles
