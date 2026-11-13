#include <3ds.h>
#include <stdio.h>
#include <string.h>

#include "draw.h"
#include "update.h"
#include "world.h"

extern const unsigned char asset_world[];
extern const unsigned int asset_world_size;

enum { EXPLORE, MENU, WARDROBE, UPDATES };
enum { TILE = 32, TOP_WIDTH = 400, BOTTOM_WIDTH = 320, HEIGHT = 240 };
static const uint32_t ink = 0x142923, panel = 0x234036, dark = 0x1a322b;
static const uint32_t border = 0x426353, paper = 0xeceacb, muted = 0xafc9a5, gold = 0xf4c76a;
static const int dx[] = {0, -1, 1, 0}, dy[] = {1, 0, 0, -1};
static const unsigned keys[] = {KEY_DOWN, KEY_LEFT, KEY_RIGHT, KEY_UP};

static World world;
static Journey journey;
static Update update, work;
static UpdateAction pendingAction;
static Thread worker;
static int mode, selected, direction, elapsed, animation, xPixel, yPixel;
static int total, found, toastTime, worldSource, lastDirection = -1;
static unsigned installedWorldRevision;
static char toast[100];

static int clamp(int value, int low, int high)
{
    if (high < low) return (low + high) / 2;
    if (value < low) return low;
    if (value > high) return high;
    return value;
}

static void say(const char *message)
{
    snprintf(toast, sizeof(toast), "%s", message);
    toastTime = 260;
}

static void loadWorld(void)
{
    worldSource = 0;
    installedWorldRevision = updateWorldRevision();
    if (worldRead(&world, CUSTOM_WORLD)) worldSource = 2;
    else if (updateReadWorld(&world)) worldSource = 1;
    else worldParse(&world, asset_world, asset_world_size);
    journeyNew(&world, &journey);
    journeyLoad(&world, &journey);
    xPixel = journey.x * TILE;
    yPixel = journey.y * TILE;
    direction = 0;
    elapsed = 0;
    total = worldGlowbells(&world);
    found = journeyCount(&world, &journey);
}

static void interact(void)
{
    int object = worldObject(&world, journey.x + dx[direction], journey.y + dy[direction]);
    if (object == SIGN)
        say("Welcome, trailkeeper! Gather the glowbells, then ring the stone arch.");
    else if (object == ARCH && found < total)
        say("The arch is quiet. Gather the remaining glowbells.");
    else if (object == ARCH && !journey.completed) {
        journey.completed = true;
        if (journeySave(&world, &journey))
            say("The arch sings. You restored the light of Mossbell Vale!");
        else
            say("The arch sings! Progress could not be saved to the SD card.");
    } else if (object == ARCH)
        say("The arch is warm. The vale remembers its trailkeeper.");
}

static void collect(void)
{
    int index = journey.y * world.width + journey.x;
    if (world.object[index] != GLOWBELL || journeyFound(&journey, index)) return;
    journeyCollect(&journey, index);
    found++;
    if (!journeySave(&world, &journey))
        say("Glowbell found. Progress could not be saved to the SD card.");
    else if (found == total)
        say("All glowbells found. Face the stone arch and press A.");
    else
        say("You found a glowbell!");
}

static void move(unsigned held, unsigned pressed)
{
    if (elapsed) {
        int speed = held & KEY_B ? 8 : 4;
        if (speed > elapsed) speed = elapsed;
        xPixel += dx[direction] * speed;
        yPixel += dy[direction] * speed;
        elapsed -= speed;
        animation = 1 + ((xPixel + yPixel) >> 4 & 1);
        if (!elapsed) collect();
        return;
    }
    animation = 0;
    if (pressed & KEY_A) interact();
    for (int i = 0; i < 4; i++)
        if (pressed & keys[i]) lastDirection = i;
    int next = lastDirection;
    if (next < 0 || !(held & keys[next])) {
        next = -1;
        for (int i = 0; i < 4; i++)
            if (held & keys[i]) next = i;
    }
    if (next < 0) return;
    direction = next;
    int x = journey.x + dx[next], y = journey.y + dy[next];
    if (!worldWalkable(&world, x, y)) return;
    journey.x = x;
    journey.y = y;
    elapsed = TILE;
}

