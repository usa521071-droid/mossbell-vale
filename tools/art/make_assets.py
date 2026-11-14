"""Regenerate the Mossbell Vale pixel art and starter world with Pillow."""

import argparse
from pathlib import Path

from PIL import Image, ImageDraw
from leafgreen_player import frames
from presets import make_presets
from region import make_project
from tileset import make_atlas


ASSETS = Path(__file__).resolve().parents[2] / 'assets'


def make_icon(tiles):
    icon = Image.new('RGBA', (48, 48), '#385449')
    draw = ImageDraw.Draw(icon)
    draw.rounded_rectangle((1, 1, 46, 46), radius=7, fill='#b18a57', outline='#f3ce89')
    draw.rounded_rectangle((3, 3, 44, 44), radius=5, fill='#64905c', outline='#443f49')
    for x, y, tile_id in [(4, 4, 16), (28, 4, 17), (4, 26, 22), (28, 26, 23)]:
        icon.alpha_composite(tiles[tile_id], (x, y))
    icon.alpha_composite(tiles[31].resize((32, 32), Image.Resampling.NEAREST), (9, 8))
    draw.point([(10, 12), (11, 11), (36, 12), (35, 11), (12, 34), (34, 35)], fill='#fff2bc')
    icon.convert('RGB').save(ASSETS / 'icon.png')


TITLE_GLYPHS = {
    'A': ('01110', '11011', '11011', '11111', '11011', '11011', '11011'),
    'B': ('11110', '11011', '11011', '11110', '11011', '11011', '11110'),
    'E': ('11111', '11000', '11000', '11110', '11000', '11000', '11111'),
    'L': ('11000', '11000', '11000', '11000', '11000', '11000', '11111'),
    'M': ('10001', '11011', '11111', '10101', '10001', '10001', '10001'),
    'O': ('01110', '11011', '11011', '11011', '11011', '11011', '01110'),
    'S': ('01111', '11000', '11000', '01110', '00011', '00011', '11110'),
    'V': ('11011', '11011', '11011', '11011', '11011', '01010', '00100'),
}


def title_word(draw, word, origin, size, fill):
    x, y = origin
    for letter in word:
        for row, line in enumerate(TITLE_GLYPHS[letter]):
            for col, pixel in enumerate(line):
                if pixel == '1':
                    left, top = x + col * size, y + row * size
                    draw.rectangle((left, top, left + size - 1, top + size - 1), fill=fill)
        x += 6 * size


def make_banner(preview, tiles):
    banner = preview.crop((100, 96, 612, 352)).resize((256, 128), Image.Resampling.NEAREST)
    overlay = Image.new('RGBA', banner.size)
    draw = ImageDraw.Draw(overlay)
    for x in range(256):
        opacity = max(20, min(210, 254 - x))
        draw.line((x, 0, x, 127), fill=(20, 43, 44, opacity))
    banner.alpha_composite(overlay)

    draw = ImageDraw.Draw(banner)
    draw.rectangle((3, 3, 252, 124), outline='#4c5746')
    draw.rectangle((5, 5, 250, 122), outline='#e0b57a')
    for x, y in [(5, 5), (250, 5), (5, 122), (250, 122)]:
        draw.rectangle((x - 1, y - 1, x + 1, y + 1), fill='#f4d398')
    for word, x, y, hue in [('MOSSBELL', 19, 34, '#f6e1a7'), ('VALE', 54, 62, '#f0ba74')]:
        title_word(draw, word, (x + 2, y + 3), 3, '#283c42')
        title_word(draw, word, (x, y), 3, hue)

    draw.ellipse((180, 28, 230, 78), fill='#274449', outline='#f2ce8f', width=2)
    draw.ellipse((184, 32, 226, 74), fill='#608c66', outline='#203b3e')
    banner.alpha_composite(tiles[31].resize((32, 32), Image.Resampling.NEAREST), (189, 37))
    draw.line((29, 97, 133, 97), fill='#9ca56f')
    draw.polygon([(81, 95), (84, 97), (81, 99), (78, 97)], fill='#f4d398')
    banner.convert('RGB').save(ASSETS / 'banner.png', optimize=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--overrides', type=Path, help='Load sprite PNG overrides from this directory.')
    options = parser.parse_args()
    ASSETS.mkdir(exist_ok=True)
    atlas, tiles = make_atlas()
    player = frames()
    if options.overrides:
        player = studio_override(options.overrides, 'player', (48, 96)) or player
        for index in range(12):
            frame = studio_override(options.overrides, f'player-{index}', (16, 24))
            if frame is not None:
                player.paste(frame, (index % 3 * 16, index // 3 * 24))
        for index in range(len(tiles)):
            art = studio_override(options.overrides, str(index), (16, 16))
            if art is not None:
                if (index < 16 or 32 <= index <= 63 or 108 <= index <= 122) and art.getchannel('A').getextrema() != (255, 255):
                    raise ValueError(f'Ground tile {index} must be opaque')
                tiles[index] = art
                atlas.paste(art, (index % 8 * 16, index // 8 * 16))
    for name, image in [('tiles', atlas), ('player', player)]:
        image.save(ASSETS / f'{name}.png', optimize=True)
        (ASSETS / f'{name}.rgba').write_bytes(image.tobytes())
    preview = make_presets(ASSETS, tiles, player)
    make_project(ASSETS, tiles, player)
    make_icon(tiles)
    make_banner(preview, tiles)


def studio_override(directory, name, expected_size):
    path = directory / f'{name}.png'
    if not path.is_file():
        return None
    with Image.open(path) as image:
        if image.size != expected_size:
            raise ValueError(f'{path.name} must measure {expected_size[0]} × {expected_size[1]} pixels')
        return image.convert('RGBA')


if __name__ == '__main__':
    main()
