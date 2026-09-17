using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Prototype.Application;

namespace Prototype.Editor
{
    /// <summary>
    /// One-shot scene authoring command used to migrate the prototype UI from runtime-created
    /// controls to an explicit, inspectable hierarchy. Delete this file after the migration if
    /// the project no longer needs the menu command.
    /// </summary>
    public static class AuthorUiHierarchy
    {
        const string ScenePath = "Assets/_Prototype/Scenes/Prototype_Main.unity";
        static readonly Color TextColor = new Color32(42, 30, 20, 255);
        // Flat light-grey (not pure white) so buttons still read as distinct clickable elements
        // against the shop's now all-white/near-white panels, without any border sprite.
        static readonly Color ButtonColor = new Color32(230, 230, 230, 255);

        /// <summary>
        /// Idempotent: reuses each named child if BuildSeedShop already ran, otherwise creates it —
        /// safe to re-run any number of times, unlike the original version of this method (which
        /// unconditionally created every element, so a second run stacked duplicates).
        ///
        /// Layout numbers below were tuned against a live screenshot (the very first run had never
        /// been visually checked — `_shopPopup` etc. were null until this method's first successful
        /// run, so nobody had ever actually seen this popup render): DetailIcon used to sit at a
        /// pivot-mismatched offset that put DetailTitle up in the popup's own TitleText row, and
        /// overlapped DetailText; the two seed buttons overflowed below ItemListUI's own bottom edge;
        /// FeedbackText overlapped PurchaseUI. FeedbackText also now lives inside ItemDetailUI (under
        /// the description) instead of the popup's bottom edge, freeing that whole strip for
        /// Purchase/Close alone.
        /// </summary>
        public static void BuildSeedShop()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var shop = Object.FindAnyObjectByType<SeedShopUI>();
            if (shop == null) throw new System.InvalidOperationException("SeedShopUI was not found in Prototype_Main.");

            var popup = Child(shop.transform, "SeedShopPopup");
            if (popup == null) throw new System.InvalidOperationException("SeedShopPopup was not found under SeedShopUI.");
            popup.gameObject.SetActive(false);
            SetRect((RectTransform)popup, new Vector2(760f, 500f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero);

            DeactivateLegacyChild(popup, "Title");
            DeactivateLegacyChild(popup, "TurnipSeed");
            DeactivateLegacyChild(popup, "PotatoSeed");
            // FeedbackText used to be a direct child of the popup; it now lives under ItemDetailUI
            // (see below). Clean up the stale one so re-running this method against an older-shaped
            // scene doesn't leave an orphaned duplicate.
            var strayFeedback = Child(popup, "FeedbackText");
            if (strayFeedback != null) Object.DestroyImmediate(strayFeedback.gameObject);

            // Every sub-panel/button used to share DialogueBox.png (a one-off NPC-dialogue composite
            // image with no 9-slice border — Sliced mode had nothing to slice by, so each panel just
            // stretched the whole picture to its own size, nesting distorted copies of it inside each
            // other — the "doubled background" look). Switching to a proper 9-slice sprite fixed the
            // distortion, but per direct request the shop now goes flat: no background sprite at all,
            // just plain white/near-white Image fills. `frame = null` below makes every EnsurePanel/
            // EnsureButton call render Type.Simple with no sprite — only the flat colour shows.
            Sprite frame = null;
            var popupImage = popup.GetComponent<Image>();
            if (popupImage != null)
            {
                popupImage.type = UnityEngine.UI.Image.Type.Simple;
                popupImage.sprite = frame;
                popupImage.color = Color.white;
            }

            var title = EnsureText(popup, "TitleText", "SeedShop", 28, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(700f, 52f), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -18f));

