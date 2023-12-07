using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using HarmonyLib;
using NicholaScott.BepInEx.Utils.Configuration;
using NicholaScott.BepInEx.Utils.Instancing;
using NicholaScott.BepInEx.Utils.Patching;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NicholaScott.LethalCompany.HundredQuota
{
    [BepInDependency("NicholaScott.BepInEx.Utils", "1.1.0")]
    [BepInDependency("NicholaScott.LethalCompany.TrueTerminal", "0.0.1")]
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

        public bool PrefabExists(string query, out GameObject target)
        {
            foreach (var pref in NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs)
            {
                if (pref.Prefab.name.ToLower().Contains(query))
                {
                    target = pref.Prefab;
                    return true;
                }
            }
            
            Logger.LogInfo($"Failed to locate a prefab that matches the name {query}!");
            target = null;
            return false;
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

            
            TrueTerminal.TrueTerminal.RegisterCommand("network.prefabs.list", parameters =>
            {
                var x = new StringBuilder();
                foreach (var pref in NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs)
                {
                    x.Append($"{pref.Prefab.name}, ");
                }
                GUIUtility.systemCopyBuffer = x.ToString();
                Logger.LogInfo("Prefabs list copied to clipboard!");
            });
            TrueTerminal.TrueTerminal.RegisterCommand("network.prefabs.spawn.bind", parameters =>
            {
                if (parameters.Length == 0)
                {
                    Logger.LogError("You must provide a prefab name from .network.prefabs.list");
                    return;
                }

                if (!PrefabExists(parameters[0], out var target)) return;
                QuotaPatcher.SpawnPrefab = target;
                QuotaPatcher.SpawnPrefabAssigned = true;
            });
            TrueTerminal.TrueTerminal.RegisterCommand("network.prefabs.spawn", parameters =>
            {
                if (parameters.Length == 0)
                {
                    Logger.LogError("You must provide a prefab name from .network.prefabs.list");
                    return;
                }

                if (!PrefabExists(parameters[0], out var target)) return;
                var playerTarget = GameNetworkManager.Instance.localPlayerController;
                if (parameters.Length > 1)
                    playerTarget = StartOfRound.Instance.allPlayerScripts.First(pl =>
                        pl.playerUsername.ToLower().Contains(parameters[1].ToLower()));
                    
                Logger.LogInfo($"Spawning {target.name} @ player {playerTarget.playerUsername}");
                    
                var go = Instantiate(target, 
                    RoundManager.Instance.GetRandomNavMeshPositionInRadius(playerTarget.gameplayCamera.transform.position), Quaternion.Euler(Vector3.zero));
                go.gameObject.GetComponentInChildren<NetworkObject>().Spawn(true);
            });
        }
    }

    [Production]
    public static class QuotaPatcher
    {
        public static GameObject SpawnPrefab;
        public static bool SpawnPrefabAssigned = false;

        public static SpawnableItemWithRarity FindPrefab(params string[] queries)
        {
            var lambda = new Predicate<SpawnableItemWithRarity>((f) =>
            {
                var toLowered = f.spawnableItem.itemName.ToLower();
                var matches = true;
                foreach (var query in queries)
                    if (query.StartsWith("!") ? toLowered.Contains(query.Substring(1)) : !toLowered.Contains(query))
                        matches = false;
                return matches;
            });
            foreach (var selectableLevel in StartOfRound.Instance.levels)
            {
                var scrapSorted = selectableLevel.spawnableScrap.Where(lambda)
                if (selectableLevel.spawnableScrap.Exists(lambda))
                    return selectableLevel.spawnableScrap.Find(lambda);
            }

            throw new Exception("The provided queries resulted in no prefab in all levels.");
        }
        
        [HarmonyPatch(typeof(RoundManager), "waitForScrapToSpawnToSync")]
        [HarmonyPrefix]
        public static void InjectAdditionalScrapItems(ref NetworkObjectReference[] spawnedScrap, ref int[] scrapValues)
        {
            var newRandom = new System.Random(StartOfRound.Instance.randomMapSeed - 13);
            var numberOfScrapToGenerate = Mathf.RoundToInt(Mathf.Pow(2, StartOfRound.Instance.daysPlayersSurvivedInARow + 1));
            numberOfScrapToGenerate = newRandom.Next(numberOfScrapToGenerate / 2, numberOfScrapToGenerate);
            var netObjRef = new NetworkObjectReference[numberOfScrapToGenerate];
            var scrapPrices = new int[numberOfScrapToGenerate];
            var spawnableScrapArray = RoundManager.Instance.currentLevel.spawnableScrap;

            var goldItem = FindPrefab("gold", "bar");
            
            for (var idx = 0; idx < numberOfScrapToGenerate; idx++)
            {
                var spawnPositions = GameObject.FindGameObjectsWithTag("OutsideAINode");
                var spawnPos = spawnPositions[newRandom.Next(spawnPositions.Length)];
                var positionInRadius = RoundManager.Instance.GetRandomNavMeshPositionInRadius(spawnPos.transform.position, 4f);
                var newGo = Object.Instantiate<GameObject>(
                    goldItem.spawnableItem.spawnPrefab, 
                    positionInRadius, Quaternion.identity, RoundManager.Instance.spawnedScrapContainer);
            
                var grabbable = newGo.GetComponent<GrabbableObject>();
                grabbable.transform.rotation = Quaternion.Euler(grabbable.itemProperties.restingRotation);
                grabbable.fallTime = 0.0f;
                grabbable.scrapValue = Mathf.RoundToInt(newRandom.Next(goldItem.spawnableItem.minValue, goldItem.spawnableItem.maxValue) *
                                                  RoundManager.Instance.scrapValueMultiplier);
                var netObj = newGo.GetComponent<NetworkObject>();
                netObj.Spawn();
            
                netObjRef[idx] = (NetworkObjectReference)netObj;
                scrapPrices[idx] = grabbable.scrapValue;
            }
            HUDManager.Instance.AddTextToChatOnServer($"Placed {numberOfScrapToGenerate} Gold Bar{(numberOfScrapToGenerate > 1 ? "s" : "")} in exterior.");
            spawnedScrap = netObjRef.Concat(spawnedScrap).ToArray();
            scrapValues = scrapPrices.Concat(scrapValues).ToArray();
        }
        
        // [HarmonyPatch(typeof(HoarderBugAI), "ChooseNestPosition")]
        // [HarmonyPostfix]
        // public static void SpawnHoarderLootPile(HoarderBugAI __instance)
        // {
        //     var newRandom = new System.Random(StartOfRound.Instance.randomMapSeed - 13);
        //     var scrapSyncCoroutine = typeof(RoundManager).GetMethod("waitForScrapToSpawnToSync",
        //         BindingFlags.Instance | BindingFlags.NonPublic);
        //
        //     var numberOfScrapToGenerate = newRandom.Next(20);
        //     var netObjRef = new NetworkObjectReference[numberOfScrapToGenerate];
        //     var scrapValues = new int[numberOfScrapToGenerate];
        //
        //     for (var idx = 0; idx < numberOfScrapToGenerate; idx++)
        //     {
        //         var spawnableScrapArray = RoundManager.Instance.currentLevel.spawnableScrap;
        //         var positionInRadius = RoundManager.Instance.GetRandomNavMeshPositionInRadius(__instance.nestPosition, 4f);
        //         var scrapToSpawn = spawnableScrapArray[newRandom.Next(spawnableScrapArray.Count())];
        //         var newGo = Object.Instantiate<GameObject>(
        //             scrapToSpawn.spawnableItem.spawnPrefab, 
        //             positionInRadius, Quaternion.identity, RoundManager.Instance.spawnedScrapContainer);
        //
        //         var grabbable = newGo.GetComponent<GrabbableObject>();
        //         grabbable.transform.rotation = Quaternion.Euler(grabbable.itemProperties.restingRotation);
        //         grabbable.fallTime = 0.0f;
        //         grabbable.scrapValue = Mathf.RoundToInt(newRandom.Next(scrapToSpawn.spawnableItem.minValue, scrapToSpawn.spawnableItem.maxValue) *
        //                                           RoundManager.Instance.scrapValueMultiplier);
        //         var netObj = newGo.GetComponent<NetworkObject>();
        //         netObj.Spawn();
        //
        //         netObjRef[idx] = (NetworkObjectReference)netObj;
        //         scrapValues[idx] = grabbable.scrapValue;
        //     }
        //
        //     Singleton<HundredQuota>.Logger.LogWarning($"We've successfully injected a loot pile of size {numberOfScrapToGenerate} @ position {__instance.nestPosition}");
        //     HUDManager.Instance.DisplayTip("Lootbug", $"Spawned {numberOfScrapToGenerate} items at position {__instance.nestPosition}");
        //     RoundManager.Instance.StartCoroutine(scrapSyncCoroutine?.Invoke(RoundManager.Instance, new object[] {netObjRef, scrapValues}) as IEnumerator);
        // }
        [HarmonyPatch(typeof(Terminal), "Update")]
        [HarmonyPrefix]
        public static void CheckForInput(Terminal __instance)
        {
            if (!UnityInput.Current.GetKeyUp(KeyCode.Insert)) return;
            var ply = GameNetworkManager.Instance.localPlayerController;
            var transform = ply.gameplayCamera.transform;
            var ray = new Ray(transform.position, transform.forward);
            if (!Physics.Raycast(ray, out var hitInfo, 100f,
                    StartOfRound.Instance.collidersAndRoomMaskAndDefault)) return;

            SpawnPrefab = RoundManager.Instance.currentLevel.DaytimeEnemies
                .First(f => f.enemyType.enemyName.ToLower().Contains("bee") && !f.enemyType.enemyName.ToLower().Contains("docile")).enemyType.enemyPrefab;
            
            var go = Object.Instantiate(SpawnPrefab, 
                hitInfo.point - transform.forward * 0.5f, Quaternion.Euler(transform.forward));
            go.gameObject.GetComponentInChildren<NetworkObject>().Spawn(true);
        }
        
        [HarmonyPatch(typeof(PatcherTool), nameof(PatcherTool.StopShockingServerRpc))]
        [HarmonyPostfix]
        public static void BlowThatBitchUp(PatcherTool __instance)
        {
            var target = __instance.shockedTargetScript;
            if (target == null) return;
            var targetHittable = target.GetNetworkObject().transform.GetComponentInChildren<IHittable>();
            if (targetHittable == null) return;
            
            Landmine.SpawnExplosion(target.GetShockablePosition(), true, 3f, 1f);
            targetHittable.Hit(10, Vector3.forward, __instance.playerHeldBy);
        } 
        
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
        // [HarmonyPatch(typeof(DeadBodyInfo), "Update")]
        // [HarmonyPostfix]
        // public static void ItsInTheShip(DeadBodyInfo __instance) => __instance.isInShip = true;
        // [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.SetBuyingRateForDay))]
        // [HarmonyPostfix]
        // public static void PatchBuyRate() => PatchBuyRate(StartOfRound.Instance);
        // [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ResetShip))]
        // [HarmonyPostfix]
        // public static void PatchBuyRate(StartOfRound __instance)
        // {
        //     __instance.companyBuyingRate = 1f;
        // }
        // [HarmonyPatch(typeof(Terminal), "Awake")]
        // [HarmonyPostfix]
        // public static void PatchKeywords(Terminal __instance)
        // {
        //     var keywordIdx = Array.FindIndex(__instance.terminalNodes.allKeywords, keyword => keyword.name == "Route");
        //     if (keywordIdx == -1) 
        //     { 
        //         Singleton<HundredQuota>.Logger.LogError("Couldn't find keyword 'Route' in Terminal nodes.");
        //         return;
        //     }
        //
        //     foreach (var keyword in __instance.terminalNodes.allKeywords[keywordIdx].compatibleNouns)
        //     {
        //         if (!keyword.noun.name.Contains("-")) continue;
        //         
        //         var moon = keyword.noun.name.Substring(keyword.noun.name.IndexOf('-') + 1);
        //         var config = Singleton<HundredQuota, HundredQuota.Configuration>.Configuration;
        //         var configCost = (int)config.GetType().GetField(moon).GetValue(config);
        //
        //         Singleton<HundredQuota>.Logger.LogInfo($"Assigning {moon} the cost {configCost}.");
        //         keyword.result.itemCost = configCost;
        //         keyword.result.terminalOptions.First(f => f.noun.name == "Confirm").result.itemCost = configCost;
        //         
        //     }
        // }
    }
}