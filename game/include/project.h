#ifndef MOSSBELL_PROJECT_H
#define MOSSBELL_PROJECT_H

#include "world.h"

#define PROJECT_MAX_MAPS 16
#define PROJECT_MAX_EVENTS 64
#define PROJECT_MAX_SIZE (12 + PROJECT_MAX_MAPS * (8 + 54 + WORLD_FILE_MAX + WORLD_CELLS + PROJECT_MAX_EVENTS * 98))
#define CUSTOM_PROJECT WORLD_DIR "/world.rpgp"
#define ONLINE_PROJECT WORLD_DIR "/online.rpgp"

enum { PROJECT_WARP = 1, PROJECT_SIGN, PROJECT_NPC };

typedef struct {
    uint8_t type, x, y, targetX, targetY;
    uint16_t target;
    char text[91];
} ProjectEvent;

typedef struct {
    uint16_t id, bells, eventCount;
    uint8_t width, height;
    char name[32], group[24];
    const uint8_t *raw, *movement;
    size_t size;
    ProjectEvent events[PROJECT_MAX_EVENTS];
} Map;

typedef struct {
    int count, start, bells;
    uint32_t signature;
    uint8_t *owned;
    Map maps[PROJECT_MAX_MAPS];
} Project;

typedef struct {
    uint8_t collected[PROJECT_MAX_MAPS][WORLD_CELLS / 8];
    bool completed;
} ProjectProgress;

bool projectParse(Project *project, const uint8_t *data, size_t size);
bool projectRead(Project *project, const char *path, unsigned *revision);
void projectClose(Project *project);
int projectIndex(const Project *project, uint16_t id);
bool mapWalkable(const Map *map, const World *world, int x, int y);
const ProjectEvent *mapEvent(const Map *map, int x, int y);
bool projectSave(const Project *project, ProjectProgress *progress, const Journey *journey,
    int mapIndex, const char *path);
int projectResume(const Project *project, ProjectProgress *progress, Journey *journey,
    const char *path);
int projectFound(const Project *project, const ProjectProgress *progress);

#endif
