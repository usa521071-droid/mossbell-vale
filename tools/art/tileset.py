"""Assemble the tile atlas in the order shared by the editor and game."""

from PIL import Image

from leafgreen_buildings import building_tiles
from leafgreen_terrain import terrain_tiles
from leafgreen_vegetation import GRASS, vegetation
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
        atlas.paste(image, (index % 8 * 16, index // 8 * 16))
    return atlas, tiles
