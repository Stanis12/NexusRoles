using System;
using System.Collections.Generic;

namespace NexusRoles.Roles.Patches
{
    public static class Upgrader
    {
        public static void UpdateStat(int amount, string steamId, string stat)
        {
            SetStat(GetStat(steamId, stat) + amount, steamId, stat);
        }

        public static void SetStat(int level, string steamId, string stat)
        {
            if (string.IsNullOrEmpty(steamId))
            {
                return;
            }

            Dictionary<string, int>? dictionary = GetOrCreateDictionary(stat);
            if (dictionary == null)
            {
                return;
            }

            dictionary[steamId] = Math.Max(0, level);
        }

        public static int GetStat(string steamId, string upgradeName)
        {
            if (string.IsNullOrEmpty(steamId))
            {
                return 0;
            }

            Dictionary<string, int>? dictionary = GetOrCreateDictionary(upgradeName);
            return dictionary != null && dictionary.TryGetValue(steamId, out int value) ? value : 0;
        }

        private static Dictionary<string, int>? GetOrCreateDictionary(string stat)
        {
            if (StatsManager.instance == null)
            {
                return null;
            }

            if (!StatsManager.instance.dictionaryOfDictionaries.TryGetValue(stat, out Dictionary<string, int> dictionary))
            {
                dictionary = new Dictionary<string, int>();
                StatsManager.instance.dictionaryOfDictionaries.Add(stat, dictionary);
            }

            return dictionary;
        }
    }
}
