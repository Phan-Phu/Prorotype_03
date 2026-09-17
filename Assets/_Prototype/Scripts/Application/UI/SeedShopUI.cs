using System.Collections.Generic;
using Prototype.Domain;
using Prototype.Application;
using Prototype.Infrastructure;
using UnityEngine;
using UnityEngine.UI;

namespace Prototype.Application
{
    /// <summary>UGUI seed shop: item list on the left, detail and purchase area on the right.</summary>
    public sealed class SeedShopUI : PopupBase
    {
        public string LastFeedback => _feedbackText != null ? _feedbackText.text : string.Empty;

        /// <summary>True while the pointer is over this popup. Instance property, not static — callers
        /// (PlayerController, DebugPanel, ...) hold an explicit reference wired by GameManager instead
        /// of reaching for a singleton.</summary>
        public bool PointerOverUI { get; private set; }

        // internal, not public: only GameManager (the composition root, same assembly) wires these —
        // nothing outside this assembly (tests included) reads or sets them directly, so there is no
        // reason for a wider-than-needed public surface here.
        internal GameState State;
        internal MasterDataAsset MasterDataAsset;
        internal IGameplayService GameplayService;
        internal IInventoryService InventoryService;
        internal PlayerController Player;

        [Header("Editor-authored SeedShopPopup")]
        [SerializeField] RectTransform _shopPopup;
        [SerializeField] Text _titleText;
        // One authored, initially-hidden button; every listed crop is a runtime clone of it (see
        // BuildItemList). Adding a crop to State.MasterData.Crops is enough for it to show up here —
        // no second hand-authored button, no per-crop code.
        [SerializeField] Button _itemButtonTemplate;
        [SerializeField] Image _detailPanel;
        [SerializeField] Image _detailIcon;
        [SerializeField] Text _detailTitle;
        [SerializeField] Text _detailText;
        [SerializeField] Image _purchasePanel;
        [SerializeField] Text _priceText;
        [SerializeField] Button _buyButton;
        [SerializeField] Text _feedbackText;
        [SerializeField] Button _closeButton;

        readonly List<Button> _itemButtons = new List<Button>();
        bool _open;
        CropId? _selectedCrop;

        CropMasterData CropData(CropId crop) => State?.MasterData?.GetCrop(crop);
        string SeedItemId(CropId crop) => CropData(crop)?.SeedItemId ?? CropDefinition.SeedItemId(crop);
        string ProduceItemId(CropId crop) => CropData(crop)?.ProduceItemId ?? CropDefinition.ProduceItemId(crop);
        int SeedPrice(CropId crop) => CropData(crop)?.SeedPrice ?? CropDefinition.SeedPrice(crop);
        int SellPrice(CropId crop) => CropData(crop)?.SellPrice ?? CropDefinition.SellPrice(crop);
        string WoodItemId => State?.MasterData?.Tree?.WoodItemId ?? TreeDefinition.WoodItemId;
        int WoodSellPrice => State?.MasterData?.Tree?.WoodSellPrice ?? TreeDefinition.WoodSellPrice;
        MasterDataAsset.ItemEntry ItemData(string itemId) => MasterDataAsset?.GetItem(itemId);

        protected override void Awake()
        {
            base.Awake();
            BindSceneUi();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnlockGameplay();
        }

        protected override RectTransform ResolvePopupTarget()
        {
            return _shopPopup;
        }

        void OnDisable()
        {
            UnlockGameplay();
            if (_shopPopup != null) _shopPopup.gameObject.SetActive(false);
        }

        void Update()
        {
            if (_open && Input.GetKeyDown(KeyCode.Escape)) CloseShop();
            if (_open) RefreshUi();
        }

        public void OpenShop()
        {
            ShowPopup();
            _open = true;
            _selectedCrop = null;
            if (_shopPopup != null) _shopPopup.gameObject.SetActive(true);
            Player?.SetGameplayLocked(true, "seed-shop");
            if (_feedbackText != null) _feedbackText.text = string.Empty;
            RefreshUi();
            SessionLogger.LogShopOpen(State);
        }

        public void CloseShop()
        {
            HidePopup();
            _open = false;
            _selectedCrop = null;
            UnlockGameplay();
            PointerOverUI = false;
        }

        void UnlockGameplay() => Player?.SetGameplayLocked(false, "seed-shop");

        void BindSceneUi()
        {
            if (_shopPopup == null || _itemButtonTemplate == null || _detailPanel == null ||
                _detailIcon == null || _detailTitle == null || _detailText == null || _purchasePanel == null ||
                _priceText == null || _buyButton == null || _feedbackText == null || _closeButton == null)
            {
                Debug.LogWarning("SeedShopUI is missing serialized SeedShopPopup references. Assign the editor-authored hierarchy in the Inspector.");
                return;
            }
            if (_titleText != null) _titleText.text = "SeedShop";
            _itemButtonTemplate.gameObject.SetActive(false);
            _buyButton.onClick.RemoveAllListeners();
            _buyButton.onClick.AddListener(BuySelectedItem);
            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(CloseShop);
            _shopPopup.gameObject.SetActive(false);
            BuildItemList();
        }

