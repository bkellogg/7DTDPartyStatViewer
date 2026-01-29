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
        // NOTE: Verify against game's Data/Config/progression.xml during testing
        public static readonly Dictionary<string, (string progressionName, string displayName)> PerkBookMappings =
            new Dictionary<string, (string progressionName, string displayName)>()
            {
                { "bookArtOfMining", ("perkArtOfMining", "Art of Mining") },
                { "bookAutoWeapons", ("perkAutoWeapons", "Automatic Weapons") },
                { "bookBatterUp", ("perkBatterUp", "Batter Up!") },
                { "bookBarBrawling", ("perkBarBrawling", "Bar Brawling") },
                { "bookFiremansAlmanac", ("perkFiremansAlmanac", "Fireman's Almanac") },
                { "bookGreatHeist", ("perkGreatHeist", "The Great Heist") },
                { "bookHuntingJournal", ("perkHuntersJournal", "Hunter's Journal") },
                { "bookLuckyLooter", ("perkLuckyLooter", "Lucky Looter") },
                { "bookEnforcer", ("perkEnforcer", "Magnum Enforcer") },
                { "bookNightStalker", ("perkNightStalker", "Night Stalker") },
                { "bookPistolPete", ("perkPistolPete", "Pistol Pete") },
                { "bookRangers", ("perkRangersGuide", "Ranger's Guide") },
                { "bookShotgunMessiah", ("perkShotgunMessiah", "Shotgun Messiah") },
                { "bookSledgeSaga", ("perkSledgeSaga", "Sledge Saga") },
                { "bookSniper", ("perkSniper", "Sniper") },
                { "bookSpearHunter", ("perkSpearHunter", "Spear Hunter") },
                { "bookUrbanCombat", ("perkUrbanCombat", "Urban Combat") },
                { "bookTechJunkie", ("perkTechJunkie", "Tech Junkie") },
                { "bookWasteTreasures", ("perkWastelandTreasures", "Wasteland Treasures") },
            };

        // Crafting Skill Magazines - item name -> (skill progression, max level, display name)
        // Item names use pattern: <skillName>SkillMagazine
        public static readonly Dictionary<string, (string skill, int maxLevel, string displayName)> CraftingMagazineMappings =
            new Dictionary<string, (string skill, int maxLevel, string displayName)>()
            {
                { "harvestingToolsSkillMagazine", ("cftHarvestingTools", 100, "Harvesting Tools") },
                { "repairToolsSkillMagazine", ("cftRepairTools", 50, "Repair Tools") },
                { "salvageToolsSkillMagazine", ("cftSalvageTools", 75, "Salvage Tools") },
                { "knucklesSkillMagazine", ("cftKnuckles", 75, "Knuckles") },
                { "bladesSkillMagazine", ("cftBlades", 75, "Blades") },
                { "clubsSkillMagazine", ("cftClubs", 75, "Clubs") },
                { "sledgehammersSkillMagazine", ("cftSledgehammers", 75, "Sledgehammers") },
                { "bowsSkillMagazine", ("cftBows", 75, "Bows") },
                { "spearsSkillMagazine", ("cftSpears", 75, "Spears") },
                { "handgunsSkillMagazine", ("cftHandguns", 100, "Handgun Crafting") },
                { "shotgunsSkillMagazine", ("cftShotguns", 100, "Shotgun Crafting") },
                { "riflesSkillMagazine", ("cftRifles", 100, "Rifle Crafting") },
                { "machineGunsSkillMagazine", ("cftMachineGuns", 100, "Machine Gun Crafting") },
                { "explosivesSkillMagazine", ("cftExplosives", 100, "Explosives") },
                { "roboticsSkillMagazine", ("cftRobotics", 100, "Robotics") },
                { "armorSkillMagazine", ("cftArmor", 100, "Armor Crafting") },
                { "medicalSkillMagazine", ("cftMedical", 75, "Medical") },
                { "foodSkillMagazine", ("cftFood", 100, "Cooking") },
                { "seedSkillMagazine", ("cftSeeds", 20, "Farming") },
                { "electricianSkillMagazine", ("cftElectrician", 100, "Electrician") },
                { "trapsSkillMagazine", ("cftTraps", 75, "Traps") },
                { "workstationSkillMagazine", ("cftWorkstations", 75, "Workstations") },
                { "vehiclesSkillMagazine", ("cftVehicles", 100, "Vehicles") },
            };
    }
}
