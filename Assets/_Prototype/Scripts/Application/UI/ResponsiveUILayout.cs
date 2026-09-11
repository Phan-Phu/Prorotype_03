using UnityEngine;

namespace Prototype.Application
{
    /// <summary>
    /// Shared IMGUI layout math for runtime prototype UI. Kept plain/static so EditMode tests can
    /// assert packaged-build screen sizes without booting a scene.
    /// </summary>
    public static class ResponsiveUILayout
    {
        public const float SafeMargin = 8f;
        public const float InventoryMaxScale = 3f;

        public const float InventoryNativeW = 259f;
        public const float InventoryNativeH = 100f;
        public const float DebugNativeW = 200f;
        public const float DebugNativeH = 476f;

        public static Rect InventoryPanelRect(int screenW, int screenH)
        {
            float scale = FitScale(InventoryNativeW, InventoryNativeH, screenW, screenH, InventoryMaxScale);
            return CenteredRect(screenW, screenH, InventoryNativeW * scale, InventoryNativeH * scale);
        }

        public static Rect DebugPanelRect(int screenW, int screenH)
        {
            float scale = DebugPanelScale(screenW, screenH);
            float w = DebugNativeW * scale;
            float h = DebugNativeH * scale;
            return new Rect(screenW - w - SafeMargin, SafeMargin, w, h);
        }

        public static float InventoryScale(int screenW, int screenH) => FitScale(InventoryNativeW, InventoryNativeH, screenW, screenH, InventoryMaxScale);
        public static float DebugPanelScale(int screenW, int screenH) => FitScale(DebugNativeW, DebugNativeH, screenW, screenH, 1f);

        static float FitScale(float nativeW, float nativeH, int screenW, int screenH, float maxScale)
        {
            float maxW = Mathf.Max(1f, screenW - SafeMargin * 2f);
            float maxH = Mathf.Max(1f, screenH - SafeMargin * 2f);
            return Mathf.Min(maxScale, maxW / nativeW, maxH / nativeH);
        }

        static Rect CenteredRect(int screenW, int screenH, float w, float h)
            => new Rect((screenW - w) * 0.5f, (screenH - h) * 0.5f, w, h);
    }
}
