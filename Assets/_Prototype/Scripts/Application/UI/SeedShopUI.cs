using Prototype.Domain;
using Prototype.Application;
using UnityEngine;
using UnityEngine.UI;

namespace Prototype.Application
{
    /// <summary>UGUI seed shop: item list on the left, detail and purchase area on the right.</summary>
    public sealed class SeedShopUI : MonoBehaviour
    {
        public static SeedShopUI Instance { get; private set; }
        public static bool PointerOverUI { get; private set; }
        public static bool IsOpen => Instance != null && Instance._open;

        public GameState State;
        public IGameplayService GameplayService;
        public IInventoryService InventoryService;
        public PlayerController Player;
        public string LastFeedback => _feedbackText != null ? _feedbackText.text : string.Empty;

        bool _open;
        CropId? _selectedCrop;
        GameObject _panel;
        Text _detailTitle, _detailText, _priceText, _feedbackText;
        Button _buyButton;

        void Awake()
        {
            Instance = this;
            BuildSceneUi();
        }

        void OnDestroy()
        {
            UnlockGameplay();
            if (Instance == this) Instance = null;
        }

        void OnDisable()
        {
            UnlockGameplay();
            if (_panel != null) _panel.SetActive(false);
        }

        void Update()
        {
            if (_open && Input.GetKeyDown(KeyCode.Escape)) CloseShop();
            if (_open) RefreshUi();
        }

        public static void OpenCurrent() => Instance?.OpenShop();

        public void OpenShop()
        {
            _open = true;
            _selectedCrop = null;
            if (_panel != null) _panel.SetActive(true);
            Player?.SetGameplayLocked(true, "seed-shop");
            if (_feedbackText != null) _feedbackText.text = string.Empty;
            RefreshUi();
            SessionLogger.LogShopOpen(State);
        }

        public void CloseShop()
        {
            _open = false;
            _selectedCrop = null;
            if (_panel != null) _panel.SetActive(false);
            UnlockGameplay();
            PointerOverUI = false;
        }

        void UnlockGameplay() => Player?.SetGameplayLocked(false, "seed-shop");

        void BuildSceneUi()
        {
            var panel = transform.Find("ShopPanel");
            if (panel == null) return;
            _panel = panel.gameObject;
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(760, 500);

            // These three objects are remnants of the old placeholder shop layout.
            // Keep them in the scene for backward compatibility, but do not let them
            // overlap the authored UGUI layout below.
            SetLegacyPlaceholderInactive(panel, "Title");
            SetLegacyPlaceholderInactive(panel, "TurnipSeed");
            SetLegacyPlaceholderInactive(panel, "PotatoSeed");

            var title = EnsureText(panel, "TitleText", "SeedShop", 24, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(760, 48), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -24));

            var list = EnsurePanel(panel, "ItemListPanel", new Vector2(300, 350), new Vector2(-210, -10));
            var listTitle = EnsureText(list.transform, "ListTitle", "Seeds", 18, TextAnchor.MiddleLeft);
            SetRect(listTitle.rectTransform, new Vector2(270, 32), new Vector2(0, 1), new Vector2(0, 1), new Vector2(15, -20));
            EnsureButton(list.transform, "TurnipSeedButton", "Turnip Seed   -   20g", new Vector2(15, -70), ListTurnipSeed);
            EnsureButton(list.transform, "PotatoSeedButton", "Potato Seed   -   50g", new Vector2(15, -135), ListPotatoSeed);

