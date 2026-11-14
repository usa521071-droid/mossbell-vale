#include "update.h"

#include <3ds.h>
#include <curl/curl.h>
#include <mbedtls/sha256.h>
#include <malloc.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#define BASE_URL "https://raw.githubusercontent.com/usa521071-droid/mossbell-vale/main/updates/"
#define RELEASE_URL "https://github.com/usa521071-droid/mossbell-vale/releases/download/"
#define CIA_PATH WORLD_DIR "/MossbellVale-update.cia"
#define CIA_TEMP CIA_PATH ".tmp"

extern const unsigned char asset_ca_bundle[];
extern const unsigned int asset_ca_bundle_size;

typedef struct {
    uint8_t *buffer;
    size_t limit, length;
    FILE *file;
    mbedtls_sha256_context *sha;
} Transfer;

static size_t receive(char *data, size_t size, size_t count, void *context)
{
    Transfer *transfer = context;
    size_t length = size * count;
    if (length > transfer->limit - transfer->length) return 0;
    if (transfer->file) {
        if (fwrite(data, 1, length, transfer->file) != length ||
            mbedtls_sha256_update_ret(transfer->sha, (const uint8_t *)data, length) != 0)
            return 0;
    } else {
        memcpy(transfer->buffer + transfer->length, data, length);
    }
    transfer->length += length;
    return length;
}

static bool fetch(const char *url, Transfer *transfer, char *error)
{
    CURL *curl = curl_easy_init();
    if (!curl) {
        strcpy(error, "Could not start the HTTPS request.");
        return false;
    }
    struct curl_blob roots = {(void *)asset_ca_bundle, asset_ca_bundle_size, CURL_BLOB_NOCOPY};
    curl_easy_setopt(curl, CURLOPT_URL, url);
    curl_easy_setopt(curl, CURLOPT_USERAGENT, "MossbellVale/" MOSSBELL_VERSION);
    curl_easy_setopt(curl, CURLOPT_FOLLOWLOCATION, 1L);
    curl_easy_setopt(curl, CURLOPT_MAXREDIRS, 4L);
    curl_easy_setopt(curl, CURLOPT_PROTOCOLS_STR, "https");
    curl_easy_setopt(curl, CURLOPT_REDIR_PROTOCOLS_STR, "https");
    curl_easy_setopt(curl, CURLOPT_CONNECTTIMEOUT, 10L);
    curl_easy_setopt(curl, CURLOPT_TIMEOUT, 45L);
    curl_easy_setopt(curl, CURLOPT_FAILONERROR, 1L);
    curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, receive);
    curl_easy_setopt(curl, CURLOPT_WRITEDATA, transfer);
    CURLcode result = curl_easy_setopt(curl, CURLOPT_CAINFO_BLOB, &roots);
    if (result == CURLE_OK) result = curl_easy_perform(curl);
    curl_easy_cleanup(curl);
    if (result != CURLE_OK) snprintf(error, 100, "HTTPS: %s", curl_easy_strerror(result));
    return result == CURLE_OK;
}

static bool parseCatalog(Update *update, const char *data)
{
    unsigned build, revision;
    char hash[65], extra;
    if (sscanf(data, "MVB1 %u %u %64[0123456789abcdefABCDEF] %c",
        &build, &revision, hash, &extra) != 3 || strlen(hash) != 64 ||
        build > 999999 || revision > 999999 || !build)
        return false;
    for (int i = 0; i < 32; i++) {
        char byte[] = {hash[i * 2], hash[i * 2 + 1], 0};
        update->digest[i] = (uint8_t)strtoul(byte, NULL, 16);
    }
    update->build = build;
    update->worldRevision = revision >= FIRST_TOWN_REVISION ? revision : 0;
    return true;
}

static bool readOnline(World *world, unsigned *revision)
{
    FILE *file = fopen(ONLINE_WORLD, "rb");
    if (!file) return false;
    uint8_t *data = malloc(4 + WORLD_FILE_MAX + 1);
    World *temporary = world ? NULL : malloc(sizeof(World));
    if (!data || (!world && !temporary)) {
        fclose(file);
        free(data);
        free(temporary);
        return false;
    }
    size_t length = fread(data, 1, 4 + WORLD_FILE_MAX + 1, file);
    bool ok = !ferror(file) && length >= 4;
    fclose(file);
    if (ok) {
        *revision = (unsigned)data[0] | (unsigned)data[1] << 8 |
            (unsigned)data[2] << 16 | (unsigned)data[3] << 24;
        ok = *revision >= FIRST_TOWN_REVISION && *revision <= 999999 &&
            worldParse(world ? world : temporary, data + 4, length - 4);
    }
    free(data);
    free(temporary);
    return ok;
}

