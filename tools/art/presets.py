"""Build the editable city, cave and mountain maps from the shared tileset."""

from collections import deque
import random
import struct

from PIL import Image, ImageDraw


ADJACENT = [(0, -1), (1, 0), (0, 1), (-1, 0)]
DIAGONALS = [(-1, -1), (1, -1), (1, 1), (-1, 1)]
CORNER_INDEX = {(0, 3): 4, (0, 1): 5, (1, 2): 6, (2, 3): 7}


class Map:
    def __init__(self, width, height, spawn, terrain, seed):
        self.ground = Image.new('L', (width, height))
        self.objects = Image.new('L', self.ground.size)
        self.floor = ImageDraw.Draw(self.ground)
        self.scenery = ImageDraw.Draw(self.objects)
        self.spawn = spawn
        self.random = random.Random(seed)
        self.ground.putdata(self.random.choices(*terrain, k=width * height))

    def cells(self):
        for y in range(self.ground.height):
            for x in range(self.ground.width):
                yield x, y

    def reach(self):
        visited = {self.spawn}
        queue = deque(visited)
        while queue:
            x, y = queue.popleft()
            for dx, dy in ADJACENT:
                p = (x + dx, y + dy)
                if p in visited or not (0 <= p[0] < self.ground.width and 0 <= p[1] < self.ground.height):
                    continue
                if self.ground.getpixel(p) in range(8, 12):
                    continue
                if self.objects.getpixel(p) not in (0, 22, 23, 31, 86, 87, 95, *range(120, 126)):
                    continue
                visited.add(p)
                queue.append(p)
        return visited

    def save(self, path):
        reachable = self.reach()
        assert self.ground.getpixel(self.spawn) not in range(8, 12) and not self.objects.getpixel(self.spawn)
        objectives = [point for point in self.cells() if self.objects.getpixel(point) in (29, 30, 31)]
        assert len(objectives) in (5, 6)
        for x, y in objectives:
            if self.objects.getpixel((x, y)) == 31:
                assert (x, y) in reachable, (path, x, y)
            else:
                assert any((x+dx, y+dy) in reachable for dx, dy in ADJACENT), (path, x, y)
        header = struct.pack('<4s5H4B', b'RPGW', 1, *self.ground.size, *self.spawn, 0, 0, 0, 0)
        path.write_bytes(header + self.ground.tobytes() + self.objects.tobytes())

    def stamp(self, x, y, width, ids):
        for i, tile_id in enumerate(ids):
            self.objects.putpixel((x + i % width, y + i // width), tile_id)


def outline(mask, point):
    x, y = point

    def inside(dx, dy):
        xx, yy = x + dx, y + dy
        if not (0 <= xx < mask.width and 0 <= yy < mask.height):
            return True
        return bool(mask.getpixel((xx, yy)))

    missing = tuple(i for i, (dx, dy) in enumerate(ADJACENT) if not inside(dx, dy))
    if len(missing) > 1:
        for side_pair, index in CORNER_INDEX.items():
            if all(side in missing for side in side_pair):
                return index
    if missing:
        return missing[0]
    for i, (dx, dy) in enumerate(DIAGONALS):
        if not inside(dx, dy):
            return i + 8
    return None


def surface(world, mask, base, fill, objects=False):
    for point in world.cells():
        if not mask.getpixel(point):
            continue
        edge = outline(mask, point)
        if edge is None:
            world.ground.putpixel(point, fill(point) if callable(fill) else fill)
        elif objects:
            world.objects.putpixel(point, base + edge)
        else:
            world.ground.putpixel(point, base + edge)


def house(world, x, y, width=3):
    assert width >= 3
    for row in range(3):
        world.stamp(x, y + row, width, [64 + row * 3] + [65 + row * 3] * (width - 2) + [66 + row * 3])
    for row in range(2):
        middle = [74 + 3 * row] * (width - 2)
        if row == 1:
            middle = [80] * (width - 2)
            middle[len(middle) // 2] = 77
        world.stamp(x, y + row + 3, width, [73 + row * 3] + middle + [75 + row * 3])
    world.stamp(x, y + 5, width, [87] * width)
    for row in range(6):
        world.objects.putpixel((x + width, y + row), 86)


def big_oak(world, x, y):
    world.stamp(x, y, 3, range(88, 94))
    world.objects.putpixel((x + 1, y + 2), 94)
    world.objects.putpixel((x + 2, y + 2), 95)


def fence(world, x, y, width):
    world.stamp(x, y, width, [82] + [83] * (width - 2) + [84])


def make_city(official=False):
    world = Map(44, 31, (16, 15), ([0, 1, 2, 3], [71, 18, 3, 8]), 215)
    trails = Image.new('1', world.ground.size)
    draw = ImageDraw.Draw(trails)
    for box in [(3, 14, 42, 17), (15, 2, 18, 26), (27, 11, 30, 25), (25, 19, 40, 23), (0, 23, 16, 26), (32, 8, 35, 20)]:
        draw.rectangle(box, fill=1)
    surface(world, trails, 32, lambda _: world.random.choices([4, 5], [87, 13])[0])
    pond = Image.new('1', world.ground.size)
    draw = ImageDraw.Draw(pond)
    draw.polygon([(3, 4), (10, 4), (10, 5), (12, 5), (12, 10), (10, 10), (10, 11), (4, 11), (4, 10), (2, 10), (2, 6), (3, 6)], fill=1)
    surface(world, pond, 44, lambda p: 9 if 4 < p[0] < 10 and 5 < p[1] < 10 else 8)

    for x, y, width in [(8, 8, 5), (20, 8, 6), (29, 3, 3), (36, 8, 4), (5, 25, 4), (21, 22, 3), (31, 24, 3)]:
        house(world, x, y, width)
    for x, y in [(0, 0), (9, 0), (12, 1), (19, 0), (23, 2), (2, 11), (5, 9), (1, 18), (4, 18), (11, 19), (19, 19), (25, 27), (36, 26), (40, 23), (41, 6), (36, 0), (13, 28), (39, 28)]:
        big_oak(world, x, y)
    for x, y, width in [(6, 18, 8), (19, 18, 7), (32, 24, 6), (20, 5, 5), (1, 27, 4)]:
        fence(world, x, y, width)
    for x, y in [(7, 20), (8, 21), (9, 20), (10, 21), (20, 20), (22, 20), (32, 12), (35, 12), (39, 13), (14, 12), (14, 13), (19, 12), (19, 13), (5, 13), (3, 28), (24, 24)]:
        world.objects.putpixel((x, y), 23)
    for x in range(7, 14):
        world.objects.putpixel((x, 19), 123)

    world.scenery.point([(16, 13)], fill=30)
    world.scenery.point([(14, 7), (10, 25), (38, 21)], fill=31)
    world.floor.rectangle((34, 16, 40, 20), fill=63)
    world.floor.rectangle((35, 17, 39, 19), fill=15)
    world.floor.rectangle((36, 17, 38, 19), fill=63)
    world.scenery.point((37, 18), fill=29)
    world.scenery.point((35, 17), fill=127)
    world.scenery.point((39, 17), fill=127)
    world.scenery.point([(34, 17), (40, 17), (34, 19), (40, 19)], fill=23)

    if official:
        world.floor.rectangle((26, 2, 28, 6), fill=56)
        world.scenery.rectangle((26, 2, 28, 2), fill=96)
        world.scenery.rectangle((26, 3, 26, 5), fill=97)
        world.scenery.rectangle((28, 3, 28, 5), fill=99)
        world.scenery.point((27, 3), fill=31)
    return world


def make_cave():
    world = Map(39, 29, (14, 14), ([56, 57, 59], [69, 24, 7]), 631)
    rock = Image.new('1', world.ground.size)
    draw = ImageDraw.Draw(rock)
    outer = [(0, 0, 38, 28), (2, 5, 37, 24)]
    draw.rectangle(outer[0], fill=1)
    draw.rectangle(outer[1], fill=0)
    for polygon in [
        [(0, 6), (6, 6), (6, 8), (14, 8), (14, 10), (19, 10), (19, 13), (0, 13)],
        [(0, 19), (7, 19), (7, 20), (13, 20), (13, 22), (21, 22), (21, 24), (0, 24)],
        [(24, 0), (38, 0), (38, 9), (34, 9), (34, 8), (28, 8), (28, 6), (24, 6)],
        [(24, 13), (25, 13), (25, 11), (30, 11), (30, 13), (32, 13), (32, 16), (29, 16), (29, 19), (27, 19), (27, 21), (24, 21)],
        [(33, 19), (38, 19), (38, 28), (29, 28), (29, 24), (31, 24), (31, 21), (33, 21)],
    ]:
        draw.polygon(polygon, fill=1)
    surface(world, rock, 96, 58, True)

    for point in world.cells():
        x, y = point
        if rock.getpixel(point) or world.random.random() >= .07:
            continue
        if 5 <= x <= 34 and 6 <= y <= 24:
            world.objects.putpixel(point, world.random.choices([124, 125, 126], [60, 30, 10])[0])
    for x, y in [(8, 15), (9, 15), (10, 16), (10, 17), (7, 16), (7, 17), (8, 17), (9, 17)]:
        world.floor.point((x, y), fill=10 if (x + y) % 4 == 0 else 9)
        world.scenery.point((x, y), fill=0)

    for x, y in [(17, 15), (22, 8), (33, 10), (20, 20), (5, 6), (11, 7), (12, 4), (21, 11), (22, 12), (16, 15), (18, 14), (21, 21), (28, 23)]:
        world.objects.putpixel((x, y), 125)
    for x, y in [(5, 16), (6, 17), (12, 15), (23, 8), (35, 13), (35, 14), (30, 18), (12, 20)]:
        world.objects.putpixel((x, y), 126)
    for y, stairs_id in [(11, 120), (12, 121), (13, 122)]:
        world.ground.putpixel((17, y), 59)
        world.objects.putpixel((17, y), stairs_id)
    world.floor.rectangle((16, 11, 18, 11), fill=59)
    world.scenery.point((14, 13), fill=30)
    world.scenery.point([(8, 6), (32, 18), (11, 18)], fill=31)
    world.floor.rectangle((30, 9, 32, 10), fill=63)
    world.scenery.point((31, 9), fill=29)
    world.scenery.point(world.spawn, fill=0)
    return world


def make_mountain():
    world = Map(42, 31, (20, 22), ([60, 61, 62], [81, 7, 12]), 469)
    cliffs = Image.new('1', world.ground.size)
    draw = ImageDraw.Draw(cliffs)
    draw.rectangle((0, 0, 41, 30), fill=1)
    draw.polygon([(3, 18), (6, 18), (6, 15), (8, 15), (8, 12), (10, 12), (10, 9), (13, 9), (13, 6), (16, 6), (16, 4), (28, 4), (28, 7), (31, 7), (31, 10), (34, 10), (34, 13), (36, 13), (36, 17), (38, 17), (38, 23), (36, 23), (36, 26), (31, 26), (31, 28), (12, 28), (12, 26), (7, 26), (7, 24), (3, 24)], fill=0)
    surface(world, cliffs, 108, 61, True)

    terraces = [
        ([(12, 15), (16, 15), (16, 13), (24, 13), (24, 14), (28, 14), (28, 17), (31, 17), (31, 22), (28, 22), (28, 23), (14, 23), (14, 21), (12, 21)], (20, 22)),
        ([(15, 11), (18, 11), (18, 9), (26, 9), (26, 11), (28, 11), (28, 15), (26, 15), (26, 17), (17, 17), (17, 15), (15, 15)], (20, 16)),
        ([(19, 6), (26, 6), (26, 8), (27, 8), (27, 12), (18, 12), (18, 8), (19, 8)], (23, 11)),
    ]
    for polygon, (sx, sy) in terraces:
        ledge = Image.new('1', world.ground.size)
        ImageDraw.Draw(ledge).polygon(polygon, fill=1)
        for p in world.cells():
            if ledge.getpixel(p):
                world.objects.putpixel(p, 0)
                world.ground.putpixel(p, 62)
        surface(world, ledge, 108, 62, True)
        for y, stairs_id in [(sy, 120), (sy+1, 121), (sy+2, 122)]:
            world.ground.putpixel((sx, y), 60)
            world.objects.putpixel((sx, y), stairs_id)

    world.spawn = (20, 25)
    for x, y in [(19, 20), (15, 24), (31, 19), (9, 21), (33, 17), (24, 25), (21, 9), (25, 8), (17, 17), (29, 19)]:
        if not world.objects.getpixel((x, y)):
            world.objects.putpixel((x, y), 124)
    for x, y in [(16, 24), (25, 24), (25, 20), (12, 24), (33, 24)]:
        if not world.objects.getpixel((x, y)):
            world.objects.putpixel((x, y), 126)
    world.floor.rectangle((21, 7, 24, 8), fill=63)
    world.scenery.point((22, 7), fill=29)
    world.scenery.point((19, 25), fill=30)
    world.scenery.point([(11, 23), (25, 19), (26, 10)], fill=31)
    return world


def preview(world, tiles, player):
    width, height = world.ground.size
    image = Image.new('RGBA', (width * 16, height * 16))
    for x, y in world.cells():
        position = (x * 16, y * 16)
        image.paste(tiles[world.ground.getpixel((x, y))], position)
        overlay = world.objects.getpixel((x, y))
        if overlay:
            image.alpha_composite(tiles[overlay], position)
    sx, sy = world.spawn
    image.alpha_composite(player.crop((0, 0, 16, 24)), (sx * 16, sy * 16 - 8))
    return image.convert('RGB')


def make_presets(assets, tiles, player):
    city, cave, mountain = make_city(), make_cave(), make_mountain()
    for name, world in [('world', city), ('dungeon', cave), ('mountain', mountain)]:
        world.save(assets / f'{name}.rpgw')
        image = preview(world, tiles, player)
        image.save(assets / f'{name}-preview.png', optimize=True)
    make_city(True).save(assets.parent / 'updates' / 'world-2.rpgw')
    return preview(city, tiles, player).convert('RGBA')
