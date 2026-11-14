"""Draw the terrain, buildings, foliage and ledges on the 16-pixel game grid."""

from PIL import Image, ImageDraw
import random


MINT = '#79c6a1'
SAND = '#eee590'
WATER = '#5baec4'
CAVE = '#a6aa8a'
CAVE_TOP = '#c7a196'
GRANITE = '#c3c2a2'
ROCK_TOP = '#b0b1a1'


def cut(image, width, height):
    return [image.crop((x, y, x + 16, y + 16))
            for y in range(0, height, 16) for x in range(0, width, 16)]


def texture(base, shades, seed, count=22):
    image = Image.new('RGBA', (16, 16), base)
    draw = ImageDraw.Draw(image)
    randomizer = random.Random(seed)
    for _ in range(count):
        x = randomizer.randrange(16)
        y = randomizer.randrange(16)
        draw.point((x, y), fill=randomizer.choice(shades))
    return image


def grass(kind):
    shades = ['#6fbd99', '#8acfa8', '#71c39f']
    image = texture(MINT, shades, kind, 8)
    draw = ImageDraw.Draw(image)
    marks = [[(4, 5), (12, 12)], [(3, 10), (10, 3), (13, 14)], [(3, 5), (9, 11)], [(6, 8), (12, 4), (2, 13)]][kind]
    for x, y in marks:
        draw.point([(x - 2, y - 1), (x, y + 1), (x + 2, y - 2)], fill='#57b490')
        draw.point([(x - 1, y - 1), (x, y), (x + 1, y - 2)], fill='#a6e0b0')
    if kind == 2:
        for x, y in [(4, 8), (12, 3), (11, 13)]:
            draw.point([(x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)], fill='#f1e9c9')
            draw.point((x, y), fill='#e8ab69')
    return image


def dirt(kind=0):
    image = texture(SAND, ['#e2cd80', '#dcca79', '#f3e99d'], 221 + kind, 18)
    draw = ImageDraw.Draw(image)
    if kind == 1:
        for x, y in [(3, 3), (11, 8), (7, 13)]:
            draw.line([(x, y), (x + 1, y - 1)], fill='#d3bf74')
            draw.point((x, y - 1), fill='#f6ecaa')
    return image


def water(kind):
    base, shadow, crest = [('#87cabc', '#70bdaf', '#a8dbcf'), (WATER, '#50a0ba', '#87c8d7'),
                           ('#528fae', '#4c81a3', '#6eb0c8'), ('#58a8c1', '#4f9eb7', '#a0d9d9')][kind]
    image = Image.new('RGBA', (16, 16), base)
    draw = ImageDraw.Draw(image)
    for x, y in [(2, 3), (9, 7), (3, 12)]:
        draw.line((x, y + 1, x + 4, y + 1), fill=shadow)
        if kind != 2:
            draw.line((x + 1, y, x + 3, y), fill=crest)
    if kind == 3:
        draw.line([(0, 9), (4, 9), (6, 8), (13, 8), (15, 7)], fill=crest)
    return image


def stone(kind):
    palettes = [(CAVE, '#bac0a0', '#898672'), (CAVE, '#c4c6a8', '#898371'),
                (CAVE_TOP, '#d5b2a6', '#b28c84'), ('#a9a98b', '#c2bea0', '#92917a'),
                (GRANITE, '#d3d1ad', '#a8a88e'), (ROCK_TOP, '#c1bfa9', '#929586'),
                ('#8abe91', '#9bce9c', '#6ca686'), ('#a3acb0', '#c2c8c3', '#7f8c99')]
    base, high, low = palettes[kind]
    image = texture(base, [high, low, base], 762 + kind, 34 if kind < 6 else 12)
    draw = ImageDraw.Draw(image)
    if kind in (2, 5, 7):
        for y in ([2, 7, 12] if kind == 2 else [3, 7, 11, 15]):
            for x in range((-3 if y % 2 else 0), 16, 7):
                draw.line([(x, y), (x + 3, y), (x + 4, y + 1), (x + 6, y + 1)], fill=low)
                draw.line([(x + 1, y + 1), (x + 3, y + 1)], fill=high)
        if kind == 2:
            draw.arc((4, 2, 13, 10), 180, 315, fill=high)
            draw.arc((1, 7, 8, 14), 25, 200, fill=low)
    else:
        for x, y in [(3, 4), (11, 6), (7, 13)]:
            draw.line((x, y, x + 2, y), fill=low)
            draw.line((x, y - 1, x + 1, y - 1), fill=high)
    return image


