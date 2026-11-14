"""Author the connected Mossbell Vale region and its runtime project file."""

from collections import deque
from dataclasses import dataclass, field
import struct

from PIL import Image, ImageDraw, ImageFont

from presets import ADJACENT, SOLID_GROUND, Map, big_oak, fence, make_cave, make_city, make_mountain, preview, surface


@dataclass
class Event:
    kind: int
    x: int
    y: int
    text: str
    target: int = 0
    landing: tuple = (0, 0)

    def encode(self):
        message = self.text.encode('ascii')
        assert 0 <= len(message) <= 90 if self.kind == 1 else 1 <= len(message) <= 90
        return struct.pack('<BBBBHBB', self.kind, self.x, self.y, len(message), self.target, *self.landing) + message


@dataclass
class RegionMap:
    key: int
    name: str
    group: str
    world: Map
    events: list = field(default_factory=list)
    movement: dict = field(default_factory=dict)

    def event(self, kind, point, text, target=0, landing=(0, 0)):
        self.events.append(Event(kind, *point, text, target, landing))

    def encode(self):
        m = self.world
        name, group = self.name.encode('ascii'), self.group.encode('ascii')
        width, height = m.ground.size
        movement = bytes(self.movement.get(p, 0) for p in m.cells())
        world = struct.pack('<4s5H4B', b'RPGW', 2, width, height, *m.spawn, 0, 0, 0, 0)
        world += m.ground.tobytes() + m.objects.tobytes()
        header = struct.pack('<HHHBB', self.key, len(world), len(self.events), len(name), len(group))
        return header + name + group + world + movement + b''.join(event.encode() for event in self.events)


def track(world, polygons):
    mask = Image.new('1', world.ground.size)
    draw = ImageDraw.Draw(mask)
    for polygon in polygons:
        draw.polygon(polygon, fill=1)
    surface(world, mask, 32, lambda _: world.random.choices([4, 5], [89, 11])[0])


def stream(world, polygon):
    mask = Image.new('1', world.ground.size)
    ImageDraw.Draw(mask).polygon(polygon, fill=1)
    surface(world, mask, 44, 9)


def clear(world, box):
    world.scenery.rectangle(box, fill=0)


def make_route():
    m = Map(36, 30, (1, 16), ([0, 1, 2, 3], [84, 10, 1, 5]), 141)
    track(m, [[(0, 14), (7, 14), (7, 13), (14, 13), (14, 15), (25, 15), (25, 12), (35, 12),
               (35, 16), (29, 16), (29, 18), (11, 18), (11, 17), (0, 17)],
              [(17, 16), (20, 16), (20, 21), (22, 21), (22, 29), (18, 29), (18, 23), (17, 23)]])
    stream(m, [(21, 0), (24, 0), (24, 4), (25, 4), (25, 9), (24, 9), (24, 14), (23, 14),
               (23, 23), (27, 23), (27, 29), (24, 29), (24, 25), (21, 25), (21, 21), (19, 21),
               (19, 13), (20, 13), (20, 8), (21, 8)])
    m.floor.rectangle((19, 15, 24, 17), fill=7)
    m.floor.rectangle((18, 22, 23, 23), fill=7)

    for x, y in [(1, 2), (5, 1), (9, 3), (15, 0), (13, 5), (16, 8), (26, 0), (29, 3),
                 (1, 23), (5, 25), (8, 21), (12, 26), (28, 20), (32, 19), (30, 26)]:
        big_oak(m, x, y)
    for x, y, width in [(4, 12, 6), (27, 17, 5), (13, 22, 4)]:
        fence(m, x, y, width)
    for x, y, tile in [(3, 10, 22), (4, 10, 23), (4, 11, 22), (13, 11, 124), (15, 11, 18),
                       (27, 9, 19), (28, 9, 21), (29, 8, 126), (30, 8, 126), (31, 9, 20),
                       (12, 19, 23), (13, 20, 23), (9, 25, 31), (20, 20, 22), (27, 22, 22)]:
        m.scenery.point((x, y), fill=tile)
    for x, y in [(7, 15), (17, 19), (33, 13)]:
        m.scenery.point((x, y), fill=30)
    m.floor.rectangle((32, 11, 35, 12), fill=60)
    m.floor.rectangle((33, 15, 35, 17), fill=60)
    for x, y in [(33, 11), (34, 11), (35, 11), (34, 12), (35, 12), (35, 16), (34, 17), (35, 17)]:
        m.scenery.point((x, y), fill=126)
    return m


