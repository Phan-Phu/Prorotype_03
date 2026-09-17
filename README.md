# Farming Prototype

Unity top-down farming prototype focused on the core loop:

`till → plant → water → harvest → sell`

## Project

- Unity: `6000.4.0f1`
- Main scene: `Assets/_Prototype/Scenes/Prototype_Main.unity`
- Runtime source: `Assets/_Prototype/Scripts/`
- Packages: `Packages/manifest.json` (UniTask, Microsoft DI, iTween for popup
  animation, test-helper.ui)
- Project settings: `ProjectSettings/`

The `main` branch is the developer source branch (`dev` currently points at
the same commit). Design notes, QA reports, Hermes/Kanban data, agent prompts,
templates and generated artifacts are kept outside this repository.

## Architecture

- `Prototype.Domain`: raw entities, value objects, policies and ports. It has
  no Unity UI or application read models. Inventory ports return `UniTask`
  directly (`Add`, `Remove`, `Count`, etc.); there are no duplicate `*Async`
  methods. Setters are kept for MasterData import/runtime composition only.
  Domain is organized by feature folder rather than one flat `Entities/`
  bucket: `Inventory/`, `Contracts/` (`Result`, `FailureCode`, `GameplayFailure`,
  `ShopResults`, `ToolResult`), `Dialogue/Services` (`DialogueFailure`),
  `MasterData/` (`MasterDataFailure`, `MasterDataSnapshot`), `State/Services`
  (`ClockFailure`, `RepositoryFailure`, `StateFailure`) and `World/Services`
  (`WorldFailure`) each carry their own failure/result types next to
  `Entities/`, `Policies/`, `Ports/` and `ValueObjects/`.
- `Prototype.Application`: the merged UI/controller layer (Presentation was
  intentionally removed). It receives input, binds scene-authored UI and
  consumes Infrastructure services/DTOs.
- Infrastructure implementations live under `Scripts/Infrastructure/` and
  own inventory, clock, farming/economy, dialogue and world/debug behavior.
  Their namespace is `Prototype.Infrastructure`; Application only consumes
  their contracts and typed results.
- Runtime balance and content are authored in
  `Assets/_Prototype/MasterData/CSV/` and imported into
  `Assets/_Prototype/Resources/MasterData.asset`. `MasterDataImporter`
  (a static helper in `Infrastructure/MasterData/MasterDataAsset.cs`) validates
  that asset and converts it to the immutable Domain `MasterDataSnapshot` before
  `GameState` is created. Player/time/tool costs, crops, trees, starting items,
  NPC dialogue, item descriptions and item art references therefore have one
  reviewable source instead of parallel hard-coded values.
- Inventory is grouped under `Domain/Inventory/`: raw entity, Domain service
  ports and inventory value objects. Its behavior implementation is under
  `Infrastructure/Inventory/`; the UI query interface and slot read data are
  under `Application/Inventory`.
- `Application/Components/Mapping/GenericMapper` is the reusable raw-to-
  application projection component. Inventory uses it to expose
  `InventorySlotData[]` without exposing Domain `ItemStack` or introducing a
  feature-specific snapshot wrapper. The inventory UI consumes the concrete
  Infrastructure service directly; there is no redundant `IInventoryQuery`
  interface.
- Infrastructure operations return typed `UniTask<Result<TFailure, TValue>>`
  values through `ResultFactory`. Expected gameplay failures use feature
  failure types such as `InventoryFailure` and `GameplayFailure`, with
  `FailureCode` values
  such as `NotEnoughMoney`, `InventoryFull`, `InsufficientInventory` and
  `InvalidArgument`; unexpected exceptions are logged and returned as
  `SystemError`.
- Infrastructure exposes UniTask operations directly; success and failure are
  returned through `Result<TFailure, TValue>` instead of duplicate sync/`*Async`
  methods for Inventory, Gameplay, NPC dialogue, clock, world and persistence.
- UI is UGUI-based. Scene-authored objects provide the layout; application
  components bind state and user intent at runtime.
