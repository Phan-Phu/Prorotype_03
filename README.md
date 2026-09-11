# Farming Prototype

A top-down farming loop prototype (Stardew-like: till → plant → water → harvest → sell) built **CLI-first** in Unity — every workflow (compile, test, build, balance sim) runs from the terminal via `Makefile` / `tools/*.sh`, no manual Editor clicking required for CI.

**Engine:** Unity **6000.4.0f1** (pinned — see `tools/unity.sh`)
**Status:** core loop is playable end-to-end (till/plant/water/harvest/sell, real art, hotbar + inventory UI, day/night clock), plus a wood side-loop (chop trees with an Axe, sell wood — `DESIGN_BRIEFS.md` [DSN-030]). WebGL module isn't installed on this machine, so `make gate` targets **Win64** by default (`DECISION_LOG.md` 2026-08-15) — that's also what every demo/playtest build has used so far. Build **#19** is the S2-DEV-01..07 wood ticket set (build #16) plus the S2-QA-01..06 QA pass on it (build #17→#19, `KANBAN.md` Sprint 4): hardened `NotChoppable`/`Blocked` branch coverage, a sim-level wood-income-per-day check, PlayMode collision/reticle tests, 2 new debug-panel hooks, a session action logger (`Artifacts/session_<seed>.csv`), and 1 real bug found-and-fixed along the way (`SessionLogger` was keying its output file off the wrong seed source) — gate green end-to-end, 55 EditMode + 3 PlayMode tests. Build **#15** (Sprint 1 "Debt Zero") is unaffected: gate green, first PlayMode test, real headless economy sim, Turnip/Potato visually distinct.

This README is the practical entry point. The full process/role playbooks (longer, in Vietnamese) live in `Agent/`:
[`AGENT_PM.md`](Agent/AGENT_PM.md) · [`AGENT_DEV.md`](Agent/AGENT_DEV.md) · [`AGENT_QA.md`](Agent/AGENT_QA.md) · [`AGENT_DESIGN.md`](Agent/AGENT_DESIGN.md)

---

## Quick start

```bash
# from repo root, bash (Git Bash on Windows)
make compile              # scripts compile, exit 0
make test-edit            # EditMode tests, exit 0
make test-play             # PlayMode tests (boots the scene for real), exit 0
make build-win BUILD=15   # -> Artifacts/Build/Win/Farm.exe
make gate                 # compile -> test-edit -> test-play -> build-win (Win64; see Known Issues for why not WebGL)
make sim                  # headless economy sim (real greedy-farmer) -> Artifacts/economy.csv
```

No `make` on Windows? Use the bash mirrors directly: `bash tools/make.sh <target>`, or `bash tools/gate.sh`.

**Run the built game:**
```powershell
Artifacts\Build\Win\Farm.exe -seed 42 -startMoney 500
```
PowerShell test launch with enough money and fast clock:
```powershell
Start-Process -FilePath ".\Artifacts\Build\Win\Farm.exe" -ArgumentList '-seed 42 -startDay 1 -startMoney 3000 -fastTime'
```
`-seed` is load-bearing: every run should be started with an explicit seed so a bug is reproducible (`BootArgs.cs`). Other flags: `-startDay`, `-fastTime`.

**Controls:** WASD move · `1`-`9`,`0` select hotbar slot · Space/Enter/Left-click use held item · `I` inventory · `F1` debug panel.

**Session log** (S2-QA-06): every real tool-use in a live session (Editor Play Mode or the packaged build above) appends a row to `Artifacts/session_<seed>.csv` — `day,time,action,tile,result,money,stamina` (`SessionLogger.cs`). It's a literal, replayable trace of what a QA/playtest session actually did, keyed by the same `-seed` used to launch the run, so a bug report can point at an exact row. Not written by `make test-edit`/`make sim` — only real interactive play. To grab it after a session: `Artifacts\Build\Win\Farm.exe -seed 42` then open `Artifacts\session_42.csv` once done playing.

---

## Repo layout

