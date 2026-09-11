using Prototype.Application;
using Prototype.Domain;
using UnityEngine;

namespace Prototype.Application
{
    /// <summary>
    /// Full inventory grid (real art: InventoryPlayer.png — a 10-slot hotbar row + 3x10 backpack
    /// grid, 40 slots total, matching Inventory.SlotCount exactly). Toggle with I. Drag an item onto
    /// another slot to swap their positions (Inventory.Swap) — the ask this screen exists to satisfy.
    /// </summary>
    public class InventoryScreenUI : MonoBehaviour
    {
        public GameState State;
        public PlayerController Player;
        private bool _open;
        private int _dragSlot = -1;

        const string GameplayLockSource = "inventory";

        /// <summary>True while the panel is open and the mouse is over it — same purpose as
        /// ToolbarUI.PointerOverUI (PlayerController checks both before firing a world tool-use click).</summary>
        public static bool PointerOverUI { get; private set; }
        public bool IsOpen => _open;

        const float Scale = ResponsiveUILayout.InventoryMaxScale; // max zoom; scales down for small windows
        const int PanelNativeW = (int)ResponsiveUILayout.InventoryNativeW, PanelNativeH = (int)ResponsiveUILayout.InventoryNativeH;
        const int Cols = Prototype.Domain.Inventory.Columns;
        const int Rows = Prototype.Domain.Inventory.Rows;

        void Update()
        {
            if (SeedShopUI.IsOpen) return;
            if (Input.GetKeyDown(KeyCode.I)) Toggle();
        }

        void OnDestroy()
        {
            UnlockGameplay();
        }

        void OnDisable()
        {
            UnlockGameplay();
            PointerOverUI = false;
        }

        public void Toggle()
        {
            if (_open) CloseInventory();
            else OpenInventory();
        }

        public void OpenInventory()
        {
            _open = true;
            Player?.SetGameplayLocked(true, GameplayLockSource);
        }

        public void CloseInventory()
        {
            _open = false;
            _dragSlot = -1;
            PointerOverUI = false;
            UnlockGameplay();
        }

        void UnlockGameplay()
        {
            Player?.SetGameplayLocked(false, GameplayLockSource);
        }

        void OnGUI()
        {
            if (!_open || State == null) { PointerOverUI = false; return; }
            var art = PlaceholderArt.Art;
            if (art == null || art.InventoryPlayer == null) { PointerOverUI = false; return; }

            var oldContentColor = GUI.contentColor;
            GUI.contentColor = new Color32(42, 30, 20, 255);

            float scale = ResponsiveUILayout.InventoryScale(Screen.width, Screen.height);
            float w = PanelNativeW * scale, h = PanelNativeH * scale;
            var panelRect = ResponsiveUILayout.InventoryPanelRect(Screen.width, Screen.height);
            float x = panelRect.x, y = panelRect.y;
            var mouse = Event.current.mousePosition;
            PointerOverUI = panelRect.Contains(mouse);

            PlaceholderArt.DrawSprite(panelRect, art.InventoryPlayer);

            float leftMargin = 4f * scale;
            float topMargin = 4f * scale;
            float slotW = 20f * scale;
            float slotH = 20f * scale;
            float dividerW = 1f * scale;
            float dividerH = 1f * scale;
            float rowGap = 10f * scale;

            var inv = State.InventorySystem;
            bool mouseUpUnhandled = Event.current.type == EventType.MouseUp && Event.current.button == 0;

            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    int slotIndex = row * Cols + col;
                    float slotX = x + leftMargin + col * (slotW + dividerW);
                    float slotY = y + topMargin + (row == 0 ? 0f : slotH + rowGap + (row - 1) * (slotH + dividerH));
                    var slotRect = new Rect(slotX, slotY, slotW, slotH);
                    var stack = inv.Slots[slotIndex];

                    if (stack != null && slotIndex != _dragSlot)
                        DrawItem(slotRect, stack);

                    if (Event.current.type == EventType.MouseDown && Event.current.button == 0
                        && slotRect.Contains(mouse) && stack != null)
                    {
                        _dragSlot = slotIndex;
                        Event.current.Use();
                    }
                    else if (mouseUpUnhandled && slotRect.Contains(mouse) && _dragSlot >= 0)
                    {
                        inv.Swap(_dragSlot, slotIndex);
                        _dragSlot = -1;
                        mouseUpUnhandled = false;
                        Event.current.Use();
                    }
                }
            }

            // Dropped outside every slot (but still released) -> cancel the drag, snap back.
            if (mouseUpUnhandled && _dragSlot >= 0)
                _dragSlot = -1;

            if (_dragSlot >= 0 && inv.Slots[_dragSlot] != null)
            {
                var followRect = new Rect(mouse.x - slotW * 0.4f, mouse.y - slotH * 0.4f, slotW * 0.8f, slotH * 0.8f);
                DrawItem(followRect, inv.Slots[_dragSlot]);
            }

            GUI.Label(new Rect(x, y - 20f, w, 20f), "Inventory (I to close) — drag to reorder");
            GUI.contentColor = oldContentColor;
        }

        static void DrawItem(Rect rect, Prototype.Domain.ItemStack stack)
        {
            var icon = PlaceholderArt.ItemIcon(stack.ItemId);
            if (icon != null)
            {
                // B2 (Sprint 1): tint the shared seed sprite per crop (turnip vs potato).
                var prevColor = GUI.color;
                GUI.color = PlaceholderArt.ItemTint(stack.ItemId);
                PlaceholderArt.DrawSpriteFit(PlaceholderArt.Shrink(rect, 0.15f), icon);
                GUI.color = prevColor;
            }
            if (stack.Count > 1)
                GUI.Label(new Rect(rect.xMax - 22f, rect.yMax - 18f, 20f, 16f), stack.Count.ToString());
        }
    }
}
