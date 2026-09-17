using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace Prototype.Application
{
    public enum PopupEase
    {
        EaseOutBack,
        EaseInBack
    }

    /// <summary>
    /// Base behaviour for editor-authored popups. Concrete popups provide content and call
    /// ShowPopup/HidePopup; PopupParent owns stacking and HideAll.
    /// </summary>
    public abstract class PopupBase : MonoBehaviour
    {
        [SerializeField] PopupParent popupParent;
        [SerializeField] float showDuration = 0.18f;
        [SerializeField] float hideDuration = 0.12f;
        [SerializeField] PopupEase showEase = PopupEase.EaseOutBack;
        [SerializeField] PopupEase hideEase = PopupEase.EaseInBack;

        RectTransform _target;
        Vector3 _visibleScale = Vector3.one;
        Coroutine _fallbackTween;
        bool _isOpen;

        public bool IsOpen => _isOpen;
        public PopupParent Parent => popupParent != null ? popupParent : PopupParent.Instance;

        /// <summary>
        /// Most popups animate their own RectTransform. A legacy IMGUI popup can opt out while
        /// still participating in PopupParent's ordering and open/close lifecycle.
        /// </summary>
        protected virtual bool UsesVisualTarget => true;

        protected virtual void Awake()
        {
            _target = ResolvePopupTarget();
            if (_target == null) _target = transform as RectTransform;
            popupParent = popupParent != null ? popupParent : GetComponentInParent<PopupParent>();
            popupParent?.Register(this);
            if (UsesVisualTarget && _target != null)
            {
                _visibleScale = _target.localScale;
                HideImmediate();
            }
        }

        protected virtual RectTransform ResolvePopupTarget() => transform as RectTransform;

        public void ShowPopup()
        {
            var owner = Parent;
            if (owner != null)
            {
                owner.Show(this);
                return;
            }
            ShowFromParent(null);
        }

        public void HidePopup() => Parent?.Hide(this);

        public void HideAllPopups() => Parent?.HideAll();

        internal void ShowFromParent(PopupParent owner)
        {
            if (owner != null) popupParent = owner;
            if (_target == null) _target = ResolvePopupTarget();

            if (!UsesVisualTarget)
            {
                _isOpen = true;
                OnPopupShown();
                return;
            }
            if (_target == null) return;

            StopTween();
            _target.gameObject.SetActive(true);
            _target.localScale = Vector3.zero;
            _isOpen = true;
            OnPopupShown();
            if (!TryRunITween(_target, _visibleScale, showDuration, showEase, false))
                _fallbackTween = StartCoroutine(AnimateScale(_visibleScale, showDuration, false));
        }

        internal void HideFromParent()
        {
            if (!_isOpen) return;
            if (_target == null) _target = ResolvePopupTarget();

            if (!UsesVisualTarget)
            {
                _isOpen = false;
                OnPopupHidden();
                return;
            }
            if (_target == null) return;

            StopTween();
            if (!_target.gameObject.activeSelf)
            {
                HideImmediate();
                return;
            }

            _isOpen = false;
            OnPopupHidden();
            if (!TryRunITween(_target, Vector3.zero, hideDuration, hideEase, true))
                _fallbackTween = StartCoroutine(AnimateScale(Vector3.zero, hideDuration, true));
        }

        /// <summary>Called by iTween's oncomplete callback and safe to call from the fallback tween.</summary>
        public void HideImmediate()
        {
            StopTween();
            if (!UsesVisualTarget)
            {
                _isOpen = false;
                OnPopupHidden();
                return;
            }
            if (_target == null) return;
            _target.localScale = Vector3.zero;
            _target.gameObject.SetActive(false);
            _isOpen = false;
            OnPopupHidden();
        }

        protected virtual void OnPopupShown() { }
        protected virtual void OnPopupHidden() { }

        IEnumerator AnimateScale(Vector3 destination, float duration, bool hideAtEnd)
        {
            Vector3 start = _target.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                _target.localScale = Vector3.LerpUnclamped(start, destination, Ease(t, hideAtEnd ? hideEase : showEase));
                yield return null;
            }

            _target.localScale = destination;
            _fallbackTween = null;
            if (hideAtEnd) _target.gameObject.SetActive(false);
        }

        static float Ease(float t, PopupEase ease)
        {
            if (ease == PopupEase.EaseInBack)
            {
                const float c1 = 1.70158f;
                const float c3 = c1 + 1f;
                return c3 * t * t * t - c1 * t * t;
            }

            const float outC1 = 1.70158f;
            const float outC3 = outC1 + 1f;
            float shifted = t - 1f;
            return 1f + outC3 * shifted * shifted * shifted + outC1 * shifted * shifted;
        }

        void StopTween()
        {
            if (_fallbackTween != null)
            {
                StopCoroutine(_fallbackTween);
                _fallbackTween = null;
            }
            if (_target != null) TryStopITween(_target.gameObject);
        }

        static bool TryRunITween(RectTransform target, Vector3 scale, float duration, PopupEase ease, bool hideAtEnd)
        {
            var type = FindITweenType();
            if (type == null) return false;

            try
            {
                var hashMethod = type.GetMethod("Hash", BindingFlags.Public | BindingFlags.Static, null,
                    new[] { typeof(object[]) }, null);
                var scaleToMethod = type.GetMethod("ScaleTo", BindingFlags.Public | BindingFlags.Static, null,
                    new[] { typeof(GameObject), typeof(Hashtable) }, null);
                if (hashMethod == null || scaleToMethod == null) return false;

                object[] args = hideAtEnd
                    ? new object[] { "scale", scale, "time", duration, "easetype", EaseName(ease),
                        "oncomplete", nameof(HideImmediate), "oncompletetarget", target.GetComponentInParent<PopupBase>().gameObject }
                    : new object[] { "scale", scale, "time", duration, "easetype", EaseName(ease) };
                var hash = hashMethod.Invoke(null, new object[] { args });
                scaleToMethod.Invoke(null, new[] { target.gameObject, hash });
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"iTween popup animation unavailable; using fallback tween. {exception.Message}");
                return false;
            }
        }

        static void TryStopITween(GameObject target)
        {
            var type = FindITweenType();
            if (type == null) return;
            try
            {
                var method = type.GetMethod("Stop", BindingFlags.Public | BindingFlags.Static, null,
                    new[] { typeof(GameObject) }, null);
                method?.Invoke(null, new object[] { target });
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Unable to stop iTween popup animation. {exception.Message}");
            }
        }

        static Type FindITweenType()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("iTween", false);
                if (type != null) return type;
            }
            return null;
        }

        static string EaseName(PopupEase ease) => ease == PopupEase.EaseInBack ? "easeInBack" : "easeOutBack";

        protected virtual void OnDestroy()
        {
            StopTween();
            popupParent?.Unregister(this);
        }
    }
}
