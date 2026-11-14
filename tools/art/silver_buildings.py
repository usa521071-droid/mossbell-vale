"""Original 16-pixel architecture using GBC-style flat tile sub-palettes."""

from PIL import Image, ImageDraw


INK = '#202433'
CREAM = '#f6f2d8'
ROOF = '#6181a0'
ROOF_LIGHT = '#a3bacf'
STONE = '#aa956f'
GLASS = '#83a7bd'


def empty(width=16, height=16):
    return Image.new('RGBA', (width, height))


def cells(image):
    width, height = image.size
    return [image.crop((x, y, x + 16, y + 16))
            for y in range(0, height, 16) for x in range(0, width, 16)]


def blue_roof():
    image = empty(48, 48)
    draw = ImageDraw.Draw(image)
    draw.polygon([(0, 11), (4, 11), (4, 8), (8, 8), (8, 5), (12, 5), (12, 2),
                  (35, 2), (35, 5), (39, 5), (39, 8), (43, 8), (43, 11),
                  (47, 11), (47, 45), (45, 47), (2, 47), (0, 45)], fill=INK)
    draw.polygon([(2, 13), (6, 13), (6, 10), (10, 10), (10, 7), (14, 7), (14, 4),
                  (33, 4), (33, 7), (37, 7), (37, 10), (41, 10), (41, 13),
                  (45, 13), (45, 43), (2, 43)], fill=ROOF)
    draw.line([(3, 14), (6, 14), (6, 11), (11, 11), (11, 8), (15, 8), (15, 5),
               (32, 5), (32, 8), (36, 8), (36, 11), (40, 11), (40, 14), (44, 14)], fill=ROOF_LIGHT, width=2)
    for row in (15, 23, 31, 39):
        draw.line((3, row, 44, row), fill=INK)
        draw.line((3, row+1, 44, row+1), fill=ROOF_LIGHT)
        for x in range(7 if row % 16 == 7 else 11, 46, 8):
            draw.line((x, row+2, x, row+7), fill=INK)
            draw.point((x+1, row+2), fill=ROOF_LIGHT)
    draw.rectangle((2, 42, 45, 44), fill=ROOF_LIGHT)
    draw.line((3, 44, 44, 44), fill=INK)
    draw.line((4, 46, 43, 46), fill=ROOF)
    return image


def facade_base():
    image = Image.new('RGBA', (48, 32), CREAM)
    draw = ImageDraw.Draw(image)
    draw.rectangle((0, 0, 47, 2), fill=INK)
    draw.rectangle((1, 3, 46, 4), fill=STONE)
    for x in (0, 47):
        draw.line((x, 0, x, 31), fill=INK, width=2)
    for x in (2, 45):
        draw.line((x, 5, x, 26), fill=STONE)
    for y in (10, 18, 26):
        draw.line((3, y, 44, y), fill=STONE)
    draw.rectangle((1, 28, 46, 29), fill=STONE)
    draw.rectangle((0, 30, 47, 31), fill=INK)
    return image


