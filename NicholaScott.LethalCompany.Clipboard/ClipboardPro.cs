using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using GameNetcodeStuff;
using HarmonyLib;
using NicholaScott.BepInEx.Utils.Instancing;
using NicholaScott.BepInEx.Utils.Patching;
using UnityEngine;

namespace NicholaScott.LethalCompany.Clipboard
{
    [BepInDependency("NicholaScott.BepInEx.Utils", "1.2.0")]
    [BepInPlugin("NicholaScott.LethalCompany.Clipboard", "Clipboard Scanner", "1.0.2")]
    public class ClipboardPro : BaseUnityPlugin
    {
        private void Awake()
        {
            Singleton<ClipboardPro>.Instance = this;
            Assembly.GetExecutingAssembly().PatchAttribute<Production>(Info.Metadata.GUID);
        }
    }

    [Production]
    public static class HUDPatcher
    {
        private static float _rainbowProgress = 0f;
        private static Color NextRainbowColor()
        {
            _rainbowProgress += Time.deltaTime / 4;
            _rainbowProgress %= 1f;
            
            var div = (Math.Abs(_rainbowProgress % 1) * 6);
            var ascending = div % 1;
            var descending = 1 - ascending;

            switch ((int) div)
            {
                case 0:
                    return new Color(1, ascending, 0, 1);
                case 1:
                    return new Color(descending, 1, 0, 1);
                case 2:
                    return new Color(0, 1, ascending, 1);
                case 3:
                    return new Color(0, descending, 1, 1);
                case 4:
                    return new Color(ascending, 0, 1, 1);
                default: // case 5:
                    return new Color(1, 0, descending, 1);
            }
        }

        // [HarmonyPatch(typeof(PlayerControllerB), "Start")]
        // [HarmonyPrefix]
        // public static void UpdateIconSizes()
        // {
        //     var idx = 1;
        //     var localPos = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/Inventory/Slot0").transform.localPosition;
        //     foreach (var instanceItemSlotIconFrame in HUDManager.Instance.itemSlotIconFrames)
        //     {
        //         instanceItemSlotIconFrame.gameObject.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 30);
        //         instanceItemSlotIconFrame.gameObject.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 30);
        //         instanceItemSlotIconFrame.transform.localPosition =
        //             new Vector3(localPos.x + 44f * idx, localPos.y, localPos.z);
        //         idx++;
        //     }
        // }
        
        private static bool IsPlayerHoldingClipboard => GameNetworkManager.Instance.localPlayerController &&
            GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer &&
            GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer.itemProperties.itemName.ToLower().Contains("mapper");
        
        private static List<(ScanNodeProperties scp, string oldHeader)> _lastModified =
            new List<(ScanNodeProperties, string)>();

        private static readonly int Scan = Animator.StringToHash("scan");
        private const float UpdateTime = 0.25f;
        private const float NoiseTime = 1f;
        private const int FadeCount = 10;
        private static float updateTimer = UpdateTime;
        private static float noiseTimer = NoiseTime;
        private static int fadeCount = FadeCount;

