"""Original vegetation. Cluster coordinates are pixels on the 16-pixel map grid."""

from PIL import Image, ImageDraw


GRASS = '#83c99f'
GRASS_LOW = '#62b48b'
GRASS_HIGH = '#a8daac'
OUTLINE = '#245b4a'
DEEP = '#327b52'
SHADE = '#459b58'
LEAF = '#62b55f'
LIGHT = '#8ac66b'
SUN = '#b1d985'
CAST_SHADOW = (43, 98, 76, 83)


def meadow(detail=0, base=GRASS):
    tile = Image.new('RGBA', (16, 16), base)
    draw = ImageDraw.Draw(tile)
    for x, y in [(), ((4, 6), (12, 11)), ((4, 5), (10, 11)), ((5, 11),)][detail]:
        draw.point([(x - 1, y - 1), (x, y), (x + 2, y - 2)], fill=GRASS_LOW)
        draw.point([(x, y - 1), (x + 1, y - 2)], fill=GRASS_HIGH)
    if detail == 2:
        draw.point([(5, 4), (7, 4), (6, 3), (6, 5)], fill='#f8e9c9')
        draw.point((6, 4), fill='#e6b666')
    return tile


def crown():
    tree = Image.new('RGBA', (48, 48))
    draw = ImageDraw.Draw(tree)
    draw.polygon([(14, 40), (21, 36), (31, 34), (38, 35), (38, 37), (44, 37), (47, 40),
                  (47, 42), (44, 45), (36, 46), (29, 46), (25, 44), (18, 44), (15, 43)], fill=CAST_SHADOW)
    draw.polygon([(18, 27), (29, 26), (29, 32), (27, 34), (26, 38), (27, 40), (31, 41),
                  (29, 43), (24, 42), (21, 44), (17, 43), (15, 44), (15, 42), (18, 39), (19, 33)], fill='#4d5340')
    draw.polygon([(21, 28), (28, 29), (25, 34), (24, 39), (26, 41), (24, 41), (21, 42),
                  (20, 40), (17, 42), (20, 38), (21, 34)], fill='#967451')
    draw.line([(23, 31), (22, 33), (22, 36), (21, 37), (21, 39)], fill='#c09a65')
    draw.point([(23, 40), (18, 41)], fill='#a9875d')

    draw.polygon([(18, 2), (28, 2), (32, 4), (35, 4), (39, 6), (42, 9), (42, 11),
                  (45, 14), (46, 18), (45, 21), (46, 24), (44, 27), (40, 28), (39, 31),
                  (35, 33), (30, 33), (28, 32), (24, 34), (19, 34), (15, 32), (11, 32),
                  (8, 30), (6, 30), (3, 27), (3, 25), (1, 23), (1, 18), (2, 15), (5, 12),
                  (5, 9), (9, 6), (12, 6), (14, 4)], fill=OUTLINE)
    draw.polygon([(18, 4), (27, 4), (31, 6), (35, 6), (38, 8), (40, 10), (40, 12), (43, 15),
                  (44, 18), (43, 21), (44, 24), (42, 26), (39, 26), (37, 30), (34, 31),
                  (30, 31), (28, 29), (24, 32), (20, 32), (16, 30), (12, 30), (9, 28),
                  (7, 28), (5, 26), (5, 24), (3, 22), (3, 18), (5, 15), (7, 13), (7, 10),
                  (10, 8), (13, 8), (15, 6)], fill=DEEP)
    draw.polygon([(17, 5), (25, 4), (29, 6), (34, 7), (37, 9), (38, 13), (41, 15), (41, 19),
                  (38, 21), (39, 24), (35, 27), (31, 27), (29, 25), (25, 28), (21, 29),
                  (17, 27), (12, 27), (10, 25), (7, 25), (6, 22), (4, 20), (6, 16),
                  (9, 14), (8, 11), (12, 9), (15, 9)], fill=SHADE)
    draw.polygon([(17, 6), (23, 5), (27, 7), (28, 9), (32, 9), (35, 11), (35, 15), (38, 16),
                  (37, 19), (34, 21), (29, 21), (28, 24), (25, 26), (21, 26), (19, 23),
                  (16, 23), (14, 25), (10, 24), (9, 21), (6, 21), (6, 18), (9, 16),
                  (11, 16), (11, 12), (13, 10), (16, 10)], fill=LEAF)

    # Leaf groups interlock with the canopy. Most highlights are horizontal rims.
    draw.polygon([(17, 7), (21, 6), (25, 7), (26, 9), (24, 9), (23, 11), (20, 10), (17, 11),
                  (15, 10)], fill=LIGHT)
    draw.polygon([(12, 12), (14, 11), (19, 11), (19, 13), (22, 14), (20, 16), (16, 16),
                  (15, 18), (12, 17), (10, 15)], fill=LIGHT)
    draw.polygon([(25, 12), (29, 10), (33, 11), (34, 13), (32, 15), (28, 15), (27, 17),
                  (24, 16), (23, 14)], fill=LIGHT)
    draw.polygon([(9, 17), (13, 17), (15, 19), (14, 21), (11, 21), (11, 22), (8, 21), (7, 19)], fill=LIGHT)
    draw.polygon([(19, 18), (24, 17), (27, 19), (27, 21), (25, 22), (21, 22), (19, 21),
                  (17, 21), (17, 19)], fill=LIGHT)
    draw.polygon([(31, 17), (35, 16), (37, 17), (36, 19), (32, 20), (30, 19)], fill=LIGHT)
    draw.line([(17, 8), (19, 7), (22, 7)], fill=SUN)
    draw.line([(12, 13), (14, 12), (16, 12)], fill=SUN)
    draw.line([(25, 13), (27, 12), (30, 12)], fill=SUN)
    draw.line([(8, 19), (9, 18), (11, 18)], fill=SUN)
    draw.line([(18, 20), (20, 19), (23, 19)], fill=SUN)
    draw.line([(7, 25), (10, 26), (14, 25), (17, 26)], fill=DEEP)
    draw.line([(28, 24), (31, 23), (34, 24), (37, 22)], fill=DEEP)
    draw.line([(32, 29), (36, 28), (37, 27)], fill=DEEP)
    return tree


