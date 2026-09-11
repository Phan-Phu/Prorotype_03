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
        public const float ToolbarMaxScale = 3f;
        public const float InventoryMaxScale = 3f;

        public const float ToolbarNativeW = 259f;
        public const float ToolbarNativeH = 28f;
        public const float InventoryNativeW = 259f;
        public const float InventoryNativeH = 100f;
        public const float SeedShopNativeW = 390f;
        public const float SeedShopNativeH = 425f;
        public const float DebugNativeW = 200f;
        public const float DebugNativeH = 476f;
        public const float HudNativeW = 240f;
        public const float HudNativeH = 56f;

        public static Rect ToolbarRect(int screenW, int screenH)
        {
            float scale = FitScale(ToolbarNativeW, ToolbarNativeH, screenW, screenH, ToolbarMaxScale);
            float w = ToolbarNativeW * scale;
            float h = ToolbarNativeH * scale;
            return new Rect((screenW - w) * 0.5f, screenH - h - SafeMargin, w, h);
        }

        public static Rect InventoryPanelRect(int screenW, int screenH)
        {
            float scale = FitScale(InventoryNativeW, InventoryNativeH, screenW, screenH, InventoryMaxScale);
            return CenteredRect(screenW, screenH, InventoryNativeW * scale, InventoryNativeH * scale);
        }

        public static Rect SeedShopPanelRect(int screenW, int screenH)
        {
            float scale = SeedShopScale(screenW, screenH);
            return CenteredRect(screenW, screenH, SeedShopNativeW * scale, SeedShopNativeH * scale);
        }

        public static Rect DebugPanelRect(int screenW, int screenH)
        {
            float scale = DebugPanelScale(screenW, screenH);
            float w = DebugNativeW * scale;
            float h = DebugNativeH * scale;
            return new Rect(screenW - w - SafeMargin, SafeMargin, w, h);
        }

        public static Rect HudRect(int screenW, int screenH)
        {
            float scale = HudScale(screenW, screenH);
            return new Rect(screenW - HudNativeW * scale - SafeMargin, SafeMargin, HudNativeW * scale, HudNativeH * scale);
        }

        public static Rect NpcPromptRect(int screenW, int screenH)
        {
            float w = Mathf.Min(260f, Mathf.Max(120f, screenW - SafeMargin * 2f));
            float h = 32f;
            float y = Mathf.Clamp(screenH - 118f, SafeMargin, Mathf.Max(SafeMargin, screenH - h - SafeMargin));
            return new Rect((screenW - w) * 0.5f, y, w, h);
        }

        public static Rect DialogueBoxRect(int screenW, int screenH)
        {
            float margin = Mathf.Clamp(screenW * 0.05625f, 12f, 36f);
            float h = Mathf.Min(150f, Mathf.Max(96f, screenH - SafeMargin * 2f));
            float y = Mathf.Max(SafeMargin, screenH - h - margin);
            return new Rect(margin, y, Mathf.Max(1f, screenW - margin * 2f), h);
        }

        public static float ToolbarScale(int screenW, int screenH) => FitScale(ToolbarNativeW, ToolbarNativeH, screenW, screenH, ToolbarMaxScale);
        public static float InventoryScale(int screenW, int screenH) => FitScale(InventoryNativeW, InventoryNativeH, screenW, screenH, InventoryMaxScale);
        public static float SeedShopScale(int screenW, int screenH) => FitScale(SeedShopNativeW, SeedShopNativeH, screenW, screenH, 1f);
        public static float DebugPanelScale(int screenW, int screenH) => FitScale(DebugNativeW, DebugNativeH, screenW, screenH, 1f);
        public static float HudScale(int screenW, int screenH)
        {
            // QA-060: at 640-wide windows the 240px HUD covered too much of the farm field.
            // Keep the full-size panel at normal desktop sizes, but shrink it on low widths.
            float lowWidthScale = Mathf.Clamp(screenW / 900f, 0.7f, 1f);
            return Mathf.Min(lowWidthScale, FitScale(HudNativeW, HudNativeH, screenW, screenH, 1f));
        }

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
