using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace NicholaScott.LethalCompany.NoHornNoise
{
    public static class NoHornPatch
    {
        public static bool DenyHorns(GrabbableObject __instance)
        {
            var isHorn = __instance.itemProperties.itemName.Contains("horn");
            var localPlayer = GameNetworkManager.Instance.localPlayerController;
            if (isHorn && __instance.playerHeldBy.actualClientId == localPlayer.actualClientId)
            {
                var transform = localPlayer.gameplayCamera.transform;
                var nRay = new Ray(transform.position, transform.forward);
                if (Physics.Raycast(nRay, out var hitInfo, 100f, StartOfRound.Instance.collidersAndRoomMaskAndPlayers))
                {
                    Landmine.SpawnExplosion(hitInfo.point, true, 5f, 3f);
                }
                return true;
            }
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