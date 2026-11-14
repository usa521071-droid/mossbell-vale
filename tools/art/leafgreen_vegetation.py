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
    draw.polygon([(16, 41), (19, 37), (31, 36), (35, 37), (40, 37), (44, 40),
                  (46, 42), (43, 45), (37, 46), (26, 47), (21, 46), (17, 45)], fill=CAST_SHADOW)
    draw.polygon([(19, 31), (27, 30), (28, 36), (26, 40), (28, 42), (30, 43),
                  (27, 44), (24, 43), (22, 45), (18, 44), (16, 45), (16, 43),
                  (19, 40), (20, 36)], fill='#4b5040')
    draw.polygon([(21, 34), (26, 34), (25, 38), (24, 41), (26, 42), (23, 42),
                  (22, 43), (20, 42), (18, 43), (21, 39)], fill='#a18057')
    draw.line([(22, 36), (22, 39), (21, 41)], fill='#c99b67')
    draw.line([(25, 37), (24, 39), (25, 41)], fill='#80674c')

    # Broad crown profiles with stepped recesses keep the tree readable against a grove.
    draw.polygon([(18, 1), (27, 1), (30, 3), (33, 3), (36, 6), (36, 8), (39, 10),
                  (41, 13), (41, 16), (43, 19), (42, 21), (43, 23), (43, 27),
                  (41, 29), (41, 32), (39, 34), (35, 35), (34, 37), (31, 38),
                  (26, 37), (23, 39), (18, 38), (16, 36), (13, 36), (10, 34),
                  (9, 32), (7, 32), (5, 29), (5, 26), (4, 25), (4, 20), (5, 18),
                  (5, 15), (7, 12), (8, 9), (11, 7), (12, 4), (16, 4)], fill='#235447')
    draw.polygon([(18, 3), (26, 3), (30, 5), (33, 5), (35, 8), (35, 10), (38, 12),
                  (39, 15), (39, 17), (41, 20), (40, 22), (41, 24), (41, 27), (39, 28),
                  (39, 32), (36, 33), (33, 35), (31, 36), (26, 34), (23, 37),
                  (19, 36), (17, 34), (13, 34), (11, 32), (10, 30), (8, 30),
                  (7, 28), (7, 25), (6, 24), (6, 20), (7, 18), (7, 15), (9, 13),
                  (10, 10), (13, 8), (14, 6), (16, 6)], fill=DEEP)

    # Shade-colored connections are visible between clusters so each lobe has one light direction.
    draw.polygon([(17, 5), (24, 4), (29, 6), (32, 7), (34, 10), (37, 13), (37, 16),
                  (39, 19), (37, 22), (39, 25), (37, 29), (34, 29), (33, 33), (29, 33),
                  (27, 31), (25, 34), (21, 35), (18, 32), (15, 33), (12, 31), (12, 28),
                  (9, 28), (8, 25), (9, 22), (7, 20), (9, 17), (8, 15), (11, 12), (11, 10),
                  (15, 8)], fill=SHADE)
    draw.polygon([(17, 7), (20, 5), (24, 5), (28, 7), (29, 9), (28, 12), (25, 13),
                  (21, 12), (18, 14), (15, 12), (15, 10)], fill=LEAF)
    draw.polygon([(10, 14), (14, 12), (18, 12), (21, 14), (21, 17), (18, 19),
                  (16, 19), (14, 22), (11, 21), (9, 19), (9, 16)], fill=LEAF)
    draw.polygon([(28, 12), (32, 10), (35, 12), (37, 14), (37, 17), (34, 19),
                  (29, 19), (27, 17), (24, 17), (24, 15)], fill=LEAF)
    draw.polygon([(19, 18), (23, 16), (27, 18), (29, 20), (30, 23), (27, 26), (24, 25),
                  (22, 28), (19, 27), (16, 25), (16, 23)], fill=LEAF)
    draw.polygon([(9, 23), (13, 21), (15, 22), (17, 24), (16, 27), (14, 28),
                  (11, 27), (9, 26)], fill=LEAF)
    draw.polygon([(31, 21), (35, 19), (38, 21), (38, 24), (36, 27), (34, 27),
                  (33, 30), (30, 30), (28, 28), (29, 24)], fill=LEAF)
    draw.polygon([(20, 28), (23, 26), (26, 28), (26, 30), (24, 33), (21, 34),
                  (19, 32), (17, 31), (17, 29)], fill=LEAF)

    # Compact rim marks instead of a smooth highlight make crisp GBA-scale clusters.
    draw.polygon([(17, 8), (20, 6), (24, 6), (27, 8), (25, 10), (22, 9), (20, 11),
                  (17, 11), (16, 10)], fill=LIGHT)
    draw.polygon([(11, 15), (14, 13), (17, 13), (19, 15), (18, 17), (15, 17),
                  (13, 19), (11, 18)], fill=LIGHT)
    draw.polygon([(28, 13), (31, 11), (34, 12), (35, 14), (33, 16), (29, 16),
                  (27, 15)], fill=LIGHT)
    draw.polygon([(19, 21), (23, 18), (25, 19), (27, 20), (27, 22), (25, 23),
                  (22, 23), (20, 25), (18, 23)], fill=LIGHT)
    draw.polygon([(10, 24), (13, 22), (15, 24), (14, 26), (11, 26)], fill=LIGHT)
    draw.polygon([(31, 22), (34, 20), (36, 21), (36, 23), (34, 24), (32, 25), (30, 24)], fill=LIGHT)
    draw.line([(18, 8), (20, 7), (23, 7)], fill=SUN)
    draw.line([(12, 15), (14, 14), (16, 14)], fill=SUN)
    draw.point([(29, 13), (30, 12), (31, 12), (20, 21), (21, 20), (22, 20)], fill=SUN)
    draw.line([(13, 29), (16, 28), (18, 29)], fill=DEEP)
    draw.line([(25, 30), (27, 28), (29, 30), (31, 30)], fill=DEEP)
    tree.paste((0, 0, 0, 0), (0, 32, 16, 48))  # The map stamp skips this cell.
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
