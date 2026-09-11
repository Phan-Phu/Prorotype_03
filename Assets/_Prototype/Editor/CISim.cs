using Prototype.Application;

namespace Prototype.Application
{
    public static class CISim
    {
        public static void RunEconomy()
        {
            int days    = int.Parse(CIBuild.Arg("-simDays")  ?? "30");
            int seed    = int.Parse(CIBuild.Arg("-simSeed")  ?? "42");
            string path = CIBuild.Arg("-simOut") ?? "Artifacts/economy.csv";

            var csv = HeadlessSim.RunGreedyFarmer(days, seed);
            System.IO.File.WriteAllText(path, csv);

            UnityEngine.Debug.Log($"[CI] sim days={days} seed={seed} -> {path}");
            UnityEditor.EditorApplication.Exit(0);
        }
    }
}
