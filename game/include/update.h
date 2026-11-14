#ifndef MOSSBELL_UPDATE_H
#define MOSSBELL_UPDATE_H

#include "world.h"

#ifndef MOSSBELL_BUILD
#define MOSSBELL_BUILD 10400
#endif
#ifndef MOSSBELL_VERSION
#define MOSSBELL_VERSION "1.4.0"
#endif
#if !MOSSBELL_PUBLISHED && !defined(MOSSBELL_GITHUB)
#define MOSSBELL_GITHUB "usa521071-droid/mossbell-vale"
#endif

typedef enum {
    UPDATE_CHECK, UPDATE_WORLD, UPDATE_GAME
} UpdateAction;

typedef struct {
    unsigned build, worldRevision;
    uint8_t digest[32];
    char status[100];
} Update;

void updateRun(Update *update, UpdateAction action);
unsigned updateWorldRevision(void);

#endif
