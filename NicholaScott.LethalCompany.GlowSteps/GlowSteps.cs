using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using NicholaScott.BepInEx.Utils;
using NicholaScott.BepInEx.Utils.Configuration;
using NicholaScott.BepInEx.Utils.Instancing;
using NicholaScott.BepInEx.Utils.Patching;
using NicholaScott.BepInEx.Utils.Resources;
using UnityEngine;
using UnityEngine.Serialization;

namespace NicholaScott.LethalCompany.GlowSteps
{
    [BepInDependency("NicholaScott.BepInEx.Utils", "1.1.0")]
    [BepInPlugin("NicholaScott.LethalCompany.GlowSteps", "Glow Steps", "1.1.1")]
    public class GlowSteps : BaseUnityPlugin
    {
        public struct Configuration
        {
            [ConfigEntryDefinition(Description = "The distance until footsteps are no longer visible.")]
            public float DistanceFalloff;
            public float SecondsUntilFade;
            [ConfigEntryDefinition(Description = "The rate to update footsteps. This should be kept <= 0.1")]
            public float UpdateRate;
            [ConfigEntryDefinition(Description = "Whether the footsteps show up only in the factory or outside as well.")]
            public bool InFactory;
            [ConfigEntryDefinition(Description = "Three normalized(0-1) numbers representing an RGB value.")]
            public Vector3 Color;
        }

        public readonly Dictionary<string, Texture2D> FootstepTexts = new Dictionary<string, Texture2D>();
        public FootstepManager footyManager;
        public void Awake()
        {
            Singleton<GlowSteps>.Instance = this;
            Singleton<GlowSteps, Configuration>.Configuration = Config.BindStruct(new Configuration()
            {
                DistanceFalloff = 20f,
                SecondsUntilFade = 60f,
                InFactory = true,
                Color = new Vector3(0.1f, 1f, 0.1f),
                UpdateRate = 0.1f,
                
            });

            var resourceDimension = new Vector2Int(512, 512);
            LoadResource("LHeavy", resourceDimension);
            LoadResource("LMedium", resourceDimension);
            LoadResource("LLight", resourceDimension);
            LoadResource("RHeavy", resourceDimension);
            LoadResource("RMedium", resourceDimension);
            LoadResource("RLight", resourceDimension);
            
            Assembly.GetExecutingAssembly().PatchAttribute<Production>(Info.Metadata.GUID, Logger.LogInfo);
        }
        private void LoadResource(string resourceName, Vector2Int dimensions)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var manifestName = assembly.GetName().Name + ".Images." + resourceName + ".png";
            var newText = new Texture2D(dimensions.x, dimensions.y);
            newText.LoadImage(assembly.GetManifestResourceStream(manifestName).ReadAllBytes());

            FootstepTexts[resourceName] = newText;
        }
    }
}