def make_glade():
    m = Map(30, 25, (14, 1), ([0, 1, 2, 3], [72, 15, 4, 9]), 217)
    track(m, [[(12, 0), (16, 0), (16, 7), (18, 7), (18, 10), (20, 10), (20, 13), (16, 13),
               (16, 10), (14, 10), (14, 8), (12, 8)],
              [(16, 13), (19, 13), (19, 19), (10, 19), (10, 17), (16, 17)]])
    stream(m, [(0, 6), (4, 6), (4, 7), (7, 7), (7, 8), (9, 8), (9, 12), (8, 12), (8, 14),
               (5, 14), (5, 15), (1, 15), (1, 14), (0, 14)])
    m.floor.rectangle((5, 12, 11, 13), fill=7)
    for x, y in [(0, 0), (3, 1), (6, 2), (8, 0), (18, 0), (22, 1), (26, 0), (27, 4), (25, 7),
                 (26, 10), (25, 14), (27, 17), (25, 20), (21, 22), (17, 22), (12, 22),
                 (7, 21), (4, 19), (1, 20), (0, 16), (6, 16), (11, 10), (19, 5)]:
        big_oak(m, x, y)
    for x, y in [(11, 3), (13, 5), (4, 11), (10, 14), (11, 17), (12, 17), (14, 20), (15, 20),
                 (18, 15), (21, 12), (21, 13), (22, 15), (24, 18), (22, 19)]:
        m.scenery.point((x, y), fill=23 if (x + y) % 3 else 22)
    m.floor.rectangle((20, 15, 23, 18), fill=62)
    m.scenery.point((22, 16), fill=31)
    m.scenery.point((17, 7), fill=30)
    return m


def make_cottage():
    m = Map(13, 10, (6, 8), ([7], [1]), 118)
    m.floor.rectangle((4, 3, 8, 7), fill=15)
    m.floor.rectangle((5, 4, 7, 6), fill=63)
    m.floor.rectangle((5, 8, 7, 9), fill=4)
    m.scenery.rectangle((0, 0, 12, 1), fill=73)
    m.scenery.rectangle((0, 2, 0, 9), fill=85)
    m.scenery.rectangle((12, 2, 12, 9), fill=85)
    m.stamp(2, 1, 9, [76, 80, 78, 74, 74, 74, 76, 80, 78])
    m.stamp(1, 3, 3, [26, 26, 26, 25, 25, 25])
    m.stamp(9, 3, 3, [27, 27, 27, 25, 25, 25])
    m.stamp(1, 7, 2, [81, 127])
    m.stamp(10, 7, 2, [28, 18])
    return m


def make_shrine():
    m = Map(15, 12, (7, 10), ([63], [1]), 519)
    m.floor.rectangle((4, 1, 10, 10), fill=15)
    m.floor.rectangle((6, 4, 8, 11), fill=60)
    m.floor.rectangle((6, 3, 8, 5), fill=63)
    m.scenery.rectangle((0, 0, 14, 1), fill=96)
    m.scenery.rectangle((0, 2, 0, 11), fill=99)
    m.scenery.rectangle((14, 2, 14, 11), fill=97)
    for p in [(3, 3), (11, 3), (3, 7), (11, 7)]:
        m.scenery.point(p, fill=127)
    for p in [(2, 4), (12, 4), (3, 9), (11, 9)]:
        m.scenery.point(p, fill=125)
    m.scenery.point((7, 3), fill=29)
    m.scenery.point((4, 3), fill=30)
    return m


