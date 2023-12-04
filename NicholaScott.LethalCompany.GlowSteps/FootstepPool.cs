using NicholaScott.BepInEx.Utils.Instancing;
using NicholaScott.LethalCompany.GlowSteps.UnityScripts;
using UnityEngine;
using UnityEngine.Pool;

namespace NicholaScott.LethalCompany.GlowSteps
{
    public class FootstepPool : ObjectPool<GlowingFootstep>
    {
        public FootstepPool() : base (
            CreateNewFootstep, 
            fs => fs.gameObject.SetActive(true), 
            fs => fs.gameObject.SetActive(false), 
            DestroyFootstep) { }

        static GlowingFootstep CreateNewFootstep()
        {
            Singleton<GlowSteps>.Logger.LogInfo(
                $"Creating new footstep object in pool. Object count {Singleton<GlowSteps>.Instance.footyManager.PooledObjects.CountAll} " +
                $"& active {Singleton<GlowSteps>.Instance.footyManager.PooledObjects.CountActive}");
            var go = new GameObject("Glow Step", typeof(GlowingFootstep));
            go.transform.SetParent(Singleton<GlowSteps>.Instance.footyManager.transform);
            return go.GetComponent<GlowingFootstep>();
        }

        static void DestroyFootstep(GlowingFootstep footstep)
        {
            Singleton<GlowSteps>.Logger.LogInfo(
                $"Destroying footstep object in pool. Object count {Singleton<GlowSteps>.Instance.footyManager.PooledObjects.CountAll} " +
                $"& active {Singleton<GlowSteps>.Instance.footyManager.PooledObjects.CountActive}");
            Object.Destroy(footstep.gameObject);
        }
    }
}