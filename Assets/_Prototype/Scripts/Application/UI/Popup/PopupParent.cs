using System.Collections.Generic;
using UnityEngine;

namespace Prototype.Application
{
    /// <summary>
    /// Popup stack owned by the editor-authored PopupParent GameObject.
    /// Popups share this layer and the active popup is always moved to the last sibling.
    /// </summary>
    public sealed class PopupParent : MonoBehaviour
    {
        static PopupParent _instance;
        readonly List<PopupBase> _popups = new List<PopupBase>();

        public static PopupParent Instance => _instance != null ? _instance : FindAnyObjectByType<PopupParent>();

        void Awake()
        {
            _instance = this;
            RegisterChildren();
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        public void Register(PopupBase popup)
        {
            if (popup != null && !_popups.Contains(popup))
                _popups.Add(popup);
        }

        public void Unregister(PopupBase popup) => _popups.Remove(popup);

        /// <summary>Shows one popup and places it last in the hierarchy so it renders above earlier popups.</summary>
        public void Show(PopupBase popup)
        {
            if (popup == null) return;
            Register(popup);
            if (popup.IsOpen)
            {
                popup.transform.SetAsLastSibling();
                return;
            }
            popup.transform.SetAsLastSibling();
            popup.ShowFromParent(this);
        }

        public void Hide(PopupBase popup)
        {
            if (popup != null) popup.HideFromParent();
        }

        /// <summary>Closes every popup registered under this PopupParent.</summary>
        public void HideAll()
        {
            foreach (var popup in _popups.ToArray())
                if (popup != null) popup.HideFromParent();
        }

        void RegisterChildren()
        {
            _popups.Clear();
            foreach (var popup in GetComponentsInChildren<PopupBase>(true))
                Register(popup);
        }
    }
}