def window(draw, x, y, width=10):
    right = x + width - 1
    draw.rectangle((x, y, right, y + 8), fill=INK)
    draw.rectangle((x + 1, y + 1, right - 1, y + 5), fill=GLASS)
    draw.line((x + width // 2, y + 1, x + width // 2, y + 5), fill=INK)
    draw.line((x + 1, y + 1, x + 2, y + 1), fill=CREAM)
    draw.line((x, y + 8, right, y + 8), fill=STONE)


def entrance(draw, x, y):
    draw.rectangle((x + 2, y, x + 13, y + 14), fill=INK)
    draw.rectangle((x + 3, y + 2, x + 12, y + 12), fill=STONE)
    draw.rectangle((x + 4, y + 2, x + 11, y + 7), fill=GLASS)
    draw.line((x + 7, y + 2, x + 7, y + 7), fill=INK)
    draw.line((x + 4, y + 8, x + 11, y + 8), fill=INK)
    draw.line((x + 9, y + 10, x + 10, y + 10), fill=CREAM)
    draw.line((x + 1, y + 14, x + 14, y + 14), fill=STONE)


def facades():
    base = facade_base()
    full = base.copy()
    draw = ImageDraw.Draw(full)
    for x in (4, 34):
        window(draw, x, 19)
    entrance(draw, 16, 16)

    door = base.crop((16, 16, 32, 32))
    entrance(ImageDraw.Draw(door), 0, 0)
    glass = base.crop((16, 16, 32, 32))
    window(ImageDraw.Draw(glass), 2, 3, 12)
    return cells(full), door, glass


def red_roof():
    image = Image.new('RGBA', (16, 16), '#af5b68')
    draw = ImageDraw.Draw(image)
    for y in (0, 8):
        draw.line((0, y, 15, y), fill=INK)
        draw.line((0, y + 1, 15, y + 1), fill='#db947e')
        for x in ((3, 11) if y == 0 else (7, 15)):
            draw.line((x, y + 2, x, y + 7), fill='#814754')
            draw.point((x+1, y+2), fill='#db947e')
    return image


def awning():
    image = Image.new('RGBA', (16, 16), INK)
    draw = ImageDraw.Draw(image)
    draw.line((0, 1, 15, 1), fill='#bb8a55')
    for x in (0, 8):
        draw.rectangle((x, 2, x + 3, 11), fill='#bd6269')
        draw.rectangle((x + 4, 2, x + 7, 11), fill=CREAM)
        draw.line((x, 12, x + 2, 12), fill='#bb8a55')
        draw.line((x+4, 12, x + 6, 12), fill='#bb8a55')
    return image


def chimney():
    image = empty()
    draw = ImageDraw.Draw(image)
    draw.polygon([(3, 4), (5, 2), (12, 2), (14, 4), (14, 12), (12, 14), (4, 14), (3, 12)], fill=INK)
    draw.rectangle((4, 5, 12, 12), fill=STONE)
    draw.line((5, 4, 11, 4), fill=CREAM)
    draw.line((5, 7, 5, 12), fill=CREAM)
    draw.line((6, 9, 12, 9), fill=INK)
    return image


def fence_post(draw, x):
    draw.polygon([(x, 5), (x+1, 5), (x+1, 3), (x+5, 3), (x+5, 5), (x+6, 5),
                  (x+6, 14), (x, 14)], fill=INK)
    draw.rectangle((x+1, 6, x+5, 12), fill='#939a8b')
    draw.rectangle((x+2, 5, x+4, 7), fill=CREAM)
    draw.line((x+1, 7, x+1, 11), fill=CREAM)


def fence(kind):
    image = empty()
    draw = ImageDraw.Draw(image)
    if kind == 3:
        draw.rectangle((6, 0, 10, 13), fill=INK)
        draw.line((7, 0, 7, 12), fill='#939a8b')
        fence_post(draw, 5)
        return image
    left, right = [(5, 15), (0, 15), (0, 12)][kind]
    draw.rectangle((left, 7, right, 11), fill=INK)
    draw.line((left, 8, right, 8), fill='#939a8b')
    for x in [(5,), (0, 8), (0, 6)][kind]:
        fence_post(draw, x)
    return image


def cast_shadow(east):
    image = empty()
    draw = ImageDraw.Draw(image)
    if east:
        draw.polygon([(0, 0), (3, 0), (3, 13), (1, 15), (0, 15)], fill='#72a857')
    else:
        draw.polygon([(0, 0), (15, 0), (15, 2), (13, 3), (0, 3)], fill='#72a857')
    return image


def building_tiles():
    facade_pieces, door, glass = facades()
    roof_pieces = cells(blue_roof())
    tiles = {25: fence(1), 26: awning(), 27: red_roof()}
    tiles.update((64 + index, image) for index, image in enumerate(roof_pieces))
    tiles.update((73 + index, image) for index, image in enumerate(facade_pieces))
    tiles.update({79: door, 80: glass, 81: chimney(),
                  82: fence(0), 83: fence(1), 84: fence(2), 85: fence(3),
                  86: cast_shadow(True), 87: cast_shadow(False)})
    return tiles
