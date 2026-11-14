#include <3ds.h>
#include <stdio.h>
#include <string.h>

#include "draw.h"
#include "project.h"
#include "update.h"
#include "world.h"

extern const unsigned char asset_world[];
extern const unsigned int asset_world_size;
extern const unsigned char asset_project[], asset_project_art[];
extern const unsigned int asset_project_size, asset_project_art_size;

enum { EXPLORE, MENU, WARDROBE, UPDATES, REGIONS };
enum { TILE = 32, TOP_WIDTH = 400, BOTTOM_WIDTH = 320, HEIGHT = 240 };
static const uint32_t ink = 0xd5e9cd, panel = 0xfffbeb, dark = 0x477b62;
static const uint32_t border = 0x7bad84, paper = 0x303945, muted = 0x526558, gold = 0x986045;
static const uint32_t frame = 0x394d58, highlight = 0xfff0b8;
static const int dx[] = {0, -1, 1, 0}, dy[] = {1, 0, 0, -1};
static const unsigned keys[] = {KEY_DOWN, KEY_LEFT, KEY_RIGHT, KEY_UP};
static World world;
static Journey journey;
static Project project;
static ProjectProgress progress;
static int mapIndex = -1;
static Update update, work;
static UpdateAction pendingAction;
static Thread worker;
static int mode, selected, direction, elapsed, animation, xPixel, yPixel;
static int total, found, toastTime, lastDirection = -1;
static unsigned installedWorldRevision;
static const char *savePath;
static const char *mapName;
static bool useCustom;
static int viewScale = 2;
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

static void window(Screen screen, int x, int y, int width, int height)
{
    fill(screen, x + 2, y + 3, width, height, 0x35475c);
    fill(screen, x + 2, y, width - 4, height, frame);
    fill(screen, x, y + 2, width, height - 4, frame);
    fill(screen, x + 2, y + 2, width - 4, height - 4, border);
    fill(screen, x + 4, y + 4, width - 8, height - 8, panel);
    fill(screen, x + 5, y + 4, width - 10, 1, 0xffffff);
}

static const Map *activeMap(void)
{
    return mapIndex >= 0 ? &project.maps[mapIndex] : NULL;
}

static void placePlayer(void)
{
    xPixel = journey.x * TILE;
    yPixel = journey.y * TILE;
    elapsed = 0;
    animation = 0;
}

static void openMap(int index)
{
    mapIndex = index;
    const Map *map = activeMap();
    worldParseProjectMap(&world, map->raw, map->size);
    mapName = map->name;
}

static bool saveJourney(void)
{
    if (activeMap()) return projectSave(&project, &progress, &journey, mapIndex, savePath);
    return journeySave(&world, &journey, savePath);
}

static bool loadGame(void)
{
    projectClose(&project);
    memset(&progress, 0, sizeof(progress));
    mapIndex = -1;
    direction = 0;
    lastDirection = -1;
    installedWorldRevision = updateWorldRevision();
    useCustom = projectRead(&project, CUSTOM_PROJECT, NULL);
    unsigned revision = 0;
    bool updated = false;
    if (!useCustom) {
        projectClose(&project);
        useCustom = worldRead(&world, CUSTOM_WORLD);
        if (!useCustom) {
            updated = projectRead(&project, ONLINE_PROJECT, &revision);
            if (!updated) {
                installedWorldRevision = 0;
                projectClose(&project);
                if (!projectParse(&project, asset_project, asset_project_size)) projectClose(&project);
            }
        }
    }
    resetArt();
    if (asset_project_art_size) loadArtBytes(asset_project_art, asset_project_art_size);
    if (useCustom) {
        if (!loadCustomArt(CUSTOM_ART) && asset_project_art_size)
            loadArtBytes(asset_project_art, asset_project_art_size);
    }
    if (project.count) {
        savePath = useCustom ? WORLD_DIR "/custom-project.sav" :
            updated ? WORLD_DIR "/online-project.sav" : WORLD_DIR "/project.sav";
        int index = projectResume(&project, &progress, &journey, savePath);
        openMap(index >= 0 ? index : project.start);
        if (index < 0) journeyNew(&world, &journey);
        total = project.bells;
        found = projectFound(&project, &progress);
    } else {
        if (!useCustom && !worldParse(&world, asset_world, asset_world_size)) return false;
        mapName = useCustom ? "Custom map" : "Mossbell Vale";
        savePath = useCustom ? WORLD_DIR "/custom.sav" : WORLD_DIR "/town.sav";
        journeyNew(&world, &journey);
        journeyLoad(&world, &journey, savePath);
        total = worldGlowbells(&world);
        found = journeyCount(&world, &journey);
    }
    placePlayer();
    return true;
}