            // Slightly different near-white shades per nesting level so panel boundaries are still
            // visible to the eye without any border art.
            var content = EnsurePanel(popup, "ContentUI", new Vector2(700f, 360f), new Vector2(0f, -22f), Color.white, frame);
            var list = EnsurePanel(content.transform, "ItemListUI", new Vector2(300f, 330f), new Vector2(-190f, 0f), new Color32(245, 245, 245, 255), frame);
            var listHeader = EnsureText(list.transform, "ListHeaderText", "Seeds", 20, TextAnchor.MiddleLeft);
            SetRect(listHeader.rectTransform, new Vector2(260f, 36f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -24f));
            // One authored, initially-hidden button; SeedShopUI clones it at runtime, one clone per
            // entry in State.MasterData.Crops — adding a crop to Master Data is enough for it to show
            // up in the shop, no second hand-authored button per crop.
            var itemTemplate = EnsureButton(list.transform, "ItemButtonTemplate", "Item", new Vector2(20f, 70f), frame);
            itemTemplate.gameObject.SetActive(false);
            // Older scenes authored one button per crop by hand — remove the leftover so it doesn't
            // sit in the hierarchy as an orphaned, unreferenced duplicate.
            var stalePotato = Child(list.transform, "PotatoSeedButton");
            if (stalePotato != null) Object.DestroyImmediate(stalePotato.gameObject);

            var detail = EnsurePanel(content.transform, "ItemDetailUI", new Vector2(340f, 250f), new Vector2(180f, 40f), new Color32(245, 245, 245, 255), frame);
            // Icon top-left, title beside it on the same row, description below both — the original
            // (icon centred at (52,58), title pinned to the panel's own top-right corner) put the
            // icon on top of both the title and the description text.
            var detailIcon = EnsureImage(detail.transform, "DetailIcon", new Vector2(64f, 64f), new Vector2(-115f, 78f));
            var detailTitle = EnsureText(detail.transform, "DetailTitle", string.Empty, 20, TextAnchor.MiddleLeft);
            SetRect(detailTitle.rectTransform, new Vector2(190f, 36f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(25f, 78f));
            var detailText = EnsureText(detail.transform, "DetailText", string.Empty, 16, TextAnchor.UpperLeft);
            detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            detailText.verticalOverflow = VerticalWrapMode.Overflow;
            SetRect(detailText.rectTransform, new Vector2(320f, 130f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -40f));
            // Purchase feedback ("Bought Turnip seed" / failure messages) lives here, under the
            // description, instead of the popup's bottom edge — that strip overlapped PurchaseUI.
            var feedback = EnsureText(detail.transform, "FeedbackText", string.Empty, 16, TextAnchor.MiddleCenter);
            feedback.fontStyle = FontStyle.BoldAndItalic;
            // The default dark-brown TextColor (near-black) was empirically invisible against this
            // panel's background at this position — confirmed by swapping to red, which showed up
            // immediately. Use a deep red instead: still reads as a shop notice, not this UI's body
            // text colour, and has enough contrast to actually render here.
            feedback.color = new Color32(150, 20, 20, 255);
            // VerticalWrapMode defaults to Truncate: a 15pt line needs more than the 20-unit-tall box
            // this used to have (~10px at this canvas's ~0.54x scale) — shorter than one line height,
            // so every character was truncated away and nothing ever rendered despite the Text
            // component reporting a correct, non-empty .text value. Overflow + a taller box fixes it.
            feedback.verticalOverflow = VerticalWrapMode.Overflow;
            feedback.horizontalOverflow = HorizontalWrapMode.Overflow;
            SetRect(feedback.rectTransform, new Vector2(320f, 32f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -110f));

            var purchase = EnsurePanel(content.transform, "PurchaseUI", new Vector2(340f, 70f), new Vector2(180f, -125f), new Color32(245, 245, 245, 255), frame);
            var price = EnsureText(purchase.transform, "PriceText", string.Empty, 18, TextAnchor.MiddleLeft);
            SetRect(price.rectTransform, new Vector2(200f, 50f), new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(14f, 0f));
            var buy = EnsureButton(purchase.transform, "BuyButton", "Buy", new Vector2(-14f, 0f), frame);
            SetRect(buy.GetComponent<RectTransform>(), new Vector2(95f, 44f), new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-14f, 0f));

            var close = EnsureButton(popup, "CloseButton", "Close", new Vector2(-18f, 20f), frame);
            SetRect(close.GetComponent<RectTransform>(), new Vector2(95f, 38f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 20f));

