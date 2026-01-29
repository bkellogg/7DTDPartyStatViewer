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
  psv cache:clear - Clear pending requests
  psv cache:info  - Show pending request info";
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
                    SkillDataCache.ClearAll();
                    Log.Out("[PartyStatViewer] Cache cleared.");
                    break;

                case "cache:info":
                    var stats = SkillDataCache.GetStats();
                    Log.Out($"[PartyStatViewer] Pending responses: {stats.responseCount}, Pending requests: {stats.pendingCount}");
                    break;

                default:
                    Log.Out($"[PartyStatViewer] Unknown subcommand: {subcommand}");
                    Log.Out("[PartyStatViewer] Subcommands: cache:clear, cache:info");
                    break;
            }
        }
    }
}
