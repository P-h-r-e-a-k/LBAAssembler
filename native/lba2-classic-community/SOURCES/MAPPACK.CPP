// LBA Assembler's maps, loaded from the game folder's MAPS folder (2026-10-15).
//
// The user: "The GR2 file contains the raw structural data for a map's physical geometry and tile architecture ... needs two external
// componets the .brk file that hold the individual graphic blocks/tiles. and the .bl2 file the layout library ... LS2 file: placement of
// twinsen, enemies, and characters ... I don't know if when loading a map raw it can also reference a brick package but that's what i'd
// like to do ... Maybe just a simple txt tile inside the zip that can be manually added later? ... save them as a zip but a zip with a
// custom extension so it can keep .lbamap but it is actually a zip file and can just be opened with a file extractor."
//
// A map is a zip, `<name>.lbamap`, of:
//   MAP.TXT     what it is, "key = value" lines ('#' a comment):
//                 game = lba2              (an LBA1 map is not this engine's: left alone)
//                 kind = interior | island
//                 name = ..., description = ...
//                 scene <number> = <file>.LS2   one line for each of its scenes, in its own order: the game's scene <number> is played
//                                               from it. A cube-change zone in a map's scene going to 30000 + k goes to its k-th.
//                 grid = <file>.GR2         an interior's grid (LBA_BKG.HQR's grid entry: its 34-byte header -- library, fragments, used
//                                           blocks -- and its columns)
//                 library = <file>.BL2      its own block library (else: the game's its grid's header names)
//                 bricks = <file>.BRK       its own bricks, an HQR: brick n is its entry n, as the library's brick numbers count them
//                                           (else: the game's)
//                 fragments = <file>.GRM    its own grid fragments, an HQR (else: the game's its grid's header names)
//                 forbidden brick = <n>     of its own bricks, the one never drawn (LBA_BKG.HQR's header's ForbidenBrick)
//                 ile = <file>.ILE, obl = <file>.OBL    an island's: its scenes are drawn from them
//                 weather = snow            an island's: snow falls on it (SNOW.CPP)
//   and those files. Members are stored or deflated (ZIPREAD.CPP).
//
// A scene number another map took first is left to it (maps are read in their file names' order). A map's scene numbered as one of the
// game's is played instead of the game's.

#include "C_EXTERN.H"
#include "DIRECTORIES.H"
#include "MAPPACK.H"

#include <SYSTEM/HQFILE.H>
#include <SYSTEM/HQRLOAD.H>
#include <SYSTEM/LOG.H>
#include <SYSTEM/ZIPREAD.H>

#include <dirent.h>
#include <stdlib.h>
#include <string.h>

#define MAPS_FOLDER "MAPS"
#define MAP_EXTENSION ".lbamap"
#define MAP_MANIFEST "MAP.TXT"
#define MAX_MAP_PACKS 64
#define MAX_PACK_MEMBERS 1100 // an island of 16 x 16 cubes has 256 scenes
#define MAX_PACK_SCENES 256
#define MAP_LOCAL_SCENE 30000
#define MAX_PACK_BRICKS 20000 // GRILLE.CPP MAX_BRICK_GAME: LoadUsedBrick's renumbering table

typedef struct {
    char Zip[ADELINE_MAX_PATH];
    char File[64]; // the zip's file name
    T_ZIP_MEMBER *Members;
    S32 NbMembers;
    S32 NbScenes;
    S32 Scenes[MAX_PACK_SCENES];      // the game scene numbers of its scenes, in its own order (-1: another map's)
    S32 SceneMember[MAX_PACK_SCENES]; // their LS2s
    S32 Grid, Library, Bricks, Fragments, Ile, Obl; // members, -1 none
    U32 NbBricks;
    S32 Forbidden;
    S32 Snow; // weather = snow
    char Island[ADELINE_MAX_PATH]; // the island name its ILE and OBL are virtual files under
    S32 NeedsDone;
    T_MAP_GRID_NEEDS Needs;
} T_MAP_PACK;

