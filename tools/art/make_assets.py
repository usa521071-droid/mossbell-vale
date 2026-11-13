"""Regenerate the Mossbell Vale pixel art and starter world with Pillow."""

from collections import deque
from pathlib import Path
import random
import struct

from PIL import Image, ImageDraw


ASSETS = Path(__file__).resolve().parents[2] / 'assets'
TRANSPARENT = (0, 0, 0, 0)


def tile(background=TRANSPARENT):
    image = Image.new('RGBA', (16, 16), background)
    return image, ImageDraw.Draw(image)


def scatter(draw, count, colors, seed):
    randomizer = random.Random(seed)
    for _ in range(count):
        x = randomizer.randint(0, 15)
        y = randomizer.randint(0, 15)
        draw.point((x, y), fill=randomizer.choice(colors))


def meadow(kind):
    image, draw = tile('#85bf69')
    scatter(draw, 19, ['#80b961', '#8ac671', '#8ec672'], 210 + kind)
    if kind in (0, 1):
        tufts = [(4, 9), (11, 3)] if kind == 0 else [(3, 3), (11, 9), (5, 13), (13, 14)]
        for x, y in tufts:
            draw.line([(x - 1, y - 1), (x, y), (x + 1, y - 2)], fill='#69a458')
            draw.point((x + 1, y - 2), fill='#9bd37c')
    if kind == 2:
        for x, y in [(4, 5), (11, 10), (5, 13)]:
            draw.line([(x - 1, y + 2), (x + 1, y + 2)], fill='#589950')
            draw.line([(x - 1, y), (x + 1, y)], fill='#fff1c1')
            draw.line([(x, y - 1), (x, y + 1)], fill='#fff1c1')
            draw.point((x, y), fill='#edb553')
    if kind == 3:
        for x, y in [(4, 4), (9, 9), (12, 4), (5, 13)]:
            draw.line([(x, y), (x + 1, y + 2)], fill='#569650')
            draw.line([(x - 1, y), (x + 1, y)], fill='#a0d578')
            draw.point((x, y - 1), fill='#b3dc82')
    return image


def footpath(kind):
    base, low, high = {
        4: ('#d5a46e', '#c89967', '#e1b480'),
        5: ('#d5a46e', '#c39267', '#e5b681'),
        6: ('#e6c88a', '#d9b97b', '#f1d59a'),
        12: ('#adab93', '#8f907e', '#c4bca4'),
        15: ('#90958b', '#747d7a', '#c0b8a5'),
    }[kind]
    image, draw = tile(base)
    scatter(draw, 32, [low, high], kind + 381)
    if kind in (5, 12):
        for x, y in [(4, 5), (11, 9), (8, 13), (13, 2)]:
            draw.line((x, y, x + 2, y), fill=low)
            draw.line((x, y - 1, x + 1, y - 1), fill=high)
    if kind == 15:
        for row in range(4):
            y = row * 4
            draw.line((0, y + 3, 15, y + 3), fill=low)
            for x in range(-3 + (row % 2) * 3, 16, 6):
                draw.line((x, y, x + 4, y), fill=high)
                draw.line((x + 5, y, x + 5, y + 2), fill=low)
    return image


def boardwalk():
    image, draw = tile('#a67753')
    for x in (0, 4, 8, 12):
        draw.rectangle((x, 0, x + 2, 15), fill='#c99a65')
        draw.line((x, 1, x, 14), fill='#dbad72')
        draw.line((x + 2, 0, x + 2, 15), fill='#b88959')
        draw.line((x + 1, 5, x + 1, 8), fill='#c18c5b')
    draw.line((0, 1, 15, 1), fill='#e0ad73')
    draw.line((0, 14, 15, 14), fill='#82583f')
    for x in (1, 5, 9, 13):
        draw.point([(x, 2), (x, 13)], fill='#805e4e')
    return image