def shrub(sapling):
    tile = Image.new('RGBA', (16, 16))
    draw = ImageDraw.Draw(tile)
    draw.ellipse((2, 10, 15, 14), fill=CAST_SHADOW)
    if sapling:
        draw.polygon([(6, 8), (10, 8), (9, 12), (9, 14), (5, 14), (6, 12)], fill='#525540')
        draw.line([(7, 10), (7, 12), (6, 13)], fill='#ac8659')
    drop = 2 if not sapling else 0
    draw.polygon([(1, 7+drop), (1, 5+drop), (3, 3+drop), (5, 3+drop), (7, 1+drop), (10, 1+drop),
                  (12, 3+drop), (14, 4+drop), (15, 7+drop), (14, 9+drop), (11, 10+drop),
                  (8, 10+drop), (7, 11+drop), (3, 10+drop)], fill=OUTLINE)
    draw.polygon([(2, 7+drop), (3, 5+drop), (6, 4+drop), (7, 2+drop), (10, 2+drop), (12, 4+drop),
                  (14, 5+drop), (14, 8+drop), (11, 9+drop), (8, 8+drop), (7, 10+drop), (3, 9+drop)], fill=SHADE)
    draw.polygon([(3, 7+drop), (4, 5+drop), (6, 5+drop), (8, 3+drop), (10, 3+drop), (11, 5+drop),
                  (13, 5+drop), (12, 7+drop), (9, 6+drop), (8, 8+drop), (5, 8+drop)], fill=LIGHT)
    draw.point([(5, 6+drop), (8, 4+drop), (9, 4+drop)], fill=SUN)
    return tile


def fir():
    tile = Image.new('RGBA', (16, 16))
    draw = ImageDraw.Draw(tile)
    draw.ellipse((4, 11, 15, 15), fill=CAST_SHADOW)
    draw.rectangle((6, 10, 9, 14), fill='#505443')
    draw.line((7, 11, 7, 13), fill='#a88a5d')
    draw.polygon([(7, 0), (9, 1), (10, 4), (12, 6), (11, 7), (14, 10), (15, 12), (12, 13),
                  (9, 12), (7, 13), (5, 12), (2, 13), (0, 12), (1, 10), (4, 7), (3, 6), (5, 4)], fill=OUTLINE)
    draw.polygon([(7, 2), (8, 2), (9, 5), (11, 6), (9, 7), (12, 9), (13, 11), (11, 11),
                  (9, 10), (7, 11), (5, 10), (3, 11), (2, 11), (5, 7), (4, 7), (6, 5)], fill=SHADE)
    draw.polygon([(7, 2), (7, 5), (9, 6), (7, 6), (6, 8), (8, 8), (8, 9), (5, 9), (3, 10), (6, 6), (5, 6)], fill=LIGHT)
    draw.point([(6, 4), (5, 8)], fill=SUN)
    return tile


