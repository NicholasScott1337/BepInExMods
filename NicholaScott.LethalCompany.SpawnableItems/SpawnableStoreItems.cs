using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using NicholaScott.BepInEx.Utils.Configuration;
using NicholaScott.BepInEx.Utils.Instancing;
using NicholaScott.BepInEx.Utils.Patching;
using NicholaScott.LethalCompany.API;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NicholaScott.LethalCompany.SpawnableItems
{
    [BepInDependency("NicholaScott.BepInEx.Utils", "1.2.0")]
    [BepInPlugin("NicholaScott.LethalCompany.SpawnableStoreItems", "Spawnable Store Items", "0.1.0")]
    public class SpawnableStoreItems : BaseUnityPlugin
    {
        private const string GenRelCategory = "General/RelativeRarities";
        private const string WipRelCategory = "WIP/RelativeRarities";
        private const string GenDescription = "The rarity relative to all other entries. Set to 0 to disable this item";
        public struct Configuration
        {
            [ConfigEntryDefinition(Category = "General", Description = "Maximum number of items that can spawn inside the factory.")]
            public int MaxItemsSpawningInside;
            [ConfigEntryDefinition(Category = WipRelCategory, Description = GenDescription)]
            public int Binoculars;
            [ConfigEntryDefinition(Category = WipRelCategory, Description = GenDescription)]
            public int Mapper;
            [ConfigEntryDefinition(Name = "ShotgunShell", Category = GenRelCategory, Description = GenDescription)]
            public int Ammo;
            [ConfigEntryDefinition(Category = GenRelCategory, Description = GenDescription)]
            public int WalkieTalkie;
            [ConfigEntryDefinition(Category = GenRelCategory, Description = GenDescription)]
            public int Flashlight;
            [ConfigEntryDefinition(Category = GenRelCategory, Description = GenDescription)]
            public int Shovel;
            [ConfigEntryDefinition(Category = GenRelCategory, Description = GenDescription)]
            public int LockPicker;
            [ConfigEntryDefinition(Category = GenRelCategory, Description = GenDescription)]
            public int ProFlashlight;
            [ConfigEntryDefinition(Category = GenRelCategory, Description = GenDescription)]
            public int StunGrenade;
            [ConfigEntryDefinition(Category = GenRelCategory, Description = GenDescription)]
            public int Boombox;
            [ConfigEntryDefinition(Category = GenRelCategory, Description = GenDescription)]
            public int ExtensionLadder;
            [ConfigEntryDefinition(Category = GenRelCategory, Description = GenDescription)]
            public int RadarBooster;
            [ConfigEntryDefinition(Category = GenRelCategory, Description = GenDescription)]
            public int SprayPaint;
            [ConfigEntryDefinition(Category = GenRelCategory, Description = GenDescription)]
            public int TZPInhalant;
            [ConfigEntryDefinition(Category = GenRelCategory, Description = GenDescription)]
            public int ZapGun;
            [ConfigEntryDefinition(Category = GenRelCategory, Description = GenDescription)]
            public int Jetpack;
        }
        private void Awake()
        {
            Singleton<SpawnableStoreItems>.Instance = this;
            Singleton<SpawnableStoreItems, Configuration>.Configuration = Config.BindStruct(new Configuration()
            {
                MaxItemsSpawningInside = 30,
                Binoculars = 0,
                Mapper = 0,
                Ammo = 5,
                WalkieTalkie = 0,
                Flashlight = 15,
                Shovel = 0,
                LockPicker =  0,
                ProFlashlight = 0,
                StunGrenade = 0,
                Boombox = 1,
                ExtensionLadder = 5,
                RadarBooster = 0,
                SprayPaint = 20,
                TZPInhalant = 1,
                ZapGun = 0,
                Jetpack = 0
            });
            Assembly.GetExecutingAssembly().PatchAttribute<Production>(Info.Metadata.GUID, Logger.LogInfo);
        }
    }

    [Production]
    public static class SpawnPatcher
    {
        private static Dictionary<string, CachedItem<int>> cachedConfigRarities = new Dictionary<string, CachedItem<int>>();
        private static SpawnableItemWithRarity ItemQualifies(Item item)
        {
            var cfgType = typeof(SpawnableStoreItems.Configuration);
            var cfg = Singleton<SpawnableStoreItems, SpawnableStoreItems.Configuration>.Configuration;
            var cleanItemName = item.itemName.Replace("-", "").Replace(" ", "").ToLower();

            if (!cachedConfigRarities.ContainsKey(cleanItemName))
                cachedConfigRarities.Add(cleanItemName, new CachedItem<int>(() =>
                {
                    var matches = cfgType.GetFields().Where(it => it.Name.ToLower() == cleanItemName).ToArray();
                    if (matches.Any())
                        return (int)matches.First().GetValue(cfg);
                    return 0;
                }));
            
            var itemWithRarity = new SpawnableItemWithRarity()
            {
                rarity = cachedConfigRarities[cleanItemName],
                spawnableItem = item
            };
                
            Singleton<SpawnableStoreItems>.Logger.LogInfo($"[{itemWithRarity.rarity}] {item.itemName}");
            
            return itemWithRarity;
        }

        private static int GetRandomWeightedIndex(System.Random random, int[] weights)
        {
            var sumOfAllPercentages = (float)weights.Where(t => t >= 0).Sum();
            if (sumOfAllPercentages <= 0)
                return random.Next(0, weights.Length);
            var seededThreshold = (float)random.NextDouble();
            var accumulator = 0f;
            for (var i = 0; i < weights.Length; i++)
            {
                if (weights[i] <= 0) continue;
                accumulator += weights[i] / sumOfAllPercentages;
                if (accumulator >= seededThreshold)
                {
                    return i;
                }
            }
            return random.Next(0, weights.Length);
        }
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnScrapInLevel))]
        [HarmonyPostfix]
        public static void SpawnStoreItemsInsideFactory(RoundManager __instance)
        {
            var itemsFromElsewhere = StartOfRound.Instance.allItemsList.itemsList
                .Where(item => !item.isScrap && item.spawnPrefab)
                .Select(ItemQualifies)
                .ToArray();
            var storeItemsWeights = itemsFromElsewhere.Select(f => f.rarity).ToArray();
            var scrapSpawns = Object.FindObjectsOfType<RandomScrapSpawn>().Where(s => !s.spawnUsed).ToList();
            
            var ranGen = new System.Random(StartOfRound.Instance.randomMapSeed - 7);
            var numOfItemsToSpawn = ranGen.Next( Singleton<SpawnableStoreItems, SpawnableStoreItems.Configuration>.Configuration.MaxItemsSpawningInside / 2, Singleton<SpawnableStoreItems, SpawnableStoreItems.Configuration>.Configuration.MaxItemsSpawningInside);

            var numOfItemsSpawned = 0;
            while (numOfItemsSpawned < numOfItemsToSpawn && scrapSpawns.Count > 0)
            {
                var ranIdx = ranGen.Next(0, scrapSpawns.Count);
                var scrapSpawn = scrapSpawns[ranIdx];
                var pos = scrapSpawn.transform.position;
                if (scrapSpawn.spawnedItemsCopyPosition)
                {
                    scrapSpawns.RemoveAt(ranIdx);
                }
                else
                {
                    pos = RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(scrapSpawn.transform.position,
                        scrapSpawn.itemSpawnRange, RoundManager.Instance.navHit);
                }
                
                var whichItem = GetRandomWeightedIndex(ranGen, storeItemsWeights);

                var go = Object.Instantiate(itemsFromElsewhere[whichItem].spawnableItem.spawnPrefab, pos + Vector3.up * 0.5f, 
                    Quaternion.identity, StartOfRound.Instance.propsContainer);
                go.GetComponent<GrabbableObject>().fallTime = 0f;
                go.GetComponent<NetworkObject>().Spawn(false);
                numOfItemsSpawned++;
            }
        }
    }
}