```
Assets/_Prototype/Scripts/   gameplay code (plain C#) + MonoBehaviour view layer
Assets/_Prototype/Editor/    CLI entry points: CICompile, CIBuild, CISim, CIArt
Assets/_Prototype/Resources/ ArtCatalog.asset (baked sprite references, see DEV section)
Assets/Tests/EditMode/       NUnit tests, no scene/MonoBehaviour required
Assets/Tests/PlayMode/       boots the scene for real (rendering/input coverage EditMode can't see)
Assets/Sprite Textures/      source art (mostly unused until CIArt wires it in — see DEV)
tools/                       unity.sh, make.sh, gate.sh, report.sh
Artifacts/                   build output + test/sim results (gitignored)
Agent/                       long-form process docs per role
KANBAN.md, DECISION_LOG.md, LATER.md   human-facing project tracking
```

---

## For DEV

### Architecture
- **Plain C# core, no MonoBehaviour**: `GameState` owns `GridMap`, `GameClock`, `Stamina`, `Wallet`, `Inventory`. Fully constructible and testable in EditMode with no scene. Since S2-DEV-01/05 (wood, `DESIGN_BRIEFS.md` [DSN-030]) it also seeds `GridMap`'s static-object layer (`GameState.SeedTrees` places `BalanceConfig.InitialTreeCount` trees at construction) and owns the generic "sell a carried item" channel (`GameState.SellItem`/`SellAllWood`) — the only selling path besides `ToolController.Harvest`'s instant crop-sell.
- **Static object layer on the grid** (S2-DEV-01): `TileData.Object` (`Grid/TileObject.cs`) is a plain-C# `TileObject` — `Type`, `HP`, `RespawnDaysLeft` — describing a static blocking object on a tile (only `Tree` exists today). `GridMap.IsOccupied(coord)` is the single occupancy query every other system reads: `ToolController` refuses Till/Plant on an occupied tile with `ToolResultCode.Blocked`, and `PlayerController` reads it for both movement collision and the cursor reticle (see below). A felled tree (`HP == 0`) counts down `RespawnDaysLeft` once per day (`TileObject.AdvanceDay`, driven by `GameState.OnDayEnded`, same cadence as `CropInstance.AdvanceDay` — no watering/care needed) and regrows to a full tree at 0.
- **View layer is a thin shell**: `GameManager` (bootstraps everything via `[RuntimeInitializeOnLoadMethod]`), `WorldView` (tiles + crop + tile-object sprites), `PlayerController`, `HUD`, `ToolbarUI`, `InventoryScreenUI`, `DebugPanel` — all MonoBehaviours that just read `GameState` and render.
- **Tool/item unification**: there's no fixed "current tool" enum selection anymore. `PlayerController.ActiveSlot` points into `Inventory.Slots[0..9]` (row 0); whatever item sits there resolves to an action (`ResolveAction`). Hoe/WateringCan/Harvest/Axe are real non-consumable inventory items (`ToolItemIds`), granted once by `GameState.GrantStartingTools()`. Axe (S2-DEV-02, `DESIGN_BRIEFS.md` [DSN-030]) resolves through the exact same `ResolveAction` path as every other tool (`ToolType.Chop`) — no separate "current tool" enum was added for it. The always-visible hotbar (`ToolbarUI`) and the full panel (`InventoryScreenUI`, `I` key) both render straight from `Inventory.Slots` — they're two views of the same array, not two systems to keep in sync.
- **Player movement now reads occupancy** (S2-DEV-04): `PlayerController.MoveWithCollision` moves per-axis and refuses to enter a tile where `GridMap.IsOccupied` is true (e.g. a standing tree), sliding along the blocker instead of stopping dead. The target-tile cursor reticle (`UpdateCursor`) shows red on the same condition (`InBounds && !IsOccupied`), so "cursor red" and "can't walk there" always agree.
- **Real art via `ArtCatalog`**: source sprites live under `Assets/Sprite Textures/` (not a `Resources/` folder), so runtime code can't `Resources.Load` them directly. `Farm.Prototype.Editor.CIArt.BuildArtCatalog` (Editor-only, `AssetDatabase`) bakes the specific sprites the game needs into `Assets/_Prototype/Resources/ArtCatalog.asset`; runtime reads that via `PlaceholderArt.Art`. **Run this after touching any source art:**
  ```bash
  bash tools/unity.sh # prints the exe path if you need it directly
  "$(bash tools/unity.sh)" -batchmode -nographics -quit -projectPath "$(cygpath -m "$(pwd)")" \
    -executeMethod Farm.Prototype.Editor.CIArt.BuildArtCatalog -logFile -
  ```
  Every `ArtCatalog` field is picked by **exact sprite name**, never by guessed index — several source sheets turned out not to be what their filename implied (see Known Pitfalls).