def fern():
    tile = Image.new('RGBA', (16, 16))
    draw = ImageDraw.Draw(tile)
    draw.line([(4, 13), (11, 13)], fill=CAST_SHADOW, width=2)
    draw.line([(7, 13), (7, 10), (8, 7), (7, 4), (5, 3)], fill=DEEP)
    for group in [[(7, 11), (5, 9), (2, 9)], [(7, 10), (10, 8), (13, 8)], [(8, 8), (6, 6), (3, 6)],
                  [(8, 7), (10, 5), (12, 4)], [(7, 6), (7, 4), (5, 2)]]:
        draw.line(group, fill=SHADE, width=2)
        draw.line(group, fill=LIGHT)
    draw.point([(3, 6), (5, 2), (13, 8)], fill=SUN)
    return tile


def flowers():
    tile = Image.new('RGBA', (16, 16))
    draw = ImageDraw.Draw(tile)
    for x, y, petal, tip in [(4, 7, '#db9c9a', '#ffdfbd'), (8, 4, '#edcf8a', '#fff3cd'),
                             (12, 9, '#d29ea9', '#f4c5bc')]:
        draw.line([(x, y + 1), (x, y + 3), (x+1, y + 5)], fill=DEEP)
        draw.point((x - 1, y + 3), fill=LIGHT)
        draw.point([(x - 1, y), (x, y + 1), (x + 1, y)], fill=petal)
        draw.point((x, y - 1), fill=tip)
        draw.point((x, y), fill='#e6b36d')
    return tile


def hedge():
    tile = Image.new('RGBA', (16, 16))
    draw = ImageDraw.Draw(tile)
    draw.rectangle((0, 12, 15, 14), fill=CAST_SHADOW)
    for left in (0, 8):
        draw.polygon([(left, 4), (left+1, 2), (left+6, 2), (left+7, 4),
                      (left+7, 12), (left, 12)], fill=OUTLINE)
        draw.polygon([(left, 5), (left+2, 3), (left+5, 3), (left+7, 5),
                      (left+7, 10), (left+5, 11), (left+1, 11), (left, 10)], fill=SHADE)
        draw.polygon([(left, 6), (left+2, 4), (left+6, 4), (left+7, 6), (left+6, 8),
                      (left+3, 8), (left+2, 9), (left, 8)], fill=LIGHT)
        draw.line((left+1, 5, left+4, 5), fill=SUN)
    return tile


def tall_grass():
    tile = Image.new('RGBA', (16, 16), '#73bb83')
    draw = ImageDraw.Draw(tile)
    for x, y in [(1, 0), (9, 1), (4, 6), (-2, 9), (11, 9)]:
        draw.polygon([(x, y+5), (x, y+2), (x+2, y+3), (x+3, y+5), (x+3, y+1),
                      (x+5, y), (x+5, y+3), (x+7, y+2), (x+7, y+4), (x+5, y+7),
                      (x+2, y+7)], fill=DEEP)
        draw.line([(x+1, y+3), (x+2, y+5), (x+3, y+5)], fill=LIGHT)
        draw.line([(x+4, y+1), (x+4, y+4), (x+3, y+6)], fill=LIGHT)
        draw.line([(x+6, y+3), (x+5, y+4), (x+5, y+5)], fill=SHADE)
    return tile


def vegetation(terrain):
    """Replace the vegetation tiles in an atlas list or ID-to-image dictionary."""
    assignments = {0: meadow(), 1: meadow(1), 2: meadow(2), 3: meadow(3),
                   13: meadow(3, '#78bc91'), 14: tall_grass(), 16: fir(),
                   17: shrub(True), 18: shrub(False), 22: fern(), 23: flowers(),
                   62: meadow(3, '#80c69b'), 123: hedge()}
    tree = crown()
    for i in range(8):
        x, y = i % 3 * 16, i // 3 * 16
        if i >= 6:
            x += 16  # The bottom-left of the tree stamp is intentionally empty.
        assignments[88 + i] = tree.crop((x, y, x+16, y+16))
    for tile_id, image in assignments.items():
        terrain[tile_id] = image
    return terrain