static T_MAP_PACK *Packs = NULL;
static S32 NbPacks = 0;
static S32 Scanned = FALSE;
static U8 *HookEntry = NULL; // the last scene record served
// LBA_BKG.HQR's header's starts (MapPack_GridNeeds)
static U32 BkgBllStart = 0, BkgBrkStart = 0, BkgMaxBrk = 0;
static S32 BkgKnown = FALSE;

static U8 *SceneHook(const char *name, S32 index, U32 *size);

// (installed with the program: HQF_Init asks it for every entry)
static struct HookInstaller {
    HookInstaller() { HQF_Hook = SceneHook; }
} TheHookInstaller;

//──────────────────────────────────────────────────────────────────────────
static void FreePacks(void) {
    for (S32 i = 0; i < NbPacks; i++) free(Packs[i].Members);
    free(Packs);
    Packs = NULL;
    NbPacks = 0;
    free(HookEntry);
    HookEntry = NULL;
    VFile_Clear();
}

void MapPack_Reset(void) {
    FreePacks();
    Scanned = FALSE;
}

static char *Trim(char *s) {
    while (*s == ' ' || *s == '\t') s++;
    char *e = s + strlen(s);
    while (e > s && (e[-1] == ' ' || e[-1] == '\t' || e[-1] == '\r' || e[-1] == '\n')) *--e = '\0';
    return s;
}

static S32 SceneTaken(S32 scene) {
    for (S32 p = 0; p < NbPacks; p++)
        for (S32 k = 0; k < Packs[p].NbScenes; k++)
            if (Packs[p].Scenes[k] == scene) return TRUE;
    return FALSE;
}

// A member MAP.TXT names ("game" or nothing: none, the game's own).
static S32 Member(T_MAP_PACK *p, const char *key, const char *value) {
    if (!value[0] || !strcasecmp(value, "game")) return -1;
    S32 m = Zip_FindMember(p->Members, p->NbMembers, value);
    if (m < 0) Log_Warn("MAPS: %s: its %s %s isn't in it", p->File, key, value);
    return m;
}

// The path a member of a map opens by (OpenRead: a virtual file).
static void MemberPath(const T_MAP_PACK *p, S32 member, char *path, U32 size) {
    snprintf(path, size, "%s|%s", p->Zip, p->Members[member].Name);
}

