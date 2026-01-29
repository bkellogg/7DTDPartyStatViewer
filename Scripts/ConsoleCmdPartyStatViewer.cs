using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace PartyStatViewer
{
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    public class ConsoleCmdPartyStatViewer : ConsoleCmdAbstract
    {
        public override string[] getCommands()
        {
            return new[] { "partystatviewer", "psv" };
        }

        public override string getDescription()
        {
            return "PartyStatViewer commands - psv <subcommand>";
        }

        public override string getHelp()
        {
            return @"PartyStatViewer commands:
  psv cache:clear - Clear the skill data cache
  psv cache:info  - Show cache info";
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            if (_params.Count == 0)
            {
                Log.Out("[PartyStatViewer] Usage: psv <subcommand>");
                Log.Out("[PartyStatViewer] Subcommands: cache:clear, cache:info");
                return;
            }

            string subcommand = _params[0].ToLower();

            switch (subcommand)
            {
                case "cache:clear":
                    SkillDataCache.InvalidateAll();
                    Log.Out("[PartyStatViewer] Cache cleared.");
                    break;

                case "cache:info":
                    var info = SkillDataCache.GetCacheInfo();
                    Log.Out($"[PartyStatViewer] Cache entries: {info.entryCount}, Pending requests: {info.pendingCount}");
                    foreach (var entry in info.entries)
                    {
                        int age = (int)(System.DateTime.Now - entry.cachedAt).TotalSeconds;
                        int playerCount = entry.playerSkills?.Count ?? 0;
                        string players = "";
                        if (entry.playerSkills != null)
                        {
                            foreach (var p in entry.playerSkills)
                            {
                                if (players.Length > 0) players += ", ";
                                players += $"{p.playerName}:{p.currentLevel}";
                            }
                        }
                        Log.Out($"  [{entry.displayName}] {entry.seriesId} - {playerCount} player(s), age: {age}s, expired: {entry.IsExpired}");
                        if (players.Length > 0)
                            Log.Out($"    Players: {players}");
                    }
                    break;

                default:
                    Log.Out($"[PartyStatViewer] Unknown subcommand: {subcommand}");
                    Log.Out("[PartyStatViewer] Subcommands: cache:clear, cache:info");
                    break;
            }
        }
    }
}