def make_region(official=False):
    town, route, cave, summit, cottage, shrine, glade = (
        make_city(), make_route(), make_cave(), make_mountain(), make_cottage(), make_shrine(), make_glade())
    for m in (town, cave, summit):
        for p in m.cells():
            if m.objects.getpixel(p) in (29, 30, 31):
                m.scenery.point(p, fill=0)

    town.floor.rectangle((41, 14, 43, 17), fill=4)
    clear(town, (41, 14, 43, 17))
    town.scenery.point([(16, 13), (40, 15)], fill=30)
    cave.floor.rectangle((0, 15, 4, 17), fill=59)
    clear(cave, (0, 15, 4, 17))
    cave.floor.rectangle((35, 12, 38, 15), fill=59)
    clear(cave, (35, 12, 38, 15))
    cave.scenery.point((3, 14), fill=30)
    cave.scenery.point((32, 18), fill=31)
    summit.floor.rectangle((20, 0, 23, 1), fill=60)
    summit.scenery.point((20, 28), fill=30)
    summit.scenery.point((25, 7), fill=31)
    summit.stamp(20, 0, 4, [127, 0, 0, 127])

    region = [RegionMap(1, 'Harbor Town', 'Settlements', town), RegionMap(2, 'Bracken Route', 'Routes', route),
              RegionMap(3, 'Ember Cave', 'Routes', cave), RegionMap(4, 'Slate Summit', 'Routes', summit),
              RegionMap(5, 'Trailkeeper Cottage', 'Interiors', cottage), RegionMap(6, 'Summit Shrine', 'Interiors', shrine),
              RegionMap(7, 'Willow Glade', 'Routes', glade)]

    def link(a, trigger_a, landing_a, b, trigger_b, landing_b):
        for source, trigger, target, landing in [(a, trigger_a, b, landing_b), (b, trigger_b, a, landing_a)]:
            for p in trigger:
                region[source-1].event(1, p, '', target, landing)

    link(1, [(43, 15), (43, 16)], (42, 16), 2, [(0, 15), (0, 16)], (1, 16))
    link(2, [(35, 13), (35, 14), (35, 15)], (34, 14), 3, [(0, 15), (0, 16), (0, 17)], (1, 16))
    link(3, [(38, 13), (38, 14)], (37, 14), 4, [(21, 31), (22, 31)], (21, 30))
    link(1, [(23, 12)], (23, 13), 5, [(6, 9)], (6, 8))
    link(4, [(21, 0), (22, 0)], (21, 1), 6, [(7, 11)], (7, 10))
    link(2, [(19, 29), (20, 29), (21, 29)], (20, 28), 7, [(13, 0), (14, 0), (15, 0)], (14, 1))

    for number, p, message in [
        (1, (16, 13), 'Harbor Town. Ask the trailkeeper in the blue cottage about the old shrine.'),
        (1, (40, 15), 'Bracken Route east. Follow the slate signs to Ember Cave.'),
        (2, (7, 15), 'Harbor Town west. The bridge ahead crosses Bracken Brook.'),
        (2, (17, 19), 'Willow Glade south. Keep to the trail past the footbridge.'),
        (2, (33, 13), 'Ember Cave east. The far passage climbs to Slate Summit.'),
        (3, (3, 14), 'Ember Cave. Look for daylight on the far side of the crystal chamber.'),
        (4, (20, 28), 'Slate Summit. Follow the stone stairs to the shrine above.'),
        (6, (4, 3), 'The shrine bell remembers every glowbell you bring from the vale.'),
        (7, (17, 7), 'Willow Glade. Please leave the bridge clear for other walkers.'),
    ]:
        region[number-1].event(2, p, message)
    region[4].event(3, (6, 3), 'I am the trailkeeper. Search the route, cave, glade, and summit for four glowbells.')
    region[6].event(3, (18, 15), 'The glowbell likes the quiet grass in the east. Watch for the white flowers.')
    region[0].event(3, (18, 16), 'The trailkeeper lives in the blue house north of here. Watch for the slate trail signs.')

    # Marks on the project are equally usable when the editor shows the movement mode.
    region[0].movement[(23, 12)] = 1
    region[3].movement[(21, 0)] = 1
    region[3].movement[(22, 0)] = 1
    if official:
        route.floor.rectangle((9, 7, 14, 9), fill=63)
        route.scenery.point((12, 8), fill=30)
        fence(route, 9, 7, 5)
        region[1].event(2, (12, 8), 'Brook lookout. The stepping stones appear when the water is low.')
    return region


