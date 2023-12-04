using NicholaScott.BepInEx.Utils.Instancing;
using Unity.Netcode;
using UnityEngine;

namespace NicholaScott.LethalCompany.GlowSteps.UnityScripts
{
    public class GlowingFootstep : MonoBehaviour
    {
        public class Data : INetworkSerializable
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public float TimeLeftAlive;
            public int Strength;
            public bool LeftFoot;
            public Vector3 Color;
            public bool ShouldDraw;
            public bool IsEnabled;
            public GlowingFootstep Linked;
            public float LastDistance;
            public void UpdateFootstepData(float delta)
            {
                if (GameNetworkManager.Instance.localPlayerController != null)
                {
                    LastDistance = Vector3.Distance(Position,
                        GameNetworkManager.Instance.localPlayerController.playerEye.position);
                    var maxDist = Singleton<GlowSteps, GlowSteps.Configuration>.Configuration.DistanceFalloff;
                    if (LastDistance <= maxDist) ShouldDraw = true;
                    if (LastDistance > maxDist || TimeLeftAlive <= 0f) ShouldDraw = false;
                }
                else
                    ShouldDraw = false;
                TimeLeftAlive -= delta;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Position);
                serializer.SerializeValue(ref Rotation);
                serializer.SerializeValue(ref TimeLeftAlive);
                serializer.SerializeValue(ref Strength);
                serializer.SerializeValue(ref LeftFoot);
                serializer.SerializeValue(ref Color);
            }

            public static int SizeOf()
            {
                return sizeof(float) * 10 + sizeof(int) + sizeof(bool);
            }
        }

        private Material _materialReference;
        private GameObject _subPlane;
        private static readonly int EmissiveExposureWeight = Shader.PropertyToID("_EmissiveExposureWeight");
        private static readonly int EmissiveColorMode = Shader.PropertyToID("_EmissiveColorMode");
        private static readonly int EmissiveColor = Shader.PropertyToID("_EmissiveColor");
        private static readonly int BaseColorMap = Shader.PropertyToID("_BaseColorMap");
        private static readonly int NormalMap = Shader.PropertyToID("_NormalMap");
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");

        public void Awake()
        {
            _materialReference = CreateNewMaterial();
            _subPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _subPlane.GetComponent<Renderer>().material = _materialReference;
            _subPlane.GetComponent<Collider>().enabled = false;
            _subPlane.transform.SetParent(transform, false);
            _subPlane.transform.localScale = Vector3.one / 15f;
        }

        public void SyncAll(Data footstepData)
        {
            SyncTransform(footstepData);
            SyncMaterial(footstepData);
            SyncColorIntensity(footstepData);
        }
        
        private void SyncTransform(Data footstepData)
        {
            var transform1 = transform;
            transform1.position = footstepData.Position;
            transform1.rotation = footstepData.Rotation;
        }
        private void SyncMaterial(Data footstepData)
        {
            var imageRefKey = (footstepData.LeftFoot ? "L" : "R") + 
                              (footstepData.Strength >= 3 ? "Heavy" : (footstepData.Strength  >= 2 ? "Medium" : "Light"));
            var imageRef = Singleton<GlowSteps>.Instance.FootstepTexts[imageRefKey];
            _materialReference.SetTexture(MainTex, imageRef);
            _materialReference.SetTexture(BaseColorMap, imageRef);
        }
        public void SyncColorIntensity(Data footstepData)
        {
            //var actualMaxIntensity = footstepData.MaxIntensity / 3f * footstepData.Strength;
            var lerpValue = Mathf.Clamp(footstepData.TimeLeftAlive, 0f, 30f) / 30f;
            var conf = Singleton<GlowSteps, GlowSteps.Configuration>.Configuration;
            lerpValue = Mathf.Min(lerpValue,
                Mathf.Pow(Mathf.Clamp(2 - footstepData.LastDistance / conf.DistanceFalloff, 0f, 1f), 4));
            
            _subPlane.GetComponent<Renderer>().material.SetVector(
                EmissiveColor, new Vector4(footstepData.Color.x, footstepData.Color.y, footstepData.Color.z, 1f) * lerpValue);
        }
        private static Material CreateNewMaterial()
        {
            var newMaterial = new Material(FootstepManager.CatwalkMaterial);
            newMaterial.SetTexture(NormalMap, null);
            newMaterial.SetFloat(EmissiveColorMode, 1f);
            newMaterial.SetFloat(EmissiveExposureWeight, 1f);
            newMaterial.mainTextureScale = Vector2.one;
            newMaterial.color = Color.white;
            return newMaterial;
        }
    }
}