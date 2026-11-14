#include "draw.h"
#include "world.h"

#include <ctype.h>
#include <stddef.h>
#include <stdio.h>
#include <string.h>

extern const unsigned char asset_tiles[], asset_player[];
enum { TILE_BYTES = 128 * 256 * 4, PLAYER_BYTES = 48 * 96 * 4 };
static unsigned char editedArt[TILE_BYTES + PLAYER_BYTES];
static const unsigned char *tiles = asset_tiles, *player = asset_player;

void resetArt(void)
{
    tiles = asset_tiles;
    player = asset_player;
}

bool loadCustomArt(const char *path)
{
    static const unsigned char expected[] = {'R','P','G','A',1,0,0,0};
    unsigned char header[sizeof(expected)];
    resetArt();
    FILE *file = fopen(path, "rb");
    if (!file) return false;
    bool ok = fread(header, 1, sizeof(header), file) == sizeof(header) &&
        memcmp(header, expected, sizeof(header)) == 0 &&
        fread(editedArt, 1, sizeof(editedArt), file) == sizeof(editedArt) &&
        fgetc(file) == EOF && !ferror(file);
    fclose(file);
    if (ok) {
        tiles = editedArt;
        player = editedArt + TILE_BYTES;
    }
    return ok;
}

static uint16_t pack(uint32_t color)
{
    return (uint16_t)(((color >> 8) & 0xf800) | ((color >> 5) & 0x07e0) |
        ((color >> 3) & 0x001f));
}

void fill(Screen screen, int x, int y, int width, int height, uint32_t color)
{
    int right = x + width, bottom = y + height;
    if (x < 0) x = 0;
    if (y < 0) y = 0;
    if (right > screen.width) right = screen.width;
    if (bottom > screen.height) bottom = screen.height;
    if (bottom <= y || right <= x) return;
    uint16_t packed = pack(color);
    for (int column = x; column < right; column++) {
        uint16_t *out = screen.pixels + column * screen.height + screen.height - bottom;
        for (int row = y; row < bottom; row++) *out++ = packed;
    }
}

/* Each glyph contains seven rows of five pixels. */
static const uint8_t font[][7] = {
    {14,17,19,21,25,17,14}, {4,12,4,4,4,4,14}, {14,17,1,2,4,8,31},
    {30,1,1,14,1,1,30}, {2,6,10,18,31,2,2}, {31,16,16,30,1,1,30},
    {14,16,16,30,17,17,14}, {31,1,2,4,8,8,8}, {14,17,17,14,17,17,14},
    {14,17,17,15,1,1,14},
    {14,17,17,31,17,17,17}, {30,17,17,30,17,17,30}, {14,17,16,16,16,17,14},
    {30,17,17,17,17,17,30}, {31,16,16,30,16,16,31}, {31,16,16,30,16,16,16},
    {14,17,16,23,17,17,15}, {17,17,17,31,17,17,17}, {14,4,4,4,4,4,14},
    {7,2,2,2,2,18,12}, {17,18,20,24,20,18,17}, {16,16,16,16,16,16,31},
    {17,27,21,21,17,17,17}, {17,25,21,19,17,17,17}, {14,17,17,17,17,17,14},
    {30,17,17,30,16,16,16}, {14,17,17,17,21,18,13}, {30,17,17,30,20,18,17},
    {15,16,16,14,1,1,30}, {31,4,4,4,4,4,4}, {17,17,17,17,17,17,14},
    {17,17,17,17,17,10,4}, {17,17,17,21,21,27,17}, {17,17,10,4,10,17,17},
    {17,17,10,4,4,4,4}, {31,1,2,4,8,16,31},
};

static const uint8_t *glyph(unsigned char letter)
{
    static const uint8_t punctuation[][7] = {
        {0,0,0,0,0,4,4}, {0,0,0,0,4,4,8}, {0,4,4,0,4,4,0},
        {0,0,0,31,0,0,0}, {1,1,2,4,8,16,16}, {4,4,4,4,4,0,4},
        {14,17,1,2,4,0,4}, {4,4,8,0,0,0,0}, {0,4,8,16,8,4,0},
        {0,4,2,1,2,4,0},
    };
    const char *symbols = ".,:-/!?'<>";
    if (letter >= '0' && letter <= '9') return font[letter - '0'];
    if (letter >= 'A' && letter <= 'Z') return font[10 + letter - 'A'];
    for (int i = 0; symbols[i]; i++)
        if (letter == (unsigned char)symbols[i]) return punctuation[i];
    return NULL;
}

