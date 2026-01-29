using System;
using System.Collections.Generic;
using PartyStatViewer.NetPackages;

namespace PartyStatViewer
{
    public static class BookTypeDetector
    {
        public struct BookInfo
        {
            public bool isValid;
            public SkillType type;
            public string seriesId;
            public string displayName;
            public int maxLevel;
        }

        public static BookInfo GetBookInfo(ItemClass itemClass)
        {
            if (itemClass == null)
                return new BookInfo { isValid = false };

            string itemName = itemClass.Name;

            // Check Perk Books first
            foreach (var kvp in PerkBookMappings)
            {
                if (itemName.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return new BookInfo
                    {
                        isValid = true,
                        type = SkillType.PerkBook,
                        seriesId = kvp.Value.progressionName,
                        displayName = kvp.Value.displayName,
                        maxLevel = 7
                    };
                }
            }

            // Check Crafting Skill Magazines
            foreach (var kvp in CraftingMagazineMappings)
            {
                if (itemName.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return new BookInfo
                    {
                        isValid = true,
                        type = SkillType.CraftingMagazine,
                        seriesId = kvp.Value.skill,
                        displayName = kvp.Value.displayName,
                        maxLevel = kvp.Value.maxLevel
                    };
                }
            }

            return new BookInfo { isValid = false };
        }

        // Perk Books (7 volumes each) - item name prefix -> (progression name, display name)
        // Progression names use pattern: skill<Name> (from progression.xml book_group entries)
        public static readonly Dictionary<string, (string progressionName, string displayName)> PerkBookMappings =
            new Dictionary<string, (string progressionName, string displayName)>()
            {
                { "bookArtOfMining", ("skillArtOfMining", "Art of Mining") },
                { "bookAutoWeapons", ("skillAutoWeapons", "Automatic Weapons") },
                { "bookBatterUp", ("skillBatterUp", "Batter Up!") },
                { "bookBarBrawling", ("skillBarBrawling", "Bar Brawling") },
                { "bookFiremansAlmanac", ("skillFiremansAlmanac", "Fireman's Almanac") },
                { "bookGreatHeist", ("skillGreatHeist", "The Great Heist") },
                { "bookHuntingJournal", ("skillHuntingJournal", "Hunter's Journal") },
                { "bookLuckyLooter", ("skillLuckyLooter", "Lucky Looter") },
                { "bookEnforcer", ("skillEnforcer", "Magnum Enforcer") },
                { "bookNightStalker", ("skillNightStalker", "Night Stalker") },
                { "bookPistolPete", ("skillPistolPete", "Pistol Pete") },
                { "bookRangers", ("skillArchery", "Ranger's Guide") },
                { "bookShotgunMessiah", ("skillShotguns", "Shotgun Messiah") },
                { "bookSledgeSaga", ("skillSledgeSaga", "Sledge Saga") },
                { "bookSniper", ("skillSniper", "Sniper") },
                { "bookSpearHunter", ("skillSpearHunter", "Spear Hunter") },
                { "bookUrbanCombat", ("skillUrbanCombat", "Urban Combat") },
                { "bookTechJunkie", ("skillTechJunkie", "Tech Junkie") },
                { "bookWasteTreasures", ("skillWasteTreasures", "Wasteland Treasures") },
            };

        // Crafting Skill Magazines - item name -> (skill progression, max level, display name)
        // Item names use pattern: <skillName>SkillMagazine
        // Progression names use pattern: crafting<SkillName> (from progression.xml)
        public static readonly Dictionary<string, (string skill, int maxLevel, string displayName)> CraftingMagazineMappings =
            new Dictionary<string, (string skill, int maxLevel, string displayName)>()
            {
                { "harvestingToolsSkillMagazine", ("craftingHarvestingTools", 100, "Harvesting Tools") },
                { "repairToolsSkillMagazine", ("craftingRepairTools", 50, "Repair Tools") },
                { "salvageToolsSkillMagazine", ("craftingSalvageTools", 75, "Salvage Tools") },
                { "knucklesSkillMagazine", ("craftingKnuckles", 75, "Knuckles") },
                { "bladesSkillMagazine", ("craftingBlades", 75, "Blades") },
                { "clubsSkillMagazine", ("craftingClubs", 75, "Clubs") },
                { "sledgehammersSkillMagazine", ("craftingSledgehammers", 75, "Sledgehammers") },
                { "bowsSkillMagazine", ("craftingBows", 75, "Bows") },
                { "spearsSkillMagazine", ("craftingSpears", 75, "Spears") },
                { "handgunsSkillMagazine", ("craftingHandguns", 100, "Handgun Crafting") },
                { "shotgunsSkillMagazine", ("craftingShotguns", 100, "Shotgun Crafting") },
                { "riflesSkillMagazine", ("craftingRifles", 100, "Rifle Crafting") },
                { "machineGunsSkillMagazine", ("craftingMachineGuns", 100, "Machine Gun Crafting") },
                { "explosivesSkillMagazine", ("craftingExplosives", 100, "Explosives") },
                { "roboticsSkillMagazine", ("craftingRobotics", 100, "Robotics") },
                { "armorSkillMagazine", ("craftingArmor", 100, "Armor Crafting") },
                { "medicalSkillMagazine", ("craftingMedical", 75, "Medical") },
                { "foodSkillMagazine", ("craftingFood", 100, "Cooking") },
                { "seedSkillMagazine", ("craftingSeeds", 20, "Farming") },
                { "electricianSkillMagazine", ("craftingElectrician", 100, "Electrician") },
                { "trapsSkillMagazine", ("craftingTraps", 75, "Traps") },
                { "workstationSkillMagazine", ("craftingWorkstations", 75, "Workstations") },
                { "vehiclesSkillMagazine", ("craftingVehicles", 100, "Vehicles") },
            };
    }
}
