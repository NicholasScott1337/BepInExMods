using System.Reflection;
using BepInEx;
using HarmonyLib;
using NicholaScott.BepInEx.Utils.Configuration;
using NicholaScott.BepInEx.Utils.Instancing;
using NicholaScott.BepInEx.Utils.Patching;

namespace NicholaScott.LethalCompany.ControlDeathVoting
{
    [BepInDependency("NicholaScott.BepInEx.Utils", "1.1.0")]
    [BepInPlugin("NicholaScott.LethalCompany.ControlDeathVoting", "Control Death Voting", "1.0.1")]
    public class ControlDeathVoting : BaseUnityPlugin
    {
        public struct Configuration
        {
            public enum VoteType { Vanilla, MoreThanOne, NoVoting }
            
            [ConfigEntryDefinition(Description = "Vanilla changes nothing. MoreThanOne requires at least 2 votes to leave. NoVoting disables early ship voting entirely.")]
            public VoteType VoteHandling;
            [ConfigEntryDefinition(Description = "Multiplied by the default value of time to wait once a successful vote is cast before the ship leaves.")]
            public float ScaleShipLeaveDelay;
        }

        public void Awake()
        {
            Singleton<ControlDeathVoting>.Instance = this;
            Singleton<ControlDeathVoting, Configuration>.Configuration = Config.BindStruct(new Configuration()
            {
                VoteHandling = Configuration.VoteType.MoreThanOne,
                ScaleShipLeaveDelay = 1f
            });

            Assembly.GetExecutingAssembly().PatchAttribute<Production>(Info.Metadata.GUID, Logger.LogInfo);
        }
    }
    [Production]
    // ReSharper disable once ClassNeverInstantiated.Global
    public static class VotingPatches
    {
        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.SetShipLeaveEarlyServerRpc))]
        [HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        public static bool IgnoreVotesIfServer(TimeOfDay __instance)
        {
            var configuration = Singleton<ControlDeathVoting, ControlDeathVoting.Configuration>.Configuration;
            switch (configuration.VoteHandling)
            {
                case ControlDeathVoting.Configuration.VoteType.Vanilla:
                    return true;
                case ControlDeathVoting.Configuration.VoteType.MoreThanOne:
                    __instance.votesForShipToLeaveEarly++;
                    if (__instance.votesForShipToLeaveEarly > 1 &&
                        __instance.votesForShipToLeaveEarly >= (StartOfRound.Instance.connectedPlayersAmount + 1 - StartOfRound.Instance.livingPlayers))
                        __instance.SetShipLeaveEarlyClientRpc(__instance.normalizedTimeOfDay + (0.1f * configuration.ScaleShipLeaveDelay), __instance.votesForShipToLeaveEarly);
                    else
                        __instance.AddVoteForShipToLeaveEarlyClientRpc();
                    return false;
                case ControlDeathVoting.Configuration.VoteType.NoVoting:
                    __instance.votesForShipToLeaveEarly++;
                    __instance.AddVoteForShipToLeaveEarlyClientRpc();
                    return false;
                default:
                    return false;

            }
        }
    }
}