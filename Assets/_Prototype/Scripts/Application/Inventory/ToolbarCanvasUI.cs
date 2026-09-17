using Prototype.Application;
using Prototype.Domain;
using Prototype.Infrastructure;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Prototype.Application
{
    /// <summary>Canvas-based navigation bar. Layout is component-driven; this class only syncs data.</summary>
    public sealed class ToolbarCanvasUI : MonoBehaviour
    {
        public PlayerController Player;
        public InventoryService InventoryService;
        public static bool PointerOverUI { get; private set; }
        const int SlotCount = 12;
        [Header("Editor-authored NavigationBarUI")]
        [SerializeField] RectTransform _navigationBar;
        [SerializeField] Button[] _slotButtons = new Button[SlotCount];
        [SerializeField] Image[] _items = new Image[SlotCount];
        [SerializeField] Image[] _highlights = new Image[SlotCount];

        void Awake()
        {
            BindExistingLayout();
        }

        void BindExistingLayout()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return; // Layout must be authored in the scene.
            if (_navigationBar == null || _slotButtons == null || _slotButtons.Length != SlotCount ||
                _items == null || _items.Length != SlotCount || _highlights == null || _highlights.Length != SlotCount)
            {
                Debug.LogWarning("ToolbarCanvasUI is missing serialized NavigationBarUI slot references. Assign all 12 slots in the Inspector.");
                return;
            }
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slotButtons[i] == null || _items[i] == null || _highlights[i] == null)
                {
                    Debug.LogWarning($"ToolbarCanvasUI slot {i + 1} has an incomplete serialized reference set.");
                    return;
                }
                var button = _slotButtons[i];
                if (button != null)
                {
                    int index = i;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => Select(index));
                }
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            if (FindAnyObjectByType<EventSystem>() == null)
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
            // Item detail is intentionally available from the inventory only, not the navbar.
            ItemDetailPopup.Hide(this);
            if (Player == null || Player.State == null) return;
            if (_items[0] == null || _highlights[0] == null) return;
            var inv = Player.State.InventorySystem;
            if (inv == null) return;
            var slots = InventoryService != null ? InventoryService.Read(inv) : null;
            for (int i = 0; i < SlotCount; i++)
            {
                var hasSlot = slots != null && i < slots.Length;
                var itemId = hasSlot
                    ? slots[i].ItemId
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
