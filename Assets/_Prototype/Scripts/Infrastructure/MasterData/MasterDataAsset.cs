using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Prototype.Domain;
using UnityEngine;

namespace Prototype.Infrastructure
{
    /// <summary>
    /// Editor-authored source of truth for runtime balance and content data.
    /// Create the asset with the Prototype/Master Data menu and keep it under Resources/MasterData.asset.
    /// </summary>
    [CreateAssetMenu(fileName = "MasterData", menuName = "Prototype/Master Data")]
    public sealed class MasterDataAsset : ScriptableObject
    {
        [Header("Player")]
        public float MoveSpeed = 4f;
        public int MaxStamina = 100;
        public int StartMoney = 500;

        [Header("Time")]
        public float SecondsPerInGameHour = 40f;
        public int DayStartHour = 6;
        public int DayEndHour = 26;

        [Header("Tool stamina costs")]
        public int TillStaminaCost = 2;
        public int WaterStaminaCost = 1;
        public int HarvestStaminaCost = 0;
        public int ChopStaminaCost = 4;

        [Header("Crops")]
        public CropEntry[] Crops = new CropEntry[0];

        [Header("Tree / wood")]
        public TreeEntry Tree = new TreeEntry();

        [Header("Starting inventory")]
        public string[] StartingItems = new string[0];

        [Header("NPCs")]
        public float NpcInteractionRadiusTiles = 1.25f;
        public NpcEntry[] Npcs = new NpcEntry[0];

        public Result<MasterDataFailure, MasterDataSnapshot> Import()
        {
            var validation = Validate();
            if (!validation.IsSuccess)
                return ResultFactory.Failure<MasterDataFailure, MasterDataSnapshot>(validation.Failure);

            try
            {
                var crops = new CropMasterData[Crops.Length];
                for (int i = 0; i < Crops.Length; i++)
                {
                    var entry = Crops[i];
                    if (!Enum.TryParse(entry.CropId, true, out CropId id))
                        return ResultFactory.Failure<MasterDataFailure, MasterDataSnapshot>(MasterDataFailure.Invalid($"crop[{i}].CropId"));
                    crops[i] = new CropMasterData(id, entry.SeedItemId, entry.ProduceItemId,
                        entry.SeedPrice, entry.SellPrice, entry.GrowthDays);
                }

                var npcs = new NpcDefinition[Npcs.Length];
                for (int i = 0; i < Npcs.Length; i++)
                {
                    var entry = Npcs[i];
                    npcs[i] = new NpcDefinition(entry.Id, entry.DisplayName,
                        new GridCoord(entry.X, entry.Y), entry.Lines ?? new string[0], entry.FallbackColor);
                }

                var snapshot = new MasterDataSnapshot(
                    new PlayerMasterData(MoveSpeed, MaxStamina, StartMoney),
                    new TimeMasterData(SecondsPerInGameHour, DayStartHour, DayEndHour),
                    new ToolMasterData(TillStaminaCost, WaterStaminaCost,
                        HarvestStaminaCost, ChopStaminaCost),
                    crops,
                    new TreeMasterData(Tree.MaxHP, Tree.ChopStaminaCost, Tree.WoodPerTree,
                        Tree.WoodSellPrice, Tree.RespawnDays, Tree.InitialCount, Tree.WoodItemId),
                    npcs,
                    NpcInteractionRadiusTiles,
                    StartingItems);
                return ResultFactory.Success<MasterDataFailure, MasterDataSnapshot>(snapshot);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return ResultFactory.Failure<MasterDataFailure, MasterDataSnapshot>(MasterDataFailure.System("master_data.import"));
            }
        }

        public void ResetToPrototypeDefaults()
        {
            MoveSpeed = BalanceConfig.MoveSpeed;
            MaxStamina = BalanceConfig.MaxStamina;
            StartMoney = BalanceConfig.StartMoney;
            SecondsPerInGameHour = BalanceConfig.SecondsPerInGameHour;
            DayStartHour = BalanceConfig.DayStartHour;
            DayEndHour = BalanceConfig.DayEndHour;
            TillStaminaCost = BalanceConfig.TillStaminaCost;
            WaterStaminaCost = BalanceConfig.WaterStaminaCost;
            HarvestStaminaCost = BalanceConfig.HarvestStaminaCost;
            ChopStaminaCost = BalanceConfig.ChopStaminaCost;
            Crops = new[]
            {
                new CropEntry("Turnip", "turnip_seed", "turnip", BalanceConfig.TurnipSeedPrice,
                    BalanceConfig.TurnipSellPrice, BalanceConfig.TurnipGrowthDays),
                new CropEntry("Potato", "potato_seed", "potato", BalanceConfig.PotatoSeedPrice,
                    BalanceConfig.PotatoSellPrice, BalanceConfig.PotatoGrowthDays)
            };
            Tree = new TreeEntry
            {
                MaxHP = BalanceConfig.TreeMaxHP,
                ChopStaminaCost = BalanceConfig.ChopStaminaCost,
                WoodPerTree = BalanceConfig.WoodPerTree,
                WoodSellPrice = BalanceConfig.WoodSellPrice,
                RespawnDays = BalanceConfig.TreeRespawnDays,
                InitialCount = BalanceConfig.InitialTreeCount,
                WoodItemId = "wood"
            };
            StartingItems = new[] { ToolItemIds.Hoe, ToolItemIds.WateringCan, ToolItemIds.Harvest, ToolItemIds.Axe };
            NpcInteractionRadiusTiles = NpcDefinitions.InteractionRadiusTiles;
            Npcs = new[]
            {
                new NpcEntry("cora", "Cora", 8, 17, new[]
                {
                    "Chào buổi sáng! Ruộng nhỏ cũng thành chuyện lớn nếu ngày nào cũng chăm.",
                    "Cây đã gieo thì nhớ tưới trước khi ngủ nhé. Đất khô là cây đứng yên đấy.",
                    "Nếu hôm nay thu hoạch được, thử nghĩ xem mai sẽ trồng thêm ô nào.",
                    "Tôi thích nhìn mảnh ruộng đổi màu sau mỗi lần cuốc — rất dễ biết mình đã làm được gì."
                }, new Color(0.9f, 0.55f, 0.95f)),
                new NpcEntry("butch", "Butch", 17, 12, new[]
                {
                    "Thấy mấy cây phía kia không? Chặt vài cây là có thêm việc trong lúc chờ mùa vụ lớn.",
                    "Rìu tốn sức hơn cuốc, nên đừng quên giữ stamina cho ruộng trước.",
                    "Gốc cây sẽ mọc lại sau vài ngày. Tôi hay quay lại đúng lúc vụ củ cải tới kỳ.",
                    "Gỗ bán được, nhưng đừng bỏ ruộng chỉ vì vài khúc củi nhanh tay."
                }, new Color(0.75f, 0.36f, 0.18f))
            };
        }

