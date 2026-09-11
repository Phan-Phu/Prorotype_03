# Farming Prototype

Unity top-down farming prototype focused on the core loop:

`till → plant → water → harvest → sell`

## Project

- Unity: `6000.4.0f1`
- Main scene: `Assets/_Prototype/Scenes/Prototype_Main.unity`
- Runtime source: `Assets/_Prototype/Scripts/`
- Packages: `Packages/manifest.json`
- Project settings: `ProjectSettings/`

The `dev` branch is the developer source branch. Design notes, QA reports,
Hermes/Kanban data, agent prompts, templates and generated artifacts are kept
outside this repository.

## Architecture

- `Prototype.Domain`: raw entities, value objects, policies and ports. It has
  no Unity UI or application read models. Inventory ports return `UniTask`
  directly (`Add`, `Remove`, `Count`, etc.); there are no duplicate `*Async`
  methods. Setters are kept for MasterData import/runtime composition only.
- `Prototype.Application`: the merged UI/controller layer (Presentation was
  intentionally removed). It receives input, binds scene-authored UI and
  consumes Infrastructure services/DTOs.
- Infrastructure implementations live under `Scripts/Infrastructure/` and
  own inventory, clock, farming/economy, dialogue and world/debug behavior.
  Their namespace is `Prototype.Infrastructure`; Application only consumes
  their contracts and typed results.
- Runtime balance and content are authored in
  `Assets/_Prototype/Resources/MasterData.asset`. `MasterDataImporter` validates
  that asset and converts it to the immutable Domain `MasterDataSnapshot` before
  `GameState` is created. Player/time/tool costs, crops, trees, starting items
  and NPC dialogue therefore have one editable source instead of parallel
  hard-coded values.
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
- The main scene owns the authored canvas and toolbar hierarchy.

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

- Work on branch `dev`.
- Keep Unity assets, scene files, packages and developer source tracked.
- Keep generated output and test/agent/design material outside the repository.
- Compile with Unity CLI before pushing.
- Each code change should be committed with a focused message.

## Master Data workflow

1. Select `Prototype/Master Data/Create or Reset Prototype Asset` in the Unity
   editor when the prototype defaults need to be restored.
2. Edit `Assets/_Prototype/Resources/MasterData.asset` in the Inspector.
3. On startup, `GameManager` calls `MasterDataImporter.Load()` and stops with a
   typed failure if the asset is missing or invalid.
4. Infrastructure passes the imported snapshot into `GameState`; gameplay and
   UI services read prices, growth days, stamina costs, NPC data and clock rules
   from that snapshot.

The asset is the content source; `BalanceConfig` and static definitions remain
fallback defaults for headless/unit construction only. They are not consulted
by a normal scene boot after a valid Master Data asset is present.
