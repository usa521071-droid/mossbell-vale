#include "project.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#define SAVE_HEADER 16
#define SAVE_BYTES (SAVE_HEADER + sizeof(((ProjectProgress *)0)->collected))

static unsigned read16(const uint8_t *data)
{
    return data[0] | (unsigned)data[1] << 8;
}

static uint32_t read32(const uint8_t *data)
{
    return read16(data) | (uint32_t)read16(data + 2) << 16;
}

static bool copyText(char *out, const uint8_t *text, size_t length)
{
    for (size_t i = 0; i < length; i++)
        if (text[i] < 32 || text[i] > 126) return false;
    memcpy(out, text, length);
    out[length] = 0;
    return true;
}

int projectIndex(const Project *project, uint16_t id)
{
    for (int i = 0; i < project->count; i++)
        if (project->maps[i].id == id) return i;
    return -1;
}

const ProjectEvent *mapEvent(const Map *map, int x, int y)
{
    for (unsigned i = 0; i < map->eventCount; i++)
        if (map->events[i].x == x && map->events[i].y == y) return &map->events[i];
    return NULL;
}

bool mapWalkable(const Map *map, const World *world, int x, int y)
{
    if (x < 0 || y < 0 || x >= world->width || y >= world->height) return false;
    const ProjectEvent *event = mapEvent(map, x, y);
    if (event && (event->type == PROJECT_SIGN || event->type == PROJECT_NPC)) return false;
    int permission = map->movement[y * world->width + x];
    return permission ? permission == 1 : worldWalkable(world, x, y);
}

static bool readEvent(Map *map, unsigned index, const uint8_t **cursor, const uint8_t *end)
{
    const uint8_t *bytes = *cursor;
    if (end - bytes < 8) return false;
    ProjectEvent *event = &map->events[index];
    unsigned type = bytes[0], x = bytes[1], y = bytes[2], length = bytes[3];
    unsigned target = read16(bytes + 4), targetX = bytes[6], targetY = bytes[7];
    if (type < PROJECT_WARP || type > PROJECT_CREATURE || x >= map->width || y >= map->height ||
        length > 90 || end - bytes - 8 < (ptrdiff_t)length ||
        (type == PROJECT_WARP ? !target :
         (target || targetX || targetY || (!length && type != PROJECT_CREATURE))))
        return false;
    for (unsigned old = 0; old < index; old++)
        if (map->events[old].x == x && map->events[old].y == y) return false;
    if (!copyText(event->text, bytes + 8, length)) return false;
    event->type = type;
    event->x = x;
    event->y = y;
    event->target = target;
    event->targetX = targetX;
    event->targetY = targetY;
    *cursor = bytes + 8 + length;
    return true;
}

static bool readMap(Map *map, World *world, const uint8_t **cursor, const uint8_t *end)
{
    const uint8_t *bytes = *cursor;
    if (end - bytes < 8) return false;
    unsigned name = bytes[6], group = bytes[7];
    map->id = read16(bytes);
    map->size = read16(bytes + 2);
    map->eventCount = read16(bytes + 4);
    if (!map->id || map->eventCount > PROJECT_MAX_EVENTS ||
        !name || name >= sizeof(map->name) || !group || group >= sizeof(map->group) ||
        (size_t)(end - bytes) < 8 + name + group + map->size ||
        !copyText(map->name, bytes + 8, name) ||
        !copyText(map->group, bytes + 8 + name, group)) return false;
    map->raw = bytes + 8 + name + group;
    if (!worldParseProjectMap(world, map->raw, map->size)) return false;
    map->width = world->width;
    map->height = world->height;
    map->bells = worldGlowbells(world);
    map->movement = map->raw + map->size;
    int cells = world->width * world->height;
    if (end - map->movement < cells) return false;
    for (int i = 0; i < cells; i++)
        if (map->movement[i] > 2) return false;
    *cursor = map->movement + cells;
    for (unsigned i = 0; i < map->eventCount; i++)
        if (!readEvent(map, i, cursor, end)) return false;
    if (!mapWalkable(map, world, world->spawnX, world->spawnY) ||
        mapEvent(map, world->spawnX, world->spawnY)) return false;
    for (unsigned i = 0; i < map->eventCount; i++) {
        const ProjectEvent *event = &map->events[i];
        if ((event->type == PROJECT_WARP || event->type == PROJECT_CREATURE) &&
            !mapWalkable(map, world, event->x, event->y))
            return false;
    }
    return true;
}

