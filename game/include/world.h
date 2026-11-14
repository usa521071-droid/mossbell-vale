#ifndef MOSSBELL_WORLD_H
#define MOSSBELL_WORLD_H

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>

#define WORLD_SIDE 128
#define WORLD_CELLS (WORLD_SIDE * WORLD_SIDE)
#define WORLD_HEADER 18
#define WORLD_FILE_MAX (WORLD_HEADER + WORLD_CELLS * 2)
#define WORLD_DIR "sdmc:/3ds/MossbellVale"
#define CUSTOM_WORLD WORLD_DIR "/world.rpgw"
#define CUSTOM_ART WORLD_DIR "/art.rpga"
#define ONLINE_WORLD WORLD_DIR "/online.rpgw"

enum { SHIRT, PANTS, HAIR, SKIN, OUTFIT_PARTS };
enum { ARCH = 29, SIGN, GLOWBELL };

typedef struct {
    int width, height, spawnX, spawnY;
    uint32_t signature;
    uint8_t outfit[OUTFIT_PARTS];
    uint8_t ground[WORLD_CELLS], object[WORLD_CELLS];
} World;

typedef struct {
    int x, y;
    uint8_t outfit[OUTFIT_PARTS];
    uint8_t found[WORLD_CELLS / 8];
    bool completed;
} Journey;

extern const char *const outfitNames[OUTFIT_PARTS];
extern const char *const colorNames[OUTFIT_PARTS][6];
extern const uint32_t outfitColors[OUTFIT_PARTS][6][2];

bool worldParse(World *world, const uint8_t *data, size_t size);
bool worldRead(World *world, const char *path);
bool worldWalkable(const World *world, int x, int y);
int worldObject(const World *world, int x, int y);
int worldGlowbells(const World *world);
bool journeyFound(const Journey *journey, int index);
void journeyCollect(Journey *journey, int index);
int journeyCount(const World *world, const Journey *journey);
void journeyNew(const World *world, Journey *journey);
bool journeyLoad(const World *world, Journey *journey, const char *path);
bool journeySave(const World *world, const Journey *journey, const char *path);
bool worldWrite(const char *path, const uint8_t *data, size_t size);
void worldMakeDirectory(void);

#endif
