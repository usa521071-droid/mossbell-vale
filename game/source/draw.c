#include "draw.h"
#include "world.h"

#include <stddef.h>
#include <stdio.h>
#include <string.h>

extern const unsigned char asset_tiles[], asset_player[], asset_blob[], asset_npc[];
enum { OLD_TILE_BYTES = 128 * 256 * 4, TILE_BYTES = 128 * 288 * 4,
    FRAME_SIDE = 16, PLAYER_STRIDE = 48, OLD_PLAYER_HEIGHT = 24, PLAYER_DIRECTIONS = 4,
    PLAYER_FRAMES = 3, OLD_PLAYER_BYTES = PLAYER_STRIDE * OLD_PLAYER_HEIGHT * PLAYER_DIRECTIONS * 4,
    PLAYER_BYTES = PLAYER_STRIDE * FRAME_SIDE * PLAYER_DIRECTIONS * 4,
    CREATURE_BYTES = 48 * 16 * 4, NPC_STRIDE = 64, NPC_BYTES = NPC_STRIDE * 32 * 4 };
static unsigned char editedArt[TILE_BYTES + PLAYER_BYTES + CREATURE_BYTES + NPC_BYTES];
static const unsigned char *tiles = asset_tiles, *player = asset_player, *creature = asset_blob, *npc = asset_npc;

static void restoreBaseTile(int id)
{
    int offset = (id / 8 * 16 * 128 + id % 8 * 16) * 4;
    for (int row = 0; row < 16; row++, offset += 128 * 4)
        memcpy(editedArt + offset, asset_tiles + offset, 16 * 4);
}

static bool groundPixelsOpaque(void)
{
    for (int tile = 0; tile < 144; tile++) {
        if (!worldGroundTile(tile)) continue;
        int offset = (tile / 8 * 16 * 128 + tile % 8 * 16) * 4 + 3;
        for (int row = 0; row < 16; row++, offset += 128 * 4)
            for (int column = 0; column < 16; column++)
                if (editedArt[offset + column * 4] != 255) return false;
    }
    return true;
}

void resetArt(void)
{
    tiles = asset_tiles;
    player = asset_player;
    creature = asset_blob;
    npc = asset_npc;
}

bool stockPlayerArt(void)
{
    return player == asset_player;
}

static void migratePlayer(unsigned char *destination, const unsigned char *source)
{
    memset(destination, 0, PLAYER_BYTES);
    for (int direction = 0; direction < PLAYER_DIRECTIONS; direction++) {
        for (int frame = 0; frame < PLAYER_FRAMES; frame++) {
            const unsigned char *original =
                source + (direction * OLD_PLAYER_HEIGHT * PLAYER_STRIDE + frame * FRAME_SIDE) * 4;
            unsigned char *replacement =
                destination + (direction * FRAME_SIDE * PLAYER_STRIDE + frame * FRAME_SIDE) * 4;
            int top = OLD_PLAYER_HEIGHT, bottom = -1;
            for (int row = 0; row < OLD_PLAYER_HEIGHT; row++) {
                for (int column = 0; column < FRAME_SIDE; column++) {
                    if (!original[(row * PLAYER_STRIDE + column) * 4 + 3]) continue;
                    if (top == OLD_PLAYER_HEIGHT) top = row;
                    bottom = row;
                    break;
                }
            }
            if (bottom < 0) continue;
            int height = bottom - top + 1;
            int crop = bottom - FRAME_SIDE + 1;
            if (crop < 0) crop = 0;
            if (crop > OLD_PLAYER_HEIGHT - FRAME_SIDE) crop = OLD_PLAYER_HEIGHT - FRAME_SIDE;
            for (int row = 0; row < FRAME_SIDE; row++) {
                int sourceRow = height > FRAME_SIDE
                    ? top + (2 * row + 1) * height / (2 * FRAME_SIDE)
                    : crop + row;
                memcpy(replacement + row * PLAYER_STRIDE * 4, original + sourceRow * PLAYER_STRIDE * 4,
                    FRAME_SIDE * 4);
            }
        }
    }
}

static bool finishArt(int version)
{
    if (version == 1) {
        static const int moved[] = {20, 21, 60, 61, 62};
        for (size_t i = 0; i < sizeof(moved) / sizeof(*moved); i++)
            restoreBaseTile(moved[i]);
        for (int id = 108; id <= 122; id++) restoreBaseTile(id);
    }
    if (!groundPixelsOpaque()) return false;
    tiles = editedArt;
    player = editedArt + TILE_BYTES;
    creature = player + PLAYER_BYTES;
    npc = creature + CREATURE_BYTES;
    return true;
}

static bool validHeader(const unsigned char *header)
{
    return !memcmp(header, "RPGA", 4) && header[4] >= 1 && header[4] <= 4 &&
        !(header[5] | header[6] | header[7]);
}

