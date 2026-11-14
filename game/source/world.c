#include "world.h"

#include <stdio.h>
#include <string.h>
#include <sys/stat.h>

const char *const outfitNames[] = {"Shirt", "Pants", "Hair", "Skin"};
const char *const colorNames[OUTFIT_PARTS][6] = {
    {"Rose", "Pine", "Gold", "Lilac", "Sky", "Berry"},
    {"Denim", "Olive", "Charcoal", "Sand", "Plum", "Navy"},
    {"Chestnut", "Ink", "Honey", "Copper", "Ash", "Auburn"},
    {"Peach", "Fair", "Gold", "Tan", "Brown", "Deep"},
};
const uint32_t outfitColors[OUTFIT_PARTS][6][2] = {
    {{0xe75977, 0xa53558}, {0x58b783, 0x317754}, {0xe2ab52, 0x9a6233},
     {0xa284d1, 0x654e96}, {0x63b4d5, 0x39738e}, {0xbc6188, 0x76395e}},
    {{0x456f9a, 0x294361}, {0x718854, 0x465435}, {0x54586a, 0x333546},
     {0xb08f65, 0x6d543d}, {0x805984, 0x4b3b5d}, {0x45627a, 0x2a3d56}},
    {{0x99664b, 0x613f39}, {0x454050, 0x2a2836}, {0xd5a24e, 0x936331},
     {0xb96d4f, 0x7d433c}, {0xa3a3ae, 0x666779}, {0x805053, 0x513644}},
    {{0xf6c291, 0xd38c69}, {0xf8d1ad, 0xdfab84}, {0xdeab73, 0xac744f},
     {0xc8875c, 0x935a43}, {0x9c634d, 0x6d423c}, {0x775045, 0x523633}},
};

static unsigned read16(const uint8_t *data)
{
    return data[0] | (unsigned)data[1] << 8;
}

static bool passable(int ground, int object)
{
    return (ground < 8 || ground > 11) && ground != 61 &&
        (ground < 108 || ground > 119) &&
        (object == 0 || object == 22 || object == 23 || object == GLOWBELL ||
         object == 86 || object == 87 || object == 95 ||
         (object >= 123 && object <= 125));
}

static bool validOutfit(const uint8_t *outfit)
{
    for (int part = 0; part < OUTFIT_PARTS; part++)
        if (outfit[part] >= 6) return false;
    return true;
}

static bool legacyTile(int ground, int object)
{
    return (ground < 16 || (ground >= 32 && ground < 64)) &&
        (object == 0 || (object >= 16 && object < 32) ||
         (object >= 64 && object < 128));
}

static bool currentTile(int ground, int object)
{
    return (ground < 16 || (ground >= 32 && ground < 64) ||
            (ground >= 108 && ground <= 122)) &&
        (object == 0 || (object >= 16 && object < 32) ||
         (object >= 64 && object < 108) || (object >= 123 && object < 128));
}

static void migrateTile(uint8_t *ground, uint8_t *object)
{
    if (*ground == 61) *ground = 60;
    if (*object >= 108 && *object <= 122) {
        *ground = *object;
        *object = 0;
    }
}

static bool parseWorld(World *world, const uint8_t *data, size_t size, bool projectMap)
{
    if (size < WORLD_HEADER || memcmp(data, "RPGW", 4)) return false;
    unsigned version = read16(data + 4);
    if (version != 2 && (version != 1 || projectMap)) return false;
    int width = read16(data + 6), height = read16(data + 8);
    int spawnX = read16(data + 10), spawnY = read16(data + 12);
    if (width < 8 || height < 8 || width > WORLD_SIDE || height > WORLD_SIDE ||
        spawnX >= width || spawnY >= height || !validOutfit(data + 14))
        return false;
    size_t cells = (size_t)width * height;
    if (size != WORLD_HEADER + cells * 2) return false;
    const uint8_t *ground = data + WORLD_HEADER;
    const uint8_t *objects = ground + cells;
    int spawn = spawnY * width + spawnX;
    for (size_t i = 0; i < cells; i++) {
        uint8_t base = ground[i], object = objects[i];
        if (version == 1) {
            if (!legacyTile(base, object)) return false;
            migrateTile(&base, &object);
        } else if (!currentTile(base, object)) {
            return false;
        }
        if (i == (size_t)spawn && !projectMap && !passable(base, object)) return false;
    }
    uint32_t signature = 2166136261u;
    for (size_t i = 0; i < size; i++) signature = (signature ^ data[i]) * 16777619u;
    world->width = width;
    world->height = height;
    world->spawnX = spawnX;
    world->spawnY = spawnY;
    world->signature = signature;
    memcpy(world->outfit, data + 14, OUTFIT_PARTS);
    memcpy(world->ground, ground, cells);
    memcpy(world->object, objects, cells);
    if (version == 1) {
        for (size_t i = 0; i < cells; i++)
            migrateTile(&world->ground[i], &world->object[i]);
    }
    return true;
}

