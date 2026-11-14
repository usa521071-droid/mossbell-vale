"""Build the editable city, cave and mountain maps from the shared tileset."""

from collections import deque
import random
import struct

from PIL import Image, ImageDraw


ADJACENT = [(0, -1), (1, 0), (0, 1), (-1, 0)]
DIAGONALS = [(-1, -1), (1, -1), (1, 1), (-1, 1)]
CORNER_INDEX = {(0, 3): 4, (0, 1): 5, (1, 2): 6, (2, 3): 7}
SOLID_GROUND = (*range(8, 12), 61, *range(108, 120))


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
                if self.ground.getpixel(p) in SOLID_GROUND:
                    continue
                if self.objects.getpixel(p) not in (0, 22, 23, 31, 86, 87, 95, 123, 124, 125):
                    continue
                visited.add(p)
                queue.append(p)
        return visited

    def save(self, path):
        reachable = self.reach()
        assert all(tile_id < 16 or 32 <= tile_id <= 63 or 108 <= tile_id <= 122 for tile_id in self.ground.tobytes())
        assert all(tile_id == 0 or 16 <= tile_id <= 31 or 64 <= tile_id <= 107 or 123 <= tile_id <= 127
                   for tile_id in self.objects.tobytes())
        assert self.ground.getpixel(self.spawn) not in SOLID_GROUND and not self.objects.getpixel(self.spawn)
        objectives = [point for point in self.cells() if self.objects.getpixel(point) in (29, 30, 31)]
        assert len(objectives) in (5, 6)
        for x, y in objectives:
            if self.objects.getpixel((x, y)) == 31:
                assert (x, y) in reachable, (path, x, y)
            else:
                assert any((x+dx, y+dy) in reachable for dx, dy in ADJACENT), (path, x, y)
        header = struct.pack('<4s5H4B', b'RPGW', 2, *self.ground.size, *self.spawn, 0, 0, 0, 0)
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


def market(world, x, y):
    for row in range(2):
        world.stamp(x, y + row, 5, [27] * 5)
    world.stamp(x, y + 2, 5, [26] * 5)
    world.stamp(x, y + 3, 5, [76, 80, 77, 80, 78])
    world.stamp(x, y + 4, 5, [87] * 5)
    for row in range(5):
        world.objects.putpixel((x + 5, y + row), 86)


def big_oak(world, x, y):
    world.stamp(x, y, 3, range(88, 94))
    world.objects.putpixel((x + 1, y + 2), 94)
    world.objects.putpixel((x + 2, y + 2), 95)


def fence(world, x, y, width):
    world.stamp(x, y, width, [82] + [83] * (width - 2) + [84])


def make_city(official=False):
    world = Map(44, 31, (16, 15), ([0, 1, 2, 3], [84, 11, 1, 4]), 215)
    trails = Image.new('1', world.ground.size)
    draw = ImageDraw.Draw(trails)
    for box in [(3, 14, 42, 18), (15, 2, 18, 26), (27, 12, 30, 25), (25, 19, 40, 23), (0, 23, 16, 26), (32, 10, 35, 20)]:
        draw.rectangle(box, fill=1)
    surface(world, trails, 32, lambda _: world.random.choices([4, 5], [87, 13])[0])
    pond = Image.new('1', world.ground.size)
    draw = ImageDraw.Draw(pond)
    draw.polygon([(0, 2), (9, 2), (9, 3), (11, 3), (11, 5), (13, 5), (13, 8), (12, 8), (12, 11), (11, 11), (11, 12), (5, 12), (5, 11), (2, 11), (2, 9), (0, 9)], fill=1)
    surface(world, pond, 44, lambda p: 9 if 2 < p[0] < 11 and 4 < p[1] < 10 else 8)
    world.floor.rectangle((4, 10, 8, 11), fill=7)

    for x, y, width in [(20, 8, 6), (33, 4, 4), (36, 9, 4), (5, 25, 4), (21, 22, 3)]:
        house(world, x, y, width)
    market(world, 9, 10)
    market(world, 25, 3)
    world.floor.rectangle((4, 12, 8, 14), fill=63)
    world.floor.rectangle((19, 13, 25, 14), fill=63)
    world.floor.rectangle((28, 12, 31, 14), fill=63)
    world.floor.rectangle((31, 15, 33, 17), fill=63)
    for x, y in [(4, 12), (8, 12), (4, 14), (19, 14), (25, 14), (28, 12), (31, 12), (31, 17), (33, 17)]:
        world.scenery.point((x, y), fill=127)
    for x, y in [(30, 8), (31, 8), (31, 9), (30, 10), (26, 12), (26, 13), (35, 21), (36, 21)]:
        world.scenery.point((x, y), fill=123)
    for x, y in [(0, 0), (10, 0), (19, 0), (0, 18), (3, 19), (11, 20), (19, 20), (27, 27), (34, 26), (40, 23), (41, 6), (36, 0), (13, 28), (39, 28)]:
        big_oak(world, x, y)
    for x, y, width in [(6, 19, 8), (19, 19, 7), (32, 24, 6), (20, 5, 5), (1, 27, 4)]:
        fence(world, x, y, width)
    for x, y in [(7, 21), (8, 22), (9, 21), (10, 22), (19, 12), (19, 13), (5, 13), (14, 12), (14, 13), (3, 28), (24, 24)]:
        world.objects.putpixel((x, y), 23)
    for x in range(7, 13):
        world.objects.putpixel((x, 20), 123)

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
        world.floor.rectangle((30, 25, 32, 29), fill=56)
        world.scenery.rectangle((30, 25, 32, 25), fill=96)
        world.scenery.rectangle((30, 26, 30, 28), fill=97)
        world.scenery.rectangle((32, 26, 32, 28), fill=99)
        world.scenery.point((31, 26), fill=31)
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
    world.floor.rectangle((16, 11, 18, 11), fill=59)
    for y, stairs_id in [(11, 120), (12, 121), (13, 122)]:
        world.ground.putpixel((17, y), stairs_id)
        world.objects.putpixel((17, y), 0)
    world.scenery.point((14, 13), fill=30)
    world.scenery.point([(8, 6), (32, 18), (11, 18)], fill=31)
    world.floor.rectangle((30, 9, 32, 10), fill=63)
    world.scenery.point((31, 9), fill=29)
    world.scenery.point(world.spawn, fill=0)
    return world


