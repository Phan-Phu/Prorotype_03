using System;

namespace Prototype.Application
{
    /// <summary>
    /// Parsed boot arguments for packaged builds so QA can start a run deterministically
    /// without clicking through menus. Mirrors the debug panel functions (AGENT_DEV §4.5).
    /// Every Random in the game must be seeded from BootArgs.Seed for reproducible bugs.
    /// </summary>
    public static class BootArgs
    {
        public static int  StartDay   { get; private set; } = 1;
        public static int  StartMoney { get; private set; } = Prototype.Domain.BalanceConfig.StartMoney;
        public static int  Seed       { get; private set; } = 0;
        public static bool FastTime   { get; private set; } = false;

        [UnityEngine.RuntimeInitializeOnLoadMethod(
            UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Parse()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            ParseQueryString(UnityEngine.Application.absoluteURL); // ?day=12&money=5000&seed=42
#else
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++)
            {
                switch (a[i])
                {
                    case "-startDay":   StartDay   = int.Parse(a[i + 1]); break;
                    case "-startMoney": StartMoney = int.Parse(a[i + 1]); break;
                    case "-seed":       Seed       = int.Parse(a[i + 1]); break;
                    case "-fastTime":   FastTime   = true;               break;
                }
            }
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        static void ParseQueryString(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            int q = url.IndexOf('?');
            if (q < 0) return;
            var query = url.Substring(q + 1);
            foreach (var pair in query.Split('&'))
            {
                var kv = pair.Split('=');
                if (kv.Length < 2) continue;
                var key = kv[0].ToLowerInvariant();
                var val = kv[1];
                switch (key)
                {
                    case "day":   StartDay   = int.Parse(val); break;
                    case "money": StartMoney = int.Parse(val); break;
                    case "seed":  Seed       = int.Parse(val); break;
                    case "fast":  FastTime   = val == "1" || val == "true"; break;
                }
            }
        }
#endif
    }
}
