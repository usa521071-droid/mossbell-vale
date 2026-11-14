"""Draw connected stone stairs and ladders for the 16×16 ground tile atlas."""

from PIL import Image, ImageDraw

from silver_terrain import CAVE, MOUNTAIN


def side_stairs(terrain, east, cave=False):
    base = terrain[56 if cave else 60]
    colors = CAVE if cave else MOUNTAIN
    width = 32 if cave else 48
    image = Image.new('RGBA', (width, 16))
    for x in range(0, width, 16):
        image.paste(base, (x, 0))
    draw = ImageDraw.Draw(image)
    for x in range(0, width, 4):
        y = 8 - x * 5 // width
        draw.rectangle((x, y, x + 3, y + 8), fill=colors[3])
        draw.rectangle((x, y + 1, x + 2, y + 7), fill=colors[1])
        draw.line((x, y + 1, x, y + 6), fill=colors[0])
        draw.line((x + 2, y + 2, x + 2, y + 7), fill=colors[2])
    if not east:
        image = image.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    tiles = [image.crop((x, 0, x + 16, 16)) for x in range(0, width, 16)]
    return tiles if east else list(reversed(tiles))


def ladder(terrain, cave=False):
    base = terrain[56 if cave else 60]
    ledge = terrain[58 if cave else 61]
    colors = CAVE if cave else MOUNTAIN
    image = Image.new('RGBA', (16, 48))
    for y in (0, 16, 32):
        image.paste(ledge if y == 16 else base, (0, y))
    draw = ImageDraw.Draw(image)
    for y in (13, 31):
        draw.rectangle((0, y, 15, y + 1), fill=colors[3])
    for x in (3, 11):
        draw.rectangle((x, 6, x + 2, 41), fill=colors[3])
        draw.line((x + 1, 7, x + 1, 40), fill=colors[0])
    for y in range(9, 40, 5):
        draw.line((5, y + 1, 11, y + 1), fill=colors[3])
        draw.line((5, y, 11, y), fill=colors[1])
    return [image.crop((0, y, 16, y + 16)) for y in (0, 16, 32)]


def vertical_route(terrain):
    tiles = side_stairs(terrain, True) + side_stairs(terrain, False)
    tiles += ladder(terrain) + ladder(terrain, cave=True)
    tiles += side_stairs(terrain, True, cave=True) + side_stairs(terrain, False, cave=True)
    terrain.update(enumerate(tiles, start=128))
