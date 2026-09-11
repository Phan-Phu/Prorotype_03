using Prototype.Application;
using Prototype.Domain;
using UnityEngine;

namespace Prototype.Application
{
    /// <summary>
    /// Always-visible hotbar — mirrors Inventory row 0 (slots 0-9) exactly, the same 10 slots
    /// InventoryScreenUI's top row shows. This is a deliberate design choice (see conversation
    /// 2026-08-15): an earlier version drew 4 fixed tool icons unrelated to inventory slot content,
    /// which meant dragging items around in the I panel never changed what the hotbar showed — visibly
    /// "out of sync". Now both are just two views of the same Inventory.Slots array, so there is
    /// nothing to keep in sync — moving an item in one instantly shows up in the other.
    /// </summary>
    public class ToolbarUI : MonoBehaviour
    {
        public PlayerController Player;

        /// <summary>
        /// True while the mouse is over the hotbar this frame. PlayerController checks this before
        /// treating a left-click as "use tool in the world", so clicking a slot to switch tools doesn't
        /// also fire a tool-use action on whatever tile is behind the UI.
        /// </summary>
        public static bool PointerOverUI { get; private set; }

        const float Scale = ResponsiveUILayout.ToolbarMaxScale; // max zoom; scales down for small windows
        const int BarNativeW = (int)ResponsiveUILayout.ToolbarNativeW, BarNativeH = (int)ResponsiveUILayout.ToolbarNativeH;
        static readonly int Cols = Prototype.Domain.Inventory.Columns;

        void OnGUI()
        {
            var art = PlaceholderArt.Art;
            if (art == null || Player == null || Player.State == null || art.InventoryBar == null)
            {
                PointerOverUI = false;
                return;
            }

            var oldContentColor = GUI.contentColor;
            GUI.contentColor = new Color32(42, 30, 20, 255);

            float scale = ResponsiveUILayout.ToolbarScale(Screen.width, Screen.height);
            float w = BarNativeW * scale, h = BarNativeH * scale;
            var barRect = ResponsiveUILayout.ToolbarRect(Screen.width, Screen.height);
            float x = barRect.x, y = barRect.y;

            var mouse = Event.current.mousePosition; // already GUI-space (top-left origin), no conversion needed
            PointerOverUI = barRect.Contains(mouse);

            PlaceholderArt.DrawSprite(barRect, art.InventoryBar);

            float leftMargin = 4f * scale;
            float topMargin = 4f * scale;
            float slotW = 20f * scale;
            float slotH = 20f * scale;
            float dividerW = 1f * scale;

            var inv = Player.State.InventorySystem;
            for (int i = 0; i < Cols; i++)
            {
                var slotRect = new Rect(x + leftMargin + i * (slotW + dividerW), y + topMargin, slotW, slotH);
                var stack = inv.Slots[i];

                if (stack != null)
                {
                    var icon = PlaceholderArt.ItemIcon(stack.ItemId);
                    if (icon != null)
                    {
                        // B2 (Sprint 1): tint the shared seed sprite per crop (turnip vs potato).
                        var prevColor = GUI.color;
                        GUI.color = PlaceholderArt.ItemTint(stack.ItemId);
                        PlaceholderArt.DrawSpriteFit(PlaceholderArt.Shrink(slotRect, 0.15f), icon);
                        GUI.color = prevColor;
                    }
                    if (stack.Count > 1)
                        GUI.Label(new Rect(slotRect.xMax - 20f, slotRect.yMax - 16f, 20f, 16f), stack.Count.ToString());
                }

                if (i == Player.ActiveSlot && art.InventoryHighlight != null)
                {
                    var highlightRect = new Rect(slotRect.x - 2f * scale, slotRect.y - 2f * scale, slotRect.width + 4f * scale, slotRect.height + 4f * scale);
                    PlaceholderArt.DrawSprite(highlightRect, art.InventoryHighlight);
                }

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0
                    && !Player.GameplayLocked
                    && slotRect.Contains(mouse))
                {
                    Player.ActiveSlot = i;
                    Event.current.Use();
                }
            }
            GUI.contentColor = oldContentColor;
        }
    }
}