        /// <summary>Generate one button per crop in State.MasterData.Crops by cloning
        /// _itemButtonTemplate. Adding a row to Master Data is enough to add an entry here —
        /// no per-crop button, no per-crop code.</summary>
        void BuildItemList()
        {
            if (_itemButtonTemplate == null) return;
            var crops = State?.MasterData?.Crops;
            int count = crops != null ? crops.Length : 0;
            while (_itemButtons.Count < count)
            {
                var clone = Instantiate(_itemButtonTemplate, _itemButtonTemplate.transform.parent);
                _itemButtons.Add(clone);
            }
            for (int i = 0; i < _itemButtons.Count; i++)
            {
                var button = _itemButtons[i];
                if (button == null) continue;
                bool active = i < count;
                button.gameObject.SetActive(active);
                if (!active) continue;

                var crop = crops[i].Id;
                var rect = button.GetComponent<RectTransform>();
                if (rect != null) rect.anchoredPosition = new Vector2(20f, 70f - i * 70f);
                button.name = $"ItemButton_{crop}";
                SetButtonLabel(button, $"{ItemName(SeedItemId(crop))}   -   {SeedPrice(crop)}g");
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => Select(crop));
            }
        }

        /// <summary>Rebuild/refresh the item list. Can be assigned to a UI Button or called by a presenter.</summary>
        public void ListItems()
        {
            RefreshUi();
        }

        /// <summary>Select an item by seed item id, suitable for a UnityEvent(string).</summary>
        public void ListItem(string itemId) => ShowItemDetail(itemId);

        /// <summary>Show detail for an item id, suitable for a UnityEvent(string). Looks up the
        /// matching crop from Master Data instead of a hardcoded per-crop check.</summary>
        public void ShowItemDetail(string itemId)
        {
            var crops = State?.MasterData?.Crops;
            if (crops == null) return;
            for (int i = 0; i < crops.Length; i++)
            {
                if (crops[i].SeedItemId == itemId)
                {
                    Select(crops[i].Id);
                    return;
                }
            }
        }

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
                if (_detailIcon != null) { _detailIcon.sprite = null; _detailIcon.enabled = false; }
            }
            else
            {
                var crop = _selectedCrop.Value;
                var seedId = SeedItemId(crop);
                var price = SeedPrice(crop);
                var item = ItemData(seedId);
                _detailTitle.text = item?.DisplayName ?? $"{Label(crop)} Seed";
                int owned = InventoryService != null
                    ? InventoryService.Count(State.InventorySystem, seedId).GetAwaiter().GetResult().Value
                    : 0;
                string description = item != null ? item.Description : "Plant this seed on prepared soil.";
                _detailText.text = $"{description}\n\nPrice: {price}g\nOwned: {owned}";
                _priceText.text = $"{price}g   Money: {State.Wallet.Money}";
                _buyButton.interactable = true;
                if (_detailIcon != null)
                {
                    _detailIcon.sprite = item?.Icon;
                    _detailIcon.color = PlaceholderArt.ItemTint(seedId);
                    _detailIcon.enabled = _detailIcon.sprite != null;
                }
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
                string itemId = SeedItemId(crop);
                int price = SeedPrice(crop);
                result = new ShopPurchaseResult(MapPurchaseCode(operation.Failure.Code), crop, itemId,
                    price, State.Wallet.Money, State.Wallet.Money, 0, 0);
            }
            SessionLogger.LogShopPurchase(State, result);
            if (_feedbackText != null)
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
                ? InventoryService.Count(State.InventorySystem, WoodItemId).GetAwaiter().GetResult().Value
                : 0;
            return SellItem(WoodItemId, WoodSellPrice, count, "Wood");
        }

        public ShopSellResult SellTurnip() => SellCrop(CropId.Turnip);
        public ShopSellResult SellPotato() => SellCrop(CropId.Potato);

        public ShopSellResult SellCrop(CropId crop)
        {
            string itemId = ProduceItemId(crop);
            int count = InventoryService != null
                ? InventoryService.Count(State.InventorySystem, itemId).GetAwaiter().GetResult().Value
                : 0;
            return SellItem(itemId, SellPrice(crop), count, Label(crop));
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
            if (_feedbackText != null)
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

        static void SetButtonLabel(Button button, string value)
        {
            if (button != null && button.GetComponentInChildren<Text>(true) is Text label)
                label.text = value;
        }

        string ItemName(string itemId) => ItemData(itemId)?.DisplayName ?? itemId;

        static string Label(CropId crop) => crop == CropId.Turnip ? "Turnip" : "Potato";
    }
}