static void drawWorld(Screen screen)
{
    fill(screen, 0, 0, screen.width, screen.height, dark);
    int cameraX = clamp(xPixel + TILE / 2 - TOP_WIDTH / 2, 0, world.width * TILE - TOP_WIDTH);
    int cameraY = clamp(yPixel + TILE / 2 - HEIGHT / 2, 0, world.height * TILE - HEIGHT);
    int left = clamp(cameraX / TILE, 0, world.width - 1);
    int top = clamp(cameraY / TILE, 0, world.height - 1);
    int right = clamp((cameraX + TOP_WIDTH) / TILE, 0, world.width - 1);
    int bottom = clamp((cameraY + HEIGHT) / TILE, 0, world.height - 1);
    for (int y = top; y <= bottom; y++)
        for (int x = left; x <= right; x++)
            drawTile(screen, x * TILE - cameraX, y * TILE - cameraY, world.ground[y * world.width + x], 2);
    bool playerDrawn = false;
    for (int y = top; y <= bottom; y++) {
        if (!playerDrawn && y * TILE > yPixel) {
            drawPlayer(screen, xPixel - cameraX, yPixel - cameraY - 16, direction,
                animation, 2, journey.outfit);
            playerDrawn = true;
        }
        for (int x = left; x <= right; x++) {
            int index = y * world.width + x;
            int object = world.object[index];
            if (object && !(object == GLOWBELL && journeyFound(&journey, index)))
                drawTile(screen, x * TILE - cameraX, y * TILE - cameraY, object, 2);
        }
    }
    if (!playerDrawn) drawPlayer(screen, xPixel - cameraX, yPixel - cameraY - 16,
        direction, animation, 2, journey.outfit);
    if (toastTime) {
        fill(screen, 12, 186, 376, 44, ink);
        fill(screen, 12, 186, 376, 2, gold);
        wrapped(screen, 24, 199, 352, toast, paper);
    }
}

static void block(Screen screen, int x, int y, int width, int height)
{
    fill(screen, x, y, width, height, border);
    fill(screen, x + 1, y + 1, width - 2, height - 2, panel);
}

static void heading(Screen screen, const char *title, const char *subtitle)
{
    fill(screen, 0, 0, BOTTOM_WIDTH, HEIGHT, ink);
    fill(screen, 0, 0, BOTTOM_WIDTH, 61, dark);
    fill(screen, 0, 60, BOTTOM_WIDTH, 1, border);
    drawTile(screen, 15, 13, GLOWBELL, 2);
    label(screen, 55, 13, title, paper, 2);
    label(screen, 56, 38, subtitle, muted, 1);
}

static void row(Screen screen, int index, int y, const char *text, bool enabled)
{
    bool active = selected == index;
    fill(screen, 14, y, 292, 25, active ? border : panel);
    if (active) fill(screen, 14, y, 3, 25, gold);
    label(screen, 25, y + 9, text, enabled ? paper : muted, 1);
}

static void drawMinimap(Screen screen)
{
    static const uint32_t colors[] = {0x719953,0x719953,0x81a857,0x66894c,
        0xc5a170,0xb28c65,0xc4ae75,0xb18b5a,0x5497a6,0x3f758e,0x386480,
        0x5f9ea8,0x9c9978,0x628054,0x8e817c,0x9c977f};
    const int size = 92;
    for (int py = 0; py < size; py++) {
        int y = py * world.height / size;
        for (int px = 0; px < size; px++) {
            int x = px * world.width / size, index = y * world.width + x;
            int object = world.object[index];
            uint32_t color = colors[world.ground[index]];
            if (object >= 16 && object <= 18) color = 0x3d6447;
            if (object == 20 || object == 21) color = 0x666453;
            fill(screen, 24 + px, 92 + py, 1, 1, color);
        }
    }
    int x = 24 + (journey.x * size + size / 2) / world.width;
    int y = 92 + (journey.y * size + size / 2) / world.height;
    fill(screen, x - 2, y - 2, 5, 5, ink);
    fill(screen, x - 1, y - 1, 3, 3, gold);
}