static void ReadPack(const char *zip, const char *file) {
    T_MAP_PACK *p = &Packs[NbPacks];
    memset(p, 0, sizeof(*p));
    strncpy(p->Zip, zip, ADELINE_MAX_PATH - 1);
    strncpy(p->File, file, sizeof(p->File) - 1);
    p->Grid = p->Library = p->Bricks = p->Fragments = p->Ile = p->Obl = -1;
    p->Forbidden = -1;

    p->Members = (T_ZIP_MEMBER *)malloc(sizeof(T_ZIP_MEMBER) * MAX_PACK_MEMBERS);
    if (!p->Members) return;
    p->NbMembers = Zip_ReadDirectory(zip, p->Members, MAX_PACK_MEMBERS);
    if (p->NbMembers < 0) {
        Log_Warn("MAPS: %s isn't a zip this reads", file);
        free(p->Members);
        return;
    }
    if (p->NbMembers > MAX_PACK_MEMBERS) {
        Log_Warn("MAPS: %s has %d files: the first %d are read", file, (int)p->NbMembers, MAX_PACK_MEMBERS);
        p->NbMembers = MAX_PACK_MEMBERS;
    }
    S32 txt = Zip_FindMember(p->Members, p->NbMembers, MAP_MANIFEST);
    U8 *text = txt < 0 ? NULL : Zip_LoadMember(zip, &p->Members[txt], 1);
    if (!text) {
        Log_Warn("MAPS: %s has no %s (a map an older LBA Assembler saved: open it and save it again)", file, MAP_MANIFEST);
        free(p->Members);
        return;
    }

    S32 lba2 = TRUE;
    char *line = (char *)text;
    while (line && *line) {
        char *next = strchr(line, '\n');
        if (next) *next++ = '\0';
        char *l = Trim(line);
        char *eq = strchr(l, '=');
        line = next;
        if (l[0] == '#' || !eq) continue;
        *eq = '\0';
        char *key = Trim(l), *value = Trim(eq + 1);
        if (!strncasecmp(key, "scene ", 6)) {
            S32 number = atoi(key + 6);
            S32 m = Member(p, "scene", value);
            if (p->NbScenes >= MAX_PACK_SCENES || m < 0) continue;
            if (number < 0 || number >= MAX_GRID_SCENES) {
                Log_Warn("MAPS: %s: scene %d is past the engine's %d scenes", file, (int)number, MAX_GRID_SCENES);
                number = -1;
            } else if (SceneTaken(number)) {
                Log_Warn("MAPS: %s: scene %d is another map's (change its number in its MAP.TXT)", file, (int)number);
                number = -1;
            }
            p->Scenes[p->NbScenes] = number;
            p->SceneMember[p->NbScenes] = m;
            p->NbScenes++;
        } else if (!strcasecmp(key, "game")) {
            lba2 = !strcasecmp(value, "lba2");
        } else if (!strcasecmp(key, "grid")) {
            p->Grid = Member(p, key, value);
        } else if (!strcasecmp(key, "library")) {
            p->Library = Member(p, key, value);
        } else if (!strcasecmp(key, "bricks")) {
            p->Bricks = Member(p, key, value);
        } else if (!strcasecmp(key, "fragments")) {
            p->Fragments = Member(p, key, value);
        } else if (!strcasecmp(key, "forbidden brick")) {
            p->Forbidden = atoi(value);
        } else if (!strcasecmp(key, "ile")) {
            p->Ile = Member(p, key, value);
        } else if (!strcasecmp(key, "obl")) {
            p->Obl = Member(p, key, value);
        } else if (!strcasecmp(key, "weather")) {
            p->Snow = !strcasecmp(value, "snow");
        }
    }
    free(text);
    if (!lba2 || !p->NbScenes) {
        if (lba2) Log_Warn("MAPS: %s names no scene", file);
        free(p->Members);
        return;
    }

    char path[ADELINE_MAX_PATH];
    // its own bricks: how many (its HQR's table, its last word the file's size)
    if (p->Bricks >= 0) {
        U32 first = 0;
        MemberPath(p, p->Bricks, path, sizeof(path));
        VFile_Add(path, zip, &p->Members[p->Bricks]);
        S32 h = OpenRead(path);
        if (h) {
            Read(h, &first, 4);
            Close(h);
        }
        p->NbBricks = first >= 8 ? first / 4 - 1 : 0;
        if (!p->NbBricks || p->NbBricks > MAX_PACK_BRICKS) {
            Log_Warn("MAPS: %s: its bricks %s can't be read (%u): the game's are used", file, p->Members[p->Bricks].Name, (unsigned)p->NbBricks);
            p->Bricks = -1;
        }
    }
    if (p->Fragments >= 0) {
        MemberPath(p, p->Fragments, path, sizeof(path));
        VFile_Add(path, zip, &p->Members[p->Fragments]);
    }
    // an island: its ILE and OBL under a name no file of the game folder can have ('|')
    if (p->Ile >= 0 && p->Obl >= 0) {
        char name[ADELINE_MAX_PATH];
        const char *stem = p->Members[p->Ile].Name;
        if (strrchr(stem, '/')) stem = strrchr(stem, '/') + 1;
        snprintf(p->Island, sizeof(p->Island), "%s|%s", file, stem);
        if (strrchr(p->Island, '.')) *strrchr(p->Island, '.') = '\0';
        snprintf(name, sizeof(name), "%s.ile", p->Island);
        GetResPath(path, ADELINE_MAX_PATH, name);
        VFile_Add(path, zip, &p->Members[p->Ile]);
        snprintf(name, sizeof(name), "%s.obl", p->Island);
        GetResPath(path, ADELINE_MAX_PATH, name);
        VFile_Add(path, zip, &p->Members[p->Obl]);
    } else if (p->Ile >= 0 || p->Obl >= 0) {
        Log_Warn("MAPS: %s: an island needs its ILE and its OBL", file);
    }

    S32 lo = 0x7FFFFFFF, hi = -1;
    for (S32 k = 0; k < p->NbScenes; k++)
        if (p->Scenes[k] >= 0) {
            if (p->Scenes[k] < lo) lo = p->Scenes[k];
            if (p->Scenes[k] > hi) hi = p->Scenes[k];
        }
    Log_Info("MAPS: %s: %d scene%s (%d to %d)%s%s%s", file, (int)p->NbScenes, p->NbScenes > 1 ? "s" : "", (int)lo, (int)hi,
             p->Island[0] ? ", an island" : p->Grid >= 0 ? ", an interior" : "", p->Library >= 0 ? ", its own block library" : "",
             p->Bricks >= 0 ? ", its own bricks" : "");
    NbPacks++;
}