            var serialized = new SerializedObject(shop);
            serialized.FindProperty("_shopPopup").objectReferenceValue = popup;
            serialized.FindProperty("_titleText").objectReferenceValue = title;
            serialized.FindProperty("_itemButtonTemplate").objectReferenceValue = itemTemplate;
            serialized.FindProperty("_detailPanel").objectReferenceValue = detail;
            serialized.FindProperty("_detailIcon").objectReferenceValue = detailIcon;
            serialized.FindProperty("_detailTitle").objectReferenceValue = detailTitle;
            serialized.FindProperty("_detailText").objectReferenceValue = detailText;
            serialized.FindProperty("_purchasePanel").objectReferenceValue = purchase;
            serialized.FindProperty("_priceText").objectReferenceValue = price;
            serialized.FindProperty("_buyButton").objectReferenceValue = buy;
            serialized.FindProperty("_feedbackText").objectReferenceValue = feedback;
            serialized.FindProperty("_closeButton").objectReferenceValue = close;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssignPopupParents();
            RenameUiRoots();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("AuthorUiHierarchy: editor-authored UI hierarchy saved.");
        }

        static Text EnsureText(Transform parent, string name, string value, int size, TextAnchor alignment)
        {
            var existing = Child(parent, name)?.GetComponent<Text>();
            if (existing != null)
            {
                existing.text = value;
                existing.fontSize = size;
                existing.alignment = alignment;
                return existing;
            }
            return Text(parent, name, value, size, alignment);
        }

        static Image EnsureImage(Transform parent, string name, Vector2 size, Vector2 position)
        {
            var existing = Child(parent, name)?.GetComponent<Image>();
            if (existing != null)
            {
                SetRect(existing.rectTransform, size, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position);
                return existing;
            }
            return CreateImage(parent, name, size, position);
        }

        static Image EnsurePanel(Transform parent, string name, Vector2 size, Vector2 position, Color color, Sprite sprite)
        {
            var existing = Child(parent, name)?.GetComponent<Image>();
            if (existing != null)
            {
                SetRect(existing.rectTransform, size, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position);
                existing.color = color;
                existing.sprite = sprite;
                existing.type = sprite != null ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
                return existing;
            }
            return Panel(parent, name, size, position, color, sprite);
        }

        static Button EnsureButton(Transform parent, string name, string label, Vector2 position, Sprite sprite)
        {
            var existing = Child(parent, name)?.GetComponent<Button>();
            if (existing != null)
            {
                SetRect(existing.GetComponent<RectTransform>(), new Vector2(270f, 54f), new Vector2(0f, .5f), new Vector2(0f, .5f), position);
                var label_ = existing.GetComponentInChildren<Text>(true);
                if (label_ != null) label_.text = label;
                var image_ = existing.GetComponent<Image>();
                if (image_ != null)
                {
                    image_.sprite = sprite;
                    image_.type = sprite != null ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
                    image_.color = ButtonColor;
                }
                return existing;
            }
            return Button(parent, name, label, position, sprite);
        }

        /// <summary>
        /// Lays out all 48 slots (Inventory.SlotCount = Columns(12) * Rows(4)) to pixel-perfect match
        /// InventoryPlayer.png, the panel's own frame art — a 259x100 source image (analysed by
        /// sampling its pixel data: 12 columns, a 1-row "hotbar" strip on top of a 3-row block below,
        /// 21px cell pitch, 20px cell, 1px divider). The rest of this UI already renders that same art
        /// pack at an exact 3x scale (NavigationBar's 60x60/63px-pitch toolbar slots use the matching
        /// InventoryBar.png at the same native pitch), so the inventory grid uses that same 3x scale
        /// for a pixel-perfect, non-distorted match: panel 777x300, slot 60x60, icon 58x58. An earlier
        /// version of this script used ad-hoc 48x48/68px-pitch numbers that didn't match the art at
        /// all (pre-dating this script, on the original Slot_01..40) — that produced a panel letterboxed
        /// away from its own slot positions and items that looked "detached" from the visible grid.
        /// Every run repositions every slot, so this is idempotent and safe to re-run.
        /// </summary>
        public static void BuildInventoryGrid()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var inventory = Object.FindAnyObjectByType<InventoryScreenUI>(FindObjectsInactive.Include);
            if (inventory == null) throw new System.InvalidOperationException("InventoryScreenUI was not found in Prototype_Main.");

