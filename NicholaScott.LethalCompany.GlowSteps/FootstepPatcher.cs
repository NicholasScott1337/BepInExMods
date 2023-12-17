using System.Collections.Generic;
using GameNetcodeStuff;
using HarmonyLib;
using NicholaScott.BepInEx.Utils.Instancing;
using NicholaScott.BepInEx.Utils.Patching;
using NicholaScott.LethalCompany.GlowSteps.UnityScripts;
using UnityEngine;

namespace NicholaScott.LethalCompany.GlowSteps
{
    [Production]
    public static class FootstepPatcher
    {
        private static Dictionary<ulong, float> _leftFoots = new Dictionary<ulong, float>();

        [HarmonyPatch(typeof(Terminal), "Start")]
        [HarmonyPostfix]
        public static void CreateFootstepManager(Terminal __instance)
        {
            if (Singleton<GlowSteps>.Instance.footyManager != null) return;
            var newGo = new GameObject("Footstep Manager", typeof(FootstepManager));
            Singleton<GlowSteps>.Instance.footyManager = newGo.GetComponent<FootstepManager>();
            Object.DontDestroyOnLoad(Singleton<GlowSteps>.Instance.footyManager.gameObject);
        }
        
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.PlayFootstepSound))]
        [HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        public static void AddPositionToTracking(PlayerControllerB __instance)
        {
            var config = Singleton<GlowSteps, GlowSteps.Configuration>.Configuration;
            var currentStrength = __instance.IsOwner ? __instance.isSprinting ? 3 : __instance.isCrouching || __instance.isExhausted || __instance.isMovementHindered != 0 ? 1 : 2 : 2;
            //if (!__instance.IsOwner) return; // Handle spawning colors for other players later, when we can actually network the behaviour
            if (!__instance.isInsideFactory && config.InFactory) return;
            if (!_leftFoots.ContainsKey(__instance.playerSteamId)) _leftFoots.Add(__instance.playerSteamId, -1.0f);

            var whichFoot = _leftFoots[__instance.playerSteamId];
            var transform = __instance.transform;
            var footRay = new Ray(
                transform.position + transform.right * 0.2f * whichFoot,
                Vector3.down);

            if (!Physics.Raycast(footRay, out var hitInfo, 10f, LayerMask.GetMask("Room", "Railing", "Default")))
                return;

            var pseudoColor = new Vector3((__instance.playerSteamId & 0xff0000) >> 16,
                (__instance.playerSteamId & 0xff00) >> 8, (__instance.playerSteamId & 0xff)) / 255;

            var footData = new GlowingFootstep.Data()
            {
                Color = __instance.IsOwner ? config.Color : pseudoColor,
                LeftFoot = (whichFoot <= 0f),
                Strength = currentStrength,
                TimeLeftAlive = config.SecondsUntilFade,
                Position = hitInfo.point + new Vector3(0f, 0.001f, 0f),
                Rotation = Quaternion.LookRotation(__instance.transform.forward * -1, hitInfo.normal)
            };
            
            Singleton<GlowSteps>.Instance.footyManager.AddNewFootstep(footData);

            _leftFoots[__instance.playerSteamId] *= -1.0f;
        }
    }
}