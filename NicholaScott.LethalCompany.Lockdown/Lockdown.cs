using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using NicholaScott.BepInEx.Utils.Configuration;
using NicholaScott.BepInEx.Utils.Instancing;
using NicholaScott.BepInEx.Utils.Patching;
using Unity.Netcode;
using UnityEngine;

namespace NicholaScott.LethalCompany.Lockdown
{
    [BepInDependency("NicholaScott.BepInEx.Utils", "1.2.0")]
    [BepInPlugin("NicholaScott.LethalCompany.Lockdown", "Lockdown", "1.0.3")]
    public class Lockdown : BaseUnityPlugin
    {
        public struct Configuration
        {
            public enum PermissionLevel { Unrestricted, HostOnly }
            
            [ConfigEntryDefinition(Name = "RestrictPurchasingItems", Description = "Whether any client, or only the host, can purchase items.")]
            public PermissionLevel PurchaseStoreItems;
            [ConfigEntryDefinition(Name = "RestrictPurchasingUnlockables", Description = "Whether any client, or only the host, can purchase unlockables.")]
            public PermissionLevel PurchaseStoreUnlocks;
            [ConfigEntryDefinition(Name = "RestrictRoutingShipToMoon", Description = "Whether any client, or only the host, can use the `route` command in the terminal.")]
            public PermissionLevel RouteShipToMoon;

            [ConfigEntryDefinition(Name = "RestrictPlacingObjectIntoStorage", Description = "Whether any client, or only the host, can put items into storage with the <B> key.")]
            public PermissionLevel PlaceObjectIntoStorage;
            [ConfigEntryDefinition(Name = "RestrictTakingObjectFromStorage", Description = "Whether any client, or only the host, can take items out of storage with the terminal.")]
            public PermissionLevel ReturnObjectFromStorage;
            [ConfigEntryDefinition(Name = "RestrictMovingObjectsInShip", Description = "Whether any client, or only the host, can move placeable objects in the ship.")]
            public PermissionLevel MoveObjects;
            [ConfigEntryDefinition(Name = "RestrictShipLights", Description = "Whether any client, or only the host, can toggle ship lights (except when host is not nearby).")]
            public PermissionLevel ToggleShipLights;
            [ConfigEntryDefinition(Description = "How fast time passes.")]
            public float TimeMultiplier;
        }

        public void Awake()
        {
            Singleton<Lockdown>.Instance = this;
            Singleton<Lockdown, Configuration>.Configuration = Config.BindStruct(new Configuration()
            {
                PurchaseStoreItems = Configuration.PermissionLevel.HostOnly,
                PurchaseStoreUnlocks = Configuration.PermissionLevel.HostOnly,
                RouteShipToMoon = Configuration.PermissionLevel.HostOnly,
                PlaceObjectIntoStorage = Configuration.PermissionLevel.HostOnly,
                ReturnObjectFromStorage = Configuration.PermissionLevel.HostOnly,
                MoveObjects = Configuration.PermissionLevel.HostOnly,
                ToggleShipLights = Configuration.PermissionLevel.HostOnly,
                TimeMultiplier = 0.5f
            });
            
            Assembly.GetExecutingAssembly().PatchAttribute<Production>(Info.Metadata.GUID, Logger.LogInfo);
        }
    }
    
