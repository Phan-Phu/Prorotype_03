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
        public readonly ItemMasterData[] Items;
        public readonly NpcDefinition[] Npcs;
        public readonly float NpcInteractionRadiusTiles;
        public readonly string[] StartingItems;

        public MasterDataSnapshot(PlayerMasterData player, TimeMasterData time,
            ToolMasterData tools, CropMasterData[] crops, TreeMasterData tree,
            NpcDefinition[] npcs, float npcInteractionRadiusTiles, string[] startingItems,
            ItemMasterData[] items = null)
        {
            Player = player;
            Time = time;
            Tools = tools;
            Crops = crops ?? new CropMasterData[0];
            Tree = tree;
            Items = items ?? new ItemMasterData[0];
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

        public ItemMasterData GetItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return null;
            for (int i = 0; i < Items.Length; i++)
                if (Items[i] != null && string.Equals(Items[i].ItemId, itemId, StringComparison.OrdinalIgnoreCase))
                    return Items[i];
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
                new[] { ToolItemIds.Hoe, ToolItemIds.WateringCan, ToolItemIds.Harvest, ToolItemIds.Axe },
                PrototypeItemMasterData());

        static ItemMasterData[] PrototypeItemMasterData() => new[]
        {
            new ItemMasterData(ToolItemIds.Hoe, "Hoe", "Tills grass into prepared soil.", "Assets/Sprite Textures/Tools/tools.png", "tools_21", false),
            new ItemMasterData(ToolItemIds.WateringCan, "Watering Can", "Waters prepared soil and growing crops.", "Assets/Sprite Textures/Tools/tools.png", "tools_63", false),
            new ItemMasterData(ToolItemIds.Harvest, "Harvest Basket", "Harvests ripe crops.", "Assets/Sprite Textures/Tools/tools.png", "tools_252", false),
            new ItemMasterData(ToolItemIds.Axe, "Axe", "Chops trees into wood.", "Assets/Sprite Textures/Tools/tools.png", "tools_0", false),
            new ItemMasterData("turnip_seed", "Turnip Seed", "Plant this seed on prepared soil.", "Assets/Sprite Textures/Objects/ParsnipSeeds.png", "", true),
            new ItemMasterData("potato_seed", "Potato Seed", "Plant this seed on prepared soil.", "Assets/Sprite Textures/Objects/ParsnipSeeds.png", "", true),
            new ItemMasterData("turnip", "Turnip", "A turnip harvested from a mature crop.", "Assets/Sprite Textures/Objects/ParsnipSeeds.png", "", true),
            new ItemMasterData("potato", "Potato", "A potato harvested from a mature crop.", "Assets/Sprite Textures/Objects/ParsnipSeeds.png", "", true),
            new ItemMasterData("wood", "Wood", "Useful wood collected from chopped trees.", "Assets/Sprite Textures/Objects/Wood.png", "", true)
        };
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

    public sealed class ItemMasterData
    {
        public readonly string ItemId;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly string IconPath;
        public readonly string IconSpriteName;
        public readonly bool Stackable;

        public ItemMasterData(string itemId, string displayName, string description,
            string iconPath, string iconSpriteName, bool stackable)
        {
            ItemId = itemId;
            DisplayName = displayName;
            Description = description;
            IconPath = iconPath;
            IconSpriteName = iconSpriteName;
            Stackable = stackable;
        }
    }
}