static int CompareNames(const void *a, const void *b) { return strcasecmp((const char *)a, (const char *)b); }

static void Scan(void) {
    char dir[ADELINE_MAX_PATH], zip[ADELINE_MAX_PATH];
    Scanned = TRUE;
    FreePacks();
    GetResPath(dir, ADELINE_MAX_PATH, MAPS_FOLDER);
    DIR *d = opendir(dir);
    if (!d) return;
    // (in their names' order: which of two maps numbering a scene the same gets it doesn't change from one run to the next)
    static char names[MAX_MAP_PACKS][64];
    S32 n = 0;
    struct dirent *e;
    while ((e = readdir(d)) != NULL) {
        size_t len = strlen(e->d_name);
        if (len < strlen(MAP_EXTENSION) || len >= 64 || strcasecmp(e->d_name + len - strlen(MAP_EXTENSION), MAP_EXTENSION)) continue;
        if (n == MAX_MAP_PACKS) {
            Log_Warn("MAPS: more than %d maps: the rest are left", MAX_MAP_PACKS);
            break;
        }
        strcpy(names[n++], e->d_name);
    }
    closedir(d);
    if (!n) return;
    qsort(names, n, sizeof(names[0]), CompareNames);
    Packs = (T_MAP_PACK *)calloc(n, sizeof(T_MAP_PACK));
    if (!Packs) return;
    for (S32 i = 0; i < n; i++) {
        snprintf(zip, sizeof(zip), "%s%c%s", dir, '/', names[i]);
        ReadPack(zip, names[i]);
    }
}

static void Ensure(void) {
    if (!Scanned) Scan();
}

static T_MAP_PACK *PackOf(S32 scene, S32 *k) {
    Ensure();
    if (scene < 0) return NULL;
    for (S32 p = 0; p < NbPacks; p++)
        for (S32 i = 0; i < Packs[p].NbScenes; i++)
            if (Packs[p].Scenes[i] == scene) {
                if (k) *k = i;
                return &Packs[p];
            }
    return NULL;
}

//──────────────────────────────────────────────────────────────────────────
// SCENE.HQR's entry n + 1 (scene n) for a map's scene: its LS2.
static U8 *SceneHook(const char *name, S32 index, U32 *size) {
    if (index < 1 || !name) return NULL;
    const char *base = name + strlen(name);
    while (base > name && base[-1] != '/' && base[-1] != '\\') base--;
    if (strcasecmp(base, SCENE_HQR_NAME)) return NULL;
    S32 k;
    T_MAP_PACK *p = PackOf(index - 1, &k);
    if (!p) return NULL;
    free(HookEntry);
    HookEntry = Zip_LoadMember(p->Zip, &p->Members[p->SceneMember[k]], 0);
    if (!HookEntry) return NULL;
    *size = p->Members[p->SceneMember[k]].Size;
    return HookEntry;
}

S32 MapPack_HasScene(S32 scene) { return PackOf(scene, NULL) != NULL; }

