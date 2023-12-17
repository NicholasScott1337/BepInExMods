using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using Unity.Netcode;

namespace NicholaScott.LethalCompany.API
{
    [BepInPlugin("NicholaScott.LethalCompany.API", "Lethal Company API", "0.0.2")]
    public class NSLCAPI : BaseUnityPlugin { }

    public static class Extensions
    {
        public static T FindPrefab<T>(this StartOfRound instance, Predicate<T> matches) where T : class
        {
            if (typeof(T) == typeof(SpawnableMapObject))
                foreach (var level in instance.levels)
                    foreach (var mapObj in level.spawnableMapObjects)
                        if (matches.Invoke(mapObj as T))
                            return mapObj as T;
            if (typeof(T) == typeof(SpawnableOutsideObject))
                foreach (var level in instance.levels)
                    foreach (var mapObj in level.spawnableOutsideObjects)
                        if (matches.Invoke(mapObj as T))
                            return mapObj as T;
            if (typeof(T) == typeof(Item))
            {
                foreach (var item in instance.allItemsList.itemsList)
                    if (matches.Invoke(item as T))
                        return item as T;
                foreach (var item in NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs)
                    if (matches.Invoke(item.Prefab.GetComponentInChildren<Item>() as T))
                        return item.Prefab.GetComponentInChildren<Item>() as T;
            }
            if (typeof(T) == typeof(EnemyType))
                foreach (var selectableLevel in instance.levels)
                {
                    foreach (var enemy in selectableLevel.Enemies)
                        if (matches.Invoke(enemy.enemyType as T))
                            return enemy.enemyType as T;
                    foreach (var enemy in selectableLevel.DaytimeEnemies)
                        if (matches.Invoke(enemy.enemyType as T))
                            return enemy.enemyType as T;
                    foreach (var enemy in selectableLevel.OutsideEnemies)
                        if (matches.Invoke(enemy.enemyType as T))
                            return enemy.enemyType as T;
                }
            throw new Exception(
                $"The provided type {typeof(T).Name} isn't supported by {nameof(Extensions.FindPrefab)}");
        }
        public static T FindPrefabByName<T>(this StartOfRound instance, params string[] queries) where T : class
        {
            var doesItPassQueries = new Predicate<string>(f => queries.Aggregate(true,
                    (passesQuery, curQuery) =>
                    {
                        var itemToLower = f.ToLower();
                        curQuery = curQuery.ToLower();
                        return passesQuery && (curQuery.StartsWith("!")
                            ? !itemToLower.Contains(curQuery.Substring(1))
                            : itemToLower.Contains(curQuery));
                    }));
            if (typeof(T) == typeof(SpawnableMapObject))
                return instance.FindPrefab<SpawnableMapObject>(
                    f => doesItPassQueries.Invoke(f.prefabToSpawn.gameObject.name)) as T;
            if (typeof(T) == typeof(SpawnableOutsideObject))
                return instance.FindPrefab<SpawnableOutsideObject>(
                    f => doesItPassQueries.Invoke(f.prefabToSpawn.gameObject.name)) as T;
            if (typeof(T) == typeof(Item))
                return instance.FindPrefab<Item>(
                    f => doesItPassQueries.Invoke(f.itemName)) as T;
            if (typeof(T) == typeof(EnemyType))
                return instance.FindPrefab<EnemyType>(
                    f => doesItPassQueries.Invoke(f.enemyName)) as T;
            throw new Exception(
                $"The provided type {typeof(T).Name} isn't supported by {nameof(Extensions.FindPrefab)}");
        }
        
        private static Func<TPredicate, bool> CreatePredicateWithQueries<TPredicate>(Func<TPredicate, string> toString, IEnumerable<string> queries)
        {
            return item =>
            {
                return queries.Aggregate(true,
                    (passesQuery, curQuery) =>
                    {
                        var itemToLower = toString(item).ToLower();
                        curQuery = curQuery.ToLower();
                        return passesQuery && (curQuery.StartsWith("!")
                            ? !itemToLower.Contains(curQuery.Substring(1))
                            : itemToLower.Contains(curQuery));
                    });
            };
        }
        
    }
}