static void drawBottom(Screen screen)
{
    char text[64];
    if (mode == EXPLORE) {
        heading(screen, "MOSSBELL VALE", worldSource == 2 ? "CUSTOM WORLD" : "TRAILKEEPER JOURNAL");
        block(screen, 14, 72, 112, 125);
        label(screen, 24, 80, "FIELD MAP", muted, 1);
        drawMinimap(screen);
        block(screen, 133, 72, 173, 125);
        label(screen, 144, 85, "GLOWBELLS", muted, 1);
        snprintf(text, sizeof(text), "%d/%d", found, total);
        label(screen, 144, 103, text, gold, 2);
        const char *goal = journey.completed ? "The arch is lit. The vale is yours to explore." :
            !total ? "Explore the vale and discover what lies along the trail." :
            found == total ? "Ring the stone arch." : "Find the glowbells scattered across the vale.";
        wrapped(screen, 144, 135, 147, goal, paper);
        fill(screen, 14, 207, 292, 24, panel);
        label(screen, 24, 216, "A INTERACT   B RUN   START MENU", muted, 1);
    } else if (mode == MENU) {
        heading(screen, "TRAIL MENU", "UP / DOWN  SELECT     A  OPEN");
        row(screen, 0, 70, "Resume journey", true);
        row(screen, 1, 100, "Wardrobe", true);
        row(screen, 2, 130, "Updates", true);
        row(screen, 3, 160, "Save journey", true);
        row(screen, 4, 190, "Return to Homebrew / HOME", true);
        label(screen, 24, 224, "B RETURN TO THE VALE", muted, 1);
    } else if (mode == WARDROBE) {
        heading(screen, "WARDROBE", "LEFT / RIGHT  CHANGE     B  DONE");
        for (int i = 0; i < OUTFIT_PARTS; i++) {
            snprintf(text, sizeof(text), "%s  < %s >", outfitNames[i], colorNames[i][journey.outfit[i]]);
            fill(screen, 14, 77 + i * 35, 185, 26, selected == i ? border : panel);
            fill(screen, 179, 84 + i * 35, 13, 12, outfitColors[i][journey.outfit[i]][0]);
            label(screen, 24, 87 + i * 35, text, paper, 1);
        }
        block(screen, 207, 77, 99, 131);
        drawPlayer(screen, 233, 97, direction, 0, 3, journey.outfit);
        label(screen, 222, 184, "A  TURN", muted, 1);
        label(screen, 24, 224, "OUTFIT SAVES WHEN YOU LEAVE", muted, 1);
    } else {
        heading(screen, "UPDATES", "GAME " MOSSBELL_VERSION "  /  GITHUB");
        row(screen, 0, 72, "Check for updates", !worker);
        row(screen, 1, 102, "Download official world", !worker && update.worldRevision > installedWorldRevision);
        row(screen, 2, 132, "Download game CIA", !worker && update.build > MOSSBELL_BUILD);
        block(screen, 14, 167, 292, 54);
        wrapped(screen, 24, 177, 274, worker ? "Connecting to GitHub. Please wait..." : update.status, paper);
        label(screen, 24, 229, "A SELECT   B BACK", muted, 1);
    }
}

static void runUpdate(void *unused)
{
    (void)unused;
    updateRun(&work, pendingAction);
}

static void pollUpdate(void)
{
    if (!worker || R_FAILED(threadJoin(worker, 0))) return;
    threadFree(worker);
    worker = NULL;
    update = work;
    if (pendingAction == UPDATE_WORLD && updateWorldRevision() == update.worldRevision) {
        installedWorldRevision = update.worldRevision;
        if (worldSource != 2) loadWorld();
        else strcpy(update.status, "Official world saved. Your custom SD world is active.");
    }
}

static void startUpdate(void)
{
    if (worker) return;
    if (selected == 1 && update.worldRevision <= installedWorldRevision) {
        strcpy(update.status, "Check for a newer world first.");
        return;
    }
    if (selected == 2 && update.build <= MOSSBELL_BUILD) {
        strcpy(update.status, "Check for a newer game first.");
        return;
    }
    pendingAction = (UpdateAction)selected;
    work = update;
    worker = threadCreate(runUpdate, NULL, 64 * 1024, 0x31, -2, false);
    if (!worker) strcpy(update.status, "Could not start the update.");
}

