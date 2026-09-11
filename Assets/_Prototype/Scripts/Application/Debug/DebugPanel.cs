using Prototype.Domain;
using Prototype.Application;
using UnityEngine;

namespace Prototype.Application
{
    /// <summary>
    /// F1 debug panel (AGENT_DEV §3.5). Every button maps 1:1 to a boot arg / GameState hook so QA
    /// can start a run deterministically (see BootArgs) without clicking through menus.
    /// </summary>
    public class DebugPanel : MonoBehaviour
    {
        const string GameplayLockSource = "debug-panel";
        public static bool IsOpen { get; private set; }
        public static bool PointerOverUI { get; private set; }

        public GameState State;
        public PlayerController Player;
        private bool _open;
        private bool _showCoords;
        private int _frameCount;
        private float _fpsAccum;
        private float _fps;

        void Update()
        {
            if (!SeedShopUI.IsOpen && Input.GetKeyDown(KeyCode.F1)) ToggleOpen();
            _frameCount++;
            _fpsAccum += Time.deltaTime;
            if (_fpsAccum >= 0.5f) { _fps = _frameCount / _fpsAccum; _frameCount = 0; _fpsAccum = 0; }
        }

        void OnDisable() => Close();
        void OnDestroy() => Close();

        public void ToggleOpen()
        {
            if (_open) Close();
            else Open();
        }

        public void Open()
        {
            _open = true;
            IsOpen = true;
            ResolvePlayer()?.SetGameplayLocked(true, GameplayLockSource);
        }

        public void Close()
        {
            if (_open || IsOpen)
                ResolvePlayer()?.SetGameplayLocked(false, GameplayLockSource);
            _open = false;
            IsOpen = false;
            PointerOverUI = false;
        }

        PlayerController ResolvePlayer()
        {
            if (Player == null) Player = FindAnyObjectByType<PlayerController>();
            return Player;
        }

        public void AddMoney1000() => State?.SetMoney(State.Wallet.Money + 1000);
        public void RefillStamina() => State?.RefillStamina();

        const int SeedCheatAmount = 5; // a cheat should be generous — no point clicking 5 times

