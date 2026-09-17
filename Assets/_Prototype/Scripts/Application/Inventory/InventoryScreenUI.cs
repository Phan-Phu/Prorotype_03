using Prototype.Domain;
using Prototype.Infrastructure;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Prototype.Application
{
    /// <summary>
    /// Controller for the editor-authored InventoryPopup. The static container and 48-slot grid
    /// (InventorySlotView per cell) are scene objects; this class opens/closes the popup, pushes
    /// InventoryService read data into each slot every frame while open, and owns the one shared
    /// drag-to-swap gesture (a slot can only ever report into its owning InventoryScreenUI).
    /// </summary>
    public sealed class InventoryScreenUI : PopupBase
    {
        public static InventoryScreenUI Instance { get; private set; }

        [SerializeField] RectTransform panel;
        [Header("Drag ghost (follows the cursor while dragging an item)")]
        [SerializeField] Image _dragGhost;

        public GameState State;
        public IInventoryService InventoryService;
        public InventoryService InventoryReadService;
        public PlayerController Player;
        internal SeedShopUI ShopUI;                // set by GameManager; replaces the old static singleton lookup

        const string GameplayLockSource = "inventory";

        /// <summary>Reserved for the inventory slot views to set while their pointer is inside.</summary>
        public static bool PointerOverUI { get; private set; }

        InventorySlotView[] _slots;
        int _dragSlot = -1;

        protected override RectTransform ResolvePopupTarget() => panel;

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
            if (panel == null)
                Debug.LogError("InventoryPopup is missing its serialized InventoryContentUI panel reference.");
            _slots = GetComponentsInChildren<InventorySlotView>(true);
            if (_dragGhost != null) _dragGhost.gameObject.SetActive(false);
        }

        void Update()
        {
            if (ShopUI != null && ShopUI.IsOpen) return;
            if (Input.GetKeyDown(KeyCode.I)) Toggle();
            if (!IsOpen) return;

            RefreshSlots();
            PointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        void OnDisable()
        {
            PointerOverUI = false;
            Player?.SetGameplayLocked(false, GameplayLockSource);
        }

        public void Toggle()
        {
            if (IsOpen) CloseInventory();
            else OpenInventory();
        }

        public void OpenInventory() => ShowPopup();

        public void CloseInventory() => HidePopup();

        protected override void OnPopupShown()
        {
            Player?.SetGameplayLocked(true, GameplayLockSource);
        }

        protected override void OnPopupHidden()
        {
            // Runs on every hide path (CloseInventory, PopupParent.HideAll, ...), not just the
            // explicit close, so a mid-drag close never leaves the ghost/tooltip stuck on screen.
            ItemDetailPopup.Hide(this);
            EndDrag();
            PointerOverUI = false;
            Player?.SetGameplayLocked(false, GameplayLockSource);
        }

        protected override void OnDestroy()
        {
            Player?.SetGameplayLocked(false, GameplayLockSource);
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        void RefreshSlots()
        {
            if (State?.InventorySystem == null || _slots == null) return;
            var data = InventoryReadService != null ? InventoryReadService.Read(State.InventorySystem) : null;

            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];
                if (slot == null) continue;

                var slotData = data != null && slot.SlotIndex >= 0 && slot.SlotIndex < data.Length
                    ? data[slot.SlotIndex]
                    : default;
                bool hideIcon = slot.SlotIndex == _dragSlot;
                var icon = slotData.IsEmpty ? null : PlaceholderArt.ItemIcon(slotData.ItemId);
                var tint = slotData.IsEmpty ? Color.white : PlaceholderArt.ItemTint(slotData.ItemId);
                slot.SetContent(slotData.ItemId, slotData.Count, icon, tint, hideIcon);
            }
        }

        // --- Drag-to-swap, driven by InventorySlotView's pointer/drag callbacks ---

        public void BeginDrag(int sourceSlot, Sprite icon, Color tint)
        {
            _dragSlot = sourceSlot;
            ItemDetailPopup.Hide(this);
            if (_dragGhost == null) return;
            _dragGhost.sprite = icon;
            _dragGhost.color = tint;
            _dragGhost.enabled = icon != null;
            _dragGhost.transform.SetAsLastSibling();
            _dragGhost.gameObject.SetActive(true);
        }

        public void UpdateDrag(PointerEventData eventData)
        {
            if (_dragSlot < 0 || _dragGhost == null) return;
            var parentRect = _dragGhost.transform.parent as RectTransform;
            if (parentRect == null) return;

            var canvas = _dragGhost.canvas;
            var eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, eventData.position, eventCamera, out var local))
                _dragGhost.rectTransform.anchoredPosition = local;
        }

        /// <summary>Ends the shared drag gesture without swapping — cancels if dropped outside any slot.</summary>
        public void EndDrag()
        {
            _dragSlot = -1;
            if (_dragGhost != null) _dragGhost.gameObject.SetActive(false);
        }

        /// <summary>Called on the slot under the cursor when a drag is released over it.</summary>
        public void CompleteDrag(int destinationSlot)
        {
            int source = _dragSlot;
            if (source >= 0 && destinationSlot != source && InventoryService != null && State?.InventorySystem != null)
                InventoryService.Swap(State.InventorySystem, source, destinationSlot).GetAwaiter().GetResult();
            EndDrag();
        }
    }
}