        [HarmonyPatch(typeof(HUDManager), "MeetsScanNodeRequirements")]
        [HarmonyPrefix]
        private static void AlwaysVisibleWithClipboard(ScanNodeProperties node, ref int __state)
        {
            __state = node.maxRange;
            if (IsPlayerHoldingClipboard)
            {
                node.maxRange += 10;
            }
        }
        [HarmonyPatch(typeof(HUDManager), "MeetsScanNodeRequirements")]
        [HarmonyPostfix]
        private static void AlwaysVisibleWithClipboardAfter(ScanNodeProperties node, ref int __state)
        {
            node.maxRange = __state;
        }
        private static void DoClipboardAutoScan(bool canUpdate, ref float playerPing)
        {
            var canTrigger = noiseTimer <= 0f;
            if (canTrigger) noiseTimer = NoiseTime;
            noiseTimer -= Time.deltaTime;

            if (!IsPlayerHoldingClipboard)
            {
                fadeCount = FadeCount;
                return;
            }
            if (canUpdate) playerPing = 0.3f;
            if (!canTrigger) return;
            HUDManager.Instance.scanEffectAnimator.transform.position = GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position;
            HUDManager.Instance.scanEffectAnimator.SetTrigger(Scan);
            if (fadeCount <= 0) return;
            var volume = (float)fadeCount / FadeCount;
            fadeCount--;
            HUDManager.Instance.UIAudio.PlayOneShot(HUDManager.Instance.scanSFX, volume);
        }
        [HarmonyPatch(typeof(HUDManager), "Update")]
        [HarmonyPrefix]
        private static void UpdateClipboardStuff(float ___updateScanInterval, ref float ___playerPingingScan)
        {
            if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null ||
                GameNetworkManager.Instance.localPlayerController.inSpecialInteractAnimation ||
                GameNetworkManager.Instance.localPlayerController.isPlayerDead) return;
            var canUpdate = updateTimer <= 0f;
            if (canUpdate) updateTimer = UpdateTime;
            updateTimer -= Time.deltaTime;
            
            DoClipboardAutoScan(canUpdate, ref ___playerPingingScan);

            if (!GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom)
            {
                return;
            }

            var toBeTracked = CalculateScrapToMeetQuota(canUpdate).ToArray();
            RemoveNonExistentTrackables(toBeTracked);
            IncorporateNewTrackables(toBeTracked);
            
            RainbowAllTrackables(canUpdate);
        }

        private static void RemoveNonExistentTrackables(ScanNodeProperties[] toBeTracked)
        {
            var idx = 0;
            if (_lastModified.Count == 0 || toBeTracked.Length == 0) return;
            while (idx < _lastModified.Count)
            {
                var current = _lastModified[idx];
                if (!toBeTracked.Contains(current.scp))
                {
                    // It's here and not there so we should reset this one & remove it
                    current.scp.headerText = current.oldHeader;
                    current.scp.requiresLineOfSight = true;
                    // Don't increase index here
                    _lastModified.RemoveAt(idx);
                }
                else
                    idx++;
            }
        }
        private static void IncorporateNewTrackables(ScanNodeProperties[] toBeTracked)
        {
            foreach (var scanNodeProperties in toBeTracked)
            {
                if (!_lastModified.Exists(tuple => tuple.scp == scanNodeProperties))
                    _lastModified.Add((scanNodeProperties, scanNodeProperties.headerText));
            }
        }

        private static void RainbowAllTrackables(bool canUpdate)
        {
            var headerInjection = $"</color><color=#{ColorUtility.ToHtmlStringRGB(NextRainbowColor())}>";
            var isPlayerHolding = IsPlayerHoldingClipboard;
            foreach (var (scp, oldHeader) in _lastModified)
            {
                scp.headerText = isPlayerHolding ? headerInjection + oldHeader : oldHeader;
                if (canUpdate)
                    scp.requiresLineOfSight = !isPlayerHolding;
            }
        }
        
        private static IEnumerable<ScanNodeProperties> CalculateScrapToMeetQuota(bool canUpdate)
        {
            if (!canUpdate) yield break;
            var poolToScanFrom = GameObject.Find("/Environment/HangarShip").GetComponentsInChildren<GrabbableObject>()
                .Where(gb => gb.itemProperties.isScrap)
                .OrderBy(gb => (gb.itemProperties.twoHanded ? -1000 : 0) + gb.gameObject.GetComponentInChildren<ScanNodeProperties>().scrapValue);
                        
            var quota = TimeOfDay.Instance.profitQuota - TimeOfDay.Instance.quotaFulfilled;
            var neededForQuota = 0;
            foreach (var grabbableObject in poolToScanFrom)
            {
                var inStorage = !grabbableObject.transform.parent.name.Contains("HangarShip");
                var curScanNode = grabbableObject.gameObject.GetComponentInChildren<ScanNodeProperties>();

                if (inStorage || neededForQuota >= quota) continue;
                            
                neededForQuota += curScanNode.scrapValue;
                yield return curScanNode;
            }
        }
    }
}