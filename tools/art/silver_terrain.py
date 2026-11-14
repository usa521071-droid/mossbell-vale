"""Overworld terrain drawn as 8×8 motifs in the 16×16 map atlas."""

from PIL import Image, ImageColor, ImageDraw, ImageOps

INK = '#202433'
CREAM = '#f6f2d8'
GRASS = '#b6d878'
OLIVE = '#72a857'
SAND = '#e8d3a0'
SAND_SHADE = '#b89667'
WATER = ('#a4d1de', '#659bc6', '#4971a9', '#304776')
CAVE = ('#ead4a6', '#c3a578', '#9a705b', '#3e303d')
MOUNTAIN = ('#eee6bd', '#c5b892', '#928263', '#383341')
WOOD = ('#eac784', '#be8e59', '#896747', '#4b3940')
STONE = ('#efebd4', '#c5c7b5', '#939b95', '#5c6570')


def tile(color=None):
    return Image.new('RGBA', (16, 16), color)


def surface(palette, detail=0):
    image = tile(palette[0])
    pen = ImageDraw.Draw(image)
    marks = [
        ((2, 3), (5, 6)),
        ((1, 6), (6, 2)),
        ((2, 2), (5, 5)),
    ][detail % 3]
    for x, y in ((0, 0), (8, 0), (0, 8), (8, 8)):
        pen.point([(x + dx, y + dy) for dx, dy in marks], fill=palette[1])
    return image


def sand(kind):
    if kind == 0:
        return tile(CREAM)
    return surface((CREAM, SAND if kind == 1 else SAND_SHADE), kind)


def water(kind):
    palettes = [(WATER[0], WATER[1], WATER[2]),
                (WATER[1], WATER[2], WATER[0]),
                (WATER[2], WATER[3], WATER[1]),
                (WATER[1], WATER[2], CREAM)]
    base, shadow, crest = palettes[kind]
    image = tile(base)
    pen = ImageDraw.Draw(image)
    for y in (1, 5, 9, 13):
        offset = 2 if y % 8 == 5 else 0
        for x in range(-8 + offset, 16, 8):
            pen.line((x + 1, y + 1, x + 4, y + 1), fill=shadow)
            pen.line((x + 2, y, x + 4, y), fill=crest)
            pen.point((x + 5, y + 2), fill=shadow)
    return image


def boardwalk():
    image = tile(WOOD[1])
    pen = ImageDraw.Draw(image)
    for y in range(0, 16, 4):
        pen.line((0, y, 15, y), fill=WOOD[3])
        pen.line((0, y + 1, 15, y + 1), fill=WOOD[0])
        pen.point(((2 if y % 8 == 0 else 7, y + 2), (10 if y % 8 == 0 else 15, y + 2)), fill=WOOD[2])
    return image


def pavers(palette):
    image = tile(palette[1])
    pen = ImageDraw.Draw(image)
    for y in (0, 4, 8, 12):
        pen.line((0, y + 3, 15, y + 3), fill=palette[2])
        for x in range(-4 if y % 8 else 0, 16, 8):
            pen.line((x, y, x, y + 2), fill=palette[3])
            pen.line((x + 1, y, x + 6, y), fill=palette[0])
    return image


def edge_distance(direction, x, y, depths):
    n, e, s, w = (y - depths[0], 15 - x - depths[1], 15 - y - depths[2], x - depths[3])
    return (n, e, s, w, min(n, w), min(n, e), min(s, e), min(s, w),
            max(n, w), max(n, e), max(s, e), max(s, w))[direction]


def land_edge(base, fill, direction, coast=False):
    image = base.copy()
    draw = ImageDraw.Draw(image)
    ramp = (1, 1, 2, 2, 2, 2, 1, 1)
    for y in range(16):
        for x in range(16):
            depth = (ramp[x % 8], ramp[y % 8], ramp[x % 8], ramp[y % 8])
            d = edge_distance(direction, x, y, depth)
            if d >= 2:
                image.putpixel((x, y), fill.getpixel((x, y)))
            elif d == 1:
                draw.point((x, y), fill=WATER[2] if coast else SAND)
            elif d == 0 and coast:
                draw.point((x, y), fill=WATER[0])
    return image