def water(kind):
    base, low, mid, high = {
        8: ('#60b9a4', '#4fa891', '#72c3ab', '#a1d7b6'),
        9: ('#459d9d', '#398b92', '#50aaa3', '#71c7b1'),
        10: ('#387f90', '#326e87', '#418b98', '#63aca8'),
        11: ('#4daaa7', '#3e929a', '#65beb2', '#c1e2c2'),
    }[kind]
    image, draw = tile(base)
    for x, y in [(1, 2), (8, 5), (2, 10), (11, 13)]:
        draw.line((x, y + 2, x + 3, y + 2), fill=low)
        draw.line((x, y + 1, x + 4, y + 1), fill=mid)
        draw.line((x + 1, y + 1, x + 2, y + 1), fill=high)
    if kind == 11:
        draw.line([(0, 12), (3, 11), (6, 11), (7, 10), (11, 10), (13, 9), (15, 9)], fill=high)
        draw.line([(0, 4), (4, 4), (6, 3), (11, 3), (13, 2), (15, 2)], fill=high)
    return image


def heath(kind):
    image, draw = tile('#72aa62' if kind == 13 else '#9aab71')
    colors = ['#6ca362', '#8aba71', '#649958'] if kind == 13 else ['#a8b67c', '#849c67', '#bca789']
    scatter(draw, 39, colors, kind + 211)
    for x, y in [(4, 4), (12, 6), (8, 12)]:
        draw.line((x - 2, y + 1, x + 2, y + 1), fill=colors[0])
        draw.line((x - 1, y, x + 1, y), fill=colors[1])
        if kind == 14:
            draw.point((x, y - 1), fill='#c398a4')
    return image


def fir():
    image, draw = tile()
    draw.ellipse((2, 12, 15, 15), fill='#50874e')
    draw.rectangle((6, 10, 9, 14), fill='#594b40')
    draw.rectangle((7, 11, 8, 13), fill='#976d4b')
    draw.polygon([(7, 0), (8, 0), (13, 7), (12, 7), (15, 12), (13, 13), (3, 13), (0, 12), (3, 7), (2, 7)], fill='#285747')
    draw.polygon([(7, 2), (8, 2), (11, 6), (10, 6), (13, 10), (11, 11), (3, 11), (2, 10)], fill='#387957')
    draw.polygon([(7, 2), (6, 5), (8, 5), (7, 6), (5, 6), (3, 9), (8, 9), (10, 8), (8, 8), (9, 7)], fill='#5a9b65')
    draw.line([(4, 11), (7, 11), (8, 10)], fill='#5b9c65')
    draw.line([(5, 5), (6, 4), (7, 4)], fill='#84b878')
    return image


def oak():
    image, draw = tile()
    draw.ellipse((2, 12, 15, 15), fill='#50874e')
    draw.rectangle((6, 9, 10, 14), fill='#57483a')
    draw.line((7, 10, 7, 13), fill='#aa7654')
    draw.polygon([(4, 1), (7, 0), (11, 1), (14, 3), (14, 5), (15, 6), (15, 9), (13, 11), (9, 11), (8, 12), (4, 11), (2, 10), (0, 8), (0, 5), (2, 4), (2, 3)], fill='#2f6345')
    draw.polygon([(4, 2), (7, 1), (11, 2), (13, 4), (13, 5), (14, 6), (14, 8), (12, 9), (8, 9), (7, 10), (3, 9), (1, 7), (2, 5)], fill='#508b4f')
    draw.polygon([(5, 2), (8, 2), (10, 3), (10, 5), (12, 5), (12, 7), (10, 8), (6, 7), (4, 8), (2, 7), (3, 5)], fill='#75ae5b')
    draw.line([(4, 5), (5, 4), (7, 4)], fill='#a1c96b')
    draw.line([(8, 7), (10, 7), (11, 6)], fill='#9ec771')
    return image


def shrub():
    image, draw = tile()
    draw.ellipse((1, 10, 15, 15), fill='#578e50')
    draw.polygon([(1, 11), (1, 8), (4, 6), (5, 6), (7, 4), (10, 4), (12, 6), (14, 7), (14, 11), (12, 13), (4, 13)], fill='#336d4c')
    draw.polygon([(2, 10), (4, 7), (6, 8), (7, 5), (10, 5), (11, 7), (13, 8), (12, 10), (8, 11), (7, 10), (4, 11)], fill='#6da65a')
    draw.point([(4, 8), (8, 6), (11, 8)], fill='#a2ca68')
    draw.point([(5, 10), (9, 8), (11, 10)], fill='#dd9982')
    return image