static void interact(void)
{
    int x = journey.x + dx[direction], y = journey.y + dy[direction];
    const Map *map = activeMap();
    const ProjectEvent *event = map ? mapEvent(map, x, y) : NULL;
    if (event && event->type != PROJECT_WARP) {
        say(event->text);
        return;
    }
    int object = worldObject(&world, x, y);
    if (object == SIGN)
        say("Welcome, trailkeeper! Gather the glowbells, then ring the stone arch.");
    else if (object == ARCH && found < total)
        say("The arch is quiet. Gather the remaining glowbells.");
    else if (object == ARCH && !journey.completed) {
        journey.completed = true;
        if (saveJourney())
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
    if (!saveJourney())
        say("Glowbell found. Progress could not be saved to the SD card.");
    else if (found == total)
        say("All glowbells found. Face the stone arch and press A.");
    else
        say("You found a glowbell!");
}

static void finishStep(void)
{
    collect();
    const Map *map = activeMap();
    const ProjectEvent *event = map ? mapEvent(map, journey.x, journey.y) : NULL;
    if (!event || event->type != PROJECT_WARP) return;
    int next = projectIndex(&project, event->target);
    if (next < 0) return;
    memcpy(progress.collected[mapIndex], journey.found, sizeof(journey.found));
    openMap(next);
    journey.x = event->targetX;
    journey.y = event->targetY;
    memcpy(journey.found, progress.collected[next], sizeof(journey.found));
    placePlayer();
    say(event->text[0] ? event->text : mapName);
    saveJourney();
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
        if (!elapsed) finishStep();
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
    const Map *map = activeMap();
    if (!(map ? mapWalkable(map, &world, x, y) : worldWalkable(&world, x, y))) return;
    journey.x = x;
    journey.y = y;
    elapsed = TILE;
}

static void drawWorldPlayer(Screen screen, int cameraX, int cameraY)
{
    drawPlayer(screen, xPixel * viewScale / 2 - cameraX,
        yPixel * viewScale / 2 - cameraY - 8 * viewScale,
        direction, animation, viewScale, journey.outfit);
}

static void drawWorld(Screen screen)
{
    fill(screen, 0, 0, screen.width, screen.height, dark);
    int tile = 16 * viewScale;
    int cameraX = clamp(xPixel * viewScale / 2 + tile / 2 - TOP_WIDTH / 2, 0,
        world.width * tile - TOP_WIDTH);
    int cameraY = clamp(yPixel * viewScale / 2 + tile / 2 - HEIGHT / 2, 0,
        world.height * tile - HEIGHT);
    int left = clamp(cameraX / tile, 0, world.width - 1);
    int top = clamp(cameraY / tile, 0, world.height - 1);
    int right = clamp((cameraX + TOP_WIDTH) / tile, 0, world.width - 1);
    int bottom = clamp((cameraY + HEIGHT) / tile, 0, world.height - 1);
    for (int y = top; y <= bottom; y++)
        for (int x = left; x <= right; x++)
            drawTile(screen, x * tile - cameraX, y * tile - cameraY,
                world.ground[y * world.width + x], viewScale);
    bool playerDrawn = false;
    for (int y = top; y <= bottom; y++) {
        if (!playerDrawn && y * TILE > yPixel) {
            drawWorldPlayer(screen, cameraX, cameraY);
            playerDrawn = true;
        }
        for (int x = left; x <= right; x++) {
            int index = y * world.width + x;
            int object = world.object[index];
            if (object && !(object == GLOWBELL && journeyFound(&journey, index)))
                drawTile(screen, x * tile - cameraX, y * tile - cameraY, object, viewScale);
        }
        const Map *map = activeMap();
        if (map) {
            for (unsigned i = 0; i < map->eventCount; i++) {
                const ProjectEvent *event = &map->events[i];
                if (event->y != y || event->x < left || event->x > right) continue;
                int x = event->x * tile - cameraX, py = y * tile - cameraY;
                if (event->type == PROJECT_NPC) {
                    const uint8_t colors[] = {2, 3, 1, 3};
                    drawPlayer(screen, x, py - 8 * viewScale, 0, 0, viewScale, colors);
                } else if (event->type == PROJECT_SIGN && !world.object[y * world.width + event->x]) {
                    drawTile(screen, x, py, SIGN, viewScale);
                }
            }
        }
    }
    if (!playerDrawn) drawWorldPlayer(screen, cameraX, cameraY);
    if (toastTime && mode == EXPLORE) {
        window(screen, 8, 186, 384, 45);
        wrapped(screen, 22, 197, 354, toast, paper);
        fill(screen, 375, 218, 7, 2, 0xab5047);
        fill(screen, 377, 220, 3, 2, 0xab5047);
    }
}

static void heading(Screen screen, const char *title, const char *subtitle)
{
    fill(screen, 0, 0, BOTTOM_WIDTH, HEIGHT, ink);
    fill(screen, 0, 0, BOTTOM_WIDTH, 53, dark);
    fill(screen, 0, 1, BOTTOM_WIDTH, 3, 0xa1c89b);
    fill(screen, 0, 52, BOTTOM_WIDTH, 2, frame);
    fill(screen, 0, 54, BOTTOM_WIDTH, 3, border);
    drawTile(screen, 14, 10, GLOWBELL, 2);
    int scale = strlen(title) <= 21 ? 2 : 1;
    label(screen, 57, 13 + (scale == 1 ? 6 : 0), title, frame, scale);
    label(screen, 55, 11 + (scale == 1 ? 6 : 0), title, 0xfff9d7, scale);
    label(screen, 55, 36, subtitle, 0xecf7d8, 1);
}

static void row(Screen screen, int index, int y, const char *text, bool enabled)
{
    bool active = selected == index;
    fill(screen, 14, y, 292, 25, border);
    fill(screen, 16, y + 2, 288, 21, active ? highlight : panel);
    if (active) label(screen, 23, y + 9, ">", gold, 1);
    label(screen, 36, y + 9, text, enabled ? paper : muted, 1);
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
            int ground = world.ground[index];
            static const uint32_t terrain[] = {0xb99c8e,0xa88c7d,0xc4a492,0x9f8376,
                0xa9a493,0x9b9885,0x8da684,0x8d919b};
            uint32_t color = ground < 16 ? colors[ground] :
                ground < 44 ? colors[4] : ground < 56 ? colors[8] :
                ground < 64 ? terrain[ground - 56] : ground < 120 ? 0x666b63 : 0xe1c88b;
            if (object >= 16 && object <= 18) color = 0x3d6447;
            if (object == 20 || object == 21) color = 0x666453;
            if (object >= 64 && object <= 72) color = 0x6983a3;
            if (object >= 88 && object <= 94) color = 0x3d6447;
            if (object >= 96 && object <= 107) color = 0x875d51;
            fill(screen, 24 + px, 92 + py, 1, 1, color);
        }
    }
    int x = 24 + (journey.x * size + size / 2) / world.width;
    int y = 92 + (journey.y * size + size / 2) / world.height;
    fill(screen, x - 2, y - 2, 5, 5, frame);
    fill(screen, x - 1, y - 1, 3, 3, 0xfff1a7);
}

