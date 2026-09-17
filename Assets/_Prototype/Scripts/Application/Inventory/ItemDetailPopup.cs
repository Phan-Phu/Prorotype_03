using Prototype.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Prototype.Application
{
    /// <summary>
    /// Editor-authored uGUI popup. The scene owns this GameObject; a popup source only updates
    /// its content and visibility. It shares PopupParent with InventoryPopup.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class ItemDetailPopup : PopupBase
    {
        const float Margin = 8f;
        const float MouseGap = 14f;
        const float DefaultWidth = 260f;
        const float DefaultHeight = 118f;

        public RectTransform Panel;
        public Image Icon;
        public Text Title;
        public Text Quantity;
        public Text Description;

        static ItemDetailPopup _instance;
        string _itemId;
        int _count;
        Object _source;
        Vector2 _mouseTopLeftPosition;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
            Panel = Panel != null ? Panel : transform as RectTransform;
            if (Panel == null || Icon == null || Title == null || Quantity == null || Description == null)
                Debug.LogWarning("ItemDetailPopup is missing one or more serialized UI references. Assign Panel, Icon, Title, Quantity and Description in the scene.");
        }

        /// <param name="mouseTopLeftPosition">Mouse position in screen pixels, with origin at top-left.</param>
        public static void Show(string itemId, int count, Vector2 mouseTopLeftPosition, Object source)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return;
            var popup = EnsureInstance();
            if (popup == null) return;

            popup._itemId = itemId;
            popup._count = count;
            popup._source = source;
            popup._mouseTopLeftPosition = mouseTopLeftPosition;
            popup.RefreshContent();
            popup.Position(mouseTopLeftPosition);
            popup.ShowPopup();
        }

        public static void Hide(Object source)
        {
            if (_instance == null || (_instance._source != null && _instance._source != source)) return;
            _instance._source = null;
            _instance.HidePopup();
        }

        static ItemDetailPopup EnsureInstance()
        {
            if (_instance != null) return _instance;
            _instance = FindAnyObjectByType<ItemDetailPopup>();
            if (_instance == null)
                Debug.LogWarning("ItemDetailPopup is not present under UICanvas. Create it in Prototype_Main scene.");
            return _instance;
        }

        void RefreshContent()
        {
            var item = GameManager.Instance?.MasterData?.GetItem(_itemId);
            string displayName = string.IsNullOrWhiteSpace(item?.DisplayName) ? _itemId : item.DisplayName;
            string description = string.IsNullOrWhiteSpace(item?.Description)
                ? "No item description available."
                : item.Description;

            if (Icon != null)
            {
                Icon.sprite = PlaceholderArt.ItemIcon(_itemId);
                Icon.color = PlaceholderArt.ItemTint(_itemId);
                Icon.preserveAspect = true;
            }
            if (Title != null) Title.text = displayName;
            if (Quantity != null) Quantity.text = $"Quantity: {_count}";
            if (Description != null) Description.text = description;
        }

        void Position(Vector2 mouseTopLeftPosition)
        {
            if (Panel == null) return;
            var canvas = Panel.GetComponentInParent<Canvas>();
            var eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            var screenSize = ScreenSize(eventCamera);
            float width = screenSize.x > 0f ? screenSize.x : DefaultWidth;
            float height = screenSize.y > 0f ? screenSize.y : DefaultHeight;
            var popupRect = PopupRect(mouseTopLeftPosition, width, height);
            var popupCenter = new Vector2(popupRect.center.x, Screen.height - popupRect.center.y);

            // PopupParent is authored with a top-left pivot. Convert screen pixels into that
            // parent's local units, so CanvasScaler and non-reference resolutions cannot offset
            // the popup from the cursor.
            var parentRect = Panel.parent as RectTransform;
            if (parentRect != null
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, popupCenter, eventCamera, out var localCenter))
            {
                Panel.anchoredPosition = localCenter;
            }
            else
            {
                Panel.position = new Vector3(popupCenter.x, popupCenter.y, 0f);
            }
        }

        Vector2 ScreenSize(Camera eventCamera)
        {
            var corners = new Vector3[4];
            Panel.GetWorldCorners(corners);
            var bottomLeft = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
            var topRight = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[2]);
            return new Vector2(Mathf.Abs(topRight.x - bottomLeft.x), Mathf.Abs(topRight.y - bottomLeft.y));
        }

        static Rect PopupRect(Vector2 mouseTopLeftPosition, float width, float height)
        {
            // Preferred: above and to the right of the cursor.
            float x = mouseTopLeftPosition.x + MouseGap;
            float y = mouseTopLeftPosition.y - height - MouseGap;

            // If the right edge would leave the screen, mirror to the left of the cursor.
            if (x + width > Screen.width - Margin)
                x = mouseTopLeftPosition.x - width - MouseGap;

            // If there is no room above, place it below the cursor.
            if (y < Margin)
                y = mouseTopLeftPosition.y + MouseGap;

            x = Mathf.Clamp(x, Margin, Mathf.Max(Margin, Screen.width - width - Margin));
            y = Mathf.Clamp(y, Margin, Mathf.Max(Margin, Screen.height - height - Margin));
            return new Rect(x, y, width, height);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_instance == this) _instance = null;
        }
    }
}