            var content = Child(inventory.transform, "InventoryContentUI");
            if (content == null) throw new System.InvalidOperationException("InventoryContentUI was not found under InventoryScreenUI.");

            var contentRect = content.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(777f, 300f); // 259x100 native art at an exact 3x scale
            var contentImage = content.GetComponent<Image>();
            if (contentImage != null) contentImage.preserveAspect = true; // panel now matches the art's own aspect exactly

            const int columns = 12;
            const int totalSlots = 48;
            // Native pixel-sampled cell centres (see class doc), scaled 3x. Column pitch is uniform
            // (63px); rows are NOT uniform — the hotbar-strip row sits 90px above row 1, but rows 1-3
            // within the "backpack" block below it are a uniform 63px apart.
            var colX = new[] { 40.5f, 103.5f, 166.5f, 229.5f, 292.5f, 355.5f, 418.5f, 481.5f, 544.5f, 607.5f, 670.5f, 733.5f };
            var rowY = new[] { -43.5f, -133.5f, -196.5f, -259.5f };

            var referenceSlot = Child(content, "Slot_01");
            var referenceImage = referenceSlot != null ? referenceSlot.GetComponent<Image>() : null;
            for (int i = 0; i < totalSlots; i++)
            {
                // Idempotent: reuse a slot this method already created instead of stacking a second
                // copy at the same position — an earlier version of this loop (before this existence
                // check existed) did that for Slot_41..48, leaving stale duplicates behind.
                var slotName = $"Slot_{i + 1:00}";
                var slot = DestroyDuplicatesAndKeepLast(content, slotName);
                if (slot == null) slot = CreateSlot(content, slotName, Vector2.zero, referenceImage);

                var rect = slot.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(60f, 60f);
                rect.anchoredPosition = new Vector2(colX[i % columns], rowY[i / columns]);
                AugmentSlot(slot, i);
            }

            // Drag ghost: one Image that follows the cursor while an item is being dragged between
            // slots. Parented directly under InventoryPopup (the popup root) so it renders above
            // every slot regardless of which one started the drag.
            var ghost = DestroyDuplicatesAndKeepLast(inventory.transform, "DragGhost")?.GetComponent<Image>();
            if (ghost == null) ghost = CreateImage(inventory.transform, "DragGhost", new Vector2(60f, 60f), Vector2.zero);
            ghost.raycastTarget = false;
            ghost.preserveAspect = true;
            ghost.gameObject.SetActive(false);

            var serializedInventory = new SerializedObject(inventory);
            serializedInventory.FindProperty("_dragGhost").objectReferenceValue = ghost;
            serializedInventory.ApplyModifiedPropertiesWithoutUndo();