    [Production]
    public static class LockdownPatches
    {
        private static readonly int PullLever = Animator.StringToHash("pullLever");
        private static bool IsHostSender(__RpcParams rpcParams) => (rpcParams.Server.Receive.SenderClientId == GameNetworkManager.Instance.localPlayerController.actualClientId);
        
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void ChangeTimeSpeedMultiplier(TimeOfDay __instance)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                __instance.globalTimeSpeedMultiplier = Singleton<Lockdown, Lockdown.Configuration>.Configuration.TimeMultiplier;
            }
        }
        [HarmonyPatch(typeof(Terminal), "__rpc_handler_4003509079")]
        [HarmonyPrefix]
        public static bool IgnoreClientButNotHostPurchases(NetworkBehaviour target, __RpcParams rpcParams)
        {
            var canPassExec = Singleton<Lockdown, Lockdown.Configuration>.Configuration.PurchaseStoreItems == Lockdown.Configuration.PermissionLevel.Unrestricted || IsHostSender(rpcParams);
            if (canPassExec) return true;
            Singleton<Lockdown>.Logger.LogInfo("Blocking purchase from client!");
            if (target is Terminal term)
                term.SyncGroupCreditsClientRpc(term.groupCredits, term.orderedItemsFromTerminal.Count);
            return false;
        }

        [HarmonyPatch(typeof(StartOfRound), "__rpc_handler_3953483456")]
        [HarmonyPrefix]
        public static bool IgnoreClientButNotHostPurchaseUnlockables(NetworkBehaviour target, __RpcParams rpcParams)
        {
            var canPassExec = Singleton<Lockdown, Lockdown.Configuration>.Configuration.PurchaseStoreUnlocks == Lockdown.Configuration.PermissionLevel.Unrestricted || IsHostSender(rpcParams);
            if (canPassExec) return true;
            Singleton<Lockdown>.Logger.LogInfo("Blocking unlock-able purchase from client!");
            if (target is StartOfRound startOfRound)
                startOfRound.BuyShipUnlockableClientRpc(Object.FindObjectOfType<Terminal>().groupCredits);
            return false;
        }

        [HarmonyPatch(typeof(StartOfRound), "__rpc_handler_1134466287")]
        [HarmonyPrefix]
        public static bool IgnoreClientButNotHostRouteChanges(__RpcParams rpcParams)
        {
            Singleton<Lockdown>.Logger.LogInfo("Checking route change from client.");
            return Singleton<Lockdown, Lockdown.Configuration>.Configuration.RouteShipToMoon == Lockdown.Configuration.PermissionLevel.Unrestricted || IsHostSender(rpcParams);
        }

        [HarmonyPatch(typeof(StartOfRound), "__rpc_handler_3380566632")]
        [HarmonyPrefix]
        public static bool IgnoreClientButNotHostReturnFromStorage(__RpcParams rpcParams)
        {
            Singleton<Lockdown>.Logger.LogInfo("Checking spawn unlock-able from client.");
            return Singleton<Lockdown, Lockdown.Configuration>.Configuration.ReturnObjectFromStorage == Lockdown.Configuration.PermissionLevel.Unrestricted || IsHostSender(rpcParams);
        }

        [HarmonyPatch(typeof(ShipBuildModeManager), "__rpc_handler_3086821980")]
        [HarmonyPrefix]
        public static bool IgnoreClientButNotHostPlaceInStorage(FastBufferReader reader, __RpcParams rpcParams)
        {
            var canPassExec = Singleton<Lockdown, Lockdown.Configuration>.Configuration.PlaceObjectIntoStorage == Lockdown.Configuration.PermissionLevel.Unrestricted || IsHostSender(rpcParams);
            if (canPassExec) return true;
            Singleton<Lockdown>.Logger.LogInfo("Blocking unlock-able move to storage from client!");
            reader.ReadValueSafe(out NetworkObjectReference objectRef);
            if (!objectRef.TryGet(out var networkObject)) return false;
            var placeableShipObject = networkObject.gameObject.GetComponentInChildren<PlaceableShipObject>();
            if (placeableShipObject != null)
                Object.FindObjectOfType<StartOfRound>().ReturnUnlockableFromStorageClientRpc(placeableShipObject.unlockableID);
            return false;
        }
        [HarmonyPatch(typeof(ShipBuildModeManager), "__rpc_handler_861494715")]
        [HarmonyPrefix]
        public static bool IgnoreClientButNotHostMoveCabinet(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            var canPassExec = Singleton<Lockdown, Lockdown.Configuration>.Configuration.MoveObjects == Lockdown.Configuration.PermissionLevel.Unrestricted || IsHostSender(rpcParams);
            if (canPassExec) return true;
            reader.ReadValueSafe(out Vector3 _);
            reader.ReadValueSafe(out Vector3 _);
            reader.ReadValueSafe(out NetworkObjectReference objectRef);
            if (!objectRef.TryGet(out var networkObject)) return false;
            var placeableShipObject = networkObject.gameObject.GetComponentInChildren<PlaceableShipObject>();
            if (placeableShipObject != null)
            {
                var transform = placeableShipObject.transform;
                (target as ShipBuildModeManager)?.PlaceShipObjectClientRpc(
                    transform.position,
                    transform.eulerAngles,
                    objectRef, (int)GameNetworkManager.Instance.localPlayerController.actualClientId);
            }

            return false;
        }
        [HarmonyPatch(typeof(ShipLights), "__rpc_handler_1625678258")]
        [HarmonyPrefix]
        public static bool IgnoreClientButNotHostSetLights(__RpcParams rpcParams)
        {
            var canPassExec = Singleton<Lockdown, Lockdown.Configuration>.Configuration.ToggleShipLights == Lockdown.Configuration.PermissionLevel.Unrestricted || IsHostSender(rpcParams);
            if (canPassExec) return true;
            var shipLights = Object.FindObjectOfType<ShipLights>();
            if (GameNetworkManager.Instance.localPlayerController.isPlayerDead ||
                Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, shipLights.transform.position) >= 20f)
                return true;

            shipLights.SetShipLightsClientRpc(shipLights.areLightsOn);
            return false;
        }
    }
}