        void OnGUI()
        {
            if (!_open || State == null) { PointerOverUI = false; return; }
            float scale = ResponsiveUILayout.DebugPanelScale(Screen.width, Screen.height);
            var panelRect = ResponsiveUILayout.DebugPanelRect(Screen.width, Screen.height);
            int w = (int)ResponsiveUILayout.DebugNativeW;
            float x = panelRect.x;
            float y = panelRect.y;
            PointerOverUI = panelRect.Contains(Event.current.mousePosition);

            var oldMatrix = GUI.matrix;
            var oldContentColor = GUI.contentColor;
            GUI.contentColor = new Color32(42, 30, 20, 255);
            GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), panelRect.position);
            GUI.Box(new Rect(x, y, w, ResponsiveUILayout.DebugNativeH), "DEBUG (F1)");
            float by = y + 24;
            if (GUI.Button(new Rect(x + 8, by, w - 16, 22), "Skip Hour")) State.Clock.SkipHour();
            by += 26;
            if (GUI.Button(new Rect(x + 8, by, w - 16, 22), "Skip Day (sleep)")) State.SkipDay();
            by += 26;
            if (GUI.Button(new Rect(x + 8, by, w - 16, 22), "Money +1000")) AddMoney1000();
            by += 26;
            if (GUI.Button(new Rect(x + 8, by, w - 16, 22), "Refill Stamina")) RefillStamina();
            by += 26;
            if (GUI.Button(new Rect(x + 8, by, w - 16, 22), "Force All Ripe")) State.ForceRipeAll();
            by += 26;
            if (GUI.Button(new Rect(x + 8, by, w - 16, 22), $"+{SeedCheatAmount} Turnip seed")) State.InventorySystem.Add(Prototype.Domain.CropDefinition.SeedItemId(Prototype.Domain.CropId.Turnip), SeedCheatAmount);
            by += 26;
            if (GUI.Button(new Rect(x + 8, by, w - 16, 22), $"+{SeedCheatAmount} Potato seed")) State.InventorySystem.Add(Prototype.Domain.CropDefinition.SeedItemId(Prototype.Domain.CropId.Potato), SeedCheatAmount);
            by += 26;
            if (GUI.Button(new Rect(x + 8, by, w - 16, 22), "Clear inventory")) { State.InventorySystem.Clear(); State.GrantStartingTools(); }
            by += 26;
            // S2-DEV-05 (DESIGN_BRIEFS.md [DSN-030]): wood goes into the inventory like any other carried
            // item (unlike crops, which auto-sell on harvest) — this is the "existing sell channel"
            // (GameState.SellAllWood) reachable in a real running build. There's still no in-game shop
            // UI (out of scope per the design brief; LATER.md 2026-08-15 BUG-3 already tracks the
            // general "no way to spend/realize money" gap) — this is a debug hook, not a shop.
            if (GUI.Button(new Rect(x + 8, by, w - 16, 22), "Sell all Crops")) State.SellAllCrops();
            by += 26;
            if (GUI.Button(new Rect(x + 8, by, w - 16, 22), "Sell all Wood")) State.SellAllWood();
            by += 26;
            // S2-QA-05: bug-bash hooks for the wood feature — spawn a tree right next to the player
            // (instead of trekking to the corner GameState.SeedTrees seeds at start) and force every
            // stump to regrow instantly (instead of waiting real TreeRespawnDays sleeps) so QA can hit
            // chop/collision/respawn scenarios on demand.
            if (GUI.Button(new Rect(x + 8, by, w - 16, 22), "+ Tree (near player)"))
            {
                var pcSpawn = FindAnyObjectByType<PlayerController>();
                if (pcSpawn != null)
                {
                    var playerCoord = State.Grid.WorldToGrid(pcSpawn.transform.position);
                    State.DebugSpawnTree(new GridCoord(playerCoord.X, playerCoord.Y + 1));
                }
            }
            by += 26;
            if (GUI.Button(new Rect(x + 8, by, w - 16, 22), "Force Respawn Trees")) State.ForceRespawnTrees();
            by += 26;
            if (GUI.Button(new Rect(x + 8, by, w - 16, 22), $"Toggle Grid Coord ({_showCoords})")) { _showCoords = !_showCoords; }
            by += 26;

            GUI.Box(new Rect(x + 8, by, w - 16, 78), "");
            GUI.Label(new Rect(x + 14, by + 2, w - 24, 18), "Inventory:");
            GUI.Label(new Rect(x + 14, by + 20, w - 24, 18),
                $"Turnip seed x{State.InventorySystem.Count(Prototype.Domain.CropDefinition.SeedItemId(Prototype.Domain.CropId.Turnip))}");
            GUI.Label(new Rect(x + 14, by + 38, w - 24, 18),
                $"Potato seed x{State.InventorySystem.Count(Prototype.Domain.CropDefinition.SeedItemId(Prototype.Domain.CropId.Potato))}");
            GUI.Label(new Rect(x + 14, by + 56, w - 24, 18),
                $"Crop x{State.InventorySystem.Count(Prototype.Domain.CropDefinition.ProduceItemId(Prototype.Domain.CropId.Turnip))}/{State.InventorySystem.Count(Prototype.Domain.CropDefinition.ProduceItemId(Prototype.Domain.CropId.Potato))}  Wood x{State.InventorySystem.Count(Prototype.Domain.TreeDefinition.WoodItemId)}");
            by += 84;

            GUI.Label(new Rect(x + 8, by, w - 16, 22), $"FPS {_fps:0.0}");

            GUI.matrix = oldMatrix;

            if (_showCoords && State != null)
            {
                var pc = FindAnyObjectByType<PlayerController>();
                if (pc != null)
                {
                    GridCoord c = State.Grid.WorldToGrid(pc.transform.position);
                    GUI.Label(new Rect(8, 120, 220, 24), $"Player tile {c}");
                }
            }
            GUI.contentColor = oldContentColor;
        }
    }
}
