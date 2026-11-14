"""Original foliage tiles drawn with four-color Game Boy Color sub-palettes."""

from PIL import Image, ImageColor, ImageDraw


GRASS = '#b6d878'
GRASS_LOW = '#72a857'
GRASS_HIGH = '#dbe8a0'
INK = '#202433'
FOLIAGE = '#488052'
SUN = '#a0c569'
CREAM = '#f6f2d8'
BARK = '#a57b51'


def meadow(detail=0, base=GRASS):
    tile = Image.new('RGBA', (16, 16), base)
    draw = ImageDraw.Draw(tile)
    for x, y in [(), ((4, 6), (12, 11)), ((4, 5), (10, 11)), ((5, 11),)][detail]:
        draw.point([(x - 1, y - 1), (x, y), (x + 2, y - 2)], fill=GRASS_LOW)
        draw.point([(x, y - 1), (x + 1, y - 2)], fill=GRASS_HIGH)
    if detail == 2:
        draw.point([(5, 3), (7, 3), (6, 2), (6, 4)], fill=CREAM)
        draw.point((6, 3), fill=GRASS_LOW)
    return tile


def pine(draw, left, top):
    # Original two-pixel stair-step branch silhouette. The paired stamp repeats it.
    contour = [(10, 0), (14, 0), (14, 2), (16, 2), (16, 5), (18, 5), (18, 8),
               (20, 8), (20, 11), (18, 11), (18, 13), (21, 13), (21, 16), (23, 16),
               (23, 18), (24, 18), (24, 21), (22, 21), (22, 23), (19, 23), (19, 25),
               (16, 25), (16, 27), (13, 27), (13, 26), (9, 26), (9, 25), (6, 25),
               (6, 23), (3, 23), (3, 21), (1, 21), (1, 18), (3, 18), (3, 15),
               (5, 15), (5, 12), (7, 12), (7, 11), (5, 11), (5, 8), (7, 8),
               (7, 5), (9, 5), (9, 2)]
    shade = [(10, 3), (14, 3), (14, 6), (16, 6), (16, 9), (18, 9), (18, 10),
             (16, 10), (16, 14), (19, 14), (19, 17), (21, 17), (21, 19), (22, 19),
             (22, 21), (19, 22), (17, 22), (17, 24), (14, 24), (12, 25), (10, 24),
             (7, 24), (7, 22), (5, 22), (5, 20), (3, 20), (3, 19), (5, 19),
             (5, 16), (7, 16), (7, 13), (9, 13), (9, 10), (7, 10), (9, 8),
             (9, 6), (10, 6)]
    light_branches = [
        [(10, 6), (13, 4), (14, 4), (14, 7), (16, 7), (16, 9), (12, 9),
         (12, 10), (9, 10), (9, 8), (10, 8)],
        [(10, 12), (13, 10), (14, 10), (14, 13), (16, 13), (16, 15), (13, 15),
         (13, 16), (9, 16), (9, 17), (7, 17), (7, 15), (9, 15), (9, 13)],
        [(10, 18), (12, 16), (16, 16), (16, 18), (19, 18), (19, 20), (16, 20),
         (16, 22), (13, 22), (13, 23), (10, 23), (10, 22), (6, 22), (6, 20), (10, 20)],
    ]
    for shape, color in [(contour, INK), (shade, FOLIAGE), *((part, SUN) for part in light_branches)]:
        draw.polygon([(x+left, y+top) for x, y in shape], fill=color)


def crown():
    tree = Image.new('RGBA', (48, 48))
    draw = ImageDraw.Draw(tree)
    draw.polygon([(16, 32), (23, 31), (29, 34), (34, 35), (42, 36), (46, 39), (46, 42),
                  (43, 44), (38, 45), (25, 45), (23, 43), (18, 43), (16, 41)], fill=FOLIAGE)
    draw.polygon([(22, 25), (28, 25), (27, 37), (29, 40), (31, 40), (31, 42), (28, 43),
                  (24, 42), (22, 42), (20, 41), (21, 39)], fill=INK)
    draw.polygon([(24, 31), (26, 32), (25, 36), (26, 39), (28, 40), (26, 41),
                  (24, 40), (22, 41), (23, 37)], fill=BARK)
    pine(draw, 0, 1)
    pine(draw, 15, 12)
    pine(draw, 23, 1)

    # At ground level the trunk and shadow pieces have their own sub-palettes.
    tree.paste((0, 0, 0, 0), (0, 32, 16, 48))
    leaf_light = ImageColor.getcolor(SUN, 'RGBA')
    leaf_shadow = ImageColor.getcolor(FOLIAGE, 'RGBA')
    for y in range(32, 48):
        for x in range(16, 48):
            if tree.getpixel((x, y)) == leaf_light:
                tree.putpixel((x, y), leaf_shadow)
    draw.polygon([(35, 38), (40, 38), (43, 39), (45, 42), (42, 44), (36, 45),
                  (33, 44), (32, 43), (32, 41)], fill=GRASS_LOW)
    return tree


