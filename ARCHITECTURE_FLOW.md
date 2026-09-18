# Architecture Flow — Farming Prototype

Short reference for how the project runs, how it's structured, and where the
functions live. Matches the current code, not a target design.

## 1. Gameplay loop

```
Keyboard/mouse
  → PlayerController reads the active toolbar slot
  → resolves it to an action:
      Hoe/WateringCan/Harvest/Axe  → GameplayService.UseTool()
      Turnip/Potato seed           → GameplayService.PlantSpecific()
      standing on the shop tile    → Player.ShopUI.OpenShop()
  → Infrastructure validates (tile, stamina, inventory) and mutates state
  → returns a typed Result<Failure, Dto>
  → PlayerController / UI redraw from the Dto
```

Other input loops that work the same way (UI → Infrastructure service →
typed `Result` → UI refresh):

- **Seed shop**: buy/sell a seed or crop → `GameplayService.BuySeed/SellItem`
- **Inventory**: drag/drop a slot → `InventoryService.Swap`
- **NPC dialogue**: `E` near an NPC → `DialogueService.Open/AdvanceOrClose`
- **Clock**: ticks every frame → `ClockService.Tick`, rolls over at day end

## 2. Code structure (3 layers)

```
Application  →  Infrastructure  →  Domain
(MonoBehaviours,   (implements Domain    (plain C#: entities,
 input, UGUI)       ports, returns        rules, ports — no
                     Result<Failure,T>)    Unity/UI reference)
```

- **`Domain/`** — no Unity, no UI. `Entities/` (`GameState`, `GridMap`,
  `Inventory`, `Wallet`, `GameClock`, ...), `Policies/` (`BalanceConfig`,
  `CropDefinition`), `Ports/` (interfaces Infrastructure implements),
  `Contracts/` (`Result`, `FailureCode`).
- **`Infrastructure/`** — implements the Domain ports: `InventoryService`,
  `ClockService`, `GameplayService`, `DialogueService`,
  `MasterDataCsvImporter`/`MasterDataAsset`, `ArtCatalog`, `SessionLogger`.
  Every operation returns `UniTask<Result<TFailure, TValue>>` — no
  exceptions for expected failures (`NotEnoughMoney`, `InventoryFull`, ...).
- **`Application/`** — MonoBehaviours only. Reads input, calls Infrastructure,
  binds the result to scene-authored UI. Never touches Domain state
  directly. Folders are just navigation (`UI`, `Player`, `NPC`, `World`,
  `Inventory`, `Debug`) — not separate layers.
- **`Composition/GameManager`** — the only composition root. Builds the
  Microsoft DI container, loads Master Data, creates/finds every
  MonoBehaviour and assigns its fields.

UI is scene-authored (built once by the `AuthorUiHierarchy` editor tool, not
at runtime) and bound through `[SerializeField]`/`internal` fields that
`GameManager` assigns — no runtime `Instantiate`-a-panel, no static
`Instance` singletons except `GameManager` itself.

## 3. Boot sequence

`GameManager.Awake()` runs once, in this order:

1. Build the Domain `GameState` (grid, clock, inventory, wallet).
2. Register Infrastructure services in the DI container.
3. Import & validate `MasterData.asset` → Domain `MasterDataSnapshot`.
4. Find-or-create and wire every MonoBehaviour: camera, world, scenery,
   player, HUD, toolbar, inventory screen, seed shop, NPC dialogue, debug
   panel — each gets its `State`/service/`Player` references assigned here.

If Master Data is missing or invalid, boot stops with a typed failure
instead of starting a half-configured session.

## 4. Feature reference (class → what it does → key methods)

| Class | Layer | Does | Key methods |
|---|---|---|---|
| `PlayerController` | Application | movement, active-slot tool use | `UseTool()` |
| `GameplayService` | Infrastructure | farming actions, shop transactions | `UseTool`, `PlantSpecific`, `BuySeed`, `SellItem` |
| `InventoryService` | Infrastructure | inventory mutation + read | `Add`, `Remove`, `Count`, `Swap`, `Clear` |
| `InventoryScreenUI` | Application | inventory popup (48-slot grid) | `OpenInventory`, `CloseInventory`, `BeginDrag/UpdateDrag` |
| `InventorySlotView` | Application | one grid cell: hover + drag/drop | forwards pointer events to `InventoryScreenUI` |
| `ToolbarCanvasUI` | Application | 10-slot hotbar | reads `InventorySlotData[]` each frame |
| `SeedShopUI` | Application | shop popup; item list generated from Master Data | `OpenShop`, `CloseShop`, `BuildItemList`, `Buy`, `SellItem` |
| `ItemDetailPopup` | Application | hover tooltip for any item | `Show(itemId, count, pos, source)`, `Hide(source)` |
| `NpcDialogueController` | Application | NPC proximity + dialogue popup | opens/advances via `DialogueService` |
| `DialogueService` | Infrastructure | dialogue state machine | `Open`, `AdvanceOrClose`, `Close` |
| `ClockService` | Infrastructure | day/time | `Tick`, `ForceEndDay`, `SkipHour` |
| `HUD` | Application | clock + wallet display | reads `GameStateSnapshotDto` |
| `WorldView` | Application | renders tiles/crops/trees | rebuilds sprites from `GameState.Grid` |
| `DebugPanel` | Application | `F1` debug overlay | spawn tree, force ripe, add money, ... |
| `PopupBase` / `PopupParent` | Application | shared open/close + animation for every popup | `ShowPopup/HidePopup` |
| `MasterDataCsvImporter` | Infrastructure (Editor) | CSV → `MasterData.asset` | run from `Prototype/Master Data/...` menu |
| `MasterDataImporter` | Infrastructure | validates asset → `MasterDataSnapshot` | called once by `GameManager` on boot |

Dependency fields on Application components (`State`, service interfaces,
`Player`, `ShopUI`, ...) are `internal`/`[SerializeField]`, assigned only by
`GameManager` — not `public`, and not resolved through a static singleton.

## 5. Testing

- `Assets/Tests/EditMode/` — pure Domain/Infrastructure rules, no scene
  (economy, crop growth, tools, clock, NPC state, shop math).
- `Assets/Tests/PlayMode/` — boots `Prototype_Main.unity` for real and
  checks component wiring, collision, and UI smoke paths.
- Run via Unity Test Runner or Unity CLI (`-runTests`).
