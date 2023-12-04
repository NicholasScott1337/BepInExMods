using System.Linq;
using BepInEx;
using HarmonyLib;

namespace NicholaScott.LethalCompany.NoHornNoise
{
    [BepInPlugin("NicholaScott.LethalCompany.NoHornNoise", "No Horn Noise", "0.0.1")]
    public class NoHorns : BaseUnityPlugin
    {
        public void Awake()
        {
            var x = new Harmony(Info.Metadata.GUID);
            x.PatchAll(typeof(NoHornPatch));
            Logger.LogInfo($"Patched {x.GetPatchedMethods().Count()}");
        }
    }
}