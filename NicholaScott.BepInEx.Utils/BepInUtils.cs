using BepInEx;

namespace NicholaScott.BepInEx.Utils
{
    [BepInPlugin("NicholaScott.BepInEx.Utils", "BepInUtils", "1.2.0")]
    public class BepInUtils : BaseUnityPlugin
    {
        public void Awake()
        {
            Logger.LogInfo("System loaded.");
        }
    }
}