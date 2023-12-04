using BepInEx;

namespace NicholaScott.LethalCompany.NetworkTest
{
    [BepInPlugin("NicholaScott.LethalCompany.NetworkTest", "Network Testing", "0.0.1")]
    public class TestingPlugin : BaseUnityPlugin
    {
        public void Awake()
        {
            Logger.LogInfo("Network Testing Loaded");
        }
    }
}