### Known pitfalls (found and fixed this project — don't reintroduce them)
1. **`AddComponent<T>()` calls `Awake()` synchronously, before you can set fields on the returned reference.** `WorldView` used to build its tile grid in `Awake()` reading `State`, but `GameManager` sets `wv.State = State` *after* `AddComponent`. Result: the grid never rendered, in every build, for weeks of iteration. Fix: any MonoBehaviour that needs an externally-assigned field before it can initialize should self-init lazily on first `Update()`, not in `Awake()`.
2. **`Sprite.Create(tex, rect, pivot)` defaults `pixelsPerUnit` to 100, not 1.** `PlaceholderArt.WhiteSprite` is scaled by every caller assuming it's exactly 1×1 world unit; left at the default it rendered at 0.01×0.01 — present, just invisible. Always pass `pixelsPerUnit` explicitly for procedural sprites.
3. **Don't assume a sprite sheet's filename tells you what it contains.** `tools.png` looked like flat UI icons from its thumbnail; a magnified export showed it's actually tiered tool-in-hand art. `farmerCharacter.png` turned out to be loose head/hair fragments, not a full body — `Output Textures/customised_farmer.png` (pre-composited) was the right asset. When in doubt, use `Read` on the actual PNG (or export a zoomed crop via a temp Editor script using `TextureImporter.isReadable` toggled true, never a `RenderTexture` blit — that produced blank output) before wiring anything.
4. **Large tilesets (`GroundTileset.png`, `DugGround.png`, `Farmhouse1.png`) are autotile/kit pieces, not single sprites.** Picking a sub-tile by guesswork risks visible transparent gaps. Where one had to be used standalone, it was picked by *measuring* opaque-pixel coverage per sub-sprite (100% opaque = safe), not by eyeballing.

### Test suite
55 EditMode tests, all pure logic (no scene): `Assets/Tests/EditMode/`. Run via `make test-edit`; read results with `bash tools/report.sh` (parses `Artifacts/editmode.xml`, exits non-zero on any failure — it also picks up `Artifacts/playmode.xml` if present). 18 of the 55 are new for the wood feature (S2-DEV-01..07, `DESIGN_BRIEFS.md` [DSN-030]): `TileObjectTests.cs` (object layer construct/occupancy/respawn, no scene), Chop/`NotChoppable`/`Blocked` cases in `ToolControllerTests.cs`, wood sell-channel cases in `GameStateTests.cs`, the wood columns + wood-profit-per-day rule in `HeadlessSimTests.cs`/`BootstrapBalanceTests.cs`. A further 13 are QA hardening on that same feature (S2-QA-01/02/05/06, `DESIGN_BRIEFS.md` [DSN-030]): `Blocked` re-tested against a felled stump (not just a live tree, a genuinely different code path), stamina-not-spent assertions on `Blocked`/`NotChoppable`, a `TileType` invariant across the whole chop→stump→respawn cycle, a sim-level (not just formula-level) wood-income-per-day check, `DebugPanel`'s `+Tree`/`ForceRespawnTrees` hooks (`GameStateTests.cs`), and `SessionLoggerTests.cs` (row formatting + a real file-write regression test, see Known pitfalls below).

3 PlayMode tests: `BootSmokeTests.cs` (Sprint 1) plus `PlayerCollisionTests.cs` (S2-QA-03, new) — tree/player collision and the red cursor reticle are exactly the class of bug EditMode can't see (a MonoBehaviour-wiring question, same as Known Pitfalls #1/#2), so these boot the real scene and exercise `PlayerController`'s actual `MoveWithCollision`/`UpdateCursor` (via reflection — this project's `PlayerController` reads legacy `UnityEngine.Input`, which there's no test-harness way to simulate key-by-key, so the tests call the real private methods directly on a real `Transform` in a real booted scene instead of faking WASD input).

1 PlayMode test (Sprint 1 B3): `Assets/Tests/PlayMode/BootSmokeTests.cs` boots the scene like a real build (no scene file load needed — `GameManager` self-bootstraps via `[RuntimeInitializeOnLoadMethod]`) and asserts the tile grid actually rendered at the right scale — the two Known Pitfalls above were previously invisible to any automated test. Run via `make test-play`.

