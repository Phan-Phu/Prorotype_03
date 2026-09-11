namespace Prototype.Application
{
    public static class CICompile
    {
        public static void Run()
        {
            UnityEngine.Debug.Log("[CI] Compile OK");
            UnityEditor.EditorApplication.Exit(0);
        }
    }
}
