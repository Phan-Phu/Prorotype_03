# Architecture flow — Farming Prototype

## Layer flow

```text
User input / scene UI
        ↓ intent
Presentation (MonoBehaviour view/controller)
        ↓ command/query
Application (use case, handler, DTO, port)
        ↓ business call
Domain (entity, value object, rule, domain event)
        ↓ port
Infrastructure (repository, persistence, master data, Unity/plugin adapter)
        ↓ result/event
Application → Presentation update
```

## Composition root

`GameManager` is being reduced to the composition root: create the DI scope, register interfaces, connect existing scene-authored components and start the application. Feature logic must move out of `GameManager` into use cases/services.

## Feature flow example — buy seed

| Step | Layer | Responsibility |
|---|---|---|
| 1 | Presentation | SeedShop UI sends `BuySeedCommand(crop)` |
| 2 | Application | Handler validates request and calls domain port |
| 3 | Domain | Wallet/inventory rules decide success or failure |
| 4 | Infrastructure | Repository/persistence stores the changed state |
| 5 | Application | Returns result and publishes domain event |
| 6 | Presentation | Shop, wallet and hotbar update from result/event |

## Feature flow example — navigation bar

| Step | Layer | Responsibility |
|---|---|---|
| 1 | Editor | Canvas, bar background, slots, anchors and item Image containers are authored in scene |
| 2 | Application | Inventory query exposes current slot data |
| 3 | Presentation | Toolbar presenter maps data to Image sprites and selection state |
| 4 | Presentation | Click sends select-slot intent to player/application |
| 5 | Domain/Application | Active item/tool decision is validated and applied |

## Async/events

Use UniTask at async boundaries. Domain events are plain contracts; the event manager is an adapter at the application boundary. UI must unsubscribe on disable/destroy. No UI component owns inventory/economy rules.
