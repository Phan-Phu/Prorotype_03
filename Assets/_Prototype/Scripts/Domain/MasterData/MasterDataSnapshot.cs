using System;
using UnityEngine;

namespace Prototype.Domain
{
    /// <summary>
    /// Immutable runtime master data imported by Infrastructure from a Unity asset.
    /// Domain entities receive this snapshot at construction; they do not know about
    /// ScriptableObject, Resources or AssetDatabase.
    /// </summary>
    public sealed class MasterDataSnapshot
    {
        public readonly PlayerMasterData Player;
        public readonly TimeMasterData Time;
        public readonly ToolMasterData Tools;
        public readonly CropMasterData[] Crops;
        public readonly TreeMasterData Tree;
        public readonly NpcDefinition[] Npcs;
        public readonly float NpcInteractionRadiusTiles;
        public readonly string[] StartingItems;

        public MasterDataSnapshot(PlayerMasterData player, TimeMasterData time,
            ToolMasterData tools, CropMasterData[] crops, TreeMasterData tree,
            NpcDefinition[] npcs, float npcInteractionRadiusTiles, string[] startingItems)
        {
            Player = player;
            Time = time;
            Tools = tools;
            Crops = crops ?? new CropMasterData[0];
            Tree = tree;
            Npcs = npcs ?? new NpcDefinition[0];
            NpcInteractionRadiusTiles = npcInteractionRadiusTiles;
            StartingItems = startingItems ?? new string[0];
        }

        public CropMasterData GetCrop(CropId id)
        {
            for (int i = 0; i < Crops.Length; i++)
                if (Crops[i] != null && Crops[i].Id == id) return Crops[i];
            return null;
        }

        public static MasterDataSnapshot CreatePrototypeDefaults()
            => new MasterDataSnapshot(
                new PlayerMasterData(BalanceConfig.MoveSpeed, BalanceConfig.MaxStamina, BalanceConfig.StartMoney),
                new TimeMasterData(BalanceConfig.SecondsPerInGameHour, BalanceConfig.DayStartHour, BalanceConfig.DayEndHour),
                new ToolMasterData(BalanceConfig.TillStaminaCost, BalanceConfig.WaterStaminaCost,
                    BalanceConfig.HarvestStaminaCost, BalanceConfig.ChopStaminaCost),
                new[]
                {
                    new CropMasterData(CropId.Turnip, "turnip_seed", "turnip",
                        BalanceConfig.TurnipSeedPrice, BalanceConfig.TurnipSellPrice, BalanceConfig.TurnipGrowthDays),
                    new CropMasterData(CropId.Potato, "potato_seed", "potato",
                        BalanceConfig.PotatoSeedPrice, BalanceConfig.PotatoSellPrice, BalanceConfig.PotatoGrowthDays)
                },
                new TreeMasterData(BalanceConfig.TreeMaxHP, BalanceConfig.ChopStaminaCost,
                    BalanceConfig.WoodPerTree, BalanceConfig.WoodSellPrice,
                    BalanceConfig.TreeRespawnDays, BalanceConfig.InitialTreeCount, "wood"),
                NpcDefinitions.All,
                NpcDefinitions.InteractionRadiusTiles,
                new[] { ToolItemIds.Hoe, ToolItemIds.WateringCan, ToolItemIds.Harvest, ToolItemIds.Axe });
    }

    public sealed class PlayerMasterData
    {
        public readonly float MoveSpeed;
        public readonly int MaxStamina;
        public readonly int StartMoney;

        public PlayerMasterData(float moveSpeed, int maxStamina, int startMoney)
        {
            MoveSpeed = moveSpeed; MaxStamina = maxStamina; StartMoney = startMoney;
        }
    }

    public sealed class TimeMasterData
    {
        public readonly float SecondsPerInGameHour;
        public readonly int DayStartHour;
        public readonly int DayEndHour;

        public TimeMasterData(float secondsPerInGameHour, int dayStartHour, int dayEndHour)
        {
            SecondsPerInGameHour = secondsPerInGameHour;
            DayStartHour = dayStartHour;
            DayEndHour = dayEndHour;
        }
    }

    public sealed class ToolMasterData
    {
        public readonly int TillStaminaCost;
        public readonly int WaterStaminaCost;
        public readonly int HarvestStaminaCost;
        public readonly int ChopStaminaCost;

        public ToolMasterData(int tillStaminaCost, int waterStaminaCost,
            int harvestStaminaCost, int chopStaminaCost)
        {
            TillStaminaCost = tillStaminaCost;
            WaterStaminaCost = waterStaminaCost;
            HarvestStaminaCost = harvestStaminaCost;
            ChopStaminaCost = chopStaminaCost;
        }
    }

    public sealed class CropMasterData
    {
        public readonly CropId Id;
        public readonly string SeedItemId;
        public readonly string ProduceItemId;
        public readonly int SeedPrice;
        public readonly int SellPrice;
        public readonly int GrowthDays;

        public CropMasterData(CropId id, string seedItemId, string produceItemId,
            int seedPrice, int sellPrice, int growthDays)
        {
            Id = id; SeedItemId = seedItemId; ProduceItemId = produceItemId;
            SeedPrice = seedPrice; SellPrice = sellPrice; GrowthDays = growthDays;
        }
    }

    public sealed class TreeMasterData
    {
        public readonly int MaxHP;
        public readonly int ChopStaminaCost;
        public readonly int WoodPerTree;
        public readonly int WoodSellPrice;
        public readonly int RespawnDays;
        public readonly int InitialCount;
        public readonly string WoodItemId;

        public TreeMasterData(int maxHP, int chopStaminaCost, int woodPerTree,
            int woodSellPrice, int respawnDays, int initialCount, string woodItemId)
        {
            MaxHP = maxHP; ChopStaminaCost = chopStaminaCost; WoodPerTree = woodPerTree;
            WoodSellPrice = woodSellPrice; RespawnDays = respawnDays;
            InitialCount = initialCount; WoodItemId = woodItemId;
        }
    }
}
