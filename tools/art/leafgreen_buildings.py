"""Original modular town architecture and ironwork for the 16-pixel atlas."""

from PIL import Image, ImageDraw


ROOF_INK = '#35455f'
ROOF_OUTLINE = '#465c79'
ROOF_SEAM = '#6f9bb6'
ROOF_FACE = '#86bbce'
WALL_INK = '#465365'
WALL_BODY = '#ead8ae'
WINDOW_INK = '#404d66'


def empty(width=16, height=16):
    return Image.new('RGBA', (width, height))


def cells(image):
    width, height = image.size
    return [image.crop((x, y, x + 16, y + 16))
            for y in range(0, height, 16) for x in range(0, width, 16)]


def blue_roof():
    image = empty(48, 48)
    draw = ImageDraw.Draw(image)
    draw.polygon([(0, 9), (3, 7), (7, 5), (11, 3), (15, 1), (32, 1),
                  (36, 3), (40, 5), (44, 7), (47, 9), (47, 46),
                  (46, 47), (1, 47), (0, 46)], fill=ROOF_INK)
    draw.polygon([(1, 10), (15, 3), (32, 3), (46, 10), (46, 40), (1, 40)],
                 fill=ROOF_FACE)
    draw.polygon([(1, 10), (15, 3), (15, 38), (1, 44)], fill='#95c8d6')
    draw.polygon([(32, 3), (46, 10), (46, 44), (32, 38)], fill='#77a9c0')
    draw.line([(2, 10), (15, 3), (32, 3), (45, 10)], fill='#badce0')
    draw.line((15, 3, 15, 38), fill='#b7d9de')
    draw.line((32, 3, 32, 38), fill='#6d9ab4')
    draw.line((2, 11, 2, 39), fill='#a7d2d8')
    draw.line((45, 11, 45, 39), fill='#89b7ca')

    for y in (8, 13, 18, 23, 28, 33, 38):
        draw.line((16, y, 31, y), fill=ROOF_SEAM)
        draw.line((16, y + 1, 31, y + 1), fill='#98c7d6')

    draw.polygon([(1, 43), (15, 37), (32, 37), (46, 43), (46, 46), (1, 46)],
                 fill=ROOF_OUTLINE)
    draw.line([(1, 43), (15, 37), (32, 37), (46, 43)], fill='#d1dee2')
    draw.line([(1, 44), (15, 38), (32, 38), (46, 44)], fill='#9db9cb')
    draw.line([(1, 46), (15, 41), (32, 41), (46, 46)], fill='#617992')
    draw.line((1, 47, 46, 47), fill=ROOF_INK)
    return image


def facade_base():
    image = empty(48, 32)
    draw = ImageDraw.Draw(image)
    draw.rectangle((0, 0, 47, 30), fill=WALL_INK)
    draw.rectangle((1, 1, 46, 30), fill=WALL_BODY)
    draw.rectangle((1, 0, 46, 3), fill='#88949b')
    draw.line((1, 4, 46, 4), fill='#b8a793')
    draw.line((1, 5, 46, 5), fill='#f4e6c3')
    for y in (11, 19, 25):
        draw.line((2, y, 45, y), fill='#cbb78e')
        draw.line((2, y + 1, 45, y + 1), fill='#f8ebcb')
    draw.rectangle((1, 27, 46, 30), fill='#82979f')
    draw.line((2, 27, 45, 27), fill='#becaca')
    draw.line((0, 30, 47, 30), fill=WALL_INK)
    draw.line((1, 31, 46, 31), fill=(42, 70, 83, 90))
    draw.rectangle((1, 4, 3, 29), fill='#a5aca2')
    draw.line((2, 5, 2, 26), fill='#e2dac0')
    draw.rectangle((44, 4, 46, 29), fill='#8b9a9d')
    draw.line((44, 5, 44, 26), fill='#bbbcad')
    return image


def window(draw, x, y, width=10):
    right = x + width - 1
    draw.rectangle((x, y, right, y + 8), fill=WINDOW_INK)
    draw.rectangle((x + 1, y + 1, right - 1, y + 6), fill='#79a8c7')
    draw.line((x + 1, y + 1, right - 1, y + 1), fill='#adcedb')
    draw.polygon([(x + 1, y + 4), (x + 3, y + 3), (x + 6, y + 3),
                  (x + 4, y + 5), (x + 1, y + 5)], fill='#9dc4d5')
    muntin = x + width // 2
    draw.line((muntin, y + 1, muntin, y + 6), fill='#516a86')
    draw.line((x, y + 8, right, y + 8), fill='#b9ccd0')
    draw.line((x, y + 9, right, y + 9), fill='#738895')


def entrance(draw, x, y):
    draw.rectangle((x + 1, y, x + 14, y + 14), fill=WALL_INK)
    draw.rectangle((x + 2, y + 1, x + 13, y + 13), fill='#79919b')
    draw.rectangle((x + 3, y + 1, x + 12, y + 13), fill='#506b82')
    draw.rectangle((x + 4, y + 2, x + 11, y + 12), fill='#84aab9')
    draw.line((x + 4, y + 2, x + 11, y + 2), fill='#b6cad1')
    draw.line((x + 4, y + 7, x + 11, y + 7), fill='#6a869a')
    for offset in (6, 9):
        draw.line((x + offset, y + 3, x + offset, y + 6), fill='#6789a1')
    draw.point((x + 10, y + 9), fill='#e9dfab')
    draw.line((x + 4, y + 12, x + 11, y + 12), fill='#637c90')
    draw.line((x + 1, y + 14, x + 14, y + 14), fill='#bdc6bc')


