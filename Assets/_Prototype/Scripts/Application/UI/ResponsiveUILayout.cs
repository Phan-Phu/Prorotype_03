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

        public const float DebugNativeW = 200f;
        public const float DebugNativeH = 476f;

        public static Rect DebugPanelRect(int screenW, int screenH)
        {
            float scale = DebugPanelScale(screenW, screenH);
            float w = DebugNativeW * scale;
            float h = DebugNativeH * scale;
            return new Rect(screenW - w - SafeMargin, SafeMargin, w, h);
        }

        public static float DebugPanelScale(int screenW, int screenH) => FitScale(DebugNativeW, DebugNativeH, screenW, screenH, 1f);

        static float FitScale(float nativeW, float nativeH, int screenW, int screenH, float maxScale)
        {
            float maxW = Mathf.Max(1f, screenW - SafeMargin * 2f);
            float maxH = Mathf.Max(1f, screenH - SafeMargin * 2f);
            return Mathf.Min(maxScale, maxW / nativeW, maxH / nativeH);
        }
    }
}
