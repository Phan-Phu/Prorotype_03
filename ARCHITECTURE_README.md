# Farming Prototype — Clean Architecture / DDD baseline

This project follows the supplied `clean_architecture_DDD.md` as a target, adapted for the existing Unity prototype.

## Current preflight findings

- DI implementation exists as `Assets/Plugins/Microsoft.Extensions.DependencyInjection.dll`.
- UniTask is now declared in `Packages/manifest.json` from the user-provided Cysharp repository.
- `GameManager` is now under `Composition/` and remains the composition root; feature setup will be reduced further in later vertical slices.
- Existing `GameState` is useful domain/application state, but feature rules and Unity view wiring must be separated incrementally.
- Existing tests are the safety gate before each migration phase.

## Target folders

```text
Assets/_Prototype/Scripts/
  Domain/          pure game rules, entities, value objects, events, ports
  Application/     commands, queries, handlers, DTOs, use cases
  Infrastructure/  repositories, persistence, master data, Unity/plugin adapters
  Presentation/    MonoBehaviour views/controllers and UI presenters
  Composition/     DI registration and startup scope
  Shared/          cross-cutting debug/bootstrap contracts
```

Feature folders are no longer top-level architecture folders. A feature may have subfolders inside a layer (for example `Domain/Economy` or `Presentation/UI`), but its code is classified by layer responsibility. Every move must preserve namespaces, tests and scene references.

## Rules

1. Create/update system or feature ERD before implementation.
2. Run package resolve, compile and existing tests before changing a layer.
3. Migrate one vertical slice at a time: port → application handler → infrastructure adapter → presentation binding → tests.
4. Keep scene-authored UI/map/layout in Editor; runtime code binds state and intent.
5. Commit each Dev task with only Unity source/config, then send to review for QA.

## Migration order

1. Dependency/package preflight and architecture smoke tests.
2. Composition root and DI registration.
3. Event contracts/adapter and UniTask boundaries.
4. Inventory/hotbar vertical slice.
5. Seed shop and NPC dialogue vertical slices.
6. Map/world presentation and remaining legacy setup methods.
7. Remove obsolete static/service-locator paths only after all consumers migrate.
