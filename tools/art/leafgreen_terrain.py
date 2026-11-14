"""Terrain and scenery pixels for the Mossbell Vale atlas."""

from PIL import Image, ImageColor, ImageDraw

SAND = '#ece3a0'
WATER = ('#8ac9c7', '#639fbb', '#4f82a8', '#73b3c5')
CAVE = '#a9a48d'
CAVE_TOP = '#bf9893'
GRANITE = '#e1d3a1'
ROCK_TOP = '#b3b4a1'


def tile(color=None):
    return Image.new('RGBA', (16, 16), color)


def sand(kind=0):
    image = tile(SAND)
    pen = ImageDraw.Draw(image)
    spots = ((3, 2), (12, 4), (7, 9), (1, 13), (11, 14))
    if kind == 1:
        spots = ((9, 1), (2, 3), (13, 8), (5, 11), (3, 15))
    if kind == 2:
        spots = ((2, 2), (9, 5), (4, 11), (14, 14))
    pen.point(spots, fill='#ccba76')
    for x, y in spots[::2]:
        pen.point((x+1, y+1), fill='#f6efbb')
    return image


def water(kind):
    image = tile(WATER[kind])
    pen = ImageDraw.Draw(image)
    shade = ('#72babb', '#548ea9', '#45739c', '#609cb8')[kind]
    glint = ('#aae0cd', '#86bfd0', '#6da8c0', '#9dd4d8')[kind]
    for x, y, width in ((1, 4, 4), (10, 8, 5), (4, 14, 3)):
        pen.line((x, y, x+width-1, y), fill=shade)
        pen.line((x+1, y-1, x+width-2, y-1), fill=glint)
    if kind == 3:
        pen.line(((0, 10), (4, 10), (6, 9), (10, 9), (12, 8), (15, 8)), fill=glint)
    return image


def boardwalk():
    image = tile('#a16e48')
    pen = ImageDraw.Draw(image)
    for left in range(0, 16, 4):
        pen.line((left, 0, left, 15), fill='#795341')
        pen.line((left+1, 0, left+1, 15), fill='#d4a16d')
        pen.line((left+2, 0, left+2, 15), fill='#c18c59')
        pen.point(((left+1, 2), (left+1, 13)), fill='#715448')
    pen.line((0, 0, 15, 0), fill='#d8a977')
    pen.line((0, 15, 15, 15), fill='#744e3a')
    return image


def rocky(base, shade, light, marks):
    image = tile(base)
    pen = ImageDraw.Draw(image)
    for x, y, width in marks:
        pen.line((x, y, x+width, y), fill=shade)
        pen.line((x, y-1, x+width-1, y-1), fill=light)
    return image


def strata(base, shadow, light):
    image = tile(base)
    pen = ImageDraw.Draw(image)
    pen.line(((0, 8), (2, 8), (4, 9), (7, 9), (9, 7), (12, 7), (14, 8), (15, 8)), fill=shadow)
    pen.line(((1, 7), (3, 7), (5, 8), (7, 8)), fill=light)
    pen.line(((0, 1), (3, 1), (5, 2), (7, 2)), fill=light)
    pen.line(((10, 12), (12, 12), (14, 13)), fill=light)
    pen.point(((7, 5), (4, 14)), fill=shadow)
    return image


def pavers(base, side, light, mortar):
    image = tile(base)
    pen = ImageDraw.Draw(image)
    for row in range(0, 16, 4):
        pen.line((0, row+3, 15, row+3), fill=mortar)
        for left in range(-4 if row % 8 else 0, 16, 8):
            pen.line((left, row, left, row+2), fill=side)
            pen.line((left+1, row, left+6, row), fill=light)
    return image


def blend_edge(base, fill, direction, rim, fringe):
    image = base.copy()
    ramp = (2, 2, 3, 3, 3, 2, 2, 2, 2, 3, 3, 3, 2, 2, 2, 2)
    width = 3
    for y in range(16):
        for x in range(16):
            n, e, s, w = y-ramp[x], 15-x-ramp[y], 15-y-ramp[x], x-ramp[y]
            distances = (
                n, e, s, w,
                min(n, w, x+y-6), min(n, e, 15-x+y-6),
                min(s, e, 30-x-y-6), min(s, w, x+15-y-6),
                max(n, w), max(n, e), max(s, e), max(s, w),
            )
            distance = distances[direction]
            if distance >= width:
                image.putpixel((x, y), fill.getpixel((x, y)))
            elif distance >= 1:
                image.putpixel((x, y), rim)
            elif distance == 0:
                image.putpixel((x, y), fringe)
    return image


# A pair of broad, irregular facets with narrow cracks. Read from the foot of the face.
FACE_ROWS = (
    '1110111111001111',
    '2211122221012222',
    '3221122220112233',
    '3321122201112333',
    '3331222111122333',
    '3332222122223333',
    '3442222232223344',
    '4443344443344444',
)


