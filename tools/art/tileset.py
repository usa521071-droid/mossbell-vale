"""Assemble the 128-tile atlas in the order shared by the editor and game."""

from PIL import Image

from leafgreen_buildings import building_tiles
from leafgreen_terrain import terrain_tiles
from leafgreen_vegetation import GRASS, vegetation


def make_atlas():
    terrain = terrain_tiles(GRASS)
    terrain.update(building_tiles())
    vegetation(terrain)
    if terrain.keys() != set(range(128)):
        raise ValueError(f'Missing or unexpected tile IDs: {sorted(terrain.keys() ^ set(range(128)))}')
    tiles = [terrain[index] for index in range(128)]
    atlas = Image.new('RGBA', (128, 256))
    for index, image in enumerate(tiles):
        if image.size != (16, 16) or image.mode != 'RGBA':
            raise ValueError(f'Tile {index} must be 16 by 16 RGBA')
        if (index < 16 or 32 <= index < 64 or 108 <= index <= 122) and image.getchannel('A').getextrema() != (255, 255):
            raise ValueError(f'Ground tile {index} must be opaque')
        atlas.paste(image, (index % 8 * 16, index // 8 * 16))
    return atlas, tiles