S32 MapPack_ZoneScene(S32 scene, S32 num) {
    if (num < MAP_LOCAL_SCENE) return num;
    T_MAP_PACK *p = PackOf(scene, NULL);
    if (!p) return num;
    S32 k = num - MAP_LOCAL_SCENE;
    if (k >= p->NbScenes || p->Scenes[k] < 0) {
        Log_Warn("MAPS: %s: a zone of scene %d goes to its scene %d, which it hasn't", p->File, (int)scene, (int)k);
        return scene;
    }
    return p->Scenes[k];
}

const char *MapPack_IslandFile(S32 scene) {
    T_MAP_PACK *p = PackOf(scene, NULL);
    return p && p->Island[0] ? p->Island : NULL;
}

S32 MapPack_Snows(S32 scene) {
    T_MAP_PACK *p = PackOf(scene, NULL);
    return p && p->Snow;
}

U32 MapPack_MaxSceneSize(void) {
    U32 biggest = 0;
    Ensure();
    for (S32 p = 0; p < NbPacks; p++)
        for (S32 k = 0; k < Packs[p].NbScenes; k++)
            if (Packs[p].Members[Packs[p].SceneMember[k]].Size > biggest) biggest = Packs[p].Members[Packs[p].SceneMember[k]].Size;
    return biggest;
}

//──────────────────────────────────────────────────────────────────────────
// The bricks a grid's used blocks name (GRILLE.CPP LoadUsedBrick's first pass) and their sizes: what its buffers need.
static void PackNeeds(T_MAP_PACK *p) {
    char bkg[ADELINE_MAX_PATH], path[ADELINE_MAX_PATH];
    U8 *grid = NULL, *library = NULL, *flags = NULL;
    U32 libsize = 0, count, table, i, j;
    S32 h = 0;

    p->NeedsDone = TRUE;
    memset(&p->Needs, 0, sizeof(p->Needs));
    if (p->Grid < 0 || !BkgKnown) return;
    GetResPath(bkg, ADELINE_MAX_PATH, BKG_HQR_NAME);
    p->Needs.Gri = p->Members[p->Grid].Size;
    grid = Zip_LoadMember(p->Zip, &p->Members[p->Grid], 0);
    if (!grid || p->Members[p->Grid].Size < 34) goto done;
    if (p->Library >= 0) {
        library = Zip_LoadMember(p->Zip, &p->Members[p->Library], 0);
        libsize = p->Members[p->Library].Size;
        p->Needs.Bll = libsize;
    } else {
        libsize = HQF_Init(bkg, BkgBllStart + grid[0]);
        library = libsize ? (U8 *)malloc(libsize + RECOVER_AREA) : NULL;
        if (library) HQF_LoadClose(library);
        else HQF_Close();
    }
    if (!library || libsize < 4) goto done;

    if (p->Bricks >= 0) {
        MemberPath(p, p->Bricks, path, sizeof(path));
        table = 0;
        count = p->NbBricks;
    } else {
        strcpy(path, bkg);
        table = BkgBrkStart << 2;
        count = BkgMaxBrk;
    }
    flags = (U8 *)calloc(count + 1, 1);
    if (!flags) goto done;
    {
        U32 nbblock = *(U32 *)library / 4;
        for (i = 1; i < 256; i++) {
            if (!(grid[2 + (i >> 3)] & (1 << (7 - (i & 7)))) || i - 1 >= nbblock) continue;
            U32 at = *(U32 *)(library + (i - 1) * 4);
            if (at + 3 > libsize) continue;
            U32 cells = library[at] * library[at + 1] * library[at + 2];
            for (j = 0; j < cells && at + 5 + j * 4 + 2 <= libsize; j++) {
                U32 brick = *(U16 *)(library + at + 5 + j * 4);
                if (brick && brick - 1 < count) flags[brick - 1] = 1;
            }
        }
    }
    h = OpenRead(path);
    if (!h) goto done;
    {
        U32 *offsets = (U32 *)malloc(count * 4 + 4);
        U32 n = 0, bytes = 0;
        if (offsets) {
            Seek(h, (S32)table, SEEK_FROM_START);
            Read(h, offsets, count * 4);
            for (i = 0; i < count; i++) {
                if (!flags[i]) continue;
                COMPRESSED_HEADER header;
                Seek(h, (S32)offsets[i], SEEK_FROM_START);
                if (Read(h, &header, sizeof(header)) != (S32)sizeof(header)) continue;
                n++;
                bytes += header.SizeFile;
            }
            free(offsets);
        }
        // (LoadUsedBrick: an offset for each brick and one after; CreateMaskGph: a mask no bigger than its brick and a byte a line more)
        p->Needs.Brick = (n + 1) * 4 + bytes;
        p->Needs.Mask = (n + 1) * 4 + bytes + n * 64;
    }
    Close(h);
done:
    free(flags);
    free(library);
    free(grid);
}

