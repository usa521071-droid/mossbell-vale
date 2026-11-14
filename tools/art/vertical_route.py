"""Original terrain transitions and the Mossbell berryblob animation."""

from PIL import Image, ImageDraw


def side_stairs(terrain, east, cave=False):
    base = terrain[56 if cave else 60]
    ledge = terrain[58 if cave else 61]
    width = 32 if cave else 48
    image = Image.new('RGBA', (width, 16))
    for x in range(0, width, 16):
        image.paste(ledge if x not in (0, width - 16) else base, (x, 0))
    draw = ImageDraw.Draw(image)
    outline, rail, mass, tread, lip = ('#66434a', '#976257', '#a98668', '#d2a37d', '#ead0a0') if cave else (
        '#586259', '#90917a', '#ada184', '#ccb88e', '#ebd6a1')
    # Low terrace at the left and slightly raised crest at the right.
    draw.polygon([(3, 10), (3, 6), (11, 5), (width-12, 3), (width-5, 3), (width-3, 4),
                  (width-3, 9), (width-8, 11), (11, 13), (5, 13)], fill=outline)
    draw.polygon([(4, 9), (4, 7), (11, 6), (width-12, 4), (width-5, 4), (width-4, 5),
                  (width-4, 8), (width-8, 10), (11, 12), (6, 12)], fill=mass)
    draw.line([(6, 7), (12, 6), (width-12, 4), (width-5, 4)], fill=rail)
    draw.line([(5, 12), (11, 12), (width-8, 10), (width-5, 9)], fill=rail)
    for x in range(7, width-5, 4):
        y = 7 - (x-5)*3//(width-9)
        draw.line((x, y, x, y+4), fill=lip)
        draw.rectangle((x+1, y, x+2, y+4), fill=tread)
        draw.point([(x+2, y+5), (x+3, y+4)], fill=mass)
    draw.line([(4, 8), (4, 10), (7, 11)], fill=lip)
    draw.line([(width-5, 5), (width-5, 8)], fill=lip)
    if not east:
        image = image.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    tiles = [image.crop((x, 0, x+16, 16)) for x in range(0, width, 16)]
    return tiles if east else list(reversed(tiles))


def ladder(terrain, cave=False):
    base = terrain[56 if cave else 60]
    ledge = terrain[58 if cave else 61]
    image = Image.new('RGBA', (16, 48))
    for y in (0, 16, 32):
        image.paste(ledge if y == 16 else base, (0, y))
    draw = ImageDraw.Draw(image)
    stone_deep, stone_light = ('#6b4749', '#cc9d81') if cave else ('#58625b', '#bfc0a3')
    beam_dark, beam, woodlight = ('#58454a', '#a27560', '#d6aa81') if cave else (
        '#6b594b', '#a48661', '#d9b985')
    draw.polygon([(0, 13), (3, 12), (12, 12), (15, 13), (15, 16), (0, 16)], fill=stone_deep)
    draw.line([(1, 13), (4, 12), (11, 12), (14, 13)], fill=stone_light)
    draw.line([(0, 32), (4, 32), (5, 33), (11, 33), (14, 32), (15, 32)], fill=stone_deep, width=2)
    draw.polygon([(3, 8), (4, 6), (13, 6), (14, 8), (14, 41), (12, 43), (4, 43), (3, 41)], fill=beam_dark)
    # Remove the center of the silhouette. Rock remains visible between individual rungs.
    for y in range(8, 42):
        backing = ledge if 16 <= y < 32 else base
        for x in range(6, 11):
            image.putpixel((x, y), backing.getpixel((x, y % 16)))
    draw.line((4, 7, 4, 41), fill=woodlight)
    draw.line((5, 8, 5, 42), fill=beam)
    draw.line((11, 8, 11, 42), fill=beam)
    draw.line((12, 7, 12, 41), fill=woodlight)
    for y in range(9, 41, 5):
        draw.line((5, y+2, 12, y+2), fill=beam_dark)
        draw.line((5, y+1, 12, y+1), fill=beam)
        draw.line((6, y, 11, y), fill=woodlight)
    draw.point([(4, 8), (12, 8), (4, 40), (12, 40)], fill='#efe0ae')
    return [image.crop((0, y, 16, y+16)) for y in (0, 16, 32)]


def vertical_route(terrain):
    """Append the walkable 16×16 transitions at ground IDs 128–143."""
    tiles = side_stairs(terrain, True) + side_stairs(terrain, False)
    tiles += ladder(terrain) + ladder(terrain, cave=True)
    tiles += side_stairs(terrain, True, cave=True) + side_stairs(terrain, False, cave=True)
    if isinstance(terrain, list):
        terrain[128:144] = tiles
    else:
        terrain.update(enumerate(tiles, start=128))
    return terrain


def blob_sheet():
    """Three transparent 16×16 poses in idle, lift, landing order."""
    sheet = Image.new('RGBA', (48, 16))
    poses = [
        [(5, 4), (10, 4), (12, 6), (12, 7), (14, 9), (14, 11), (12, 13), (4, 13),
         (2, 12), (1, 10), (2, 8), (3, 7), (3, 6)],
        [(5, 2), (9, 2), (10, 3), (12, 4), (13, 6), (13, 9), (11, 11), (5, 12),
         (3, 11), (2, 9), (2, 6), (3, 4)],
        [(5, 6), (10, 6), (12, 7), (13, 8), (14, 9), (15, 11), (14, 13), (11, 14),
         (4, 14), (2, 13), (1, 12), (0, 11), (1, 9), (3, 7)],
    ]
    for frame, perimeter in enumerate(poses):
        image = Image.new('RGBA', (16, 16))
        draw = ImageDraw.Draw(image)
        draw.ellipse((3, 13, 14, 15) if frame == 1 else (1, 13, 15, 15), fill=(83, 61, 76, 92))
        draw.polygon(perimeter, fill='#703e5a')
        shift = (0, -2, 2)[frame]
        lower = (12, 11, 13)[frame]
        draw.polygon([(5, 5+shift), (10, 5+shift), (12, 7+shift), (12, 8+shift), (13, 9+shift),
                      (13, lower-1), (11, lower), (4, lower), (2, lower-1), (2, 10+shift),
                      (4, 8+shift), (4, 6+shift)], fill='#df7e9d')
        draw.polygon([(5, 5+shift), (9, 5+shift), (11, 6+shift), (11, 9+shift), (9, 11+shift),
                      (5, 11+shift), (3, 10+shift), (4, 8+shift), (4, 7+shift)], fill='#ed9fb2')
        draw.line([(5, 7+shift), (6, 6+shift), (8, 6+shift)], fill='#ffd2cf')
        draw.line((5, 8+shift, 5, 9+shift), fill='#fff2e9')
        draw.line((6, 8+shift, 6, 9+shift), fill='#4a354c')
        draw.line((10, 8+shift, 10, 9+shift), fill='#4a354c')
        draw.point((8, 10+shift), fill='#a35473')
        draw.point([(4, 10+shift), (11, 10+shift)], fill='#d8688c')
        sheet.paste(image, (frame*16, 0))
    return sheet
