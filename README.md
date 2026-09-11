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
  no Unity UI, DTOs, async code or feature/service behavior. Setters are kept
  for MasterData import/runtime composition only.
- `Prototype.Application`: the merged UI/controller layer (Presentation was
  intentionally removed). It receives input, binds scene-authored UI and
  consumes Infrastructure services/DTOs.
- Infrastructure implementations live under `Scripts/Infrastructure/` and
  own inventory, clock, farming/economy, dialogue and world/debug behavior.
  The namespace is intentionally limited to `Prototype.Application` or
  `Prototype.Domain`.
- Infrastructure exposes UniTask async entry points (`ReadAsync`, `TickAsync`,
  `UseToolAsync`, `BuySeedAsync`, `SellItemAsync`, dialogue and debug async
  methods) so storage/MasterData can become asynchronous later.
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