static void drawBottom(Screen screen)
{
    char text[64];
    if (mode == EXPLORE) {
        heading(screen, MOSSBELL_TITLE, mapName);
        window(screen, 14, 67, 112, 130);
        label(screen, 24, 80, "Field Map", muted, 1);
        drawMinimap(screen);
        window(screen, 133, 67, 173, 130);
        label(screen, 144, 85, "Glowbells", muted, 1);
        snprintf(text, sizeof(text), "%d/%d", found, total);
        label(screen, 144, 103, text, gold, 2);
        const char *goal = journey.completed ? "The arch is lit. The vale is yours to explore." :
            !total ? "Explore the vale and discover what lies along the trail." :
            found == total ? "Ring the stone arch." : "Find the glowbells scattered across the vale.";
        wrapped(screen, 144, 135, 147, goal, paper);
        window(screen, 14, 207, 292, 26);
        label(screen, 24, 216, "A TALK   B RUN   X ZOOM   START MENU", muted, 1);
    } else if (mode == MENU) {
        heading(screen, "Trail Menu", "Up / Down  Select     A  Open");
        row(screen, 0, 70, "Wardrobe", true);
        row(screen, 1, 100, "Connected maps", activeMap() != NULL);
        row(screen, 2, 130, "Updates", true);
        row(screen, 3, 160, "Save journey", true);
        row(screen, 4, 190, "Return to Homebrew / HOME", true);
        label(screen, 24, 224, "B RETURN TO THE VALE", muted, 1);
    } else if (mode == REGIONS) {
        heading(screen, "Connected maps", "Walk roads and doorways to travel");
        int first = clamp(selected - 2, 0, project.count < 5 ? 0 : project.count - 5);
        for (int i = first; i < project.count && i < first + 5; i++) {
            snprintf(text, sizeof(text), "%s%s", project.maps[i].name, i == mapIndex ? "  (Here)" : "");
            row(screen, i, 67 + (i - first) * 29, text, true);
        }
        label(screen, 24, 224, "UP / DOWN VIEW    B BACK", muted, 1);
    } else if (mode == WARDROBE) {
        heading(screen, "Wardrobe", "Left / Right  Change     B  Done");
        for (int i = 0; i < OUTFIT_PARTS; i++) {
            snprintf(text, sizeof(text), "%s  < %s >", outfitNames[i], colorNames[i][journey.outfit[i]]);
            fill(screen, 14, 77 + i * 35, 185, 26, border);
            fill(screen, 16, 79 + i * 35, 181, 22, selected == i ? highlight : panel);
            fill(screen, 179, 84 + i * 35, 13, 12, outfitColors[i][journey.outfit[i]][0]);
            label(screen, 24, 87 + i * 35, text, paper, 1);
        }
        window(screen, 207, 77, 99, 131);
        drawPlayer(screen, 233, 97, direction, 0, 3, journey.outfit);
        label(screen, 222, 184, "A  TURN", muted, 1);
        label(screen, 24, 224, "OUTFIT SAVES WHEN YOU LEAVE", muted, 1);
    } else {
        heading(screen, "Updates", "Game " MOSSBELL_VERSION "  /  GitHub");
        row(screen, 0, 72, "Check for updates", !worker);
        row(screen, 1, 102, "Download official world", !worker && update.worldRevision > installedWorldRevision);
        row(screen, 2, 132, "Download game CIA", !worker && update.build > MOSSBELL_BUILD);
        window(screen, 14, 167, 292, 54);
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
    if (!worker || threadJoin(worker, 0) != 0) return;
    threadFree(worker);
    worker = NULL;
    update = work;
    if (pendingAction == UPDATE_WORLD && update.worldRevision > installedWorldRevision &&
        updateWorldRevision() == update.worldRevision) {
        installedWorldRevision = update.worldRevision;
        if (!useCustom) {
            saveJourney();
            loadGame();
        } else strcpy(update.status, "Official world saved. Your SD project stays active.");
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
        if (pressed & KEY_X) viewScale = 3 - viewScale;
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
            if (!saveJourney()) say("Could not save the outfit to the SD card.");
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
    int count = mode == MENU ? 5 : mode == REGIONS ? project.count : 3;
    if (pressed & KEY_UP) selected = (selected + count - 1) % count;
    if (pressed & KEY_DOWN) selected = (selected + 1) % count;
    if (mode == REGIONS) return true;
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
    case 0: selectMode(WARDROBE); break;
    case 1:
        if (activeMap()) {
            selectMode(REGIONS);
            selected = mapIndex;
        }
        break;
    case 2: selectMode(UPDATES); break;
    case 3:
        say(saveJourney() ? "Journey saved." : "Could not save to the SD card.");
        selectMode(EXPLORE);
        break;
    case 4: return false;
    }
    return true;
}

int main(void)
{
    gfxInit(GSP_RGB565_OES, GSP_RGB565_OES, false);
    if (!loadGame()) {
        gfxExit();
        return 1;
    }
    strcpy(update.status, "Check GitHub for new game versions and official worlds.");
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
    saveJourney();
    projectClose(&project);
    gfxExit();
    return 0;
}