def rock(kind):
    image, draw = tile()
    if kind == 19:
        draw.ellipse((1, 10, 15, 15), fill='#588753')
        draw.polygon([(1, 11), (2, 7), (5, 4), (9, 3), (13, 5), (15, 9), (14, 12), (10, 14), (4, 13)], fill='#526966')
        draw.polygon([(2, 9), (3, 7), (6, 5), (9, 4), (12, 6), (13, 8), (10, 10), (5, 10)], fill='#9aa28b')
        draw.polygon([(3, 8), (6, 5), (10, 5), (11, 6), (7, 8)], fill='#c4c0a0')
        draw.line([(6, 12), (8, 12), (9, 11), (12, 11)], fill='#7e8c7e')
    elif kind == 20:
        draw.polygon([(0, 12), (4, 7), (5, 4), (8, 1), (9, 1), (12, 6), (13, 10), (15, 12), (15, 14), (10, 15), (3, 15), (0, 14)], fill='#526663')
        draw.polygon([(1, 12), (5, 7), (6, 4), (8, 2), (8, 6), (6, 9), (8, 11), (7, 14), (3, 14)], fill='#89948a')
        draw.polygon([(8, 2), (11, 6), (11, 9), (14, 12), (11, 13), (9, 12), (9, 9), (7, 7), (9, 6)], fill='#6e8079')
        draw.polygon([(6, 4), (8, 2), (10, 4), (11, 6), (9, 5), (8, 6), (7, 5), (5, 7)], fill='#d0c7ad')
        draw.line([(1, 13), (3, 12), (5, 12), (5, 13)], fill='#739566')
        draw.line([(9, 14), (11, 13), (13, 14)], fill='#739566')
    else:
        draw.polygon([(0, 3), (3, 1), (6, 1), (8, 2), (11, 1), (15, 2), (15, 13), (13, 15), (10, 14), (6, 15), (3, 14), (0, 15)], fill='#657674')
        draw.polygon([(0, 3), (3, 2), (6, 2), (8, 3), (11, 2), (15, 3), (15, 5), (12, 4), (9, 5), (5, 4), (2, 6), (0, 5)], fill='#b2af94')
        draw.polygon([(0, 7), (2, 6), (5, 5), (5, 8), (3, 10), (3, 13), (0, 14)], fill='#889285')
        draw.polygon([(6, 6), (8, 6), (10, 5), (11, 5), (10, 8), (8, 9), (8, 13), (6, 14)], fill='#8f998b')
        draw.polygon([(12, 5), (15, 6), (15, 12), (13, 14), (12, 13), (14, 10), (13, 8)], fill='#7f8b80')
        draw.point([(3, 3), (10, 3), (14, 4)], fill='#d4c6a3')
    return image


def small_plant(flowers):
    image, draw = tile()
    draw.ellipse((2, 12, 14, 14), fill='#659e58')
    if not flowers:
        draw.line([(7, 13), (8, 8), (7, 4)], fill='#487d50')
        for branch in [[(7, 11), (5, 9), (2, 9)], [(8, 9), (10, 7), (13, 7)], [(8, 8), (5, 6), (3, 6)], [(8, 7), (10, 4), (12, 3)], [(8, 6), (7, 3), (6, 2)]]:
            draw.line(branch, fill='#518e50', width=2)
            draw.line(branch, fill='#9bc66a')
    else:
        for x, y, hue in [(4, 8, '#e68583'), (8, 5, '#e6ae67'), (12, 9, '#d59ab6')]:
            draw.line([(x + 1, 13), (x, 11), (x, y)], fill='#4c8953')
            draw.line([(x - 1, y), (x + 1, y)], fill=hue, width=2)
            draw.point((x, y - 1), fill='#ffe6b4')
        draw.point([(3, 11), (9, 10), (13, 12)], fill='#a5c86b')
    return image


