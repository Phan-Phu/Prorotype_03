using System.Collections.Generic;
using Prototype.Domain;
using Prototype.Application;
using UnityEngine;

namespace Prototype.Application
{
    /// <summary>
    /// Thin MonoBehaviour: reads input, moves the player, and forwards tool actions to GameState.
    /// Target tile = the tile immediately in front of the player (facing), not a free mouse cursor,
    /// so the interaction is deterministic and QA can drive it headlessly (AGENT_DEV §5.3).
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public GameState State;                    // set by GameManager
        public IGameplayService GameplayService;  // Infrastructure behavior boundary
        public IGameWorldService WorldService;     // Infrastructure world query boundary
        internal SeedShopUI ShopUI;                // set by GameManager; replaces the old static singleton lookup

        /// <summary>
        /// Index into State.InventorySystem.Slots (0-9 = row 0, the hotbar row ToolbarCanvasUI shows). This
        /// replaces a fixed ToolType selection: whatever item sits in the active slot IS the tool now
        /// — select a slot (1-9, 0 for the 10th) or click it on the hotbar, same as picking up an item.
        /// </summary>
        public int ActiveSlot;
        public bool GameplayLocked => _gameplayLocks.Count > 0;

        private readonly HashSet<string> _gameplayLocks = new HashSet<string>();

        private enum FacingDir { Down, Up, Left, Right }

        private Vector2Int _facing = new Vector2Int(0, 1); // tool-aim direction (same 4-way axis as sprite)
        private FacingDir _spriteDir = FacingDir.Down;      // sprite-facing direction (4-way only)
        private SpriteRenderer _sr;
        private const float TileSize = 1f;

        // Real art, confirmed per-direction (see ArtCatalog doc); null => placeholder mode.
        private Sprite _idleDown, _idleLeft, _idleUp;
        private Sprite[] _walkDown, _walkLeft, _walkUp;
        private const float SecondsPerFrame = 0.12f;
        private float _animClock;
        private int _animFrame;

        private SpriteRenderer _cursorSr; // reticle over the tile a tool-use would land on

        void Awake()
        {
            _sr = gameObject.AddComponent<SpriteRenderer>();
            var art = PlaceholderArt.Art;
            _idleDown = art?.IdleDown; _idleLeft = art?.IdleLeft; _idleUp = art?.IdleUp;
            _walkDown = art?.WalkDown; _walkLeft = art?.WalkLeft; _walkUp = art?.WalkUp;
            bool hasArt = _idleDown != null && _idleLeft != null && _idleUp != null
                       && _walkDown != null && _walkDown.Length > 0 && _walkDown[0] != null
                       && _walkLeft != null && _walkLeft.Length > 0 && _walkLeft[0] != null
                       && _walkUp   != null && _walkUp.Length   > 0 && _walkUp[0]   != null;
            if (hasArt)
            {
                _sr.sprite = _idleDown; // real farmer sprite, standing facing the camera
                _sr.color = Color.white;
            }
            else
            {
                _idleDown = _idleLeft = _idleUp = null;
                _walkDown = _walkLeft = _walkUp = null;
                _sr.sprite = PlaceholderArt.WhiteSprite; // catalog not baked yet: fall back to placeholder
                _sr.color = new Color(0.2f, 0.5f, 1.0f);
            }
            _sr.sortingOrder = 5; // draw above tile squares and crop sprites
            _sr.transform.localScale = new Vector3(TileSize * 0.7f, TileSize * 0.7f, 1f);

            if (art != null && art.CursorValid != null && art.CursorInvalid != null)
            {
                var cursorGo = new GameObject("TargetCursor");
                _cursorSr = cursorGo.AddComponent<SpriteRenderer>();
                _cursorSr.sortingOrder = 2; // above tile/crop, below player
            }
        }