int creatureAt(const Map *map, const Creature *creatures, int x, int y)
{
    for (unsigned i = 0; i < map->eventCount; i++) {
        if (map->events[i].type != PROJECT_CREATURE) continue;
        const Creature *actor = &creatures[i];
        if ((actor->x == x && actor->y == y) ||
            (actor->interpolation && actor->fromX == x && actor->fromY == y))
            return (int)i;
    }
    return -1;
}

void creatureTick(const Map *map, const World *world, Creature *creatures,
    int playerX, int playerY, int fromX, int fromY)
{
    static const int dx[] = {0, -1, 1, 0}, dy[] = {1, 0, 0, -1};
    for (unsigned i = 0; i < map->eventCount; i++) {
        const ProjectEvent *home = &map->events[i];
        if (home->type != PROJECT_CREATURE) continue;
        Creature *actor = &creatures[i];
        if (actor->interpolation) actor->interpolation--;
        if (++actor->turns < 28 + (home->x + home->y) % 10) continue;
        actor->turns = 0;
        int first = (actor->x * 3 + actor->y + home->x + home->y) % 4;
        for (int offset = 0; offset < 4; offset++) {
            int direction = (first + offset) % 4;
            int x = actor->x + dx[direction], y = actor->y + dy[direction];
            const ProjectEvent *event = mapEvent(map, x, y);
            if (abs(x - home->x) + abs(y - home->y) > 2 ||
                (x == playerX && y == playerY) || (x == fromX && y == fromY) ||
                (event && event != home) || creatureAt(map, creatures, x, y) >= 0 ||
                worldObject(world, x, y) == GLOWBELL || !mapWalkable(map, world, x, y)) continue;
            actor->fromX = actor->x;
            actor->fromY = actor->y;
            actor->x = x;
            actor->y = y;
            actor->interpolation = 8;
            break;
        }
    }
}

static bool parseProject(Project *project, World *mapWorld, const uint8_t *data, size_t size)
{
    if (size < 12 || size > PROJECT_MAX_SIZE || memcmp(data, "RPGP", 4) ||
        (read16(data + 4) != 1 && read16(data + 4) != 2) || read16(data + 10)) return false;
    int count = read16(data + 6);
    uint16_t start = read16(data + 8);
    if (count < 1 || count > PROJECT_MAX_MAPS) return false;
    const uint8_t *cursor = data + 12, *end = data + size;
    project->count = 0;
    project->bells = 0;
    for (int i = 0; i < count; i++) {
        Map *map = &project->maps[i];
        if (!readMap(map, mapWorld, &cursor, end) || projectIndex(project, map->id) >= 0)
            return false;
        project->bells += map->bells;
        project->count++;
    }
    project->start = projectIndex(project, start);
    if (cursor != end || project->start < 0) return false;

    /* Visit each destination once to validate every inbound connection. */
    for (int i = 0; i < count; i++) {
        const Map *target = &project->maps[i];
        worldParseProjectMap(mapWorld, target->raw, target->size);
        for (int from = 0; from < count; from++) {
            const Map *source = &project->maps[from];
            for (unsigned j = 0; j < source->eventCount; j++) {
                const ProjectEvent *event = &source->events[j];
                if (event->type != PROJECT_WARP) continue;
                if (i == 0 && projectIndex(project, event->target) < 0) return false;
                if (event->target != target->id) continue;
                if (!mapWalkable(target, mapWorld, event->targetX, event->targetY) ||
                    mapEvent(target, event->targetX, event->targetY)) return false;
            }
        }
    }
    uint32_t signature = 2166136261u;
    for (size_t i = 0; i < size; i++) signature = (signature ^ data[i]) * 16777619u;
    project->signature = signature;
    return true;
}