def wooden(kind):
    image, draw = tile()
    if kind == 24:
        draw.ellipse((0, 8, 15, 15), fill='#5c8b50')
        draw.polygon([(0, 9), (3, 5), (11, 5), (14, 7), (15, 12), (12, 14), (3, 14), (0, 12)], fill='#59483e')
        draw.polygon([(1, 8), (4, 6), (11, 6), (12, 7), (9, 10), (2, 11)], fill='#9e7650')
        draw.line([(3, 8), (7, 8), (9, 7)], fill='#bc925c')
        draw.ellipse((10, 8, 14, 13), fill='#c39163')
        draw.ellipse((11, 9, 14, 12), fill='#6f4c41')
        draw.point((12, 10), fill='#3f3a35')
        draw.line([(4, 6), (5, 5), (8, 5), (9, 6), (7, 7)], fill='#78a35c')
    elif kind == 25:
        draw.rectangle((0, 6, 15, 11), fill='#775744')
        draw.line((0, 7, 15, 7), fill='#ce9b6e')
        draw.line((0, 9, 15, 9), fill='#af805b')
        for x in (1, 12):
            draw.polygon([(x, 14), (x, 4), (x + 1, 2), (x + 2, 4), (x + 2, 14)], fill='#875e49')
            draw.line((x + 1, 4, x + 1, 12), fill='#dab383')
    else:
        draw.ellipse((2, 10, 14, 15), fill='#598753')
        draw.polygon([(3, 8), (4, 5), (11, 5), (12, 8), (12, 12), (9, 14), (5, 13), (2, 14)], fill='#785741')
        draw.ellipse((3, 4, 12, 9), fill='#b98659')
        draw.ellipse((5, 5, 10, 7), fill='#e0ad72')
        draw.line((5, 10, 5, 12), fill='#9e704e')
        draw.line((9, 10, 9, 12), fill='#a3754c')
    return image


