using Prototype.Application;
using Prototype.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Prototype.Application
{
    /// <summary>Canvas-based navigation bar. Layout is component-driven; this class only syncs data.</summary>
    public sealed class ToolbarCanvasUI : MonoBehaviour
    {
        public PlayerController Player;
        public IInventoryQuery InventoryQuery;
        public static bool PointerOverUI { get; private set; }
        const int SlotCount = 12;
        readonly Image[] _items = new Image[SlotCount];
        readonly Image[] _highlights = new Image[SlotCount];

        void Awake()
        {
            BindExistingLayout();
        }

        void BindExistingLayout()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return; // Layout must be authored in the scene.
            var bar = transform.Find("NavigationBar");
            if (bar == null) return;
            for (int i = 0; i < SlotCount; i++)
            {
                var slot = bar.Find($"Slot_{i + 1:00}");
                if (slot == null) return;
                _items[i] = slot.Find("Item")?.GetComponent<Image>();
                _highlights[i] = slot.Find("Active")?.GetComponent<Image>();
                if (_items[i] == null || _highlights[i] == null) return;
                var button = slot.GetComponent<Button>();
                if (button != null)
                {
                    int index = i;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => Select(index));
                }
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("EventSystem");
                events.AddComponent<EventSystem>();
                events.AddComponent<StandaloneInputModule>();
            }

            for (int i = 0; i < SlotCount; i++)
                _items[i].preserveAspect = true;
        }

        void Update()
        {
            if (Player == null || Player.State == null) return;
            if (_items[0] == null || _highlights[0] == null) return;
            var snapshot = InventoryQuery != null ? InventoryQuery.Read() : null;
            var inv = Player.State.InventorySystem;
            if (inv == null) return;
            var snapshotSlots = snapshot?.Slots;
            for (int i = 0; i < SlotCount; i++)
            {
                var hasSnapshot = snapshotSlots != null && i < snapshotSlots.Length;
                var itemId = hasSnapshot
                    ? snapshotSlots[i].ItemId
                    : (i < inv.Slots.Length ? inv.Slots[i]?.ItemId : null);
                _items[i].sprite = string.IsNullOrEmpty(itemId) ? null : PlaceholderArt.ItemIcon(itemId);
                _items[i].color = string.IsNullOrEmpty(itemId) ? Color.clear : PlaceholderArt.ItemTint(itemId);
                _highlights[i].enabled = i == Player.ActiveSlot;
            }
            PointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        void Select(int index) { if (Player != null && !Player.GameplayLocked) Player.ActiveSlot = index; }

    }
}
