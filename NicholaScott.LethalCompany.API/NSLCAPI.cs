using System;
using System.Linq;
using BepInEx;

namespace NicholaScott.LethalCompany.API
{
    [BepInPlugin("NicholaScott.LethalCompany.API", "Lethal Company API", "0.0.1")]
    public class NSLCAPI : BaseUnityPlugin { }

    public static class Extensions
    {
        public static SpawnableItemWithRarity FindPrefab(this StartOfRound instance, params string[] queries)
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
                if (selectableLevel.spawnableScrap.Exists(lambda))
                    return selectableLevel.spawnableScrap.Find(lambda);
            }

            throw new Exception("The provided queries resulted in no prefab in all levels.");
        }
    }
}