bool projectParse(Project *project, const uint8_t *data, size_t size)
{
    World *mapWorld = malloc(sizeof(*mapWorld));
    if (!mapWorld) return false;
    bool ok = parseProject(project, mapWorld, data, size);
    free(mapWorld);
    if (!ok) project->count = 0;
    return ok;
}

bool projectRead(Project *project, const char *path, unsigned *revision)
{
    FILE *file = fopen(path, "rb");
    if (!file) return false;
    long length = -1;
    if (!fseek(file, 0, SEEK_END)) length = ftell(file);
    unsigned offset = revision ? 4 : 0;
    if (length < 12 + (long)offset || length > PROJECT_MAX_SIZE + (long)offset ||
        fseek(file, 0, SEEK_SET)) {
        fclose(file);
        return false;
    }
    uint8_t *data = malloc((size_t)length);
    bool ok = data && fread(data, 1, (size_t)length, file) == (size_t)length &&
        fgetc(file) == EOF && !ferror(file);
    fclose(file);
    if (ok && revision) ok = read32(data) > 0 && read32(data) <= 999999;
    if (ok) ok = projectParse(project, data + offset, (size_t)length - offset);
    if (!ok) {
        free(data);
        return false;
    }
    project->owned = data;
    if (revision) *revision = read32(data);
    return true;
}

void projectClose(Project *project)
{
    free(project->owned);
    memset(project, 0, sizeof(*project));
}

bool projectSave(const Project *project, ProjectProgress *progress, const Journey *journey,
    int mapIndex, const char *path)
{
    static uint8_t data[SAVE_BYTES] = {'M', 'P', '0', '1'};
    uint16_t id = project->maps[mapIndex].id;
    for (int i = 0; i < 4; i++) data[4 + i] = project->signature >> (8 * i);
    data[8] = id;
    data[9] = id >> 8;
    data[10] = journey->x;
    data[11] = journey->y;
    memcpy(data + 12, journey->outfit, OUTFIT_PARTS);
    memcpy(progress->collected[mapIndex], journey->found, sizeof(journey->found));
    memcpy(data + SAVE_HEADER, progress->collected, sizeof(progress->collected));
    data[3] = journey->completed ? '2' : '1';
    progress->completed = journey->completed;
    worldMakeDirectory();
    return worldWrite(path, data, sizeof(data));
}

int projectResume(const Project *project, ProjectProgress *progress, Journey *journey,
    const char *path, uint32_t previousSignature)
{
    FILE *file = fopen(path, "rb");
    if (!file) return -1;
    static uint8_t data[SAVE_BYTES];
    bool ok = fread(data, 1, sizeof(data), file) == sizeof(data) &&
        fgetc(file) == EOF && !ferror(file);
    fclose(file);
    if (!ok || memcmp(data, "MP0", 3) || (data[3] != '1' && data[3] != '2')) return -1;
    uint32_t signature = read32(data + 4);
    if (signature != project->signature && (!previousSignature || signature != previousSignature)) return -1;
    int index = projectIndex(project, read16(data + 8));
    if (index < 0) return -1;
    for (int part = 0; part < OUTFIT_PARTS; part++)
        if (data[12 + part] >= 6) return -1;
    const Map *map = &project->maps[index];
    static World resumeWorld;
    worldParseProjectMap(&resumeWorld, map->raw, map->size);
    if (!mapWalkable(map, &resumeWorld, data[10], data[11])) return -1;
    memcpy(progress->collected, data + SAVE_HEADER, sizeof(progress->collected));
    progress->completed = data[3] == '2';
    journey->x = data[10];
    journey->y = data[11];
    journey->completed = progress->completed;
    memcpy(journey->outfit, data + 12, OUTFIT_PARTS);
    memcpy(journey->found, progress->collected[index], sizeof(journey->found));
    return index;
}

int projectFound(const Project *project, const ProjectProgress *progress)
{
    int total = 0;
    for (int i = 0; i < project->count; i++) {
        const Map *map = &project->maps[i];
        const uint8_t *objects = map->raw + WORLD_HEADER + map->width * map->height;
        for (int j = 0; j < map->width * map->height; j++)
            if (objects[j] == GLOWBELL && (progress->collected[i][j / 8] & (1 << (j % 8))))
                total++;
    }
    return total;
}
