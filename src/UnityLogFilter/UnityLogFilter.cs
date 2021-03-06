﻿using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace KeelPlugins
{
    public class UnityLogFilter
    {
        public const string Version = "1.0.1." + BuildNumber.Version;

        public static IEnumerable<string> TargetDLLs { get; } = new string[0];
        public static void Patch(AssemblyDefinition ass) { }

        private static Harmony Harmony;
        private static ManualLogSource Logger;

        private static List<Regex> filters = new List<Regex>();
        private static string FilterFile = Path.Combine(BepInEx.Paths.ConfigPath, "UnityLogFilter.txt");

        public static void Finish()
        {
            Harmony = new Harmony(nameof(UnityLogFilter));
            Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(UnityLogFilter));

            if(File.Exists(FilterFile))
            {
                filters = File.ReadAllLines(FilterFile).Where(VerifyRegex).Select(x => new Regex(x)).ToList();
                Logger.LogInfo($"Loaded {filters.Count} filter{(filters.Count == 1 ? "" : "")}");
            }
            else
            {
                File.Create(FilterFile);
                Logger.LogInfo($"{FilterFile} created, add regular expressions to it.");
            }

            Harmony.Patch(AccessTools.Method(typeof(Chainloader), nameof(Chainloader.Initialize)),
                              postfix: new HarmonyMethod(AccessTools.Method(typeof(UnityLogFilter), nameof(ChainloaderHook))));
        }

        public static void ChainloaderHook()
        {
            Harmony.Patch(AccessTools.Method(typeof(UnityLogSource), "UnityLogMessageHandler"),
                          prefix: new HarmonyMethod(AccessTools.Method(typeof(UnityLogFilter), nameof(LogPatch))));
        }

        public static bool LogPatch(LogEventArgs eventArgs)
        {
            return !(eventArgs.Data is string msg && filters.Any(x => x.IsMatch(msg)));
        }

        public static bool VerifyRegex(string testPattern)
        {
            bool isValid = true;

            if((testPattern != null) && (testPattern.Trim().Length > 0))
            {
                try
                {
                    Regex.Match("", testPattern);
                }
                catch(ArgumentException)
                {
                    // BAD PATTERN: Syntax error
                    isValid = false;
                }
            }
            else
            {
                //BAD PATTERN: Pattern is null or blank
                isValid = false;
            }

            return isValid;
        }
    }
}