def shrub(sapling):
    tile = Image.new('RGBA', (16, 16))
    draw = ImageDraw.Draw(tile)
    if sapling:
        draw.polygon([(6, 8), (10, 8), (10, 11), (9, 12), (9, 14), (5, 14), (5, 13), (6, 12)], fill=INK)
        draw.line((7, 10, 7, 13), fill=FOLIAGE)
    y = 0 if sapling else 2
    draw.polygon([(1, 6+y), (3, 6+y), (3, 4+y), (5, 4+y), (5, 2+y), (10, 2+y), (10, 4+y),
                  (13, 4+y), (13, 6+y), (15, 6+y), (15, 10+y), (13, 10+y), (13, 12+y),
                  (9, 12+y), (8, 11+y), (5, 12+y), (3, 10+y), (1, 10+y)], fill=INK)
    draw.polygon([(2, 7+y), (4, 7+y), (4, 5+y), (6, 5+y), (6, 3+y), (9, 3+y), (9, 5+y),
                  (12, 5+y), (12, 7+y), (14, 7+y), (14, 9+y), (12, 9+y), (12, 10+y),
                  (9, 10+y), (8, 9+y), (5, 10+y), (4, 9+y), (2, 9+y)], fill=FOLIAGE)
    draw.line([(4, 7+y), (5, 6+y), (7, 6+y), (7, 5+y), (9, 5+y), (10, 6+y), (12, 6+y)], fill=SUN, width=2)
    draw.point([(4, 8+y), (11, 8+y), (12, 8+y)], fill=SUN)
    return tile


def fir():
    tile = Image.new('RGBA', (16, 16))
    draw = ImageDraw.Draw(tile)
    draw.polygon([(7, 0), (8, 0), (8, 2), (10, 2), (10, 4), (12, 4), (12, 6), (10, 6),
                  (10, 7), (12, 7), (12, 9), (14, 9), (14, 11), (15, 11), (15, 13), (9, 13),
                  (9, 15), (6, 15), (6, 13), (0, 13), (0, 11), (2, 11), (2, 9), (4, 9), (4, 7),
                  (6, 7), (6, 6), (3, 6), (3, 4), (5, 4), (5, 2), (7, 2)], fill=INK)
    draw.polygon([(7, 2), (8, 2), (8, 4), (10, 4), (10, 5), (8, 6), (8, 7), (10, 8),
                  (12, 9), (12, 11), (13, 11), (13, 12), (8, 12), (7, 11), (5, 12), (2, 12),
                  (2, 11), (4, 10), (4, 9), (6, 8), (6, 7), (7, 6), (5, 5), (7, 4)], fill=FOLIAGE)
    draw.line([(7, 3), (7, 5), (6, 5)], fill=SUN)
    draw.line([(6, 8), (6, 9), (4, 10)], fill=SUN)
    draw.line([(7, 8), (8, 9), (9, 9)], fill=SUN)
    return tile


def fern():
    tile = Image.new('RGBA', (16, 16))
    draw = ImageDraw.Draw(tile)
    draw.line([(7, 14), (7, 10), (8, 7), (7, 4), (5, 2)], fill=FOLIAGE, width=2)
    for branch in [[(7, 11), (5, 9), (2, 9)], [(8, 10), (11, 8), (14, 8)],
                   [(7, 8), (5, 6), (2, 6)], [(8, 7), (11, 5), (13, 5)]]:
        draw.line(branch, fill=FOLIAGE, width=2)
        draw.point(branch[-1], fill=SUN)
    return tile


def flowers():
    tile = Image.new('RGBA', (16, 16))
    draw = ImageDraw.Draw(tile)
    for x, y in [(4, 8), (9, 4), (12, 10)]:
        draw.line([(x, y + 1), (x, y + 3), (x + 1, y + 4)], fill=FOLIAGE)
        draw.point([(x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)], fill=CREAM)
        draw.point((x, y), fill='#d67e83')
    return tile


def hedge():
    tile = Image.new('RGBA', (16, 16))
    draw = ImageDraw.Draw(tile)
    for x in (0, 8):
        draw.polygon([(x, 5), (x+2, 5), (x+2, 3), (x+6, 3), (x+7, 5), (x+7, 12), (x, 12)], fill=INK)
        draw.rectangle((x, 7, x+7, 10), fill=FOLIAGE)
        draw.rectangle((x+2, 5, x+5, 8), fill=SUN)
        draw.point([(x+1, 7), (x+6, 7)], fill=SUN)
    return tile


def tall_grass():
    tile = Image.new('RGBA', (16, 16), GRASS_LOW)
    draw = ImageDraw.Draw(tile)
    for x, y in [(0, 0), (8, 0), (0, 8), (8, 8)]:
        draw.polygon([(x, y+2), (x+2, y+2), (x+2, y+4), (x+3, y+4), (x+3, y+1),
                      (x+5, y+1), (x+5, y+4), (x+6, y+4), (x+6, y+3), (x+7, y+3),
                      (x+7, y+5), (x+5, y+7), (x+2, y+7), (x+2, y+6), (x, y+5)], fill=FOLIAGE)
        draw.line([(x+1, y+3), (x+2, y+5), (x+4, y+5), (x+4, y+2)], fill=GRASS)
        draw.point((x+6, y+5), fill=GRASS)
    return tile


def vegetation(terrain):
    """Replace the vegetation tiles in an atlas list or ID-to-image dictionary."""
    assignments = {0: meadow(), 1: meadow(1), 2: meadow(2), 3: meadow(3),
                   13: meadow(3, '#a8c86d'), 14: tall_grass(), 16: fir(),
                   17: shrub(True), 18: shrub(False), 22: fern(), 23: flowers(),
                   62: meadow(3, '#99c56e'), 123: hedge()}
    tree = crown()
    for i in range(8):
        x, y = i % 3 * 16, i // 3 * 16
        if i >= 6:
            x += 16  # The bottom-left of the tree stamp is intentionally empty.
        assignments[88 + i] = tree.crop((x, y, x+16, y+16))
    for tile_id, image in assignments.items():
        terrain[tile_id] = image
    return terrain
