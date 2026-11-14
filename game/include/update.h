#ifndef MOSSBELL_UPDATE_H
#define MOSSBELL_UPDATE_H

#include "world.h"

#ifndef MOSSBELL_BUILD
#define MOSSBELL_BUILD 10200
#endif
#ifndef MOSSBELL_VERSION
#define MOSSBELL_VERSION "1.2.0"
#endif
#define FIRST_TOWN_REVISION 3

typedef enum {
    UPDATE_CHECK, UPDATE_WORLD, UPDATE_GAME
} UpdateAction;

typedef struct {
    unsigned build, worldRevision;
    uint8_t digest[32];
    char status[100];
} Update;

void updateRun(Update *update, UpdateAction action);
bool updateReadWorld(World *world);
unsigned updateWorldRevision(void);

#endif
