using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace NicholaScott.LethalCompany.NoHornNoise
{
    public static class NoHornPatch
    {
        public const int ShovelMask = 11012424;
        public static bool DenyHorns(GrabbableObject __instance)
        {
            var isHorn = __instance.itemProperties.itemName.Contains("horn");
            return !isHorn;
        }

        [HarmonyPatch(typeof(GrabbableObject), "ActivateItemClientRpc")]
        [HarmonyPrefix]
        public static bool DenyOnClient(GrabbableObject __instance) => DenyHorns(__instance);
        [HarmonyPatch(typeof(GrabbableObject), "ActivateItemServerRpc")]
        [HarmonyPrefix]
        public static bool DenyOnHost(GrabbableObject __instance) => DenyHorns(__instance);
    }
}