bool updateReadWorld(World *world)
{
    unsigned revision;
    return readOnline(world, &revision);
}

unsigned updateWorldRevision(void)
{
    unsigned revision = 0;
    return readOnline(NULL, &revision) ? revision : 0;
}

static void updateWorld(Update *update)
{
    char url[160];
    snprintf(url, sizeof(url), BASE_URL "world-%u.rpgw", update->worldRevision);
    static uint8_t data[4 + WORLD_FILE_MAX];
    Transfer transfer = {data + 4, WORLD_FILE_MAX, 0, NULL, NULL};
    if (!fetch(url, &transfer, update->status)) return;
    static World validation;
    if (!worldParse(&validation, data + 4, transfer.length)) {
        strcpy(update->status, "The downloaded world file is invalid.");
        return;
    }
    for (int i = 0; i < 4; i++) data[i] = update->worldRevision >> (8 * i);
    worldMakeDirectory();
    if (!worldWrite(ONLINE_WORLD, data, transfer.length + 4)) {
        strcpy(update->status, "Could not write the update to the SD card.");
        return;
    }
    strcpy(update->status, "Official world updated. Press B to return.");
}

static void updateGame(Update *update)
{
    char url[180];
    unsigned build = update->build;
    snprintf(url, sizeof(url), RELEASE_URL "v%u.%u.%u/MossbellVale.cia",
        build / 10000, build / 100 % 100, build % 100);
    worldMakeDirectory();
    FILE *file = fopen(CIA_TEMP, "wb");
    if (!file) {
        strcpy(update->status, "Could not open the SD card for the download.");
        return;
    }
    mbedtls_sha256_context sha;
    mbedtls_sha256_init(&sha);
    uint8_t hash[32];
    Transfer transfer = {NULL, 32 * 1024 * 1024, 0, file, &sha};
    bool ok = mbedtls_sha256_starts_ret(&sha, 0) == 0 &&
        fetch(url, &transfer, update->status);
    if (fclose(file) != 0) {
        strcpy(update->status, "Could not finish writing to the SD card.");
        ok = false;
    }
    if (ok && (mbedtls_sha256_finish_ret(&sha, hash) != 0 ||
        memcmp(hash, update->digest, sizeof(hash)))) {
        strcpy(update->status, "Download checksum did not match.");
        ok = false;
    }
    mbedtls_sha256_free(&sha);
    if (ok) {
        ok = rename(CIA_TEMP, CIA_PATH) == 0;
        strcpy(update->status, ok ? "CIA saved. Open your CIA installer to install it." :
            "Could not save the downloaded CIA.");
    }
    if (!ok) remove(CIA_TEMP);
}

void updateRun(Update *update, UpdateAction action)
{
    uint32_t *socketMemory = memalign(0x1000, 0x100000);
    if (!socketMemory) {
        strcpy(update->status, "Not enough memory for networking.");
        return;
    }
    if (R_FAILED(socInit(socketMemory, 0x100000))) {
        strcpy(update->status, "Could not connect. Check your Wi-Fi settings.");
        free(socketMemory);
        return;
    }
    if (R_FAILED(sslcInit(0))) {
        strcpy(update->status, "Could not initialize the 3DS TLS service.");
        socExit();
        free(socketMemory);
        return;
    }
    if (curl_global_init(CURL_GLOBAL_DEFAULT) != CURLE_OK) {
        strcpy(update->status, "Could not initialize HTTPS.");
    } else {
        if (action == UPDATE_CHECK) {
            char data[128] = {0};
            Transfer transfer = {(uint8_t *)data, sizeof(data) - 1, 0, NULL, NULL};
            if (fetch(BASE_URL "catalog.txt", &transfer, update->status)) {
                if (!parseCatalog(update, data))
                    strcpy(update->status, "The GitHub update catalog is invalid.");
                else if (update->build > MOSSBELL_BUILD ||
                    update->worldRevision > updateWorldRevision())
                    strcpy(update->status, "Updates are available.");
                else
                    strcpy(update->status, "Your game and official world are up to date.");
            }
        } else if (action == UPDATE_WORLD) {
            updateWorld(update);
        } else {
            updateGame(update);
        }
        curl_global_cleanup();
    }
    sslcExit();
    socExit();
    free(socketMemory);
}