# A front-facing ledge has deep shadows, light blocks and staggered facet widths.
FACE = (
    '33333333',
    '33333333',
    '33223333',
    '32112333',
    '32111233',
    '32011223',
    '32000123',
    '33211133',
    '33322233',
    '22222222',
)
SIDE = (
    '33333333',
    '33233333',
    '22222122',
    '10011101',
    '22221222',
    '22222222',
)


def rim_tile(direction, mountain, top, outside):
    image = outside.copy() if mountain else tile()
    colors = tuple(ImageColor.getcolor(c, 'RGBA') for c in (MOUNTAIN if mountain else CAVE))
    if not mountain:
        colors = (colors[1], *colors[1:])
    for y in range(16):
        for x in range(16):
            distance = edge_distance(direction, x, y, (2, 2, 2, 2))
            if distance < 0:
                continue
            n, e, s, w = y, 15 - x, 15 - y, x
            face = direction == 2 or (direction in (6, 7, 10, 11) and s <= min(e, w) + 2)
            rows = FACE if face else SIDE
            side = direction in (1, 3) or (direction in (4, 5, 6, 7, 8, 9, 10, 11) and min(e, w) < min(n, s))
            along = y if side and not face else x
            if distance < len(rows):
                image.putpixel((x, y), colors[int(rows[distance][along % 8])])
            else:
                image.putpixel((x, y), top.getpixel((x, y)))
    return image


def stairs(part, base):
    image = base.copy()
    pen = ImageDraw.Draw(image)
    pen.rectangle((2, 0, 13, 15), fill=MOUNTAIN[3])
    pen.rectangle((3, 0, 12, 15), fill=MOUNTAIN[1])
    for y in range(-part % 4, 16, 4):
        pen.line((3, y, 12, y), fill=MOUNTAIN[0])
        if y < 15:
            pen.line((3, y + 1, 12, y + 1), fill=MOUNTAIN[2])
    return image


def boulder(kind):
    image = tile()
    pen = ImageDraw.Draw(image)
    if kind == 124:
        for x, y in ((3, 6), (10, 11)):
            pen.polygon(((x, y + 1), (x + 2, y - 1), (x + 4, y + 1), (x + 3, y + 2), (x + 1, y + 2)),
                        fill=INK)
            pen.line((x + 1, y + 1, x + 2, y), fill=STONE[1])
        return image
    if kind == 125:
        for x, y, height in ((2, 7, 5), (6, 2, 10), (10, 5, 7)):
            pen.polygon(((x, y + height), (x, y + 3), (x + 3, y), (x + 5, y + 3),
                         (x + 5, y + height), (x + 3, y + height + 1)), fill='#966eaf', outline=INK)
            pen.line(((x + 1, y + height - 1), (x + 1, y + 3), (x + 3, y + 1)), fill='#ecc4db')
        return image
    if kind == 127:
        pen.polygon(((3, 14), (3, 5), (6, 2), (10, 2), (13, 5), (13, 14)),
                    fill=STONE[2], outline=INK)
        pen.polygon(((4, 5), (6, 3), (10, 3), (12, 5), (10, 6), (4, 6)), fill=STONE[0])
        pen.line(((7, 7), (9, 9), (7, 11), (9, 12)), fill=INK)
        return image
    mid, light = (CAVE[2], CAVE[0]) if kind in (19, 21) else (MOUNTAIN[2], MOUNTAIN[0])
    top = 2 if kind == 126 else 5
    pen.polygon(((1, 10), (5, top + 1), (9, top), (13, top + 3), (15, 10),
                 (12, 14), (4, 14), (1, 12)), fill=mid, outline=INK)
    pen.polygon(((3, 10), (6, top + 2), (9, top + 1), (11, top + 3), (7, 10)), fill=light)
    if kind in (20, 21):
        pen.polygon(((9, 13), (11, 10), (14, 10), (15, 13), (14, 15), (10, 15)), fill=mid, outline=INK)
        pen.line((11, 11, 13, 11), fill=light)
    return image