bool loadArtBytes(const unsigned char *data, unsigned size)
{
    resetArt();
    if (size < 8 || !validHeader(data)) return false;
    int version = data[4];
    if (version == 4) {
        if (size != 8 + sizeof(editedArt)) return false;
        memcpy(editedArt, data + 8, sizeof(editedArt));
    } else {
        unsigned tileSize = version == 3 ? TILE_BYTES : OLD_TILE_BYTES;
        unsigned blobSize = version == 3 ? CREATURE_BYTES : 0;
        if (size != 8 + tileSize + OLD_PLAYER_BYTES + blobSize) return false;
        const unsigned char *oldPlayer = data + 8 + tileSize;
        memcpy(editedArt, data + 8, tileSize);
        if (tileSize < TILE_BYTES)
            memcpy(editedArt + tileSize, asset_tiles + tileSize, TILE_BYTES - tileSize);
        migratePlayer(editedArt + TILE_BYTES, oldPlayer);
        unsigned char *blob = editedArt + TILE_BYTES + PLAYER_BYTES;
        memcpy(blob, blobSize ? oldPlayer + OLD_PLAYER_BYTES : asset_blob, CREATURE_BYTES);
        memcpy(blob + CREATURE_BYTES, asset_npc, NPC_BYTES);
    }
    return finishArt(version);
}

bool loadCustomArt(const char *path)
{
    resetArt();
    FILE *file = fopen(path, "rb");
    if (!file) return false;
    static unsigned char data[8 + sizeof(editedArt) + 1];
    size_t length = fread(data, 1, sizeof(data), file);
    bool ok = !ferror(file) && loadArtBytes(data, length);
    fclose(file);
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
    static const uint8_t lowercase[][7] = {
        {0,0,14,1,15,17,15}, {16,16,22,25,17,17,30}, {0,0,14,17,16,17,14},
        {1,1,13,19,17,17,15}, {0,0,14,17,31,16,14}, {6,9,8,30,8,8,8},
        {0,0,15,17,15,1,14}, {16,16,22,25,17,17,17}, {4,0,12,4,4,4,14},
        {2,0,6,2,2,18,12}, {16,16,18,20,24,20,18}, {12,4,4,4,4,4,14},
        {0,0,26,21,21,17,17}, {0,0,22,25,17,17,17}, {0,0,14,17,17,17,14},
        {0,0,30,17,30,16,16}, {0,0,15,17,15,1,1}, {0,0,22,25,16,16,16},
        {0,0,15,16,14,1,30}, {8,8,30,8,8,9,6}, {0,0,17,17,17,19,13},
        {0,0,17,17,17,10,4}, {0,0,17,17,21,21,10}, {0,0,17,10,4,10,17},
        {0,0,17,17,15,1,14}, {0,0,31,2,4,8,31},
    };
    static const uint8_t punctuation[][7] = {
        {0,0,0,0,0,4,4}, {0,0,0,0,4,4,8}, {0,4,4,0,4,4,0},
        {0,0,0,31,0,0,0}, {1,1,2,4,8,16,16}, {4,4,4,4,4,0,4},
        {14,17,1,2,4,0,4}, {4,4,8,0,0,0,0}, {0,4,8,16,8,4,0},
        {0,4,2,1,2,4,0}, {2,4,8,8,8,4,2}, {8,4,2,2,2,4,8},
        {0,0,0,0,0,0,31}, {10,10,20,0,0,0,0}, {0,4,4,31,4,4,0},
        {0,0,31,0,31,0,0}, {10,10,31,10,31,10,10},
    };
    const char *symbols = ".,:-/!?'<>()_\"+=#";
    if (letter >= '0' && letter <= '9') return font[letter - '0'];
    if (letter >= 'A' && letter <= 'Z') return font[10 + letter - 'A'];
    if (letter >= 'a' && letter <= 'z') return lowercase[letter - 'a'];
    for (int i = 0; symbols[i]; i++)
        if (letter == (unsigned char)symbols[i]) return punctuation[i];
    return NULL;
}

void label(Screen screen, int x, int y, const char *text, uint32_t color, int scale)
{
    for (; *text; text++, x += 6 * scale) {
        const uint8_t *rows = glyph((unsigned char)*text);
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

void drawCreature(Screen screen, int x, int y, int frame, int scale)
{
    blit(screen, x, y, creature + frame * 16 * 4, 48, 16, 16, scale, NULL);
}

void drawPlayer(Screen screen, int x, int y, int direction, int frame, int scale,
    const uint8_t *outfit)
{
    blit(screen, x, y, player + (direction * FRAME_SIDE * PLAYER_STRIDE + frame * FRAME_SIDE) * 4,
        PLAYER_STRIDE, FRAME_SIDE, FRAME_SIDE, scale, outfit);
}

void drawNpc(Screen screen, int x, int y, int variant, int frame, int scale)
{
    const unsigned char *sprite = npc + FRAME_SIDE * (frame * NPC_STRIDE + variant) * 4;
    blit(screen, x, y, sprite, NPC_STRIDE, FRAME_SIDE, FRAME_SIDE, scale, NULL);
}