static void selectMode(int next)
{
    mode = next;
    selected = 0;
}

static int touchRow(int y, int base, int count)
{
    if (y < base) return -1;
    int relative = y - base;
    int row = relative / 30;
    return row < count && relative % 30 < 25 ? row : -1;
}

static bool input(unsigned pressed, unsigned held)
{
    touchPosition touch = {0};
    if (pressed & KEY_TOUCH) hidTouchRead(&touch);
    if (mode == EXPLORE) {
        if (elapsed) {
            move(held, 0);
        } else if (pressed & KEY_START || ((pressed & KEY_TOUCH) && touch.py >= 207)) {
            selectMode(MENU);
        } else if (pressed & KEY_SELECT) {
            selectMode(WARDROBE);
        } else {
            move(held, pressed);
        }
        return true;
    }
    if (mode == WARDROBE) {
        if (pressed & (KEY_B | KEY_START | KEY_SELECT)) {
            selectMode(MENU);
            if (!journeySave(&world, &journey)) say("Could not save the outfit to the SD card.");
            return true;
        }
        if (pressed & KEY_UP) selected = (selected + 3) % 4;
        if (pressed & KEY_DOWN) selected = (selected + 1) % 4;
        if (pressed & KEY_LEFT) journey.outfit[selected] = (journey.outfit[selected] + 5) % 6;
        if (pressed & KEY_RIGHT) journey.outfit[selected] = (journey.outfit[selected] + 1) % 6;
        if (pressed & KEY_A) direction = (direction + 1) % 4;
        return true;
    }
    if (pressed & (KEY_B | KEY_START)) {
        selectMode(mode == MENU ? EXPLORE : MENU);
        return true;
    }
    int count = mode == MENU ? 5 : 3;
    if (pressed & KEY_UP) selected = (selected + count - 1) % count;
    if (pressed & KEY_DOWN) selected = (selected + 1) % count;
    bool activate = pressed & KEY_A;
    if ((pressed & KEY_TOUCH) && touch.px >= 14 && touch.px < 306) {
        int hit = touchRow(touch.py, mode == MENU ? 70 : 72, count);
        if (hit >= 0) {
            selected = hit;
            activate = true;
        }
    }
    if (!activate) return true;
    if (mode == UPDATES) {
        startUpdate();
        return true;
    }
    switch (selected) {
    case 0: selectMode(EXPLORE); break;
    case 1: selectMode(WARDROBE); break;
    case 2: selectMode(UPDATES); break;
    case 3:
        say(journeySave(&world, &journey) ? "Journey saved." : "Could not save to the SD card.");
        selectMode(EXPLORE);
        break;
    case 4: return false;
    }
    return true;
}

int main(void)
{
    gfxInit(GSP_RGB565_OES, GSP_RGB565_OES, false);
    loadWorld();
    strcpy(update.status, "Check GitHub for new game versions and official worlds.");
    say("Welcome to Mossbell Vale. Find the trail sign and press A.");
    while (aptMainLoop()) {
        pollUpdate();
        hidScanInput();
        if (!input(hidKeysDown(), hidKeysHeld())) break;
        Screen top = {(uint16_t *)gfxGetFramebuffer(GFX_TOP, GFX_LEFT, NULL, NULL), TOP_WIDTH, HEIGHT};
        Screen bottom = {(uint16_t *)gfxGetFramebuffer(GFX_BOTTOM, GFX_LEFT, NULL, NULL), BOTTOM_WIDTH, HEIGHT};
        drawWorld(top);
        drawBottom(bottom);
        if (toastTime && mode == EXPLORE) toastTime--;
        gfxFlushBuffers();
        gfxSwapBuffers();
        gspWaitForVBlank();
    }
    if (worker) {
        threadJoin(worker, UINT64_MAX);
        threadFree(worker);
    }
    journeySave(&world, &journey);
    gfxExit();
    return 0;
}
