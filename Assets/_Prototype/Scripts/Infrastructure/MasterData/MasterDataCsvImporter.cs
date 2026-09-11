using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Prototype.Domain;
using UnityEngine;

namespace Prototype.Infrastructure
{
    /// <summary>
    /// Text source files used by the editor importer. Keeping the source as plain CSV makes balance
    /// and content reviewable outside Unity, while MasterDataAsset remains the runtime asset boundary.
    /// </summary>
    public sealed class MasterDataCsvBundle
    {
        public readonly string Player;
        public readonly string Time;
        public readonly string Tools;
        public readonly string Crops;
        public readonly string Tree;
        public readonly string Items;
        public readonly string StartingInventory;
        public readonly string Npcs;

        public MasterDataCsvBundle(string player, string time, string tools, string crops,
            string tree, string items, string startingInventory, string npcs)
        {
            Player = player;
            Time = time;
            Tools = tools;
            Crops = crops;
            Tree = tree;
            Items = items;
            StartingInventory = startingInventory;
            Npcs = npcs;
        }
    }

    /// <summary>
    /// Infrastructure conversion from CSV rows into the editor-authored ScriptableObject.
    /// AssetDatabase access stays in the Editor adapter; this class only receives an icon resolver.
    /// </summary>
    public static class MasterDataCsvImporter
    {
        public static Result<MasterDataFailure, Unit> Apply(MasterDataAsset target,
            MasterDataCsvBundle source, Func<string, string, Sprite> resolveIcon)
        {
            if (target == null) return ResultFactory.Failure<MasterDataFailure>(MasterDataFailure.MissingAsset("target"));
            if (source == null) return ResultFactory.Failure<MasterDataFailure>(MasterDataFailure.Invalid("csv.source"));

            try
            {
                var player = CsvTable.Parse(source.Player, "player").Rows.Single();
                target.MoveSpeed = player.Float("MoveSpeed");
                target.MaxStamina = player.Int("MaxStamina");
                target.StartMoney = player.Int("StartMoney");

                var time = CsvTable.Parse(source.Time, "time").Rows.Single();
                target.SecondsPerInGameHour = time.Float("SecondsPerInGameHour");
                target.DayStartHour = time.Int("DayStartHour");
                target.DayEndHour = time.Int("DayEndHour");

                var tools = CsvTable.Parse(source.Tools, "tools").Rows.Single();
                target.TillStaminaCost = tools.Int("TillStaminaCost");
                target.WaterStaminaCost = tools.Int("WaterStaminaCost");
                target.HarvestStaminaCost = tools.Int("HarvestStaminaCost");
                target.ChopStaminaCost = tools.Int("ChopStaminaCost");

                target.Crops = CsvTable.Parse(source.Crops, "crops").Rows.Select(row =>
                    new MasterDataAsset.CropEntry(row.Text("CropId"), row.Text("SeedItemId"),
                        row.Text("ProduceItemId"), row.Int("SeedPrice"), row.Int("SellPrice"),
                        row.Int("GrowthDays"))).ToArray();

                var tree = CsvTable.Parse(source.Tree, "tree").Rows.Single();
                target.Tree = new MasterDataAsset.TreeEntry
                {
                    MaxHP = tree.Int("MaxHP"),
                    ChopStaminaCost = tree.Int("ChopStaminaCost"),
                    WoodPerTree = tree.Int("WoodPerTree"),
                    WoodSellPrice = tree.Int("WoodSellPrice"),
                    RespawnDays = tree.Int("RespawnDays"),
                    InitialCount = tree.Int("InitialCount"),
                    WoodItemId = tree.Text("WoodItemId")
                };

                target.Items = CsvTable.Parse(source.Items, "items").Rows.Select(row =>
                {
                    string path = row.Text("IconPath");
                    string spriteName = row.OptionalText("IconSpriteName");
                    return new MasterDataAsset.ItemEntry(row.Text("ItemId"), row.Text("DisplayName"),
                        row.Text("Description"), path, spriteName, row.Bool("Stackable"))
                    {
                        Icon = resolveIcon == null ? null : resolveIcon(path, spriteName)
                    };
                }).ToArray();

                target.StartingItems = CsvTable.Parse(source.StartingInventory, "starting_inventory")
                    .Rows.Select(row => row.Text("ItemId")).ToArray();

                var npcRows = CsvTable.Parse(source.Npcs, "npcs");
                var firstNpc = npcRows.Rows.FirstOrDefault();
                target.NpcInteractionRadiusTiles = firstNpc == null
                    ? NpcDefinitions.InteractionRadiusTiles
                    : firstNpc.Float("InteractionRadiusTiles");
                target.Npcs = npcRows.Rows.Select(row => new MasterDataAsset.NpcEntry(
                    row.Text("Id"), row.Text("DisplayName"), row.Int("X"), row.Int("Y"),
                    row.Text("Lines").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries),
                    new Color(row.Float("ColorR"), row.Float("ColorG"), row.Float("ColorB"), row.Float("ColorA"))
                )).ToArray();

                var imported = target.Import();
                if (!imported.IsSuccess)
                    return ResultFactory.Failure<MasterDataFailure>(imported.Failure);
                return ResultFactory.Success<MasterDataFailure, Unit>(Unit.Value);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return ResultFactory.Failure<MasterDataFailure>(MasterDataFailure.Invalid("csv.parse"));
            }
        }