void label(Screen screen, int x, int y, const char *text, uint32_t color, int scale)
{
    for (; *text; text++, x += 6 * scale) {
        const uint8_t *rows = glyph((unsigned char)toupper((unsigned char)*text));
        if (!rows) continue;
        for (int dy = 0; dy < 7; dy++)
            for (int dx = 0; dx < 5; dx++)
                if (rows[dy] & (16 >> dx))
                    fill(screen, x + dx * scale, y + dy * scale, scale, scale, color);
    }
}

void wrapped(Screen screen, int x, int y, int width, const char *text, uint32_t color)
{
    int limit = width / 6;
    while (*text) {
        while (*text == ' ') text++;
        char line[65];
        int end = 0, space = 0;
        while (text[end] && end < limit && end < (int)sizeof(line) - 1) {
            if (text[end] == ' ') space = end;
            end++;
        }
        if (text[end] && text[end] != ' ' && space > 0) end = space;
        for (int i = 0; i < end; i++) line[i] = text[i];
        line[end] = 0;
        label(screen, x, y, line, color, 1);
        text += end;
        y += 12;
    }
}

static uint32_t recolor(uint32_t color, const uint8_t *outfit)
{
    for (int part = 0; part < OUTFIT_PARTS; part++)
        for (int shade = 0; shade < 2; shade++)
            if (color == outfitColors[part][0][shade])
                return outfitColors[part][outfit[part]][shade];
    return color;
}

static uint16_t blend(uint16_t background, uint16_t foreground, unsigned alpha)
{
    unsigned inverse = 255 - alpha;
    unsigned r = ((background >> 11) * inverse + (foreground >> 11) * alpha + 127) / 255;
    unsigned g = (((background >> 5) & 63) * inverse + ((foreground >> 5) & 63) * alpha + 127) / 255;
    unsigned b = ((background & 31) * inverse + (foreground & 31) * alpha + 127) / 255;
    return (uint16_t)((r << 11) | (g << 5) | b);
}

static void blit(Screen screen, int x, int y, const unsigned char *sheet,
    int stride, int width, int height, int scale, const uint8_t *outfit)
{
    for (int dx = 0; dx < width; dx++) {
        int column = x + dx * scale;
        if (column >= screen.width || column + scale <= 0) continue;
        for (int dy = 0; dy < height; dy++) {
            const unsigned char *pixel = sheet + (dy * stride + dx) * 4;
            int row = y + dy * scale;
            if (!pixel[3] || row >= screen.height || row + scale <= 0) continue;
            uint32_t rgb = (uint32_t)pixel[0] << 16 | (uint32_t)pixel[1] << 8 | pixel[2];
            if (outfit) rgb = recolor(rgb, outfit);
            uint16_t color = pack(rgb);
            int firstY = row < 0 ? 0 : row;
            int endY = row + scale < screen.height ? row + scale : screen.height;
            int firstX = column < 0 ? 0 : column;
            int endX = column + scale < screen.width ? column + scale : screen.width;
            for (int sx = firstX; sx < endX; sx++) {
                uint16_t *out = screen.pixels + sx * screen.height + screen.height - endY;
                for (int sy = firstY; sy < endY; sy++, out++)
                    *out = pixel[3] == 255 ? color : blend(*out, color, pixel[3]);
            }
        }
    }
}

void drawTile(Screen screen, int x, int y, int tile, int scale)
{
    blit(screen, x, y, tiles + ((tile / 8 * 16 * 128) + tile % 8 * 16) * 4,
        128, 16, 16, scale, NULL);
}

void drawPlayer(Screen screen, int x, int y, int direction, int frame, int scale,
    const uint8_t *outfit)
{
    blit(screen, x, y, player + (direction * 24 * 48 + frame * 16) * 4,
        48, 16, 24, scale, outfit);
}