def rim_tile(direction, mountain, top, outside):
    image = outside.copy() if mountain else tile()
    palette = [ImageColor.getcolor(color, 'RGBA') for color in (
        ('#56605d', '#69766b', '#848d7b', '#a8ad91', '#c6c8aa')
        if mountain else ('#613e47', '#7f5156', '#9b6260', '#b47b72', '#d5a198')
    )]
    side = direction in (1, 3)
    front = direction in (2, 6, 7, 10, 11)
    for y in range(16):
        for x in range(16):
            n, e, s, w = y, 15-x, 15-y, x
            distances = (n, e, s, w, min(n, w), min(n, e), min(s, e),
                         min(s, w), max(n, w), max(n, e), max(s, e), max(s, w))
            d = distances[direction]
            along = y if side else x
            if direction in (4, 5, 6, 7):
                d = min(d, (x+y if direction == 4 else 15-x+y if direction == 5
                            else 30-x-y if direction == 6 else x+15-y)//2-1)
            d -= 1 if 5 <= along % 8 <= 6 else 0
            face = 10 if front else 7 if side else 7
            if d < 2:
                continue
            if d <= face:
                row = (d-2) * (len(FACE_ROWS)-1) // (face-2)
                shade = int(FACE_ROWS[row][along])
                image.putpixel((x, y), palette[shade])
            elif mountain or d <= face + 4:
                image.putpixel((x, y), top.getpixel((x, y)))
    return image


def stairs(part, base):
    image = base.copy()
    pen = ImageDraw.Draw(image)
    pen.rectangle((1, 0, 14, 15), fill='#6a685d')
    pen.rectangle((2, 0, 13, 15), fill='#ac9d7c')
    pen.rectangle((3, 0, 12, 15), fill='#c5b087')
    for y in range((2-part) % 3, 16, 3):
        pen.line((3, y, 12, y), fill='#efe0ae')
        if y < 15:
            pen.line((4, y+1, 12, y+1), fill='#897859')
    return image


def facets(kind):
    image = tile()
    pen = ImageDraw.Draw(image)
    if kind == 124:
        for x, y, width in ((2, 10, 4), (8, 5, 5), (10, 12, 3)):
            pen.polygon(((x, y), (x+1, y-1), (x+width-2, y-2), (x+width, y),
                         (x+width-1, y+1), (x+1, y+1)), fill='#686b65')
            pen.line((x+1, y-1, x+width-2, y-1), fill='#beac90')
        return image
    if kind == 125:
        pen.ellipse((2, 11, 14, 14), fill=(57, 52, 68, 91))
        for shape in (
            ((2, 11), (3, 6), (6, 8), (7, 11), (6, 13), (3, 12)),
            ((6, 11), (7, 4), (10, 2), (12, 5), (12, 10), (10, 13)),
            ((10, 12), (11, 8), (14, 6), (15, 9), (14, 12), (12, 13)),
        ):
            pen.polygon(shape, fill='#795f96', outline='#4a445e')
        pen.line(((8, 10), (9, 5), (10, 4)), fill='#d1a7c9')
        pen.point(((4, 8), (12, 9)), fill='#b495c1')
        return image
    if kind == 127:
        pen.ellipse((3, 11, 15, 15), fill=(51, 56, 62, 89))
        pen.polygon(((4, 12), (4, 3), (7, 1), (11, 1), (13, 4), (13, 12),
                     (11, 14), (6, 14)), fill='#8d9390', outline='#535b60')
        pen.polygon(((5, 4), (7, 2), (10, 2), (12, 4), (10, 5), (5, 5)), fill='#c4bfa6')
        pen.line((6, 6, 6, 11), fill='#b9b6a0')
        pen.line(((7, 8), (9, 9), (10, 12)), fill='#676f72')
        return image
    warm = kind in (19, 21)
    edge, dark, main, light = (
        ('#62484b', '#987172', '#bd9690', '#dfb1a1') if warm else
        ('#565e5e', '#80877b', '#acaa92', '#d4c6a7')
    )
    peak = 3 if kind == 126 else 5
    pen.ellipse((2, 10, 15, 14), fill=(40, 49, 55, 73))
    pen.polygon(((1, 10), (3, 7), (5, peak+1), (9, peak), (12, peak+2), (14, 9),
                 (13, 12), (9, 14), (4, 13)), fill=dark, outline=edge)
    pen.polygon(((3, 9), (5, peak+2), (9, peak+1), (12, peak+3), (11, 8),
                 (7, 9), (6, 11), (3, 11)), fill=main)
    pen.line(((4, 8), (6, peak+2), (9, peak+1)), fill=light)
    pen.line(((7, 11), (10, 10), (12, 7)), fill=edge)
    if kind in (20, 21):
        pen.polygon(((10, 11), (12, 9), (14, 9), (15, 11), (14, 13), (11, 13)), fill=dark, outline=edge)
        pen.line((12, 10, 14, 10), fill=light)
    return image


def markers(kind):
    image = tile()
    pen = ImageDraw.Draw(image)
    if kind == 31:
        pen.ellipse((2, 11, 13, 14), fill=(44, 76, 63, 75))
        pen.line(((7, 12), (7, 8), (8, 6)), fill='#35684e')
        pen.line(((9, 11), (10, 9), (12, 9)), fill='#5fa664')
        pen.line(((6, 11), (5, 9), (3, 9)), fill='#5fa664')
        pen.polygon(((4, 5), (6, 3), (10, 3), (12, 5), (12, 7), (10, 9),
                     (6, 9), (4, 7)), fill='#e1a55b', outline='#785746')
        pen.polygon(((5, 5), (7, 4), (10, 4), (11, 6), (10, 7), (5, 7)), fill='#ffdf82')
        pen.line((7, 5, 9, 5), fill='#fff2be')
        return image
    if kind == 30:
        pen.ellipse((3, 11, 14, 14), fill=(51, 71, 61, 78))
        pen.rectangle((6, 6, 9, 13), fill='#9c7053', outline='#574747')
        pen.rectangle((1, 2, 14, 9), fill='#d5a572', outline='#634a43')
        pen.line((2, 3, 13, 3), fill='#f0c795')
        pen.line(((5, 6), (10, 6), (9, 5)), fill='#86573c')
        pen.point((9, 7), fill='#86573c')
        return image
    pen.ellipse((2, 10, 15, 15), fill=(62, 67, 80, 100))
    pen.polygon(((1, 13), (1, 8), (3, 5), (6, 3), (10, 3), (13, 5), (14, 9), (14, 13),
                 (10, 13), (10, 9), (8, 7), (6, 9), (6, 13)), fill='#77858a', outline='#495961')
    pen.line(((2, 8), (4, 5), (6, 4), (10, 4), (12, 6)), fill='#c3c4aa')
    pen.line(((4, 7), (6, 6), (10, 6), (12, 8), (12, 12)), fill='#9fa791')
    pen.point(((5, 5), (10, 8)), fill='#6c9b67')
    return image


def old_wood(kind):
    image = tile()
    pen = ImageDraw.Draw(image)
    if kind == 24:
        pen.ellipse((2, 9, 15, 14), fill=(48, 65, 53, 80))
        pen.polygon(((1, 6), (10, 4), (14, 6), (15, 9), (13, 12), (5, 13),
                     (1, 11)), fill='#976746', outline='#524741')
        pen.line(((2, 7), (9, 5), (12, 6)), fill='#c39a65')
        pen.ellipse((9, 7, 13, 11), fill='#654c45', outline='#d7ac70')
    else:
        pen.ellipse((2, 10, 14, 14), fill=(54, 69, 59, 74))
        pen.rectangle((4, 6, 11, 12), fill='#996c49', outline='#66503e')
        pen.ellipse((3, 4, 12, 9), fill='#d7ae78', outline='#825c47')
        pen.arc((5, 6, 10, 8), 0, 270, fill='#a37651')
    return image


def terrain_tiles(grass):
    meadow = tile(grass)
    paths = [sand(i) for i in range(3)]
    waters = [water(i) for i in range(4)]
    clip_base = rocky(GRANITE, '#bfab83', '#f1e3b7', ((2, 4, 2), (11, 11, 2), (5, 14, 1)))
    cave_top = strata(CAVE_TOP, '#b58c87', '#cca49c')
    rock_top = strata(ROCK_TOP, '#a0a28d', '#c0c0ab')
    cave_floor = rocky(CAVE, '#89816f', '#c4ba9d', ((2, 5, 2), (11, 8, 2), (5, 13, 1)))
    stone_walk = pavers('#9ea9ae', '#74828a', '#c1c8c5', '#86929a')
    terrain = {
        4: paths[0], 5: paths[1], 6: paths[2], 7: boardwalk(),
        12: pavers('#b2a588', '#8b806d', '#d7c6a0', '#9f9178'),
        15: pavers('#b1b8b3', '#929c9c', '#d3d5c9', '#9fa8a4'),
        24: old_wood(24), 28: old_wood(28),
        56: cave_floor,
        57: rocky(CAVE, '#827b6b', '#d2c4a4', ((5, 2, 2), (9, 6, 1), (2, 11, 3), (12, 14, 2))),
        58: cave_top,
        59: rocky('#b0a586', '#95866c', '#c9bd9b', ((4, 6, 3), (9, 11, 2))),
        60: clip_base, 61: rock_top,
        62: rocky(grass, '#69ad84', '#9ed5a3', ((4, 5, 1), (11, 12, 1))),
        63: stone_walk,
    }
    terrain.update({8+i: image for i, image in enumerate(waters)})
    for id in (19, 20, 21, 124, 125, 126, 127):
        terrain[id] = facets(id)
    for id in (29, 30, 31):
        terrain[id] = markers(id)
    path_rim, path_fringe, shore_rim, shore_fringe = (
        ImageColor.getcolor(color, 'RGBA') for color in ('#dfd790', '#a2d4a0', '#e5d89c', '#bdd79c')
    )
    for index in range(12):
        terrain[32+index] = blend_edge(meadow, paths[0], index, path_rim, path_fringe)
        terrain[44+index] = blend_edge(meadow, waters[0], index, shore_rim, shore_fringe)
        terrain[96+index] = rim_tile(index, False, cave_top, cave_floor)
        terrain[108+index] = rim_tile(index, True, rock_top, clip_base)
    for index in range(3):
        terrain[120+index] = stairs(index, clip_base)
    return terrain