- The main scene owns the authored canvas, toolbar and popup hierarchy. Popups
  (inventory, seed shop, item detail tooltip) are scene-authored GameObjects
  bound through serialized fields — not built at runtime. `Application/UI/Popup/`
  holds the shared `PopupBase`/`PopupParent` open-close/animation/stacking
  framework; `InventoryScreenUI`, `SeedShopUI` and `ItemDetailPopup` derive from
  `PopupBase` and share one `PopupParent` stack so only one popup animates to
  front at a time. Show/hide animation uses `iTween` when present in the
  project and falls back to a plain coroutine scale-tween otherwise.
- The inventory popup renders a 48-slot grid (12x4) authored by
  `AuthorUiHierarchy.BuildInventoryGrid()`; each cell is an `InventorySlotView`
  (hover tooltip + drag-and-drop swap), driven every frame by
  `InventoryScreenUI`. The seed shop's item list is Master-Data-driven: one
  authored button template is cloned once per `State.MasterData.Crops` entry,
  so adding a crop row to Master Data is enough to add it to the shop — no
  per-crop button or per-crop code.
- UI/Application components are wired by `GameManager` through explicit
  `[SerializeField]`/`internal` fields assigned at composition time, not
  through static `Instance` singletons — e.g. `PlayerController`,
  `InventoryScreenUI` and `DebugPanel` each hold an `internal SeedShopUI
  ShopUI` reference instead of a static `SeedShopUI.Instance` lookup.
  `GameManager` itself is the one script that keeps a static `Instance`,
  reserved for the composition root.

The runtime dependency direction is:

```text
Application UI/controllers  ->  Infrastructure services + DTOs  ->  Domain raw state/ports
```

`GameManager` is the composition root and the only place that builds the
Microsoft DI container. Domain does not reference Infrastructure or Unity UI.

## Run in Unity

1. Open the project with Unity `6000.4.0f1`.
2. Open `Assets/_Prototype/Scenes/Prototype_Main.unity`.
3. Press Play.

Controls: WASD to move, `1`–`9`/`0` to select toolbar slots, Space/Enter or
left click to use the selected item, `I` for inventory, and `E` to interact.

## Run from Windows PowerShell

The project can also be opened with Unity CLI:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.0f1\Editor\Unity.exe" `
  -projectPath "C:\Work\Farming_prototype"
```

For a headless compile check:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.0f1\Editor\Unity.exe" `
  -projectPath "C:\Work\Farming_prototype" `
  -batchmode -quit -acceptSoftwareTermsForThisRunOnly
```

## Developer workflow

- Work on branch `main`.
- Keep Unity assets, scene files, packages and developer source tracked.
- Keep generated output and test/agent/design material outside the repository.
- Compile with Unity CLI before pushing.
- Each code change should be committed with a focused message.

## Master Data workflow

1. Edit the CSV files under `Assets/_Prototype/MasterData/CSV/` (`player.csv`,
   `time.csv`, `tools.csv`, `crops.csv`, `tree.csv`, `items.csv`,
   `starting_inventory.csv`, `npcs.csv`).
2. Select `Prototype/Master Data/Import CSV to Scriptable Asset` in the Unity
   editor (`Prototype/Master Data/Create or Reset Prototype Asset` creates the
   asset from CSV the first time). The Infrastructure converter
   (`MasterDataCsvImporter`) parses the CSV, validates it, resolves
   `IconPath` + `IconSpriteName` through the editor asset database, and writes
   the result to the ScriptableObject.
3. Use `Assets/_Prototype/Resources/MasterData.asset` to review or fine-tune
   the generated Sprite and UI content references.
4. On startup, `GameManager` calls `MasterDataImporter.Load()` and stops with a
   typed failure if the asset is missing or invalid.
5. Infrastructure passes the imported snapshot into `GameState`; gameplay and
   UI services read prices, growth days, stamina costs, NPC data and clock rules
   from that snapshot.

The asset is the content source; `BalanceConfig` and static definitions remain
fallback defaults for headless/unit construction only. They are not consulted
by a normal scene boot after a valid Master Data asset is present.