            AssignPopupParents();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("AuthorUiHierarchy: inventory grid (48 slots, pixel-perfect 3x layout) authored and saved.");
        }

        static void AugmentSlot(Transform slotTransform, int slotIndex)
        {
            // Every Slot_NN's own Image (pre-dating this script, on Slot_01..40) was wired to
            // InventoryHighlight.png — a selection-ring sprite, not a slot frame — so it rendered as
            // a permanent bright outline over all 48 cells, hiding the real grid art (InventoryPlayer.png,
            // already correctly set as InventoryContentUI's own background). Keep the Image (Raycast
            // Target for hover/drag still needs one) but make it fully transparent so only the panel's
            // real frame art and this slot's Icon/Count show.
            var background = slotTransform.GetComponent<Image>();
            if (background != null) background.color = new Color(1f, 1f, 1f, 0f);

            var view = slotTransform.GetComponent<InventorySlotView>();
            if (view == null) view = slotTransform.gameObject.AddComponent<InventorySlotView>();
            view.SlotIndex = slotIndex;

            // 58x58 inside a 60x60 slot — matches NavigationBar's own Item icon sizing exactly
            // (toolbar Slot_01/Item is 58x58 in a 60x60 slot), so the inventory grid and the toolbar
            // read as the same visual system.
            var icon = Child(slotTransform, "Icon")?.GetComponent<Image>();
            if (icon == null)
            {
                icon = CreateImage(slotTransform, "Icon", new Vector2(58f, 58f), Vector2.zero);
                icon.raycastTarget = false;
                icon.preserveAspect = true;
                icon.enabled = false;
            }
            else
            {
                icon.rectTransform.sizeDelta = new Vector2(58f, 58f);
            }

            var count = Child(slotTransform, "Count")?.GetComponent<Text>();
            if (count == null)
            {
                count = Text(slotTransform, "Count", string.Empty, 12, TextAnchor.LowerRight);
                SetRect(count.rectTransform, new Vector2(54f, 18f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-2f, 2f));
            }

            var serializedSlot = new SerializedObject(view);
            serializedSlot.FindProperty("_icon").objectReferenceValue = icon;
            serializedSlot.FindProperty("_count").objectReferenceValue = count;
            serializedSlot.ApplyModifiedPropertiesWithoutUndo();
        }

        static Transform CreateSlot(Transform parent, string name, Vector2 position, Image referenceImage)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(60f, 60f);
            rect.anchoredPosition = position;

            var image = go.GetComponent<UnityEngine.UI.Image>();
            if (referenceImage != null)
            {
                image.sprite = referenceImage.sprite;
                image.type = referenceImage.type;
                image.color = referenceImage.color;
                image.raycastTarget = referenceImage.raycastTarget;
            }
            return go.transform;
        }

        static void AssignPopupParents()
        {
            var popupParent = Object.FindAnyObjectByType<PopupParent>();
            if (popupParent == null) return;
            foreach (var popup in Object.FindObjectsByType<PopupBase>(FindObjectsInactive.Include))
            {
                var serialized = new SerializedObject(popup);
                var property = serialized.FindProperty("popupParent");
                if (property != null) property.objectReferenceValue = popupParent;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void RenameUiRoots()
        {
            var npc = Object.FindAnyObjectByType<NpcDialogueController>();
            if (npc != null) npc.gameObject.name = "NpcDialogueUI";
        }

        static Transform Child(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name == name) return parent.GetChild(i);
            return null;
        }

        /// <summary>
        /// Finds every direct child named `name`, destroys all but the most recently added one (the
        /// last sibling — the one a follow-up run of this script would have configured most recently)
        /// and returns it, or null if none exist. Makes an authoring step safe to re-run even after
        /// an earlier run already created duplicates under the old (non-idempotent) code path.
        /// </summary>
        static Transform DestroyDuplicatesAndKeepLast(Transform parent, string name)
        {
            var matches = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name == name) matches.Add(parent.GetChild(i));
            if (matches.Count == 0) return null;

            for (int i = 0; i < matches.Count - 1; i++)
                Object.DestroyImmediate(matches[i].gameObject);
            return matches[matches.Count - 1];
        }

        static void DeactivateLegacyChild(Transform parent, string name)
        {
            var child = Child(parent, name);
            if (child != null) child.gameObject.SetActive(false);
        }

        static void SetRect(RectTransform rect, Vector2 size, Vector2 min, Vector2 max, Vector2 position)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2((min.x + max.x) * .5f, (min.y + max.y) * .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        static Image Panel(Transform parent, string name, Vector2 size, Vector2 position, Color color, Sprite sprite)
        {
            var image = CreateImage(parent, name, size, position);
            image.color = color;
            image.sprite = sprite;
            image.type = sprite != null ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            return image;
        }

        static Image CreateImage(Transform parent, string name, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            SetRect(rect, size, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position);
            return go.GetComponent<UnityEngine.UI.Image>();
        }

        static Text Text(Transform parent, string name, string value, int size, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = TextColor;
            text.raycastTarget = false;
            // Arial.ttf was removed from the built-in font set in newer Unity versions; the engine's
            // own drop-in replacement is LegacyRuntime.ttf (same default uGUI Text() font, new name).
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
        }

        static Button Button(Transform parent, string name, string label, Vector2 position, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            SetRect(rect, new Vector2(270f, 54f), new Vector2(0f, .5f), new Vector2(0f, .5f), position);
            var image = go.GetComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.type = sprite != null ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.color = ButtonColor;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var text = Text(go.transform, "Label", label, 16, TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero);
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return button;
        }
    }
}
