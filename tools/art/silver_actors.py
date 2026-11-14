"""Draw the original Silver-style overworld actors on a 16×16 frame grid."""

from PIL import Image, ImageColor

INK = '#202433'
CREAM = '#f6f2d8'
SHIRT = '#e75977'  # Existing wardrobe marker for the first shirt color.

FRONT = (
    '................',
    '.....#######....',
    '....#rrrrrrr#...',
    '...#rwwrrrrrr#..',
    '...#rrrrrrrrr#..',
    '...###########..',
    '....#w#ww#w#....',
    '....#w#ww#w#....',
    '.....#wwww#.....',
    '....##r##r##....',
    '...#w#rrrr#w#...',
    '...###rrrr###...',
    '.....######.....',
    '.....##..##.....',
    '....###..###....',
    '................',
)
LEFT = (
    '................',
    '.....#######....',
    '....#rrrrrrr#...',
    '...#rrwwrrrr#...',
    '..#rrrrrrrrr#...',
    '.###########....',
    '..#ww#ww###.....',
    '..#ww#www##.....',
    '...#wwww###.....',
    '....####rr##....',
    '....#rr#w#r#....',
    '.....#r#w#r#....',
    '.....######.....',
    '.....##.##......',
    '....###.###.....',
    '................',
)
BACK = (
    '................',
    '.....#######....',
    '....#rrrrrrr#...',
    '...#rrwwrrrrr#..',
    '...#rrrrrrrrr#..',
    '...##rrrrrrr##..',
    '....########....',
    '....###ww###....',
    '.....#wwww#.....',
    '....#######.....',
    '...#w#rrww#r#...',
    '...###rrww###...',
    '.....######.....',
    '.....##..##.....',
    '....###..###....',
    '................',
)
STEPS = (
    ('.....##..###....', '....###..###....', '.........###....'),
    ('....###..##.....', '....###..###....', '....###.........'),
)
SIDE_STEPS = (
    ('....###...##....', '...####...##....', '..........###...'),
    ('......##.##.....', '.....###.###....', '.....###........'),
)


def sprite(rows, accent):
    if len(rows) != 16 or any(len(row) != 16 for row in rows):
        raise ValueError('Actor frames must be 16×16')
    palette = {'.': (0, 0, 0, 0), '#': ImageColor.getcolor(INK, 'RGBA'),
               'w': ImageColor.getcolor(CREAM, 'RGBA'), 'r': ImageColor.getcolor(accent, 'RGBA')}
    image = Image.new('RGBA', (16, 16))
    image.putdata([palette[pixel] for row in rows for pixel in row])
    return image


def frames():
    sheet = Image.new('RGBA', (48, 64))
    for direction, pose in enumerate((FRONT, LEFT, LEFT, BACK)):
        side = direction in (1, 2)
        for step in range(3):
            rows = list(pose)
            if step:
                rows[13:16] = (SIDE_STEPS if side else STEPS)[step - 1]
                if side:
                    rows[10] = '....#r#w#rr#....' if step == 1 else '....#rrr#w##....'
                else:
                    rows[11] = ('...#w#rrrr##....' if step == 1 else '....##rrrr#w#...')
            if direction == 2:
                rows = [row[::-1] for row in rows]
            sheet.paste(sprite(rows, SHIRT), (step * 16, direction * 16))
    return sheet


# The townsfolk share eye position and size with the explorer, with distinct silhouettes.
TOWNSFOLK = (
    ('................', '.....######.....', '....#rrrrrr#....', '...#rrrrrrrr#...',
     '...#rrr###rr#...', '...##r#www#r#...', '...#r#w#w#w##...', '...#r#w#w#w##...',
     '....#wwwww##....', '...###www###....', '...#rr###rr#....', '...#w##r##w#....',
     '....#rrrrr#.....', '....#rrrrr#.....', '....###.###.....', '................'),
    ('................', '................', '.....#######....', '....#rrrrrrr#...',
     '...#rrrrrrrrr#..', '..############..', '....#w#ww#w#....', '....#w#ww#w#....',
     '.....#www##.....', '....###w###.....', '...#wr#r#rw#....', '...##r#r#r##....',
     '....#r###r#.....', '....###.###.....', '....###.###.....', '................'),
    ('................', '......####......', '.....#rrrr#.....', '....#rrrrrr#....',
     '...#rrrrrrrr#...', '...#rr####rr#...', '....#r#ww#r#....', '....#w#ww#w#....',
     '.....#wwww#.....', '....##rwwr##....', '...#w#rrrr#w#...',
     '...#w#rrrr#w#...', '....#r####r#....', '.....##..##.....', '....###..###....',
     '................'),
    ('................', '......####......', '.....#rrrr#.....', '....#rrrrrr#....',
     '....#rwwrrr#....', '...##rrrrrr##...', '...##########...',
     '....#w#ww#w#....', '.....#wwww#.....', '....###ww###....',
     '...#rr####rr#...', '...#w##rr##w#...', '....#r####r#....', '....##...##.....',
     '....###.###.....', '................'),
)
NPC_ACCENTS = ('#b671a0', '#5279ac', '#668e55', '#c67448')


def npc_sheet():
    sheet = Image.new('RGBA', (64, 32))
    for variant, (pose, accent) in enumerate(zip(TOWNSFOLK, NPC_ACCENTS)):
        sheet.paste(sprite(pose, accent), (variant * 16, 0))
        alternate = list(pose)
        alternate[13] = '.....##.###.....'
        alternate[14] = '....###..###....'
        sheet.paste(sprite(alternate, accent), (variant * 16, 16))
    return sheet


def blob_sheet():
    idle = (
        '................', '................', '................', '.....######.....',
        '...##wrrrrr##...', '..#wwrrrrrrrr#..', '..#wrrrrrrrrr#..', '.#rr##rr##rrrr#.',
        '.#rr##rr##rrrr#.', '.#rrrrrrrrrrrr#.', '.#rrrr#rrrrrrr#.', '..#rrrrrrrrrr#..',
        '...##########...', '................', '................', '................',
    )
    squish = (
        '................', '................', '................', '................',
        '................', '....########....', '..##wwrrrrrr##..', '.#wrrrrrrrrrrr#.',
        '#rr##rrrr##rrrr#', '#rr##rrrr##rrrr#', '#rrrrr#rrrrrrrr#', '.#rrrrrrrrrrrr#.',
        '..############..', '................', '................', '................',
    )
    sheet = Image.new('RGBA', (48, 16))
    for step, pose in enumerate((idle, idle[2:] + idle[:2], squish)):
        sheet.paste(sprite(pose, '#ee91af'), (step * 16, 0))
    return sheet
