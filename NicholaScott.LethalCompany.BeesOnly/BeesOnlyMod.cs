using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using NicholaScott.BepInEx.Utils.Instancing;
using NicholaScott.BepInEx.Utils.Patching;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NicholaScott.LethalCompany.BeesOnly
{
    [BepInDependency("NicholaScott.BepInEx.Utils", "1.1.0")]
    [BepInPlugin("NicholaScott.LethalCompany.BeesOnly", "Bee Hives Only", "0.0.1")]
    public class BeesOnlyMod : BaseUnityPlugin
    {
        public void Awake()
        {
            Assembly.GetExecutingAssembly().PatchAttribute<Production>(Info.Metadata.GUID, Logger.LogInfo);
            Singleton<BeesOnlyMod>.Instance = this;
        }
    }

    [Production]
    public static class BeesOnlyPatches
    {
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.GeneratedFloorPostProcessing))]
        [HarmonyPrefix]
        public static void GenerateExtraBeehives(RoundManager __instance)
        {
            if (!__instance.IsServer) return;
            var query = RoundManager.Instance.currentLevel.DaytimeEnemies.Where(enemy =>
                enemy.enemyType.enemyName.ToLower().Contains("bee") &&
                !enemy.enemyType.enemyName.ToLower().Contains("docile"));
            var spawnableEnemyWithRarities = query.ToList();
            if (!spawnableEnemyWithRarities.Any()) return;

            var spawnPrefab = spawnableEnemyWithRarities.First().enemyType.enemyPrefab;
            var ran = new System.Random(StartOfRound.Instance.randomMapSeed - 35);

            var spawnPoints = GameObject.FindGameObjectsWithTag("OutsideAINode");
            HUDManager.Instance.DisplayTip("New Hive Number", Math.Pow(2, StartOfRound.Instance.daysPlayersSurvivedInARow + 1).ToString());
            for (var idx = 0; idx < Math.Pow(2, StartOfRound.Instance.daysPlayersSurvivedInARow + 1); idx++)
            {
                var positionInRadius = __instance.GetRandomNavMeshPositionInRadius(spawnPoints[ran.Next(spawnPoints.Length)].transform.position, 4f);
                var go = Object.Instantiate(spawnPrefab, 
                    positionInRadius, Quaternion.Euler(Vector3.zero));
                go.gameObject.GetComponentInChildren<NetworkObject>().Spawn(true);
            }
        }
    } 
}