def validate(region):
    maps = {record.key: record for record in region}
    events = {(record.key, event.x, event.y): event for record in region for event in record.events}
    assert len(events) == sum(len(record.events) for record in region)

    def walkable(key, point):
        record = maps[key]
        x, y = point
        if not (0 <= x < record.world.ground.width and 0 <= y < record.world.ground.height):
            return False
        event = events.get((key, x, y))
        if event and event.kind != 1:
            return False
        movement = record.movement.get(point, 0)
        if movement:
            return movement == 1
        return record.world.ground.getpixel(point) not in SOLID_GROUND and record.world.objects.getpixel(point) in (
            0, 22, 23, 31, 86, 87, 95, 123, 124, 125)

    for record in region:
        assert walkable(record.key, record.world.spawn), record.name
        assert (record.key, *record.world.spawn) not in events, record.name
        for event in record.events:
            if event.kind == 1:
                assert walkable(record.key, (event.x, event.y)), (record.name, event)
                assert walkable(event.target, event.landing), (record.name, event)
                assert (event.target, *event.landing) not in events, (record.name, event)

    start = (region[0].key, *region[0].world.spawn)
    visited = {start}
    entered_warps = set()
    queue = deque([start])
    while queue:
        key, x, y = queue.popleft()
        for dx, dy in ADJACENT:
            point = (x + dx, y + dy)
            if not walkable(key, point):
                continue
            node = (key, *point)
            event = events.get(node)
            if event and event.kind == 1:
                entered_warps.add(node)
                node = (event.target, *event.landing)
            if node not in visited:
                queue.append(node)
                visited.add(node)
    assert len({key for key, _, _ in visited}) == len(region)
    assert entered_warps == {node for node, event in events.items() if event.kind == 1}

    def reachable_adjacent(key, x, y):
        return any((key, x + dx, y + dy) in visited for dx, dy in ADJACENT)

    total = 0
    for record in region:
        for x, y in record.world.cells():
            item = record.world.objects.getpixel((x, y))
            if item == 31:
                total += 1
                assert (record.key, x, y) in visited, (record.name, x, y)
            if item == 29:
                assert reachable_adjacent(record.key, x, y), (record.name, x, y)
        for event in record.events:
            if event.kind in (2, 3):
                assert reachable_adjacent(record.key, event.x, event.y), (record.name, event)
    assert total == 4


def draw_region(region, previews, path):
    image = Image.new('RGB', (996, 550), '#edf0df')
    draw = ImageDraw.Draw(image)
    positions = {1: (22, 96), 2: (268, 96), 3: (514, 96), 4: (760, 96),
                 5: (22, 330), 7: (268, 330), 6: (760, 330)}
    links = [(1, 2, None), (2, 3, None), (3, 4, None), (1, 5, 'Cottage door'),
             (2, 7, 'South fork'), (4, 6, 'Summit stairs')]
    label = ImageFont.load_default(size=15)
    small = ImageFont.load_default(size=13)
    draw.text((22, 23), 'Mossbell Vale', font=ImageFont.load_default(size=31), fill='#263d32')
    draw.text((23, 62), 'Connected region', font=small, fill='#526755')
    for a, b, connection in links:
        x, y = positions[a]
        xx, yy = positions[b]
        if connection:
            draw.line((x+107, y+176, xx+107, yy), fill='#7f916d', width=4)
            draw.rectangle((x+28, y+195, x+185, y+219), fill='#edf0df')
            draw.text((x+107, y+207), connection, font=small, fill='#526755', anchor='mm')
        else:
            draw.line((x+214, y+80, xx, yy+80), fill='#7f916d', width=4)
            draw.ellipse((x+226, y+75, x+236, y+85), fill='#7f916d')
    for record in region:
        x, y = positions[record.key]
        draw.rounded_rectangle((x, y, x+214, y+176), radius=8, fill='#faf9ee', outline='#a9b097', width=2)
        screenshot = previews[record.key].copy()
        screenshot.thumbnail((202, 136), Image.Resampling.NEAREST)
        image.paste(screenshot, (x+(214-screenshot.width)//2, y+7+(136-screenshot.height)//2))
        draw.text((x+9, y+156), record.name, font=label, fill='#263d32')
    image.save(path, optimize=True)


def make_project(assets, tiles, player):
    region = make_region()
    for maps, output in [(region, assets / 'project.rpgp'), (make_region(True), assets.parent / 'updates' / 'project-1.rpgp')]:
        validate(maps)
        header = struct.pack('<4sHHHH', b'RPGP', 1, len(maps), maps[0].key, 0)
        output.write_bytes(header + b''.join(record.encode() for record in maps))
    previews = {record.key: preview(record.world, tiles, player, show_player=False) for record in region}
    draw_region(region, previews, assets / 'region-preview.png')