def edge_distance(x, y, direction, inset=3):
    north, east, south, west = y - inset, 15 - inset - x, 15 - inset - y, x - inset
    return (north, east, south, west,
            min(north, west, (north + west - 2) // 2), min(north, east, (north + east - 2) // 2),
            min(south, east, (south + east - 2) // 2), min(south, west, (south + west - 2) // 2),
            max(north, west), max(north, east), max(south, east), max(south, west))[direction]


def transition(base, fill, direction, border, highlight):
    image = base.copy()
    for y in range(16):
        for x in range(16):
            distance = edge_distance(x, y, direction)
            if direction < 4:
                longitudinal = x if direction in (0, 2) else y
                distance -= (longitudinal // 4 + longitudinal // 8) % 2
            if distance >= 2:
                image.putpixel((x, y), fill.getpixel((x, y)))
            elif distance >= 0:
                image.putpixel((x, y), border[distance])
            elif distance == -1 and (x + y) % 3:
                image.putpixel((x, y), highlight)
    return image


def building():
    roof = Image.new('RGBA', (48, 48))
    draw = ImageDraw.Draw(roof)
    for x in range(48):
        border = 5 - min(5, min(x, 47-x) // 2)
        shade = '#78b4c5' if x > 4 else '#a9cfce'
        if x >= 44:
            shade = '#6495ad'
        draw.line((x, border, x, 43), fill=shade)
        draw.point((x, border), fill='#526983')
        draw.point((x, border + 1), fill='#bbe1d9')
    for y in range(8, 41, 4):
        draw.line((2, y, 45, y), fill='#689bb4')
        draw.line((3, y + 1, 44, y + 1), fill='#a3cad0')
        for x in range(7 + y % 8, 43, 8):
            draw.line((x, y - 2, x, y - 1), fill='#70a6bb')
    draw.line((1, 8, 1, 44), fill='#586d87')
    draw.line((46, 8, 46, 44), fill='#506482')
    draw.rectangle((0, 42, 47, 47), fill='#4f627d')
    draw.line((0, 43, 47, 43), fill='#9ebac0')
    draw.line((2, 46, 45, 46), fill='#617996')

    wall = Image.new('RGBA', (48, 32), '#eadab6')
    draw = ImageDraw.Draw(wall)
    draw.rectangle((0, 0, 47, 3), fill='#596881')
    draw.rectangle((1, 4, 46, 7), fill='#938984')
    for y in (9, 14, 19, 24):
        draw.line((1, y, 46, y), fill='#BEAF96')
        draw.line((1, y+1, 46, y+1), fill='#f2e8c8')
    draw.rectangle((0, 27, 47, 31), fill='#7f95a1')
    draw.line((0, 27, 47, 27), fill='#bcc9c4')
    draw.line((0, 31, 47, 31), fill='#596a84')

    for x in (0, 15, 31, 46):
        draw.rectangle((x, 6, x + 1, 30), fill='#69798a')
        draw.point((x, 8), fill='#aebcb9')

    for x in (3, 34):
        draw.rectangle((x, 12, x+10, 21), fill='#64718b')
        draw.rectangle((x+1, 13, x+9, 19), fill='#91bace')
        draw.line((x+1, 13, x+9, 13), fill='#ccdedb')
        draw.line((x, 21, x+10, 21), fill='#e8e4c7')
        draw.line((x+5, 13, x+5, 19), fill='#798b98')

    door = Image.new('RGBA', (16, 16), '#667f9a')
    draw = ImageDraw.Draw(door)
    draw.rectangle((2, 0, 13, 15), fill='#7d9aaa', outline='#54647e')
    draw.rectangle((4, 2, 11, 8), fill='#97bfcc', outline='#657e94')
    draw.line((5, 3, 10, 3), fill='#b3d5d6')
    draw.line((7, 3, 7, 7), fill='#7f9baa')
    draw.line((4, 12, 10, 12), fill='#668394')
    draw.point((10, 10), fill='#e6d392')
    draw.line((0, 15, 15, 15), fill='#a6b0ae')
    wall.paste(door, (16, 16))
    draw = ImageDraw.Draw(wall)
    draw.rectangle((17, 10, 30, 15), fill='#5e748f')
    draw.rectangle((19, 11, 28, 13), fill='#88acbe')
    draw.line((20, 11, 27, 11), fill='#b9d9d9')
    return cut(roof, 48, 48) + cut(wall, 48, 32), door, wall.crop((0, 16, 16, 32))


def chimney():
    image = Image.new('RGBA', (16, 16))
    draw = ImageDraw.Draw(image)
    draw.polygon([(2, 4), (5, 2), (13, 2), (14, 4), (14, 14), (12, 15), (3, 14)], fill='#65778e')
    draw.rectangle((4, 5, 11, 13), fill='#a0a6aa')
    draw.rectangle((4, 3, 12, 5), fill='#c3c0b5')
    draw.line((5, 4, 10, 4), fill='#667487')
    draw.line((5, 8, 10, 8), fill='#808b93')
    draw.line((5, 11, 10, 11), fill='#808b93')
    return image


def fence(kind):
    image = Image.new('RGBA', (16, 16))
    draw = ImageDraw.Draw(image)
    ground = '#639e8c'
    dark, mid, light = '#5d6e86', '#9babb1', '#dce6dc'
    if kind == 3:
        draw.rectangle((7, 0, 11, 15), fill=dark)
        draw.line((8, 0, 8, 15), fill=mid)
    else:
        start, end = ((4, 15), (0, 15), (0, 11))[kind]
        draw.rectangle((start, 10, end, 14), fill=ground)
        draw.rectangle((start, 6, end, 10), fill=dark)
        draw.line((start, 7, end, 7), fill=mid)
    for x in ([5, 13] if kind == 0 else [2, 10] if kind == 1 else [2, 10] if kind == 2 else [7]):
        draw.polygon([(x, 13), (x, 4), (x + 1, 2), (x + 4, 2), (x + 5, 4), (x + 5, 13)], fill=dark)
        draw.rectangle((x + 1, 4, x + 4, 11), fill=mid)
        draw.line((x + 1, 4, x + 1, 9), fill=light)
        draw.line((x + 1, 4, x + 3, 4), fill=light)
    return image


def shade(kind):
    image = Image.new('RGBA', (16, 16))
    draw = ImageDraw.Draw(image)
    if kind == 0:
        draw.polygon([(0, 1), (4, 4), (6, 6), (6, 15), (0, 15)], fill=(66, 98, 110, 76))
    else:
        draw.polygon([(0, 0), (15, 0), (15, 4), (11, 5), (0, 5)], fill=(66, 98, 110, 76))
    return image


def oak():
    image = Image.new('RGBA', (48, 32))
    draw = ImageDraw.Draw(image)
    draw.ellipse((0, 4, 47, 32), fill='#388967')
    draw.ellipse((1, 3, 45, 30), fill='#4ba369')
    for box in [(1, 10, 24, 27), (8, 4, 31, 26), (18, 1, 38, 24), (26, 7, 46, 28), (12, 10, 37, 30)]:
        draw.ellipse(box, fill='#60b26a')
        x1, y1, x2, y2 = box
        draw.arc((x1+1, y1+1, x2-1, y2-1), 190, 290, fill='#89c776', width=2)
        draw.arc((x1+3, y1+3, x2-2, y2-2), 10, 84, fill='#51a565', width=2)
    for x, y in [(5, 16), (15, 11), (24, 7), (30, 17), (37, 13), (17, 23), (36, 23)]:
        draw.line([(x, y + 1), (x + 2, y), (x + 4, y)], fill='#90cf7e')
        draw.point((x + 1, y + 3), fill='#4ba16b')
    trunk = Image.new('RGBA', (16, 16))
    draw = ImageDraw.Draw(trunk)
    draw.ellipse((0, 0, 15, 8), fill='#60ad89')
    draw.polygon([(4, 0), (12, 0), (11, 7), (13, 10), (7, 11), (3, 10), (5, 7)], fill='#6f645b')
    draw.line([(7, 1), (7, 7), (5, 9)], fill='#a99373', width=2)
    return cut(image, 48, 32) + [trunk, shade(1)]


def cliff(direction, mountain, plateau):
    face, light, rim, low, shadow = ('#777d72', '#a6a694', '#d0caab', '#5d655e', '#6b766a') if mountain else (
        '#915c58', '#b27e74', '#dbafa1', '#70464a', '#80534f')
    south = direction in (2, 6, 7, 10, 11)
    north = direction in (0, 4, 5, 8, 9)
    face_size = 11 if south else 9 if north else 7
    image = Image.new('RGBA', (16, 16))
    draw = ImageDraw.Draw(image)
    for x, y in [(x, y) for y in range(16) for x in range(16)]:
        distance = edge_distance(x, y, direction)
        step_length = (x + (3 if mountain else 0)) % 8
        if direction in (1, 3):
            step_length = (y + 1) % 8
        offset = 1 if step_length in (0, 7) else 0
        if distance >= face_size:
            image.putpixel((x, y), plateau.getpixel((x, y)))
        elif distance == face_size - 1:
            draw.point((x, y), fill=rim)
        elif distance >= 0:
            stripe = (x + (y // 4) + (2 if mountain else 0)) % (5 if mountain else 7)
            if direction in (1, 3):
                stripe = (y + x // 4) % 7
            if distance == 0 or stripe == 0 or distance == 1 and stripe > 2:
                color = low
            elif stripe in (2, 3) or distance == face_size - 2:
                color = light
            else:
                color = face
            if offset and distance == 0:
                color = shadow
            draw.point((x, y), fill=color)
        elif distance == -1:
            draw.point((x, y), fill=shadow)
    return image


def stairs(part):
    image = Image.new('RGBA', (16, 16))
    draw = ImageDraw.Draw(image)
    draw.rectangle((2, 0, 13, 15), fill='#86765d')
    draw.line((2, 0, 2, 15), fill='#635e54')
    draw.line((13, 0, 13, 15), fill='#635e54')
    for y in range(0, 16, 4):
        draw.line((3, y, 12, y), fill='#f1dd9e')
        draw.rectangle((3, y + 1, 12, y + 2), fill='#c7b880')
        draw.line((4, y + 1, 4, y + 2), fill='#dacb90')
    if part == 0:
        draw.line((1, 0, 14, 0), fill='#6f7663')
        draw.line((2, 1, 13, 1), fill='#e9d89d')
    if part == 2:
        draw.line((1, 14, 14, 14), fill='#8f8569')
        draw.line((0, 15, 15, 15), fill='#6f6c5c')
    return image


def prop(kind):
    image = Image.new('RGBA', (16, 16))
    draw = ImageDraw.Draw(image)
    if kind == 0:
        draw.rectangle((0, 2, 15, 13), fill='#59a966')
        draw.line((0, 2, 15, 2), fill='#9ad37e')
        draw.line((0, 13, 15, 13), fill='#478f64')
        for x in (1, 5, 9, 13):
            draw.line([(x, 5), (x + 1, 3), (x + 2, 4)], fill='#badf91')
            draw.line([(x, 12), (x + 2, 10), (x + 3, 11)], fill='#6fb76a')
    elif kind == 1:
        for x, y, w in [(3, 5, 3), (10, 7, 4), (6, 12, 4)]:
            draw.ellipse((x, y, x + w, y + 2), fill='#777966')
            draw.line((x + 1, y, x + w - 1, y), fill='#cbc6aa')
    elif kind == 2:
        draw.ellipse((2, 11, 14, 14), fill='#878675')
        draw.polygon([(4, 12), (3, 7), (6, 4), (8, 8), (9, 3), (12, 4), (13, 7), (11, 12)], fill='#72617f')
        draw.polygon([(6, 5), (6, 10), (9, 11), (11, 6), (10, 4), (9, 8), (8, 9)], fill='#bc9fc2')
        draw.point([(6, 5), (10, 4)], fill='#efd1dc')
    elif kind == 3:
        draw.ellipse((1, 11, 15, 15), fill='#898975')
        draw.polygon([(1, 10), (3, 4), (8, 1), (12, 3), (14, 9), (13, 13), (10, 14), (4, 13)], fill='#6f7371')
        draw.polygon([(2, 9), (4, 4), (8, 2), (11, 4), (9, 8), (6, 11)], fill='#bdb4a3')
        draw.polygon([(7, 11), (10, 8), (12, 6), (13, 10), (12, 12), (10, 13)], fill='#95928a')
    else:
        draw.ellipse((2, 12, 14, 15), fill='#84816e')
        draw.polygon([(4, 13), (4, 3), (6, 1), (11, 2), (12, 5), (11, 13)], fill='#6d7371')
        draw.polygon([(5, 12), (5, 4), (7, 2), (10, 3), (10, 10), (8, 12)], fill='#b8b6a1')
        draw.line([(6, 7), (8, 9), (10, 7)], fill='#858c86')
        draw.point([(7, 6), (9, 6)], fill='#6f9c8b')
    return image


def make_atlas(legacy):
    grass_tiles = [grass(i) for i in range(4)]
    path = dirt()
    water_tiles = [water(i) for i in range(4)]
    stone_tiles = [stone(i) for i in range(8)]
    terrain = grass_tiles + [path, dirt(1), dirt(), legacy[7]] + water_tiles
    terrain += [stone_tiles[4], grass_tiles[3], stone_tiles[6], stone_tiles[7]]
    buildings, door, window = building()
    terrain += legacy[16:26] + [buildings[12], buildings[4]] + legacy[28:32]
    transition_sets = [(grass_tiles[0], path, ('#9cd7a2', '#d3e3a1'), '#85cfa5'),
                       (grass_tiles[0], water_tiles[0], ('#e6d88e', '#d7ddb2'), '#a2d3a0')]
    for base, fill, border, highlight in transition_sets:
        border = tuple(Image.new('RGBA', (1, 1), color).getpixel((0, 0)) for color in border)
        highlight = Image.new('RGBA', (1, 1), highlight).getpixel((0, 0))
        terrain += [transition(base, fill, direction, border, highlight) for direction in range(12)]
    terrain += stone_tiles
    terrain += buildings + [door, window, chimney()] + [fence(i) for i in range(4)] + [shade(0), shade(1)]
    terrain += oak()
    terrain += [cliff(i, False, stone_tiles[2]) for i in range(12)]
    terrain += [cliff(i, True, stone_tiles[5]) for i in range(12)]
    terrain += [stairs(i) for i in range(3)] + [prop(i) for i in range(5)]
    assert len(terrain) == 128
    atlas = Image.new('RGBA', (128, 256))
    for i, image in enumerate(terrain):
        atlas.paste(image, (i % 8 * 16, i // 8 * 16))
    return atlas, terrain