            var detail = EnsurePanel(panel, "DetailPanel", new Vector2(350, 270), new Vector2(185, 45));
            _detailTitle = EnsureText(detail.transform, "DetailTitle", string.Empty, 20, TextAnchor.MiddleCenter);
            SetRect(_detailTitle.rectTransform, new Vector2(320, 40), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -25));
            _detailText = EnsureText(detail.transform, "DetailText", string.Empty, 16, TextAnchor.UpperLeft);
            SetRect(_detailText.rectTransform, new Vector2(300, 150), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -10));

            var purchase = EnsurePanel(panel, "PurchasePanel", new Vector2(350, 70), new Vector2(185, -145));
            _priceText = EnsureText(purchase.transform, "PriceText", string.Empty, 18, TextAnchor.MiddleLeft);
            SetRect(_priceText.rectTransform, new Vector2(210, 50), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(15, 0));
            _buyButton = EnsureButton(purchase.transform, "BuyButton", "Buy", new Vector2(-15, 0), BuySelectedItem);
            SetRect(_buyButton.GetComponent<RectTransform>(), new Vector2(90, 44), new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-15, 0));

            _feedbackText = EnsureText(panel, "FeedbackText", string.Empty, 15, TextAnchor.MiddleCenter);
            SetRect(_feedbackText.rectTransform, new Vector2(500, 28), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 35));
            var close = EnsureButton(panel, "CloseButton", "Close", new Vector2(-15, 15), CloseShop);
            SetRect(close.GetComponent<RectTransform>(), new Vector2(90, 36), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-15, 15));
            _panel.SetActive(_open);
        }

        static void SetLegacyPlaceholderInactive(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null) child.gameObject.SetActive(false);
        }

        /// <summary>Rebuild/refresh the item list. Can be assigned to a UI Button or called by a presenter.</summary>
        public void ListItems()
        {
            BuildSceneUi();
            RefreshUi();
        }

        /// <summary>Select an item by seed item id, suitable for a UnityEvent(string).</summary>
        public void ListItem(string itemId) => ShowItemDetail(itemId);

        /// <summary>Select Turnip Seed from an Inspector Button.</summary>
        public void ListTurnipSeed() => ShowTurnipSeedDetail();

        /// <summary>Select Potato Seed from an Inspector Button.</summary>
        public void ListPotatoSeed() => ShowPotatoSeedDetail();

        /// <summary>Show detail for an item id, suitable for a UnityEvent(string).</summary>
        public void ShowItemDetail(string itemId)
        {
            if (itemId == CropDefinition.SeedItemId(CropId.Turnip))
                ShowTurnipSeedDetail();
            else if (itemId == CropDefinition.SeedItemId(CropId.Potato))
                ShowPotatoSeedDetail();
        }

        public void ShowTurnipSeedDetail() => Select(CropId.Turnip);
        public void ShowPotatoSeedDetail() => Select(CropId.Potato);

        void Select(CropId crop)
        {
            _selectedCrop = crop;
            if (_feedbackText != null) _feedbackText.text = string.Empty;
            RefreshUi();
        }

        /// <summary>Buy the currently selected item. Assign this to the Buy Button OnClick event.</summary>
        public void BuySelectedItem()
        {
            if (_selectedCrop.HasValue) Buy(_selectedCrop.Value);
        }

        /// <summary>Buy Turnip Seed directly from an Inspector Button.</summary>
        public void BuyTurnipItem() => Buy(CropId.Turnip);

        /// <summary>Buy Potato Seed directly from an Inspector Button.</summary>
        public void BuyPotatoItem() => Buy(CropId.Potato);

        void RefreshUi()
        {
            if (State == null || State.InventorySystem == null || _detailTitle == null) return;
            if (!_selectedCrop.HasValue)
            {
                _detailTitle.text = string.Empty;
                _detailText.text = string.Empty;
                _priceText.text = string.Empty;
                _buyButton.interactable = false;
            }
            else
            {
                var crop = _selectedCrop.Value;
                var seedId = CropDefinition.SeedItemId(crop);
                var price = CropDefinition.SeedPrice(crop);
                _detailTitle.text = $"{Label(crop)} Seed";
                int owned = InventoryService != null
                    ? InventoryService.Count(State.InventorySystem, seedId).GetAwaiter().GetResult().Value
                    : 0;
                _detailText.text = $"Plant this seed on prepared soil.\n\nPrice: {price}g\nOwned: {owned}";
                _priceText.text = $"{price}g   Money: {State.Wallet.Money}";
                _buyButton.interactable = true;
            }
            if (_feedbackText != null && !_open) _feedbackText.text = string.Empty;
            PointerOverUI = _open;
        }

        public ShopPurchaseResult Buy(CropId crop)
        {
            var operation = GameplayService != null
                ? GameplayService.BuySeed(State, crop).GetAwaiter().GetResult()
                : ResultFactory.Failure<GameplayFailure, ShopPurchaseDto>(GameplayFailure.NotInitialized("gameplay"));
            ShopPurchaseResult result;
            if (operation.IsSuccess)
            {
                var dto = operation.Value;
                result = new ShopPurchaseResult(dto.Code, dto.Crop, dto.ItemId, dto.Price,
                    dto.MoneyBefore, dto.MoneyAfter, dto.CountBefore, dto.CountAfter);
            }
            else
            {
                string itemId = CropDefinition.SeedItemId(crop);
                int price = CropDefinition.SeedPrice(crop);
                result = new ShopPurchaseResult(MapPurchaseCode(operation.Failure.Code), crop, itemId,
                    price, State.Wallet.Money, State.Wallet.Money, 0, 0);
            }
            SessionLogger.LogShopPurchase(State, result);
            _feedbackText.text = result.IsSuccess
                ? $"Bought {Label(crop)} seed"
                : PurchaseFailureMessage(operation.Failure.Code);
            RefreshUi();
            return result;
        }

        // Compatibility entry points retained for existing PlayMode tests and callers.
        // The player-facing shop remains the UGUI seed-buy flow above.
        public ShopPurchaseResult BuyTurnip() => Buy(CropId.Turnip);
        public ShopPurchaseResult BuyPotato() => Buy(CropId.Potato);

        public ShopSellResult SellWood()
        {
            int count = InventoryService != null
                ? InventoryService.Count(State.InventorySystem, TreeDefinition.WoodItemId).GetAwaiter().GetResult().Value
                : 0;
            return SellItem(TreeDefinition.WoodItemId, TreeDefinition.WoodSellPrice, count, "Wood");
        }

        public ShopSellResult SellTurnip() => SellCrop(CropId.Turnip);
        public ShopSellResult SellPotato() => SellCrop(CropId.Potato);

        public ShopSellResult SellCrop(CropId crop)
        {
            string itemId = CropDefinition.ProduceItemId(crop);
            int count = InventoryService != null
                ? InventoryService.Count(State.InventorySystem, itemId).GetAwaiter().GetResult().Value
                : 0;
            return SellItem(itemId, CropDefinition.SellPrice(crop), count, Label(crop));
        }

        public ShopSellResult SellItem(string itemId, int pricePerUnit, int count)
            => SellItem(itemId, pricePerUnit, count, itemId);

        ShopSellResult SellItem(string itemId, int pricePerUnit, int count, string displayName)
        {
            var operation = GameplayService != null
                ? GameplayService.SellItem(State, itemId, pricePerUnit, count).GetAwaiter().GetResult()
                : ResultFactory.Failure<GameplayFailure, ShopSellDto>(GameplayFailure.NotInitialized("gameplay"));
            ShopSellResult result;
            if (operation.IsSuccess)
            {
                var dto = operation.Value;
                result = new ShopSellResult(dto.Code, dto.ItemId, pricePerUnit, count, dto.Earned,
                    dto.MoneyBefore, dto.MoneyAfter, dto.CountBefore, dto.CountAfter);
            }
            else
            {
                result = new ShopSellResult(MapSellCode(operation.Failure.Code), itemId, pricePerUnit,
                    count, 0, State.Wallet.Money, State.Wallet.Money, 0, 0);
            }
            _feedbackText.text = result.IsSuccess
                ? $"Sold {displayName} for {result.Earned}g"
                : SellFailureMessage(operation.Failure.Code, displayName);
            return result;
        }

        static ShopPurchaseResultCode MapPurchaseCode(FailureCode code)
            => code == FailureCode.NotEnoughMoney
                ? ShopPurchaseResultCode.InsufficientFunds
                : code == FailureCode.InventoryFull || code == FailureCode.LockedSlot
                    ? ShopPurchaseResultCode.InventoryFull
                    : code == FailureCode.SystemError
                        ? ShopPurchaseResultCode.SystemError
                        : ShopPurchaseResultCode.InvalidItem;

        static ShopSellResultCode MapSellCode(FailureCode code)
            => code == FailureCode.InsufficientInventory || code == FailureCode.ItemNotFound
                ? ShopSellResultCode.EmptyInventory
                : code == FailureCode.SystemError
                    ? ShopSellResultCode.SystemError
                    : code == FailureCode.InvalidArgument
                        ? ShopSellResultCode.InvalidCount
                        : ShopSellResultCode.InvalidItem;

        static string PurchaseFailureMessage(FailureCode code)
        {
            switch (code)
            {
                case FailureCode.NotEnoughMoney: return "Not enough money";
                case FailureCode.InventoryFull:
                case FailureCode.LockedSlot: return "Inventory full";
                case FailureCode.SystemError: return "Shop system error";
                default: return "Unable to buy item";
            }
        }

        static string SellFailureMessage(FailureCode code, string displayName)
        {
            switch (code)
            {
                case FailureCode.InsufficientInventory:
                case FailureCode.ItemNotFound: return $"No {displayName} to sell";
                case FailureCode.SystemError: return "Shop system error";
                default: return $"Unable to sell {displayName}";
            }
        }

        Button EnsureButton(Transform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            var go = parent.Find(name)?.gameObject ?? new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            image.color = new Color32(238, 220, 190, 255);
            var button = go.GetComponent<Button>() ?? go.AddComponent<Button>();
            button.onClick.RemoveAllListeners(); button.onClick.AddListener(action);
            var text = EnsureText(go.transform, "Label", label, 16, TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero);
            text.rectTransform.offsetMin = Vector2.zero; text.rectTransform.offsetMax = Vector2.zero;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, .5f); rect.anchorMax = new Vector2(0, .5f); rect.pivot = new Vector2(0, .5f); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(270, 54);
            return button;
        }

        Image EnsurePanel(Transform parent, string name, Vector2 size, Vector2 position)
        {
            var go = parent.Find(name)?.gameObject ?? new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            image.color = new Color32(245, 235, 210, 235);
            SetRect(image.rectTransform, size, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position);
            return image;
        }

        Text EnsureText(Transform parent, string name, string value, int fontSize, TextAnchor alignment)
        {
            var go = parent.Find(name)?.gameObject ?? new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>() ?? go.AddComponent<Text>();
            text.text = value; text.fontSize = fontSize; text.alignment = alignment; text.color = new Color32(42, 30, 20, 255); text.raycastTarget = false;
            return text;
        }

        static void SetRect(RectTransform rect, Vector2 size, Vector2 min, Vector2 max, Vector2 position)
        {
            rect.anchorMin = min; rect.anchorMax = max; rect.anchoredPosition = position; rect.sizeDelta = size;
        }

        static string Label(CropId crop) => crop == CropId.Turnip ? "Turnip" : "Potato";
    }
}
