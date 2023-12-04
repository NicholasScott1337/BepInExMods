using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using NicholaScott.BepInEx.Utils.Configuration;
using NicholaScott.BepInEx.Utils.Instancing;
using NicholaScott.BepInEx.Utils.Patching;
using Unity.Netcode;

namespace NicholaScott.LethalCompany.HundredQuota
{
    [BepInPlugin("NicholaScott.LethalCompany.HundredQuota", "Hundred Quota", "0.0.1")]
    public class HundredQuota : BaseUnityPlugin
    {
        public struct Configuration
        {
            public int Experimentation;
            public int Assurance;
            public int Vow;
            public int March;
            public int Offense;
            public int Rend;
            public int Dine;
            public int Titan;
        }
        public void Awake()
        {
            Singleton<HundredQuota>.Instance = this;
            Singleton<HundredQuota, Configuration>.Configuration = Config.BindStruct(new Configuration()
            {
                Experimentation = 0,
                Assurance = 50,
                Vow = 100,
                March = 150,
                Offense = 150,
                Rend = 200,
                Dine = 300,
                Titan = 400
            });
            Assembly.GetExecutingAssembly().PatchAttribute<Production>(Info.Metadata.GUID, Logger.LogInfo);
        }
    }

    [Production]
    public static class QuotaPatcher
    {
        [HarmonyPatch(typeof(QuickMenuManager), nameof(QuickMenuManager.ConfirmKickUserFromServer))]
        [HarmonyPrefix]
        public static bool SilentKick(QuickMenuManager __instance)
        {
            var playObjToKick = (int) typeof(QuickMenuManager)
                .GetField("playerObjToKick", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(__instance);
            if (playObjToKick > 0 && playObjToKick <= 3)
            {
                __instance.ConfirmKickUserPanel.SetActive(false);
                if (!StartOfRound.Instance.IsServer) return false;
                NetworkManager.Singleton.DisconnectClient(StartOfRound.Instance.allPlayerScripts[playObjToKick].actualClientId, "No message received..");
            }

            return false;
        }
        [HarmonyPatch(typeof(DeadBodyInfo), "Update")]
        [HarmonyPostfix]
        public static void ItsInTheShip(DeadBodyInfo __instance) => __instance.isInShip = true;
        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.SetBuyingRateForDay))]
        [HarmonyPostfix]
        public static void PatchBuyRate() => PatchBuyRate(StartOfRound.Instance);
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ResetShip))]
        [HarmonyPostfix]
        public static void PatchBuyRate(StartOfRound __instance)
        {
            __instance.companyBuyingRate = 1f;
        }
        [HarmonyPatch(typeof(Terminal), "Awake")]
        [HarmonyPostfix]
        public static void PatchKeywords(Terminal __instance)
        {
            var keywordIdx = Array.FindIndex(__instance.terminalNodes.allKeywords, keyword => keyword.name == "Route");
            if (keywordIdx == -1) 
            { 
                Singleton<HundredQuota>.Logger.LogError("Couldn't find keyword 'Route' in Terminal nodes.");
                return;
            }

            foreach (var keyword in __instance.terminalNodes.allKeywords[keywordIdx].compatibleNouns)
            {
                if (!keyword.noun.name.Contains("-")) continue;
                
                var moon = keyword.noun.name.Substring(keyword.noun.name.IndexOf('-') + 1);
                var config = Singleton<HundredQuota, HundredQuota.Configuration>.Configuration;
                var configCost = (int)config.GetType().GetField(moon).GetValue(config);

                Singleton<HundredQuota>.Logger.LogInfo($"Assigning {moon} the cost {configCost}.");
                keyword.result.itemCost = configCost;
                keyword.result.terminalOptions.First(f => f.noun.name == "Confirm").result.itemCost = configCost;
                
            }
        }
    }
}