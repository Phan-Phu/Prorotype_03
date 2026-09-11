using System;
using System.IO;
using UnityEngine;
using Prototype.Domain;

namespace Prototype.Application
{
    /// <summary>
    /// Session action logger (S2-QA-06). Appends one CSV row (day,time,action,tile,result,money,stamina)
    /// to Artifacts/session_&lt;seed&gt;.csv every time the player actually uses a tool in a real running
    /// session (Editor Play Mode or a packaged build) — a literal, replayable trace of what happened,
    /// complementing "-seed is load-bearing" reproducibility (BootArgs doc, README Quick Start).
    /// Deliberately NOT wired into HeadlessSim/EditMode tests: those already have their own record
    /// (economy.csv, or nothing — they don't drive PlayerController), and this exists specifically for
    /// QA's manual/bug-bash sessions, not automated runs.
    /// Row formatting (FormatRow) is pure and EditMode-testable; only LogAction touches disk, and it
    /// swallows IO errors so a logging hiccup can never crash the game.
    /// </summary>
    public static class SessionLogger
    {
        public const string Header = "day,time,action,tile,result,money,stamina";
        public const string ShopHeader = "day,time,event,item,result,money_before,money_after,count_before,count_after";

        /// <summary>Pure formatting — no file IO, EditMode-testable.</summary>
        public static string FormatRow(int day, int hour, string action, Prototype.Domain.GridCoord tile,
            string result, int money, int stamina)
            => $"{day},{Prototype.Domain.GameClock.WrapHour(hour):00}:00,{action},{tile},{result},{money},{stamina}";

        /// <summary>Pure shop-log formatting for DSN-040 QA metrics.</summary>
        public static string FormatShopRow(int day, int hour, string evt, string item, string result,
            int moneyBefore, int moneyAfter, int countBefore, int countAfter)
            => $"{day},{Prototype.Domain.GameClock.WrapHour(hour):00}:00,{evt},{item},{result},{moneyBefore},{moneyAfter},{countBefore},{countAfter}";

        private static string _path;
        private static int _cachedSeed;

        /// <summary>
        /// Resolves Artifacts/session_&lt;seed&gt;.csv, cached per seed (not just once per process —
        /// a real running game only ever has one GameState/seed for its whole lifetime, but caching
        /// unconditionally on first call would silently keep writing to the FIRST seed's file if this
        /// were ever called against a different-seeded GameState later, e.g. from a tool/manual check).
        /// In the Editor that's the project's own Artifacts/ folder; in a packaged build it's the
        /// Artifacts/ folder the Win64 build always lands under (Artifacts/Build/Win/Farm.exe,
        /// CIBuild.BuildWin64) — walked up from Application.dataPath and verified by name before
        /// trusting it, falling back to next to the exe (never silently writing somewhere unrelated) if
        /// that assumption doesn't hold, e.g. a manually-copied build.
        /// </summary>
        static string ResolvePath(int seed)
        {
            if (_path == null || _cachedSeed != seed)
            {
                string artifactsDir = ResolveArtifactsDir();
                Directory.CreateDirectory(artifactsDir);
                _path = Path.Combine(artifactsDir, $"session_{seed}.csv");
                _cachedSeed = seed;
            }
            return _path;
        }

        static string ResolveShopPath(int seed)
        {
            string artifactsDir = ResolveArtifactsDir();
            Directory.CreateDirectory(artifactsDir);
            return Path.Combine(artifactsDir, $"shop_{seed}.csv");
        }

        static string ResolveArtifactsDir()
        {
#if UNITY_EDITOR
            return Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName, "Artifacts");
#else
            var dataDir = new DirectoryInfo(UnityEngine.Application.dataPath); // .../Artifacts/Build/Win/Farm_Data
            var candidate = dataDir.Parent?.Parent?.Parent;        // .../Artifacts
            if (candidate != null && string.Equals(candidate.Name, "Artifacts", StringComparison.OrdinalIgnoreCase))
                return candidate.FullName;
            return dataDir.Parent?.FullName ?? UnityEngine.Application.persistentDataPath;
#endif
        }

        /// <summary>Appends one row for a real tool-use (called from PlayerController.UseTool).</summary>
        public static void LogAction(Prototype.Domain.GameState state, string action,
            Prototype.Domain.GridCoord tile, string result)
        {
            try
            {
                // GameState.Seed (not the global BootArgs.Seed) — see that field's doc. In the real
                // game the two always match (GameManager: `new GameState(seed: BootArgs.Seed)`), but
                // reading the state's own value is the honest source of truth.
                string path = ResolvePath(state.Seed);
                if (!File.Exists(path)) File.WriteAllText(path, Header + Environment.NewLine);
                string row = FormatRow(state.Clock.Day, state.Clock.Hour, action, tile, result, state.Wallet.Money, state.Stamina.Current);
                File.AppendAllText(path, row + Environment.NewLine);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[SessionLogger] Ghi log thất bại (bỏ qua, không chặn gameplay): {e.Message}");
            }
        }

        public static void LogShopOpen(Prototype.Domain.GameState state)
        {
            if (state == null) return;
            try
            {
                string path = ResolveShopPath(state.Seed);
                if (!File.Exists(path)) File.WriteAllText(path, ShopHeader + Environment.NewLine);
                string row = FormatShopRow(state.Clock.Day, state.Clock.Hour, "open", "seed_shop", "opened",
                    state.Wallet.Money, state.Wallet.Money, 0, 0);
                File.AppendAllText(path, row + Environment.NewLine);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[SessionLogger] Ghi shop log thất bại (bỏ qua, không chặn gameplay): {e.Message}");
            }
        }

        public static void LogShopPurchase(Prototype.Domain.GameState state, Prototype.Domain.ShopPurchaseResult result)
        {
            if (state == null) return;
            try
            {
                string path = ResolveShopPath(state.Seed);
                if (!File.Exists(path)) File.WriteAllText(path, ShopHeader + Environment.NewLine);
                string row = FormatShopRow(state.Clock.Day, state.Clock.Hour, "buy", result.ItemId, result.Code.ToString(),
                    result.MoneyBefore, result.MoneyAfter, result.CountBefore, result.CountAfter);
                File.AppendAllText(path, row + Environment.NewLine);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[SessionLogger] Ghi shop log thất bại (bỏ qua, không chặn gameplay): {e.Message}");
            }
        }

        public static void LogShopSell(Prototype.Domain.GameState state, Prototype.Domain.ShopSellResult result)
        {
            if (state == null) return;
            try
            {
                string path = ResolveShopPath(state.Seed);
                if (!File.Exists(path)) File.WriteAllText(path, ShopHeader + Environment.NewLine);
                string row = FormatShopRow(state.Clock.Day, state.Clock.Hour, "sell", result.ItemId, result.Code.ToString(),
                    result.MoneyBefore, result.MoneyAfter, result.CountBefore, result.CountAfter);
                File.AppendAllText(path, row + Environment.NewLine);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[SessionLogger] Ghi shop log thất bại (bỏ qua, không chặn gameplay): {e.Message}");
            }
        }
    }
}