bool worldParse(World *world, const uint8_t *data, size_t size)
{
    return parseWorld(world, data, size, false);
}

bool worldParseProjectMap(World *world, const uint8_t *data, size_t size)
{
    return parseWorld(world, data, size, true);
}

bool worldRead(World *world, const char *path)
{
    FILE *file = fopen(path, "rb");
    if (!file) return false;
    static uint8_t data[WORLD_FILE_MAX + 1];
    size_t size = fread(data, 1, sizeof(data), file);
    bool ok = !ferror(file) && worldParse(world, data, size);
    fclose(file);
    return ok;
}

bool worldWalkable(const World *world, int x, int y)
{
    if (x < 0 || y < 0 || x >= world->width || y >= world->height) return false;
    int index = y * world->width + x;
    return passable(world->ground[index], world->object[index]);
}

int worldObject(const World *world, int x, int y)
{
    if (x < 0 || y < 0 || x >= world->width || y >= world->height) return 0;
    return world->object[y * world->width + x];
}

int worldGlowbells(const World *world)
{
    int total = 0;
    for (int i = 0; i < world->width * world->height; i++)
        if (world->object[i] == GLOWBELL) total++;
    return total;
}

bool journeyFound(const Journey *journey, int index)
{
    return (journey->found[index / 8] & (1 << (index % 8))) != 0;
}

void journeyCollect(Journey *journey, int index)
{
    journey->found[index / 8] |= 1 << (index % 8);
}

int journeyCount(const World *world, const Journey *journey)
{
    int total = 0;
    for (int i = 0; i < world->width * world->height; i++)
        if (world->object[i] == GLOWBELL && journeyFound(journey, i)) total++;
    return total;
}

void journeyNew(const World *world, Journey *journey)
{
    memset(journey, 0, sizeof(*journey));
    journey->x = world->spawnX;
    journey->y = world->spawnY;
    memcpy(journey->outfit, world->outfit, OUTFIT_PARTS);
}

void worldMakeDirectory(void)
{
    mkdir("sdmc:/3ds", 0777);
    mkdir(WORLD_DIR, 0777);
}

bool worldWrite(const char *path, const uint8_t *data, size_t size)
{
    char temporary[128];
    if (snprintf(temporary, sizeof(temporary), "%s.tmp", path) >= (int)sizeof(temporary))
        return false;
    FILE *file = fopen(temporary, "wb");
    if (!file) return false;
    bool ok = fwrite(data, 1, size, file) == size;
    if (fclose(file) != 0) ok = false;
    if (ok) ok = rename(temporary, path) == 0;
    if (!ok) remove(temporary);
    return ok;
}

enum { SAVE_HEADER = 15, SAVE_SIZE = SAVE_HEADER + WORLD_CELLS / 8 };

bool journeyLoad(const World *world, Journey *journey, const char *path)
{
    FILE *file = fopen(path, "rb");
    if (!file) return false;
    uint8_t data[SAVE_SIZE];
    bool ok = fread(data, 1, sizeof(data), file) == sizeof(data) &&
        fgetc(file) == EOF && !ferror(file);
    fclose(file);
    if (!ok || memcmp(data, "MV01", 4) || !validOutfit(data + 10)) return false;
    uint32_t signature = (uint32_t)read16(data + 4) | (uint32_t)read16(data + 6) << 16;
    if (world->signature != signature || !worldWalkable(world, data[8], data[9]))
        return false;
    journey->x = data[8];
    journey->y = data[9];
    memcpy(journey->outfit, data + 10, OUTFIT_PARTS);
    journey->completed = data[14] != 0;
    memcpy(journey->found, data + SAVE_HEADER, sizeof(journey->found));
    return true;
}

bool journeySave(const World *world, const Journey *journey, const char *path)
{
    uint8_t data[SAVE_SIZE] = {'M', 'V', '0', '1'};
    for (int i = 0; i < 4; i++) data[4 + i] = world->signature >> (8 * i);
    data[8] = journey->x;
    data[9] = journey->y;
    memcpy(data + 10, journey->outfit, OUTFIT_PARTS);
    data[14] = journey->completed;
    memcpy(data + SAVE_HEADER, journey->found, sizeof(journey->found));
    worldMakeDirectory();
    return worldWrite(path, data, sizeof(data));
}
