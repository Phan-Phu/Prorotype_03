# Farming Prototype

Unity top-down farming prototype. Core loop:

`till → plant → water → harvest → sell`

## Tech stack

- Unity `6000.4.0f1`, UGUI
- Microsoft.Extensions.DependencyInjection (Infrastructure services only)
- UniTask, iTween (popup animation)
- Main scene: `Assets/_Prototype/Scenes/Prototype_Main.unity`

## Run it

**Editor:** open the project → open `Prototype_Main.unity` → Press Play.

Controls: WASD move, `1`-`9`/`0` select toolbar slot, Space/Enter/click to use
item, `I` inventory, `E` interact.

**CLI (PowerShell):**

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.0f1\Editor\Unity.exe" `
  -projectPath "C:\Work\Farming_prototype"

# headless compile check
& "C:\Program Files\Unity\Hub\Editor\6000.4.0f1\Editor\Unity.exe" `
  -projectPath "C:\Work\Farming_prototype" -batchmode -quit -acceptSoftwareTermsForThisRunOnly
```

## Project structure

```
Assets/_Prototype/
  Scenes/Prototype_Main.unity   the only scene
  Scripts/
    Domain/          plain C#, no Unity/UI. Entities, rules, ports.
    Infrastructure/   implements Domain ports (inventory, clock, gameplay,
                      dialogue, master data import, art, logging)
    Application/      MonoBehaviours: input, UI, presentation
    Composition/      GameManager — the only composition root
  Editor/            AuthorUiHierarchy — one-shot scene-authoring tool
  MasterData/CSV/    game content (prices, growth days, dialogue, ...)
  Resources/          MasterData.asset (imported from CSV)
Assets/Tests/
  EditMode/          domain-rule tests, no scene
  PlayMode/          boots the real scene, tests component wiring
```

Dependency direction: `Application → Infrastructure → Domain`. Domain never
references Infrastructure or Unity UI. `GameManager` is the only place that
builds the DI container and wires every MonoBehaviour's fields.

## Main features / where to find them

| Feature | Key classes |
|---|---|
| Move, tools, active-slot actions | `PlayerController`, `GameplayService` |
| Till / plant / water / harvest / chop | `GameplayService.UseTool`, `PlantSpecific` |
| Inventory (48-slot grid, drag/drop, tooltip) | `InventoryScreenUI`, `InventorySlotView`, `ItemDetailPopup`, `InventoryService` |
| Toolbar (hotbar, 10 slots) | `ToolbarCanvasUI` |
| Seed shop (buy/sell) | `SeedShopUI`, `GameplayService.BuySeed/SellItem` |
| NPC dialogue | `NpcDialogueController`, `DialogueService` |
| Day/time clock | `ClockService`, `HUD` |
| Popup open/close/animation | `PopupBase`, `PopupParent` (`Application/UI/Popup/`) |
| World rendering (tiles, crops, trees) | `WorldView`, `PlaceholderArt`/`ArtCatalog` |
| Debug overlay (F1) | `DebugPanel` |
| Master Data import (CSV → runtime) | `MasterDataCsvImporter` (Editor), `MasterDataImporter` (runtime) |

The seed shop's item list is generated from Master Data (one button cloned
per crop) — adding a crop row to the CSV adds it to the shop, no code change.

## Master Data workflow

1. Edit CSV under `Assets/_Prototype/MasterData/CSV/` (player, time, tools,
   crops, tree, items, starting_inventory, npcs).
2. In the Editor: `Prototype/Master Data/Import CSV to Scriptable Asset`
   (first time: `Create or Reset Prototype Asset`).
3. `GameManager` loads and validates `MasterData.asset` on boot; an invalid
   asset stops startup with a typed failure instead of partial content.

## Dev workflow

- Branches: `main` and `dev` (kept in sync).
- Compile with Unity CLI before pushing.
- Generated output, design docs, and agent/tooling material stay out of the
  repo (`.gitignore`).
