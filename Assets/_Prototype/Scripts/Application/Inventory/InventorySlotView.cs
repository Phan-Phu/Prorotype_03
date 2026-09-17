using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Prototype.Application
{
    /// <summary>
    /// One cell of the editor-authored inventory grid (AuthorUiHierarchy.BuildInventoryGrid).
    /// Purely a dumb view + pointer/drag surface: InventoryScreenUI owns every slot's read data,
    /// the shared drag-ghost and the actual InventoryService.Swap call. SlotIndex is assigned once
    /// by the authoring script and matches the Domain Inventory.Slots index this cell represents.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class InventorySlotView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public int SlotIndex;
        [SerializeField] Image _icon;
        [SerializeField] Text _count;

        string _itemId;
        int _itemCount;

        /// <summary>Pushed once per frame by InventoryScreenUI while the popup is open.</summary>
        public void SetContent(string itemId, int count, Sprite icon, Color tint, bool hideIcon)
        {
            _itemId = itemId;
            _itemCount = count;

            if (_icon != null)
            {
                _icon.sprite = hideIcon ? null : icon;
                _icon.color = tint;
                _icon.enabled = !hideIcon && icon != null;
            }
            if (_count != null)
                _count.text = !hideIcon && count > 1 ? count.ToString() : string.Empty;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(_itemId)) return;
            // ItemDetailPopup expects screen pixels with the origin at top-left; PointerEventData
            // (like Input.mousePosition) uses a bottom-left origin.
            var topLeft = new Vector2(eventData.position.x, Screen.height - eventData.position.y);
            ItemDetailPopup.Show(_itemId, _itemCount, topLeft, this);
        }

        public void OnPointerExit(PointerEventData eventData) => ItemDetailPopup.Hide(this);

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(_itemId)) return;
            var owner = InventoryScreenUI.Instance;
            owner?.BeginDrag(SlotIndex, _icon != null ? _icon.sprite : null, _icon != null ? _icon.color : Color.white);
            owner?.UpdateDrag(eventData); // avoid a one-frame flash at the ghost's default position
        }

        public void OnDrag(PointerEventData eventData) => InventoryScreenUI.Instance?.UpdateDrag(eventData);

        public void OnEndDrag(PointerEventData eventData) => InventoryScreenUI.Instance?.EndDrag();

        // Fires on the slot under the cursor when the drag is released; OnEndDrag (above) fires
        // afterwards on the slot the drag started from and always resets the shared drag state.
        public void OnDrop(PointerEventData eventData) => InventoryScreenUI.Instance?.CompleteDrag(SlotIndex);
    }
}
