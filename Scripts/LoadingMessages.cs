using System;
using PartyStatViewer.NetPackages;

namespace PartyStatViewer
{
    public static class LoadingMessages
    {
        private static readonly Random _random = new Random();

        private static readonly string[] PerkBookMessages = new[]
        {
            "Flipping through pages...",
            "Checking bookmarks...",
            "Reading the fine print...",
            "Consulting the index...",
            "Skimming chapters...",
            "Looking for dog-eared pages...",
        };

        private static readonly string[] CraftingMagazineMessages = new[]
        {
            "Browsing the latest issue...",
            "Checking the subscription list...",
            "Flipping to the good articles...",
            "Scanning the table of contents...",
            "Reading reader reviews...",
            "Looking at the pictures...",
        };

        // Specific messages for certain skill types
        private static readonly string[] CookingMessages = new[]
        {
            "Taste-testing recipes...",
            "Checking ingredient lists...",
            "Preheating the oven...",
        };

        private static readonly string[] MedicalMessages = new[]
        {
            "Consulting medical records...",
            "Checking vital signs...",
            "Reviewing patient charts...",
        };

        private static readonly string[] VehicleMessages = new[]
        {
            "Checking under the hood...",
            "Revving the engine...",
            "Kicking the tires...",
        };

        private static readonly string[] WeaponMessages = new[]
        {
            "Checking the armory...",
            "Counting ammo...",
            "Cleaning the barrel...",
        };

        private static readonly string[] WorkstationMessages = new[]
        {
            "Firing up the forge...",
            "Warming up the workbench...",
            "Gathering blueprints...",
        };

        private static readonly string[] ExplosivesMessages = new[]
        {
            "Handling with care...",
            "Checking the fuse...",
            "Standing back...",
        };

        private static readonly string[] SchematicMessages = new[]
        {
            "Unrolling the blueprint...",
            "Studying the diagrams...",
            "Checking the parts list...",
            "Squinting at the fine print...",
            "Comparing notes...",
            "Deciphering the instructions...",
        };

        public static string GetLoadingMessage(SkillType skillType, string seriesId)
        {
            // Check for specific skill types first
            string[] specificMessages = GetSpecificMessages(seriesId);
            if (specificMessages != null && _random.Next(2) == 0) // 50% chance to use specific
            {
                return specificMessages[_random.Next(specificMessages.Length)];
            }

            // Fall back to general type messages
            string[] messages;
            if (skillType == SkillType.PerkBook)
                messages = PerkBookMessages;
            else if (skillType == SkillType.Schematic)
                messages = SchematicMessages;
            else
                messages = CraftingMagazineMessages;

            return messages[_random.Next(messages.Length)];
        }

        private static string[] GetSpecificMessages(string seriesId)
        {
            if (seriesId == null) return null;

            string lower = seriesId.ToLower();

            if (lower.Contains("food") || lower.Contains("cooking"))
                return CookingMessages;
            if (lower.Contains("medical"))
                return MedicalMessages;
            if (lower.Contains("vehicle"))
                return VehicleMessages;
            if (lower.Contains("workstation"))
                return WorkstationMessages;
            if (lower.Contains("explosive"))
                return ExplosivesMessages;
            if (lower.Contains("handgun") || lower.Contains("shotgun") ||
                lower.Contains("rifle") || lower.Contains("machinegun"))
                return WeaponMessages;

            return null;
        }
    }
}