        sealed class CsvTable
        {
            public readonly string Name;
            public readonly string[] Headers;
            public readonly List<CsvRow> Rows;

            CsvTable(string name, string[] headers, List<CsvRow> rows)
            {
                Name = name;
                Headers = headers;
                Rows = rows;
            }

            public static CsvTable Parse(string text, string name)
            {
                if (string.IsNullOrWhiteSpace(text))
                    throw new FormatException($"CSV '{name}' is empty.");
                var rawRows = ReadRows(text);
                if (rawRows.Count < 2)
                    throw new FormatException($"CSV '{name}' needs a header and at least one row.");

                var headers = rawRows[0].Select((header, index) =>
                    string.IsNullOrWhiteSpace(header) ? $"column_{index}" : header.TrimStart('\uFEFF').Trim())
                    .ToArray();
                var headerSet = new HashSet<string>(headers, StringComparer.OrdinalIgnoreCase);
                if (headerSet.Count != headers.Length)
                    throw new FormatException($"CSV '{name}' has duplicate headers.");

                var rows = new List<CsvRow>();
                for (int i = 1; i < rawRows.Count; i++)
                {
                    if (rawRows[i].All(string.IsNullOrWhiteSpace)) continue;
                    if (rawRows[i].Count != headers.Length)
                        throw new FormatException($"CSV '{name}' row {i + 1} has {rawRows[i].Count} fields, expected {headers.Length}.");
                    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    for (int c = 0; c < headers.Length; c++) values[headers[c]] = rawRows[i][c].Trim();
                    rows.Add(new CsvRow(name, i + 1, values));
                }
                return new CsvTable(name, headers, rows);
            }

            static List<List<string>> ReadRows(string text)
            {
                var rows = new List<List<string>>();
                var row = new List<string>();
                var cell = new StringBuilder();
                bool quoted = false;
                for (int i = 0; i < text.Length; i++)
                {
                    char ch = text[i];
                    if (ch == '"')
                    {
                        if (quoted && i + 1 < text.Length && text[i + 1] == '"')
                        {
                            cell.Append('"');
                            i++;
                        }
                        else quoted = !quoted;
                    }
                    else if (ch == ',' && !quoted)
                    {
                        row.Add(cell.ToString());
                        cell.Length = 0;
                    }
                    else if ((ch == '\n' || ch == '\r') && !quoted)
                    {
                        if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                        row.Add(cell.ToString());
                        cell.Length = 0;
                        rows.Add(row);
                        row = new List<string>();
                    }
                    else cell.Append(ch);
                }
                if (cell.Length > 0 || row.Count > 0)
                {
                    row.Add(cell.ToString());
                    rows.Add(row);
                }
                return rows;
            }
        }

        sealed class CsvRow
        {
            readonly string _name;
            readonly int _line;
            readonly Dictionary<string, string> _values;

            public CsvRow(string name, int line, Dictionary<string, string> values)
            {
                _name = name; _line = line; _values = values;
            }

            public string Text(string column)
            {
                if (!_values.TryGetValue(column, out var value) || string.IsNullOrWhiteSpace(value))
                    throw new FormatException($"CSV '{_name}' line {_line} is missing '{column}'.");
                return value;
            }

            public string OptionalText(string column)
                => _values.TryGetValue(column, out var value) ? value : string.Empty;

            public int Int(string column) => int.Parse(Text(column), CultureInfo.InvariantCulture);
            public float Float(string column) => float.Parse(Text(column), CultureInfo.InvariantCulture);
            public bool Bool(string column) => bool.Parse(Text(column));
        }
    }
}