        Result<MasterDataFailure, Unit> Validate()
        {
            if (MoveSpeed <= 0f || MaxStamina <= 0 || StartMoney < 0)
                return ResultFactory.Failure<MasterDataFailure>(MasterDataFailure.Invalid("player"));
            if (SecondsPerInGameHour <= 0f || DayStartHour < 0 || DayEndHour <= DayStartHour)
                return ResultFactory.Failure<MasterDataFailure>(MasterDataFailure.Invalid("time"));
            if (Crops == null || Crops.Length == 0)
                return ResultFactory.Failure<MasterDataFailure>(MasterDataFailure.Invalid("crops"));
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var crop in Crops)
            {
                if (crop == null || string.IsNullOrWhiteSpace(crop.CropId)
                    || string.IsNullOrWhiteSpace(crop.SeedItemId) || string.IsNullOrWhiteSpace(crop.ProduceItemId)
                    || crop.SeedPrice < 0 || crop.SellPrice < 0 || crop.GrowthDays <= 0)
                    return ResultFactory.Failure<MasterDataFailure>(MasterDataFailure.Invalid("crop"));
                if (!ids.Add(crop.CropId))
                    return ResultFactory.Failure<MasterDataFailure>(MasterDataFailure.Invalid("duplicate_crop"));
            }
            if (Tree == null || Tree.MaxHP <= 0 || Tree.ChopStaminaCost < 0 || Tree.WoodPerTree <= 0
                || Tree.WoodSellPrice < 0 || Tree.RespawnDays <= 0 || Tree.InitialCount < 0
                || string.IsNullOrWhiteSpace(Tree.WoodItemId))
                return ResultFactory.Failure<MasterDataFailure>(MasterDataFailure.Invalid("tree"));
            if (Npcs == null) return ResultFactory.Failure<MasterDataFailure>(MasterDataFailure.Invalid("npcs"));
            if (NpcInteractionRadiusTiles <= 0f)
                return ResultFactory.Failure<MasterDataFailure>(MasterDataFailure.Invalid("npc_interaction_radius"));
            foreach (var npc in Npcs)
                if (npc == null || string.IsNullOrWhiteSpace(npc.Id) || string.IsNullOrWhiteSpace(npc.DisplayName)
                    || npc.Lines == null || npc.Lines.Length == 0)
                    return ResultFactory.Failure<MasterDataFailure>(MasterDataFailure.Invalid("npc"));
            return ResultFactory.Success<MasterDataFailure, Unit>(Unit.Value);
        }

        [Serializable]
        public sealed class CropEntry
        {
            public string CropId;
            public string SeedItemId;
            public string ProduceItemId;
            public int SeedPrice;
            public int SellPrice;
            public int GrowthDays;

            public CropEntry() { }
            public CropEntry(string cropId, string seedItemId, string produceItemId,
                int seedPrice, int sellPrice, int growthDays)
            {
                CropId = cropId; SeedItemId = seedItemId; ProduceItemId = produceItemId;
                SeedPrice = seedPrice; SellPrice = sellPrice; GrowthDays = growthDays;
            }
        }

        [Serializable]
        public sealed class TreeEntry
        {
            public int MaxHP;
            public int ChopStaminaCost;
            public int WoodPerTree;
            public int WoodSellPrice;
            public int RespawnDays;
            public int InitialCount;
            public string WoodItemId;
        }

        [Serializable]
        public sealed class NpcEntry
        {
            public string Id;
            public string DisplayName;
            public int X;
            public int Y;
            [TextArea(2, 6)] public string[] Lines;
            public Color FallbackColor = Color.white;

            public NpcEntry() { }
            public NpcEntry(string id, string displayName, int x, int y,
                string[] lines, Color fallbackColor)
            {
                Id = id; DisplayName = displayName; X = x; Y = y;
                Lines = lines; FallbackColor = fallbackColor;
            }
        }
    }

    public static class MasterDataImporter
    {
        public static UniTask<Result<MasterDataFailure, MasterDataSnapshot>> Load()
        {
            var asset = Resources.Load<MasterDataAsset>("MasterData");
            if (asset == null)
                return ResultFactory.UniTaskFailure<MasterDataFailure, MasterDataSnapshot>(MasterDataFailure.MissingAsset());
            return UniTask.FromResult(asset.Import());
        }
    }
}