---

## For QA

### Gate
```bash
make gate        # compile -> test-edit -> test-play -> build-win (Win64 — see Known Issues)
```
WebGL build fails on this machine ("build target was unsupported" — the WebGL module isn't installed in the local Unity Editor); `DECISION_LOG.md` 2026-08-15 made Win64 the default gate target instead of leaving it red. `make build-webgl BUILD=<n>` still exists to retry once the module is installed. `make build`/`build-win BUILD=<n>` gives the real, runnable artifact; every playtest build referenced in this project has been Win64.

### Debug panel (`F1` in a running build)
Skip Hour / Skip Day (sleep) / Money +1000 / Refill Stamina / Force All Ripe / +5 Turnip seed / +5 Potato seed / Clear inventory (re-grants starting tools so you're never left with nothing) / **Sell all Wood** (S2-DEV-05 — the "existing sell channel" `GameState.SellAllWood` reachable in a real build; not a shop, same debug-hook pattern as the other buttons here) / **+ Tree (near player)** (S2-QA-05 — drops a fresh tree one tile north of the player via `GameState.DebugSpawnTree`, so QA can hit chop/collision scenarios without trekking to the corner `GameState.SeedTrees` seeds at start; no-ops without a crash if that tile is off-grid or already occupied/non-Grass) / **Force Respawn Trees** (S2-QA-05 — `GameState.ForceRespawnTrees` instantly regrows every felled stump on the map, skipping the real `TreeRespawnDays` wait; leaves standing trees untouched) / Toggle grid-coord readout / live FPS + inventory counts (now including Wood).

### Known issues / gaps
Sprint 1 "Debt Zero" (2026-08-15) cleared every item that used to be listed here (sim stub, no PlayMode tests, identical Turnip/Potato art) — see `KANBAN.md` Sprint 2 board and `DECISION_LOG.md` for what changed and why. What's left is deliberately deferred, not forgotten (`LATER.md` has the full reasoning):
- **WebGL module still not installed** on this dev machine — gate targets Win64 instead (see Gate above). Installing the module is in `LATER.md`, not blocking since Win64 is what every playtest has used anyway.
- **`ToolbarUI`/`InventoryScreenUI.PointerOverUI` is one frame stale** relative to `PlayerController.Update()` (set in `OnGUI()`, which runs after `Update()` in Unity's frame order) — found in the Sprint 1 B0 bug bash. Only reachable at the exact UI-boundary pixel on a fast-moving click; not blocking, not yet observed to cause a real misclick. `LATER.md` 2026-08-15.
- **No shop / way to spend money in the real game** — `Wallet.Money` only goes up (sell crops, debug cheats); `CropDefinition.SeedPrice` is charged only inside `HeadlessSim`, never by the actual player-facing tools. This was out of scope for DEV-002 from the start, not a new gap — flagged again in `LATER.md` because the upcoming wood/NPC scope will likely need it.
- Turnip and Potato still share one seed sprite and one crop-stage sheet (no distinct source art) — B2 (Sprint 1) tells them apart with a colour tint (`PlaceholderArt.ItemTint`/`CropTint`) instead of new art, which is enough to read at a glance but isn't final art.
- No Axe icon on the hotbar — `PlaceholderArt.ItemIcon` returns null for it (renders as a blank slot, still selectable/usable). S2-DEV-06's art scope only covered the tree/stump sprites; picking an Axe icon out of `tools.png`'s 26 unlabeled sub-sprites without a visual export first would be exactly the guess-by-index mistake Known Pitfall #3/#4 exist to prevent. Flagged, not fixed, in this pass.

**S2-QA-04 tree bug bash (2026-08-15, ~90 min, code-review-style — same format as the Sprint 1 B0 bug bash, no two-person live session in this pass):** focus was corner/edge softlocks with a tree present, chopping at 0 stamina, tree respawn landing back on the exact tile, and the cursor reticle over a blocked tile.
- **No stuck/softlock found.** `GameState.SeedTrees` places its corner strip *on* the grid's own outer edge (row `Height-1`, columns `Width-1` down) — there is no space beyond the trees to be trapped in, so the tree strip can only ever block passage through that edge, never enclose the player. Flagged as a latent risk for later map-size tuning: if `InitialTreeCount` were ever close to `Width`, or a future map shrank `Width`, the same placement logic *could* wall off a corner — worth re-checking then, not an issue with today's 20×20/8-tree defaults.
- **Chop at 0 stamina: no crash, correct feedback.** Returns `NoStamina`/`Miss` (red flash), same as Till/Water already did — not a new gap, `ToolControllerTests.HetStamina_ChatCay_TraVe_NoStamina` covers it.
- **Respawn lands on the same tile, `TileType` never drifts.** Confirmed both by test (`TileObjectTests`, `ToolControllerTests.TileType_KhongDoi_...`) and live in `make sim`'s `economy.csv` (trees felled and re-harvested on schedule at days 1/5/9). One real gap found and closed here: `Blocked` had only ever been tested against a *standing* tree — a felled stump (`TileObject` stays non-null, only `HP`/`RespawnDaysLeft` change) is a genuinely different code path and wasn't covered; added `ToolControllerTests.Cuoc_LenOCoGocCayDaHa_VanTraVe_Blocked_...`.
- **Reticle correctly reddens on a blocked tile and un-reddens off it** — verified live in `PlayerCollisionTests.Reticle_...` (PlayMode), including the reverse check (aiming at open ground goes back to green) so a "stuck red" regression would actually fail the test, not just a "goes red once" check.
- **Bonus finding (not part of the 4 focus areas, found during S2-QA-06 manual file verification):** `SessionLogger` named its output file from the global `BootArgs.Seed` instead of the `GameState` actually being logged — invisible in real play (`GameManager` always constructs `GameState` with `BootArgs.Seed`, so they never diverge there) but a real bug for any other caller, reproduced by hand, fixed (`GameState.Seed`), and regression-tested (`SessionLoggerTests.LogAction_DungSeedCuaGameState_...`).

### Regression coverage map
Every `ToolResultCode` branch (`WrongTool`, `NoStamina`, `InvalidTile`, `NoSeed`, `NotChoppable`, `Blocked`) has at least one test in `ToolControllerTests.cs` — `NotChoppable`/`Blocked` added S2-DEV-03 (`DESIGN_BRIEFS.md` [DSN-030]). The full till→plant→water→harvest→sell happy path is `GameStateCoreLoopTests.HappyPath_...` in `GameStateTests.cs`. `HeadlessSimTests.cs` covers the greedy-farmer sim (row-per-day, same-seed determinism, Potato > Turnip profit, and since S2-DEV-07 that the sim actually chops trees and sells wood — `wood_harvested`/`wood_income` columns are non-zero over a run, not a stub). `TileObjectTests.cs` covers the static object layer standalone (S2-DEV-01: construct with no scene, `GridMap.IsOccupied`, felled-tree respawn countdown via `GameState.OnDayEnded`). `BootstrapBalanceTests.BalanceConfig_WoodProfitPerDay_BelowPotatoThresholdRule` is the test named in `DESIGN_BRIEFS.md` [DSN-030] S2-DES-02 — any Balance Change Ticket touching the 6 tree/wood numbers or Potato's must pass it before merge. `GameClockTests.WrapHour_...` is the regression test for BUG-1 (Sprint 1 B0 bug bash — HUD showed "24:00"/"25:00" past midnight instead of wrapping). `BootSmokeTests.cs` (PlayMode) is the regression test for Known Pitfalls #1/#2. `PlayerCollisionTests.cs` (PlayMode, S2-QA-03) is the regression test for tree/player collision and the red cursor reticle — both are MonoBehaviour-wiring questions EditMode can't see, same class of risk as Pitfalls #1/#2. `SessionLoggerTests.LogAction_DungSeedCuaGameState_...` is the regression test for the S2-QA-04/06 bug-bash finding above (session file named from the wrong seed source).

---

## For Design

### Current balance numbers (`Assets/_Prototype/Scripts/Core/BalanceConfig.cs` — `[DESIGN-OWNED]`, single source of truth, no hardcoded numbers elsewhere)
| | |
|---|---|
| MoveSpeed | 4.0 tiles/s |
| MaxStamina | 100 |
| StartMoney | 500 |
| Day length | 40s real = 1 in-game hour, 06:00→02:00 (20h/day, ~13 min real/day) |
| Till / Water / Harvest stamina cost | 2 / 1 / 0 |
| Turnip | 4-day growth, 20 seed / 60 sell (10/day profit) |
| Potato | 6-day growth, 50 seed / 160 sell (18.3/day profit — intentionally more profitable/day, see `BootstrapBalanceTests.BalanceConfig_ProfitPerDay_InternalBalanceRule`) |
| Tree/Wood: TreeMaxHP | 3 hits to fell a tree |
| Tree/Wood: ChopStaminaCost | 4/hit (deliberately pricier than Till/Water so chopping stays a side activity, not the stamina-efficient default) |
| Tree/Wood: WoodPerTree / WoodSellPrice | 5 wood/tree, 8/wood |
| Tree/Wood: TreeRespawnDays | 4 days (matches TurnipGrowthDays on purpose — a tree behaves on the same rhythm as an existing crop) |
| Tree/Wood: InitialTreeCount | 8 trees on the map at start (finite by design) |
| **Rule:** wood profit/day per tree | `(WoodPerTree × WoodSellPrice) ÷ TreeRespawnDays` = 10.0/day, must be **≤ 70%** of Potato's 18.3/day (= 12.83) — wood has zero seed cost, so the 30% margin keeps Potato the dominant income source even accounting for wood's free capital. Full derivation: `DESIGN_BRIEFS.md` [DSN-030]. |

### Open Question Board (`KANBAN.md`)
| Q | Question | Status |
|---|---|---|
| Q1 | Is the till–plant–water–harvest loop fun in the first 15 minutes? | 🟡 testable now — core loop + real art + UI are in place |
| Q2 | Is the in-game day length right? | ⚪ not tested |
| Q3 | Do players plan ahead for tomorrow on their own? | ⚪ not tested |
| Q4 | Does stamina create pressure or just frustration? | ⚪ not tested |
| Q5 | Does top-down tile-targeting feel precise? | ⚪ not tested — the green/red cursor reticle (`PlayerController` + `GreenGridCursor`/`RedGridCursor`) exists specifically to help answer this |
| Q6 | Does chopping trees enrich the loop, or is it just a more-profitable button? | ⚪ not tested — see `DESIGN_BRIEFS.md` [DSN-030] |

Q1 is the one this project's playtest build is actually ready to test now — build #15, Win64, seed-reproducible (superseded build #13: same core loop, plus Sprint 1 "Debt Zero" fixes — see KANBAN.md Build Log). See `Agent/AGENT_DESIGN.md` §6 for the playtest report format and §5 for the full design framework (tension sources, feel matrix).

### Feel/feedback already implemented
Every tool-use returns a `FeedbackKind` (Till/Water/Plant/Harvest/Chop/Miss) that flashes a colour over the target tile (`GameManager.SpawnFeedback`) — a wrong action always gives visible feedback, never silent failure. Crop growth is conveyed by sprite stage (5 steps, `ParsnipCrop_0..4`), not just colour. `FeedbackKind.Chop` (S2-DEV-02/04, `DESIGN_BRIEFS.md` [DSN-030] S2-DES-04) flashes a bark-brown colour on every tree hit, never shares `Miss`; a tile with no live tree (bare ground, or a stump waiting to respawn) uses `Miss` instead, per that spec's table. The felling hit is distinguishable by sight independent of the flash: `WorldView` swaps the tile's sprite from the standing-tree art to the stump art (`ArtCatalog.TreeStanding`/`TreeStump`, S2-DEV-06) the moment `TileObject.HP` reaches 0, so a chopped tile still reads as "already chopped" a while later, not just in the instant of the flash. (The brief's finer shake-animation detail wasn't built — this prototype's established pattern for tool feedback is a colour flash, not real animation, same as Till/Water/Harvest.)

---

## Links
- Tracking: [`KANBAN.md`](KANBAN.md) · [`DECISION_LOG.md`](DECISION_LOG.md) · [`LATER.md`](LATER.md) · [`DESIGN_BRIEFS.md`](DESIGN_BRIEFS.md) · [`BUG_REPORTS.md`](BUG_REPORTS.md)
- Process docs: [`Agent/AGENT_PM.md`](Agent/AGENT_PM.md) · [`Agent/AGENT_DEV.md`](Agent/AGENT_DEV.md) · [`Agent/AGENT_QA.md`](Agent/AGENT_QA.md) · [`Agent/AGENT_DESIGN.md`](Agent/AGENT_DESIGN.md)