def markers(kind):
    image = tile()
    pen = ImageDraw.Draw(image)
    if kind == 31:
        pen.polygon(((5, 3), (10, 3), (10, 4), (12, 6), (12, 9), (13, 10),
                     (11, 12), (9, 12), (9, 13), (6, 13), (6, 12), (4, 12), (2, 10),
                     (3, 9), (3, 6), (5, 4)), fill='#e6aa47', outline=INK)
        pen.line((6, 5, 9, 5), fill=CREAM)
        pen.line((5, 6, 5, 8), fill=CREAM)
        pen.line((5, 10, 10, 10), fill=INK)
    elif kind == 30:
        pen.rectangle((7, 7, 9, 14), fill=INK)
        pen.rectangle((1, 2, 14, 9), fill=WOOD[0], outline=INK)
        pen.line((2, 8, 13, 8), fill=WOOD[1])
        pen.line(((9, 4), (11, 5), (9, 6)), fill=INK)
        pen.line((4, 5, 10, 5), fill=INK)
    else:
        pen.polygon(((1, 14), (1, 7), (5, 3), (10, 3), (14, 7), (14, 14),
                     (10, 14), (10, 8), (8, 7), (6, 8), (6, 14)), fill=STONE[2], outline=INK)
        pen.line(((2, 7), (5, 4), (10, 4), (13, 7)), fill=CREAM)
        pen.line((3, 9, 3, 13), fill=CREAM)
    return image


def old_wood(kind):
    image = tile()
    pen = ImageDraw.Draw(image)
    if kind == 24:
        pen.polygon(((1, 7), (4, 5), (13, 5), (15, 8), (15, 11), (13, 13),
                     (5, 13), (1, 11)), fill=WOOD[1], outline=INK)
        pen.rectangle((3, 7, 11, 8), fill=WOOD[0])
        pen.ellipse((10, 6, 14, 12), fill=WOOD[0], outline=INK)
        pen.point((12, 9), fill=INK)
    else:
        pen.rectangle((4, 7, 11, 12), fill=WOOD[1], outline=INK)
        pen.ellipse((3, 4, 12, 9), fill=WOOD[0], outline=INK)
        pen.line((6, 6, 9, 6), fill=WOOD[1])
    return image


def terrain_tiles(grass):
    meadow = tile(grass)
    paths = [sand(i) for i in range(3)]
    waters = [water(i) for i in range(4)]
    mountain_floor = surface(MOUNTAIN)
    mountain_top = surface((MOUNTAIN[1], MOUNTAIN[2]), 1)
    cave_floor = surface(CAVE)
    cave_top = surface((CAVE[1], CAVE[2]), 2)
    terrain = {
        4: paths[0], 5: paths[1], 6: paths[2], 7: boardwalk(),
        12: pavers(WOOD), 15: pavers((CREAM, '#d8dbb6', '#aab6a0', '#758b82')),
        24: old_wood(24), 28: old_wood(28),
        56: cave_floor, 57: surface(CAVE, 1), 58: cave_top,
        59: surface(CAVE, 2), 60: mountain_floor, 61: mountain_top,
        62: surface((grass, OLIVE), 2), 63: pavers(STONE),
    }
    terrain.update(enumerate(waters, start=8))
    for index in (19, 20, 21, 124, 125, 126, 127):
        terrain[index] = boulder(index)
    for index in (29, 30, 31):
        terrain[index] = markers(index)
    for index in range(12):
        terrain[32 + index] = land_edge(meadow, paths[0], index)
        terrain[44 + index] = land_edge(meadow, waters[0], index, coast=True)
        terrain[96 + index] = rim_tile(index, False, cave_top, cave_floor)
        terrain[108 + index] = rim_tile(index, True, mountain_top, mountain_floor)
    for index in range(3):
        terrain[120 + index] = stairs(index, mountain_floor)
    return terrain
