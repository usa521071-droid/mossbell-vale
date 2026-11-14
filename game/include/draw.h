#ifndef MOSSBELL_DRAW_H
#define MOSSBELL_DRAW_H

#include <stdint.h>
#include <stdbool.h>

typedef struct {
    uint16_t *pixels;
    int width, height;
} Screen;

void fill(Screen screen, int x, int y, int width, int height, uint32_t color);
void label(Screen screen, int x, int y, const char *text, uint32_t color, int scale);
void wrapped(Screen screen, int x, int y, int width, const char *text, uint32_t color);
void drawTile(Screen screen, int x, int y, int tile, int scale);
void drawCreature(Screen screen, int x, int y, int frame, int scale);
bool loadCustomArt(const char *path);
bool loadArtBytes(const unsigned char *data, unsigned size);
void resetArt(void);
void drawPlayer(Screen screen, int x, int y, int direction, int frame, int scale,
    const uint8_t *outfit);

#endif