def facades():
    full = facade_base()
    draw = ImageDraw.Draw(full)
    for x in (5, 33):
        window(draw, x, 20)
    draw.line((16, 12, 31, 12), fill='#f5e5c1')
    draw.line((16, 13, 31, 13), fill='#e5cea5')
    entrance(draw, 16, 16)

    door = facade_base().crop((16, 16, 32, 32))
    entrance(ImageDraw.Draw(door), 0, 0)

    glass = facade_base().crop((16, 16, 32, 32))
    window(ImageDraw.Draw(glass), 2, 4, 12)
    return cells(full), door, glass


def red_roof():
    image = empty()
    draw = ImageDraw.Draw(image)
    draw.rectangle((0, 0, 15, 15), fill='#ae777c')
    for y in (0, 4, 8, 12):
        draw.line((0, y, 15, y), fill='#7d5467')
        draw.line((0, y + 1, 15, y + 1), fill='#cb9393')
        for x in ((3, 11) if y % 8 == 0 else (7, 15)):
            draw.line((x, y + 1, x, y + 3), fill='#986573')
    return image


def awning():
    image = empty()
    draw = ImageDraw.Draw(image)
    draw.rectangle((0, 0, 15, 15), fill='#788b91')
    draw.line((0, 0, 15, 0), fill=ROOF_OUTLINE)
    draw.line((0, 1, 15, 1), fill='#c1b9a8')
    draw.line((0, 2, 15, 2), fill='#6f5b60')
    for left in (0, 8):
        draw.rectangle((left, 3, left + 3, 11), fill='#d89666')
        draw.rectangle((left + 4, 3, left + 7, 11), fill='#deb779')
        draw.line((left, 3, left, 10), fill='#edbc91')
        draw.line((left + 4, 3, left + 4, 10), fill='#f5d694')
        draw.line((left + 3, 4, left + 3, 10), fill='#af7265')
        draw.line((left + 7, 4, left + 7, 10), fill='#b38a66')
        draw.rectangle((left, 12, left + 3, 13), fill='#a77066')
        draw.rectangle((left + 4, 12, left + 7, 13), fill='#b89063')
        draw.line((left, 13, left + 7, 13), fill='#7a5667')
        draw.point((left, 14), fill='#644f60')
    draw.line((0, 15, 15, 15), fill='#536072')
    return image


def chimney():
    image = empty()
    draw = ImageDraw.Draw(image)
    draw.polygon([(4, 13), (3, 4), (6, 2), (12, 2), (14, 4), (14, 11),
                  (11, 14), (6, 15)], fill='#50607a')
    draw.polygon([(5, 5), (9, 5), (9, 13), (5, 13)], fill='#aebabb')
    draw.rectangle((10, 5, 13, 12), fill='#81939f')
    draw.polygon([(4, 4), (7, 2), (12, 2), (14, 4), (11, 6), (5, 6)], fill='#d2d1c2')
    draw.line((6, 4, 12, 4), fill='#69798a')
    draw.line((5, 9, 9, 9), fill='#8d9c9d')
    draw.point((11, 9), fill='#65798e')
    return image


def fence_post(draw, x, y=2):
    draw.polygon([(x, y + 3), (x + 1, y + 1), (x + 3, y), (x + 5, y + 1),
                  (x + 6, y + 3), (x + 6, y + 11), (x + 5, y + 12),
                  (x, y + 12)], fill='#58677b')
    draw.rectangle((x + 1, y + 3, x + 5, y + 10), fill='#9ca7ae')
    draw.polygon([(x + 1, y + 3), (x + 3, y + 1), (x + 4, y + 2),
                  (x + 5, y + 3), (x + 5, y + 4), (x + 1, y + 4)], fill='#c9cecb')
    draw.line((x + 1, y + 4, x + 1, y + 10), fill='#d7dbd3')
    draw.line((x + 4, y + 6, x + 4, y + 10), fill='#82939f')
    draw.line((x + 1, y + 11, x + 5, y + 11), fill='#788697')


def fence(kind):
    image = empty()
    draw = ImageDraw.Draw(image)
    if kind == 3:
        draw.rectangle((7, 0, 10, 13), fill='#59677a')
        draw.line((8, 0, 8, 11), fill='#91a4a8')
        draw.polygon([(8, 11), (11, 13), (13, 15), (8, 15)], fill=(49, 81, 81, 95))
        fence_post(draw, 4)
        return image

    left, right = [(5, 15), (0, 15), (0, 12)][kind]
    posts = [(5,), (0, 8), (0, 6)][kind]
    for x in posts:
        draw.polygon([(x + 1, 10), (x + 5, 11), (x + 8, 14), (x + 8, 15),
                      (x + 4, 15), (x + 2, 13)], fill=(49, 81, 81, 98))
    draw.rectangle((left, 6, right, 10), fill='#626a80')
    draw.line((left, 7, right, 7), fill='#acb4b9')
    draw.line((left, 9, right, 9), fill='#818f9e')
    for x in posts:
        fence_post(draw, x)
    return image


def cast_shadow(east):
    image = empty()
    draw = ImageDraw.Draw(image)
    edge = (45, 83, 90, 82)
    softness = (55, 109, 107, 49)
    if east:
        draw.rectangle((0, 0, 2, 15), fill=edge)
        draw.line((3, 0, 3, 15), fill=softness)
    else:
        draw.rectangle((0, 0, 15, 2), fill=edge)
        draw.line((0, 3, 15, 3), fill=softness)
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