void MapPack_GridNeeds(U32 bllStart, U32 brkStart, U32 maxBrk, T_MAP_GRID_NEEDS *needs) {
    BkgBllStart = bllStart;
    BkgBrkStart = brkStart;
    BkgMaxBrk = maxBrk;
    BkgKnown = TRUE;
    memset(needs, 0, sizeof(*needs));
    Ensure();
    for (S32 i = 0; i < NbPacks; i++) {
        T_MAP_PACK *p = &Packs[i];
        PackNeeds(p);
        if (p->Needs.Gri > needs->Gri) needs->Gri = p->Needs.Gri;
        if (p->Needs.Bll > needs->Bll) needs->Bll = p->Needs.Bll;
        if (p->Needs.Brick > needs->Brick) needs->Brick = p->Needs.Brick;
        if (p->Needs.Mask > needs->Mask) needs->Mask = p->Needs.Mask;
    }
}

S32 MapPack_LoadGrid(S32 scene, U8 *bufmap, U8 *tabblock, const T_MAP_GRID_NEEDS *room, S32 *ownLibrary) {
    T_MAP_PACK *p = PackOf(scene, NULL);
    *ownLibrary = FALSE;
    if (!p || p->Grid < 0) return FALSE;
    if (!p->NeedsDone) PackNeeds(p);
    if (p->Needs.Gri > room->Gri || p->Needs.Bll > room->Bll || p->Needs.Brick > room->Brick || p->Needs.Mask > room->Mask) {
        Log_Warn("MAPS: %s: scene %d's grid doesn't fit the engine's buffers (grid %u/%u, library %u/%u, bricks %u/%u, masks %u/%u): "
                 "the game is started again with it in MAPS to make room",
                 p->File, (int)scene, (unsigned)p->Needs.Gri, (unsigned)room->Gri, (unsigned)p->Needs.Bll, (unsigned)room->Bll,
                 (unsigned)p->Needs.Brick, (unsigned)room->Brick, (unsigned)p->Needs.Mask, (unsigned)room->Mask);
        return FALSE;
    }
    U8 *grid = Zip_LoadMember(p->Zip, &p->Members[p->Grid], 0);
    if (!grid) return FALSE;
    memcpy(bufmap, grid, p->Members[p->Grid].Size);
    free(grid);
    if (p->Library >= 0) {
        U8 *library = Zip_LoadMember(p->Zip, &p->Members[p->Library], 0);
        if (library) {
            memcpy(tabblock, library, p->Members[p->Library].Size);
            free(library);
            *ownLibrary = TRUE;
        }
    }
    return TRUE;
}

S32 MapPack_Bricks(S32 scene, char *path, U32 pathSize, U32 *count, S32 *forbidden) {
    T_MAP_PACK *p = PackOf(scene, NULL);
    if (!p || p->Bricks < 0) return FALSE;
    MemberPath(p, p->Bricks, path, pathSize);
    *count = p->NbBricks;
    *forbidden = p->Forbidden;
    return TRUE;
}

S32 MapPack_LoadFragment(S32 scene, S32 n, void *dst) {
    char path[ADELINE_MAX_PATH];
    T_MAP_PACK *p = PackOf(scene, NULL);
    if (!p || p->Fragments < 0) return FALSE;
    MemberPath(p, p->Fragments, path, sizeof(path));
    return Load_HQR(path, dst, n) != 0;
}