        void Update()
        {
            if (State == null) return;
            if (GameplayLocked)
            {
                AnimateSprite(false);
                UpdateCursor();
                return;
            }

            float dx = 0f, dy = 0f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    dy += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  dy -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  dx -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dx += 1f;

            bool moving = TryMoveForInput(new Vector2(dx, dy), Time.deltaTime);
            AnimateSprite(moving);
            UpdateCursor();

            // Hotbar slot select: 1-9 then 0 for the 10th slot (row 0 of the inventory grid).
            for (int i = 0; i < 10; i++)
            {
                KeyCode key = i < 9 ? KeyCode.Alpha1 + i : KeyCode.Alpha0; // Alpha1..Alpha9, then Alpha0
                if (Input.GetKeyDown(key)) TrySelectHotbarSlot(i);
            }

            // Use tool: space / enter / left mouse (left-click ignored while over the toolbar or the
            // inventory panel, so clicking a UI slot doesn't also fire a tool-use on the tile behind it)
            bool clickInWorld = Input.GetMouseButtonDown(0)
                && !ToolbarCanvasUI.PointerOverUI
                && !InventoryScreenUI.PointerOverUI
                && !(ShopUI != null && ShopUI.PointerOverUI)
                && !Prototype.Application.DebugPanel.PointerOverUI;
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || clickInWorld)
                TryUseActiveToolForInput();
        }

        public bool TryMoveForInput(Vector2 rawDirection, float deltaSeconds)
        {
            if (GameplayLocked) return false;
            if (rawDirection == Vector2.zero) return false;

            Vector2 dir = rawDirection.normalized;
            // Art is 4-way only, so diagonal input must resolve to one 4-way axis for BOTH
            // sprite facing and tool target. Movement remains diagonal; aim/reticle/tool-use do not.
            _facing = ResolveFourWayFacing(rawDirection);
            _spriteDir = SpriteDirFromFacing(_facing);

            float moveSpeed = State.MasterData?.Player?.MoveSpeed ?? Prototype.Domain.BalanceConfig.MoveSpeed;
            MoveWithCollision(dir * moveSpeed * deltaSeconds);
            return true;
        }

        /// <summary>
        /// Resolves raw movement input to the one tile the 4-way sprite visually communicates.
        /// Ties deliberately prefer vertical, matching the existing animation rule, so W+D/W+A
        /// face/target Up and S+D/S+A face/target Down instead of surprising diagonal tool-use.
        /// </summary>
        public static Vector2Int ResolveFourWayFacing(Vector2 rawDirection)
        {
            if (rawDirection == Vector2.zero) return new Vector2Int(0, 1);
            if (Mathf.Abs(rawDirection.y) >= Mathf.Abs(rawDirection.x))
                return rawDirection.y >= 0f ? new Vector2Int(0, 1) : new Vector2Int(0, -1);
            return rawDirection.x > 0f ? new Vector2Int(1, 0) : new Vector2Int(-1, 0);
        }

        private static FacingDir SpriteDirFromFacing(Vector2Int facing)
        {
            if (facing.y > 0) return FacingDir.Up;
            if (facing.y < 0) return FacingDir.Down;
            return facing.x > 0 ? FacingDir.Right : FacingDir.Left;
        }

        public bool TrySelectHotbarSlot(int slotIndex)
        {
            if (GameplayLocked) return false;
            if (slotIndex < 0 || slotIndex >= 10) return false;
            ActiveSlot = slotIndex;
            return true;
        }

        public bool TryUseActiveToolForInput()
        {
            if (GameplayLocked) return false;
            UseTool();
            return true;
        }

        /// <summary>
        /// Picks the walk-cycle array for the current facing direction and cycles it while moving;
        /// shows the dedicated Idle* sprite (a genuine standing pose, not a walk frame) when idle.
        /// Walk* no longer includes the sheet's own standing frame (column 0) — mixing it into the
        /// cycle made the animation visibly snap back to standing once per loop. There's no dedicated
        /// right-facing art, so Right reuses Left mirrored (flipX) — always correct, no guessing.
        /// No-op in placeholder mode (_walkDown null).
        /// </summary>
        void AnimateSprite(bool moving)
        {
            if (_walkDown == null) return;

            Sprite idle; Sprite[] walk;
            switch (_spriteDir)
            {
                case FacingDir.Up:    idle = _idleUp;   walk = _walkUp;   break;
                case FacingDir.Left:  idle = _idleLeft; walk = _walkLeft; break;
                case FacingDir.Right: idle = _idleLeft; walk = _walkLeft; break; // mirrored below
                default:              idle = _idleDown; walk = _walkDown; break;
            }
            _sr.flipX = _spriteDir == FacingDir.Right;

            if (!moving)
            {
                _animClock = 0f;
                _animFrame = 0;
                _sr.sprite = idle;
                return;
            }

            _animClock += Time.deltaTime;
            if (_animClock >= SecondsPerFrame)
            {
                _animClock -= SecondsPerFrame;
                _animFrame = (_animFrame + 1) % walk.Length;
            }
            _sr.sprite = walk[_animFrame];
        }

        /// <summary>Tile directly in front of the player — same coord UseTool() would act on.</summary>
        GridCoord TargetCoord() => State.Grid.WorldToGrid(
            transform.position + new Vector3(_facing.x, _facing.y, 0f) * TileSize);

        /// <summary>Shows green over the target tile, red if it's out of bounds OR occupied by a
        /// static object (S2-DEV-04, e.g. a tree) — same occupancy the player's own movement is
        /// blocked by, so "cursor red" and "can't walk there" always agree. No-op without art.</summary>
        void UpdateCursor()
        {
            if (_cursorSr == null) return;
            var coord = TargetCoord();
            bool valid = State.Grid.InBounds(coord) && !State.Grid.IsOccupied(coord);
            _cursorSr.sprite = valid ? PlaceholderArt.Art.CursorValid : PlaceholderArt.Art.CursorInvalid;
            _cursorSr.transform.position = State.Grid.GridToWorld(coord);
        }

        /// <summary>
        /// Moves the player by delta, per axis, refusing to enter a tile occupied by a static object
        /// (S2-DEV-04 — e.g. a standing tree; DESIGN_BRIEFS.md [DSN-030]: "player không đi xuyên qua ô
        /// có cây"). Per-axis (not a single combined move) so brushing past a tree's corner still slides
        /// along it instead of stopping the player dead, same feel as typical tile-based collision.
        /// Map-edge clamping happens inside ClampPos, same as before this ticket.
        /// </summary>
        void MoveWithCollision(Vector2 delta)
        {
            Vector3 pos = transform.position;

            Vector3 afterX = ClampPos(pos + new Vector3(delta.x, 0f, 0f));
            if (!State.Grid.IsOccupied(State.Grid.WorldToGrid(afterX))) pos = afterX;

            Vector3 afterY = ClampPos(pos + new Vector3(0f, delta.y, 0f));
            if (!State.Grid.IsOccupied(State.Grid.WorldToGrid(afterY))) pos = afterY;

            transform.position = pos;
        }

        Vector3 ClampPos(Vector3 p)
        {
            float halfW = State.Grid.Width * 0.5f;
            float halfH = State.Grid.Height * 0.5f;
            p.x = Mathf.Clamp(p.x, -halfW + 0.5f, halfW - 0.5f);
            p.y = Mathf.Clamp(p.y, -halfH + 0.5f, halfH - 0.5f);
            return p;
        }

        void UseTool()
        {
            if (ShopUI != null && ShopUI.IsOpen) return;

            GridCoord coord = TargetCoord();
            bool isShopTile = false;
            if (WorldService != null)
            {
                var shopTile = WorldService.IsSeedShopTile(State, coord).GetAwaiter().GetResult();
                isShopTile = shopTile.IsSuccess && shopTile.Value;
            }
            if (isShopTile)
            {
                ShopUI?.OpenShop();
                GameManager.SpawnFeedback(coord, FeedbackKind.Plant);
                return;
            }

            var slot = ActiveSlot >= 0 && ActiveSlot < State.InventorySystem.Slots.Length
                ? State.InventorySystem.Slots[ActiveSlot] : null;

            var result = ResolveAction(slot?.ItemId, coord);
            GameManager.SpawnFeedback(coord, result.Feedback);
            // S2-QA-06: every real tool-use in a live session gets a row in Artifacts/session_<seed>.csv
            // — not fired by HeadlessSim or EditMode tests, only real Editor Play / packaged-build input.
            Prototype.Infrastructure.SessionLogger.LogAction(State, slot?.ItemId ?? "(empty)", coord, result.Code.ToString());
        }

        /// <summary>Maps the active slot's item id to the action it performs. Empty/unrecognised slot = a no-op miss (WrongTool), same as any other invalid tool-use.</summary>
        ToolActionDto ResolveAction(string itemId, GridCoord coord)
        {
            if (GameplayService == null) return new ToolActionDto(ToolResultCode.WrongTool, FeedbackKind.Miss);
            if (itemId == ToolItemIds.Hoe)         return GameplayService.UseTool(State, ToolType.Hoe, coord).GetAwaiter().GetResult().Value;
            if (itemId == ToolItemIds.WateringCan) return GameplayService.UseTool(State, ToolType.WateringCan, coord).GetAwaiter().GetResult().Value;
            if (itemId == ToolItemIds.Harvest)     return GameplayService.UseTool(State, ToolType.Harvest, coord).GetAwaiter().GetResult().Value;
            if (itemId == ToolItemIds.Axe)         return GameplayService.UseTool(State, ToolType.Chop, coord).GetAwaiter().GetResult().Value;
            if (itemId == Prototype.Domain.CropDefinition.SeedItemId(Prototype.Domain.CropId.Turnip))
                return GameplayService.PlantSpecific(State, Prototype.Domain.CropId.Turnip, coord).GetAwaiter().GetResult().Value;
            if (itemId == Prototype.Domain.CropDefinition.SeedItemId(Prototype.Domain.CropId.Potato))
                return GameplayService.PlantSpecific(State, Prototype.Domain.CropId.Potato, coord).GetAwaiter().GetResult().Value;
            return new ToolActionDto(ToolResultCode.WrongTool, FeedbackKind.Miss);
        }

        /// <summary>Friendly label for the active slot's item — HUD shows this instead of a fixed tool name.</summary>
        public string ActiveItemName()
        {
            if (State == null) return "-";
            var slot = ActiveSlot >= 0 && ActiveSlot < State.InventorySystem.Slots.Length
                ? State.InventorySystem.Slots[ActiveSlot] : null;
            if (slot == null) return "(empty)";
            if (slot.ItemId == ToolItemIds.Hoe) return "Hoe";
            if (slot.ItemId == ToolItemIds.WateringCan) return "Watering Can";
            if (slot.ItemId == ToolItemIds.Harvest) return "Harvest";
            if (slot.ItemId == ToolItemIds.Axe) return "Axe";
            if (slot.ItemId == Prototype.Domain.TreeDefinition.WoodItemId) return "Wood";
            if (slot.ItemId == Prototype.Domain.CropDefinition.SeedItemId(Prototype.Domain.CropId.Turnip)) return "Turnip Seed";
            if (slot.ItemId == Prototype.Domain.CropDefinition.SeedItemId(Prototype.Domain.CropId.Potato)) return "Potato Seed";
            return slot.ItemId;
        }

        public void SetGameplayLocked(bool locked) => SetGameplayLocked(locked, "default");

        public void SetGameplayLocked(bool locked, string source)
        {
            source = string.IsNullOrEmpty(source) ? "default" : source;
            if (locked) _gameplayLocks.Add(source);
            else _gameplayLocks.Remove(source);
        }
    }
}