def cottage(roof):
    image, draw = tile()
    if not roof:
        draw.rectangle((0, 0, 15, 15), fill='#bc9066')
        draw.rectangle((0, 0, 15, 2), fill='#845f4c')
        draw.rectangle((0, 3, 15, 11), fill='#e4bf89')
        draw.line((0, 6, 15, 6), fill='#eccb94')
        draw.rectangle((0, 13, 15, 15), fill='#79817a')
        draw.point([(2, 13), (6, 14), (11, 14), (14, 13)], fill='#ada893')
        draw.rectangle((4, 4, 11, 11), fill='#80634e')
        draw.rectangle((5, 5, 10, 9), fill='#4e7981')
        draw.line((5, 5, 9, 5), fill='#85b2a9')
        draw.line((8, 5, 8, 9), fill='#bb9869')
        draw.line((4, 10, 11, 10), fill='#edcb91')
    else:
        draw.rectangle((0, 0, 15, 15), fill='#7d494d')
        draw.rectangle((0, 0, 15, 12), fill='#a55a59')
        for y in (0, 4, 8, 12):
            draw.line((0, y, 15, y), fill='#c97b6d')
            for x in range((y % 8) // 2 - 3, 16, 5):
                draw.line((x, y + 1, x, min(y + 3, 13)), fill='#934d51')
        draw.line((0, 14, 15, 14), fill='#5e3e47')
    return image


def shrine():
    image, draw = tile()
    draw.ellipse((0, 12, 15, 15), fill='#5d835c')
    draw.polygon([(0, 15), (0, 7), (3, 3), (7, 1), (9, 1), (13, 3), (15, 7), (15, 15), (10, 15), (10, 8), (8, 6), (7, 6), (5, 8), (5, 15)], fill='#4d6569')
    draw.polygon([(1, 14), (1, 7), (4, 4), (7, 2), (8, 2), (8, 5), (6, 6), (4, 8), (4, 14)], fill='#a5ae94')
    draw.polygon([(9, 2), (12, 4), (14, 7), (14, 14), (11, 14), (11, 8), (9, 6)], fill='#798a81')
    draw.line([(2, 8), (3, 8), (4, 7)], fill='#d0c7a1')
    draw.line([(2, 11), (3, 11)], fill='#677c72')
    draw.line([(12, 11), (14, 11)], fill='#566e6c')
    draw.line([(7, 3), (8, 4), (9, 3)], fill='#94d0ad')
    draw.point([(2, 13), (3, 13), (13, 13), (14, 12)], fill='#81a464')
    return image


def sign():
    image, draw = tile()
    draw.ellipse((3, 12, 12, 15), fill='#609252')
    draw.rectangle((7, 6, 9, 14), fill='#674e3d')
    draw.line((8, 8, 8, 13), fill='#b3885c')
    draw.rectangle((2, 3, 13, 9), fill='#785241')
    draw.rectangle((3, 4, 12, 8), fill='#c59862')
    draw.line((3, 4, 12, 4), fill='#e6b679')
    draw.line([(6, 6), (10, 6), (9, 5)], fill='#784e42')
    draw.point((9, 7), fill='#784e42')
    return image


def glowbell():
    image, draw = tile()
    draw.ellipse((2, 11, 13, 14), fill='#669950')
    draw.line([(6, 13), (7, 9), (7, 6)], fill='#397a53')
    draw.line([(7, 12), (10, 9), (12, 9)], fill='#a2ca6c')
    draw.line([(6, 11), (4, 9), (2, 9)], fill='#83b45c')
    draw.polygon([(5, 3), (9, 3), (11, 5), (11, 7), (9, 9), (5, 9), (3, 7), (3, 5)], fill='#9c7945')
    draw.polygon([(5, 4), (9, 4), (10, 5), (10, 7), (8, 8), (6, 8), (4, 7), (4, 5)], fill='#f7c45f')
    draw.line((5, 6, 9, 6), fill='#ffe9a1')
    draw.point([(6, 5), (8, 5), (7, 7), (2, 3), (12, 2), (13, 6)], fill='#fff3bc')
    return image


def make_tiles():
    tiles = [meadow(i) for i in range(4)]
    tiles += [footpath(i) for i in (4, 5, 6)] + [boardwalk()]
    tiles += [water(i) for i in range(8, 12)]
    tiles += [footpath(12), heath(13), heath(14), footpath(15)]
    tiles += [fir(), oak(), shrub()] + [rock(i) for i in (19, 20, 21)]
    tiles += [small_plant(False), small_plant(True), wooden(24), wooden(25), cottage(False), cottage(True), wooden(28), shrine(), sign(), glowbell()]
    atlas = Image.new('RGBA', (128, 64), TRANSPARENT)
    for i, image in enumerate(tiles):
        atlas.paste(image, ((i % 8) * 16, (i // 8) * 16))
    return atlas, tiles


def player_frame(direction, step):
    image = Image.new('RGBA', (16, 24), TRANSPARENT)
    draw = ImageDraw.Draw(image)
    outline = '#343340'
    hair, hair_shade = '#99664b', '#613f39'
    skin, skin_shade = '#f6c291', '#d38c69'
    shirt, shirt_shade = '#e75977', '#a53558'
    pants, pants_shade = '#456f9a', '#294361'
    boots = '#60515a'
    draw.ellipse((1, 20, 14, 23), fill=(37, 63, 43, 70))

    if direction in (0, 3):
        left_y, right_y = [(20, 20), (19, 21), (21, 19)][step]
        for x, y in [(4, left_y), (9, right_y)]:
            draw.rectangle((x, 17, x + 2, y + 1), fill=outline)
            draw.point((x + 1, y), fill=boots)
            draw.line((x + 1, 18, x + 1, y - 1), fill=pants_shade)
        draw.rectangle((4, 15, 11, 18), fill=outline)
        draw.rectangle((5, 15, 10, 17), fill=pants)
        draw.line((6, 17, 9, 17), fill=pants_shade)

        draw.polygon([(2, 13), (4, 10), (11, 10), (13, 13), (13, 16), (11, 17), (4, 17), (2, 16)], fill=outline)
        draw.polygon([(3, 13), (5, 11), (10, 11), (12, 13), (12, 15), (10, 16), (5, 16), (3, 15)], fill=shirt)
        draw.line((5, 15, 10, 15), fill=shirt_shade)
        draw.point((3, 15 if step != 2 else 14), fill=skin_shade)
        draw.point((12, 15 if step != 1 else 14), fill=skin_shade)

        draw.polygon([(2, 9), (2, 5), (4, 2), (6, 1), (10, 1), (12, 2), (13, 5), (13, 9), (11, 12), (5, 12)], fill=outline)
        if direction == 0:
            draw.polygon([(3, 8), (4, 5), (11, 5), (12, 8), (11, 10), (9, 11), (6, 11), (4, 10)], fill=skin_shade)
            draw.polygon([(4, 7), (6, 5), (10, 5), (11, 7), (11, 9), (9, 10), (6, 10), (4, 9)], fill=skin)
            draw.line((5, 7, 5, 8), fill=outline)
            draw.line((10, 7, 10, 8), fill=outline)
            draw.polygon([(3, 6), (4, 3), (6, 2), (10, 2), (12, 4), (12, 7), (11, 6), (9, 5), (7, 6), (5, 5)], fill=hair_shade)
            draw.polygon([(4, 4), (6, 2), (9, 2), (11, 3), (12, 5), (9, 4), (8, 5), (6, 4), (4, 5)], fill=hair)
            draw.point((7, 9), fill=skin_shade)
            draw.point((8, 12), fill='#f8dda2')
        else:
            draw.polygon([(3, 6), (4, 3), (6, 2), (10, 2), (12, 4), (12, 9), (10, 11), (5, 11), (3, 9)], fill=hair_shade)
            draw.polygon([(4, 5), (6, 2), (10, 2), (12, 4), (12, 8), (9, 9), (6, 8), (4, 9)], fill=hair)
            draw.point([(3, 9), (12, 9)], fill=skin_shade)
            draw.line([(5, 13), (7, 14), (10, 13)], fill=shirt_shade)
        return image

    foot = [(0, 0), (-2, 2), (2, -2)][step]
    for x, y, hue in [(8 + foot[0], 20, pants_shade), (5 + foot[1], 21, pants)]:
        draw.rectangle((x, 17, x + 3, y), fill=outline)
        draw.line((x + 1, 18, x + 2, y - 1), fill=hue, width=2)
        draw.line((x, y + 1, x + 2, y + 1), fill=outline)
        draw.point((x + 1, y), fill=boots)
    draw.polygon([(3, 14), (5, 10), (10, 10), (12, 13), (12, 17), (4, 17)], fill=outline)
    draw.polygon([(4, 14), (6, 11), (9, 11), (11, 13), (11, 16), (5, 16)], fill=shirt)
    draw.line([(7, 13), (9, 13), (9 + step % 2, 15)], fill=shirt_shade)
    draw.point((9 + step % 2, 16), fill=skin_shade)

    draw.polygon([(2, 6), (3, 3), (6, 1), (10, 1), (13, 3), (13, 8), (12, 10), (10, 12), (5, 12), (3, 10), (2, 9)], fill=outline)
    draw.polygon([(3, 6), (5, 4), (10, 4), (11, 8), (10, 10), (7, 11), (5, 10), (3, 9)], fill=skin_shade)
    draw.polygon([(3, 7), (5, 5), (8, 5), (9, 7), (8, 9), (6, 10), (3, 9)], fill=skin)
    draw.line((5, 7, 5, 8), fill=outline)
    draw.polygon([(3, 6), (4, 3), (6, 2), (10, 2), (12, 4), (12, 8), (10, 11), (8, 9), (8, 6), (5, 5)], fill=hair_shade)
    draw.polygon([(4, 4), (6, 2), (10, 2), (12, 4), (12, 7), (10, 8), (10, 5), (7, 4)], fill=hair)
    draw.point((8, 8), fill=skin_shade)
    return image.transpose(Image.Transpose.FLIP_LEFT_RIGHT) if direction == 2 else image


def make_player():
    player = Image.new('RGBA', (48, 96), TRANSPARENT)
    for direction in range(4):
        for step in range(3):
            player.paste(player_frame(direction, step), (step * 16, direction * 24))
    return player


def make_world():
    width, height = 64, 48
    spawn = (12, 27)
    randomizer = random.Random(8047)
    ground = Image.new('L', (width, height))
    terrain = ImageDraw.Draw(ground)
    terrain.polygon([(0, 0), (29, 0), (24, 7), (24, 16), (16, 22), (0, 17)], fill=13)
    terrain.polygon([(42, 0), (63, 0), (63, 17), (54, 19), (45, 17), (40, 12)], fill=14)
    terrain.polygon([(48, 22), (63, 19), (63, 47), (47, 47), (41, 37)], fill=13)
    terrain.polygon([(0, 33), (11, 39), (15, 43), (34, 43), (38, 47), (0, 47)], fill=14)

    for y in range(height):
        for x in range(width):
            if ground.getpixel((x, y)) == 0:
                ground.putpixel((x, y), randomizer.choices([0, 1, 2, 3], weights=[50, 27, 9, 14])[0])

    terrain.polygon([(20, 22), (25, 20), (29, 20), (34, 23), (36, 27), (34, 32), (31, 34), (24, 34), (20, 32), (18, 28)], fill=6)
    terrain.polygon([(21, 23), (25, 21), (29, 21), (33, 24), (35, 27), (33, 31), (31, 33), (24, 33), (21, 31), (19, 28)], fill=8)
    terrain.polygon([(23, 24), (26, 23), (30, 23), (32, 25), (33, 28), (30, 31), (25, 31), (22, 30), (21, 28)], fill=9)
    terrain.polygon([(25, 25), (28, 24), (30, 26), (30, 29), (25, 29), (24, 28)], fill=10)
    river = [(40, -3), (39, 3), (36, 7), (36, 13), (39, 19), (41, 23), (39, 29), (39, 34), (42, 40), (45, 48)]
    for size, tile_id in [(7, 6), (5, 8), (3, 9)]:
        terrain.line(river, fill=tile_id, width=size, joint='curve')
    for x, y in [(37, 10), (36, 12), (36, 13), (38, 17), (39, 20), (41, 23), (39, 31), (40, 36), (42, 41)]:
        terrain.point((x, y), fill=11)

    west_trail = [(8, 28), (14, 28), (15, 25), (15, 21), (16, 18), (14, 15), (12, 14), (12, 12)]
    lake_trail = [(14, 28), (15, 32), (18, 35), (24, 36), (29, 36), (33, 36), (36, 33), (36, 29), (35, 27), (36, 26)]
    east_trail = [(43, 26), (46, 26), (47, 28), (50, 29), (52, 29), (55, 33), (58, 34)]
    ridge_trail = [(45, 26), (46, 22), (47, 19), (49, 17), (48, 14), (46, 12), (46, 9)]
    outer_trail = [(15, 21), (18, 18), (22, 16), (26, 16), (29, 15), (31, 13), (32, 10)]
    trails = [west_trail, lake_trail, east_trail, ridge_trail, outer_trail]
    for route in trails:
        terrain.line(route, fill=12 if route == ridge_trail else 4, width=2, joint='curve')

    terrain.rectangle((35, 25, 44, 26), fill=7)
    terrain.rectangle((8, 26, 13, 30), fill=15)
    terrain.rectangle((45, 7, 47, 11), fill=15)
    for y in range(height):
        for x in range(width):
            if ground.getpixel((x, y)) == 4 and randomizer.random() < .24:
                ground.putpixel((x, y), 5)

    objects = Image.new('L', (width, height))
    scenery = ImageDraw.Draw(objects)
    scenery.polygon([(0, 0), (24, 0), (24, 4), (18, 8), (12, 6), (7, 9), (0, 12)], fill=16)
    scenery.polygon([(43, 0), (63, 0), (63, 11), (58, 12), (52, 8), (50, 6), (43, 5)], fill=20)
    scenery.polygon([(58, 15), (63, 12), (63, 47), (49, 47), (52, 42), (59, 40), (60, 35), (62, 26)], fill=16)
    scenery.polygon([(0, 38), (5, 42), (12, 44), (22, 46), (33, 44), (43, 46), (44, 47), (0, 47)], fill=16)
    scenery.rectangle((0, 7, 0, 38), fill=17)
    scenery.rectangle((1, 0, 62, 0), fill=16)
    scenery.rectangle((62, 0, 63, 47), fill=16)

    for y in range(1, height - 1):
        for x in range(1, width - 1):
            tile_id = ground.getpixel((x, y))
            existing = objects.getpixel((x, y))
            if existing == 16:
                objects.putpixel((x, y), randomizer.choices([16, 17, 18], weights=[66, 29, 5])[0])
            elif existing == 20 and randomizer.random() < .21:
                objects.putpixel((x, y), 21)
            elif not existing and tile_id in (0, 1, 2, 3, 13, 14):
                woodland = tile_id == 13 and (x > 50 or x < 10 or y < 9)
                if randomizer.random() < (.26 if woodland else .08):
                    choices = [16, 17, 18, 19, 22, 23, 24, 28]
                    weights = [29, 26, 13, 3, 19, 3, 4, 3] if woodland else [9, 12, 13, 12, 25, 22, 3, 4]
                    objects.putpixel((x, y), randomizer.choices(choices, weights=weights)[0])

    scenery.rectangle((6, 22, 15, 31), fill=0)
    scenery.rectangle((7, 22, 9, 23), fill=27)
    scenery.rectangle((7, 24, 9, 24), fill=26)
    scenery.rectangle((12, 21, 13, 22), fill=27)
    scenery.rectangle((12, 23, 13, 23), fill=26)
    scenery.line([(6, 25), (8, 25)], fill=25, width=1)
    scenery.line([(7, 31), (11, 31)], fill=25, width=1)
    for x, y, tile_id in [(6, 26, 23), (10, 24, 18), (7, 29, 23), (11, 25, 23), (13, 29, 23), (14, 28, 18), (15, 30, 22)]:
        scenery.point((x, y), fill=tile_id)

    specials = {
        (13, 26): 30,
        (12, 12): 31,
        (29, 36): 31,
        (53, 30): 31,
        (46, 8): 29,
    }
    for x, y in [spawn, (12, 26), (13, 27), (14, 28), (46, 9)]:
        scenery.point((x, y), fill=0)
    for (x, y), tile_id in specials.items():
        scenery.point((x, y), fill=tile_id)

    reachable = reachable_cells(ground, objects, spawn)
    assert all(position in reachable for position, item in specials.items() if item == 31)
    assert (46, 9) in reachable
    assert (13, 27) in reachable

    write_world(ASSETS / 'world.rpgw', ground, objects, spawn)
    return ground, objects, spawn


def write_world(path, ground, objects, spawn):
    header = struct.pack('<4s5H4B', b'RPGW', 1, ground.width, ground.height, *spawn, 0, 0, 0, 0)
    path.write_bytes(header + ground.tobytes() + objects.tobytes())


def make_official_world(ground, objects, spawn):
    ground, objects = ground.copy(), objects.copy()
    terrain, scenery = ImageDraw.Draw(ground), ImageDraw.Draw(objects)
    terrain.line([(50, 29), (49, 33), (51, 35), (51, 37)], fill=12, width=2, joint='curve')
    terrain.polygon([(49, 36), (53, 36), (55, 38), (55, 41), (53, 43), (49, 43), (47, 41), (47, 38)], fill=15)
    scenery.rectangle((48, 34, 54, 43), fill=0)
    scenery.rectangle((49, 37, 53, 37), fill=21)
    scenery.rectangle((48, 38, 48, 41), fill=19)
    scenery.rectangle((54, 38, 54, 41), fill=19)
    for point in [(49, 42), (53, 42), (50, 38), (52, 38)]:
        scenery.point(point, fill=23)
    scenery.point((51, 40), fill=31)
    # Leave a gap in the ruined wall at the end of the trail.
    scenery.point((51, 37), fill=0)

    reachable = reachable_cells(ground, objects, spawn)
    objectives = {(x, y) for y in range(objects.height) for x in range(objects.width)
                  if objects.getpixel((x, y)) == 31}
    assert len(objectives) == 4 and objectives <= reachable
    assert {(13, 27), (46, 9)} <= reachable

    updates = ASSETS.parent / 'updates'
    updates.mkdir(exist_ok=True)
    write_world(updates / 'world-1.rpgw', ground, objects, spawn)


def reachable_cells(ground, objects, spawn):
    reachable = {spawn}
    queue = deque([spawn])
    while queue:
        x, y = queue.popleft()
        for x2, y2 in [(x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)]:
            position = (x2, y2)
            if position in reachable or not (0 <= x2 < ground.width and 0 <= y2 < ground.height):
                continue
            if ground.getpixel(position) in range(8, 12) or objects.getpixel(position) in (*range(16, 22), *range(24, 31)):
                continue
            reachable.add(position)
            queue.append(position)
    return reachable


def make_preview(tiles, player, ground, objects, spawn):
    preview = Image.new('RGBA', (ground.width * 16, ground.height * 16))
    for y in range(ground.height):
        for x in range(ground.width):
            position = (x * 16, y * 16)
            preview.paste(tiles[ground.getpixel((x, y))], position)
            object_id = objects.getpixel((x, y))
            if object_id:
                preview.alpha_composite(tiles[object_id], position)
    preview.alpha_composite(player.crop((0, 0, 16, 24)), (spawn[0] * 16, spawn[1] * 16 - 8))
    preview.convert('RGB').save(ASSETS / 'world-preview.png', optimize=True)
    return preview


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
    banner = preview.crop((96, 352, 608, 608)).resize((256, 128), Image.Resampling.NEAREST)
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
    ASSETS.mkdir(exist_ok=True)
    atlas, tiles = make_tiles()
    player = make_player()
    for name, image in [('tiles', atlas), ('player', player)]:
        image.save(ASSETS / f'{name}.png', optimize=True)
        (ASSETS / f'{name}.rgba').write_bytes(image.tobytes())
    ground, objects, spawn = make_world()
    make_official_world(ground, objects, spawn)
    preview = make_preview(tiles, player, ground, objects, spawn)
    make_icon(tiles)
    make_banner(preview, tiles)


if __name__ == '__main__':
    main()
