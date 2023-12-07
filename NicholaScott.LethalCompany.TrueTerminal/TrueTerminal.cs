using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using HarmonyLib;
using NicholaScott.BepInEx.Utils.Patching;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NicholaScott.LethalCompany.TrueTerminal
{
    public static class ExtensionMethods
    {
        public static void ScrollTo(this Terminal instance, TrueTerminal.VerticalDirection topOrBottom)
        {
            
            var coroutineField = typeof(Terminal)
                .GetField("forceScrollbarCoroutine", BindingFlags.Instance | BindingFlags.NonPublic);
            var forceScrollbar = typeof(Terminal)
                .GetMethod("forceScrollbar" + (topOrBottom == TrueTerminal.VerticalDirection.Top ? "Up" : "Down"),
                    BindingFlags.Instance | BindingFlags.NonPublic);

            if (coroutineField == null || forceScrollbar == null) return;

            Debug.Log("Scrolling!");
            if (coroutineField.GetValue(instance) != null)
                instance.StopCoroutine(coroutineField.GetValue(instance) as Coroutine);
            coroutineField.SetValue(instance, instance.StartCoroutine(forceScrollbar.Invoke(instance, null) as IEnumerator));
        }
    }
    [BepInPlugin("NicholaScott.LethalCompany.TrueTerminal", "True Terminal", "0.0.1")]
    public class TrueTerminal : BaseUnityPlugin
    {
        public static Terminal TerminalAccessor { get; internal set; }
        public delegate void CommandCallback(string[] parameters);
        public enum VerticalDirection { Top, Bottom}

        internal static Dictionary<string, CommandCallback> RegisteredCommands =
            new Dictionary<string, CommandCallback>();

        public static void RegisterCommand(string keyword, CommandCallback onSubmit)
        {
            RegisteredCommands[keyword] = onSubmit;
        }

        public void Awake()
        {
            Logger.LogInfo("Loaded Terminal API...");
            Assembly.GetExecutingAssembly().PatchAttribute<Production>(Info.Metadata.GUID, Logger.LogInfo);
        }
    }

    [Production]
    public static class TerminalPatches
    {
        [HarmonyPatch(typeof(Terminal), "Awake")]
        [HarmonyPostfix]
        public static void SetAccessor(Terminal __instance) => TrueTerminal.TerminalAccessor = __instance;
        
        [HarmonyPatch(typeof(Terminal), nameof(Terminal.OnSubmit))]
        [HarmonyPrefix]
        public static bool HookTerminalOnSubmit(Terminal __instance)
        {
            var hasEvaluatedToCommand = false;
            if (!__instance.terminalInUse || __instance.textAdded == 0) return true;

            var fullCommand = __instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded);
            var splitCommand = fullCommand.Split(' ');
            splitCommand = splitCommand.Length > 0 ? splitCommand : new[] { fullCommand };

            if (splitCommand.Length > 0 && splitCommand[0].StartsWith(".") && TrueTerminal.RegisteredCommands.ContainsKey(splitCommand[0].Substring(1).ToLower()))
            {
                hasEvaluatedToCommand = true;
                TrueTerminal.RegisteredCommands[splitCommand[0].Substring(1).ToLower()].Invoke(splitCommand.Skip(1).ToArray());
            }
            
            if (!hasEvaluatedToCommand)
                return true;

            __instance.GetType().GetField("modifyingText", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(__instance, true);
            __instance.screenText.text =
                __instance.screenText.text.Substring(0, __instance.screenText.text.Length - __instance.textAdded);
            __instance.currentText = __instance.screenText.text;
            __instance.textAdded = 0;
            __instance.screenText.ActivateInputField();
            __instance.screenText.Select();
            
            TrueTerminal.TerminalAccessor.ScrollTo(TrueTerminal.VerticalDirection.Bottom);
            
            return false;
        }

    } 
}