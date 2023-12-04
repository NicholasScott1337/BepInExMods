using System.Collections.Generic;
using System.Linq;
using NicholaScott.BepInEx.Utils.Instancing;
using NicholaScott.LethalCompany.GlowSteps.UnityScripts;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace NicholaScott.LethalCompany.GlowSteps
{
    public class FootstepManager : MonoBehaviour
    {
        public readonly List<GlowingFootstep.Data> FootstepData = new List<GlowingFootstep.Data>();
        public readonly FootstepPool PooledObjects = new FootstepPool();

        public static Material CatwalkMaterial
        {
            get
            {
                if (_catwalkMaterial == null)
                    _catwalkMaterial = GameObject.Find("Environment/HangarShip/CatwalkShip")
                        ?.GetComponent<Renderer>()?.material;
                return _catwalkMaterial;
            }
        }
        private static Material _catwalkMaterial;

        public void Start()
        {
            InvokeRepeating(nameof(UpdateAllFootstepData), 1f, Singleton<GlowSteps, GlowSteps.Configuration>.Configuration.UpdateRate);
        }

        public void AddNewFootstep(GlowingFootstep.Data footstepData)
        {
            FootstepData.Add(footstepData);
        }
        public void UpdateAllFootstepData()
        {
            if (CatwalkMaterial == null) return;
            for (var i = 0; i < FootstepData.Count; i++)
            {
                var dataInList = FootstepData[i];
                dataInList.UpdateFootstepData(Singleton<GlowSteps, GlowSteps.Configuration>.Configuration.UpdateRate);
                
                if (dataInList.IsEnabled && dataInList.ShouldDraw)
                {
                    dataInList.Linked.SyncColorIntensity(dataInList);
                }
                if (!dataInList.IsEnabled && dataInList.ShouldDraw)
                {
                    var objToDraw = PooledObjects.Get();
                    dataInList.IsEnabled = true;
                    dataInList.Linked = objToDraw;
                    objToDraw.SyncAll(dataInList);
                }
                if (dataInList.IsEnabled && !dataInList.ShouldDraw)
                {
                    PooledObjects.Release(dataInList.Linked);
                    dataInList.IsEnabled = false;
                }

                FootstepData[i] = dataInList;
            }

            FootstepData.RemoveAll(d => !d.IsEnabled && !d.ShouldDraw && d.TimeLeftAlive <= 0f);
        }
    }
}