def make_mountain():
    world = Map(43, 32, (21, 28), ([61], [1]), 469)
    left = [19, 19, 17, 17, 16, 16, 14, 14, 13, 13, 11, 11, 10, 10, 8, 8,
            7, 7, 5, 5, 6, 6, 8, 8, 11, 11, 13, 13, 16, 16, 19, 19]
    right = [24, 24, 26, 26, 28, 28, 29, 29, 31, 31, 32, 32, 34, 34, 35, 35,
             37, 37, 39, 39, 38, 38, 36, 36, 34, 34, 32, 32, 29, 29, 24, 24]
    # Narrow each stepped ridge toward the summit and widen the terraces at its foot.
    for offset in (12, 9, 6, 3, 0):
        basin = Image.new('1', world.ground.size)
        draw = ImageDraw.Draw(basin)
        for y, (a, b) in enumerate(zip(left, right)):
            draw.line((max(0, a - offset), y, min(42, b + offset), y), fill=1)
        for point in world.cells():
            if not basin.getpixel(point):
                continue
            edge = outline(basin, point)
            if edge is not None:
                tile_id = 108 + edge
            elif offset:
                tile_id = 61
            else:
                tile_id = 62 if point[1] > 19 else 60
            world.ground.putpixel(point, tile_id)

    for bounds, middle, tile in [
        ((14, 20, 33, 23), 19, 62), ((15, 15, 29, 19), 23, 60),
        ((17, 10, 28, 14), 20, 60), ((19, 5, 27, 9), 24, 60),
    ]:
        x1, y1, x2, y2 = bounds
        world.floor.rectangle((x1, y1, x2, y2), fill=tile)
        for x in range(x1, x2 + 1):
            world.ground.putpixel((x, y2), 115 if x == x1 else 114 if x == x2 else 110)
        for y in range(y1, y2):
            world.ground.putpixel((x1, y), 111)
            world.ground.putpixel((x2, y), 109)
        for y, stairs_id in [(y2-1, 120), (y2, 121), (y2+1, 122)]:
            world.ground.putpixel((middle, y), stairs_id)

    world.floor.rectangle((20, 1, 23, 4), fill=60)
    world.floor.rectangle((20, 2, 23, 3), fill=63)
    world.floor.rectangle((20, 26, 23, 31), fill=60)
    world.floor.line((21, 24, 21, 27), fill=60)
    for x, y in [(16, 21), (17, 21), (16, 22), (18, 25), (26, 26), (29, 24), (30, 23), (14, 24), (33, 19), (34, 18)]:
        if world.ground.getpixel((x, y)) == 62:
            world.floor.point((x, y), fill=1)
    for x, y in [(18, 17), (21, 12), (25, 6), (24, 25), (10, 19), (28, 16), (16, 25), (14, 22)]:
        if world.ground.getpixel((x, y)) not in SOLID_GROUND:
            world.scenery.point((x, y), fill=124)
    for x, y in [(20, 24), (14, 25), (33, 18), (29, 21), (27, 10)]:
        if world.ground.getpixel((x, y)) not in SOLID_GROUND:
            world.scenery.point((x, y), fill=126)
    world.scenery.point((21, 2), fill=29)
    world.scenery.point((20, 28), fill=30)
    world.scenery.point([(13, 24), (25, 17), (25, 7)], fill=31)
    return world


def preview(world, tiles, player, show_player=True):
    width, height = world.ground.size
    image = Image.new('RGBA', (width * 16, height * 16))
    for x, y in world.cells():
        position = (x * 16, y * 16)
        image.paste(tiles[world.ground.getpixel((x, y))], position)
        overlay = world.objects.getpixel((x, y))
        if overlay:
            image.alpha_composite(tiles[overlay], position)
    if show_player:
        sx, sy = world.spawn
        image.alpha_composite(player.crop((0, 0, 16, 24)), (sx * 16, sy * 16 - 8))
    return image.convert('RGB')


def make_presets(assets, tiles, player):
    city, cave, mountain = make_city(), make_cave(), make_mountain()
    for name, world in [('world', city), ('dungeon', cave), ('mountain', mountain)]:
        world.save(assets / f'{name}.rpgw')
        image = preview(world, tiles, player)
        image.save(assets / f'{name}-preview.png', optimize=True)
    make_city(True).save(assets.parent / 'updates' / 'world-3.rpgw')
    return preview(city, tiles, player).convert('RGBA')
