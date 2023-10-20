using R2API;
using System;

namespace SteelMechorilla.Modules
{
    internal static class Tokens
    {
        internal static void AddTokens()
        {
            string prefix = MechorillaPlugin.developerPrefix + "_MECHORILLA_BODY_";

            string desc = "The Steel Mechorilla is a big guy.<color=#CCD3E0>" + Environment.NewLine + Environment.NewLine;
            desc = desc + "< ! > This 'character' is not viable." + Environment.NewLine + Environment.NewLine;
            desc = desc + "< ! > He genuinely is not fun." + Environment.NewLine + Environment.NewLine;
            desc = desc + "< ! > Don't play this expecting to have a good time." + Environment.NewLine + Environment.NewLine;
            desc = desc + "< ! > Thank you." + Environment.NewLine + Environment.NewLine;

            string outro = "..and so it left, leaving irreparable damage in its wake.";
            string outroFailure = "..and so it vanished, returning to its eternal slumber.";

            string lore = "A construction Chimera designed to destroy the old Chimera Lab to make way for a new building. Seems to have gotten lost in transit at some point and went berserk as a result.";

            LanguageAPI.Add(prefix + "NAME", "Steel Mechorilla");
            LanguageAPI.Add(prefix + "DESCRIPTION", desc);
            LanguageAPI.Add(prefix + "SUBTITLE", "Mechanical Chimera");
            LanguageAPI.Add(prefix + "LORE", lore);
            LanguageAPI.Add(prefix + "OUTRO_FLAVOR", outro);
            LanguageAPI.Add(prefix + "OUTRO_FAILURE", outroFailure);

            #region Skins
            LanguageAPI.Add(prefix + "DEFAULT_SKIN_NAME", "Default");
            LanguageAPI.Add(prefix + "MONSOON_SKIN_NAME", "Gorilla");
            #endregion

            #region Passive
            //LanguageAPI.Add(prefix + "PASSIVE_NAME", "Slow Start");
            //LanguageAPI.Add(prefix + "PASSIVE_DESCRIPTION", $"<style=cIsHealth>Stats are halved</style> upon spawning. Defeating <style=cIsUtility>10 enemies</style> will restore Regigigas to <style=cIsDamage>full strength</style>.");
            #endregion

            #region Primary
            LanguageAPI.Add(prefix + "PRIMARY_GRAB_NAME", "Slam");
            LanguageAPI.Add(prefix + "PRIMARY_GRAB_DESCRIPTION", $"Slam the ground in front for <style=cIsDamage>{100f * 1.5f}% damage</style>.");

            //LanguageAPI.Add(prefix + "PRIMARY_PUNCH_NAME", "Brick Break");
            //LanguageAPI.Add(prefix + "PRIMARY_PUNCH_DESCRIPTION", $"Punch for <style=cIsDamage>{SkillStates.Regigigas.PunchCombo.damageCoefficientOverride * 100f}% damage</style>.");

            //LanguageAPI.Add(prefix + "PRIMARY_DRAINPUNCH_NAME", "Drain Punch");
            //LanguageAPI.Add(prefix + "PRIMARY_DRAINPUNCH_DESCRIPTION", $"Punch for <style=cIsDamage>{SkillStates.Regigigas.DrainPunch.damageCoefficientOverride * 100f}% damage</style>, <style=cIsHealing>healing for 50% of damage dealt</style>.");
            #endregion

            #region Secondary
            LanguageAPI.Add(prefix + "SECONDARY_GROUNDSMASH_NAME", "Ground Smash");
            LanguageAPI.Add(prefix + "SECONDARY_GROUNDSMASH_DESCRIPTION", $"Smash the ground with all your might, sending forth two <style=cIsDamage>shockwaves</style> along the ground that deal <style=cIsDamage>{100f * 2.5f}% damage</style>.");

            //LanguageAPI.Add(prefix + "SECONDARY_ANCIENTPOWER_NAME", "Ancient Power");
            //LanguageAPI.Add(prefix + "SECONDARY_ANCIENTPOWER_DESCRIPTION", $"Charge up a barrage of rocks for <style=cIsDamage>{100f * SkillStates.Regigigas.FireAncientPower.damageCoefficient}% damage</style> each. Costs <style=cIsHealth>10% max health</style> for each rock if out of stock.");
            #endregion

            #region Utility
            LanguageAPI.Add(prefix + "UTILITY_SWEEPINGBEAM_NAME", "Sweeping Beam");
            LanguageAPI.Add(prefix + "UTILITY_SWEEPINGBEAM_DESCRIPTION", "Fire a sweeping laser beam in front.");
            #endregion

            #region Special
            LanguageAPI.Add(prefix + "SPECIAL_BLAST_NAME", "Blast");
            LanguageAPI.Add(prefix + "SPECIAL_BLAST_DESCRIPTION", $"Fire a barrage of aerial blasts for <style=cIsDamage>{100f * 5}% damage</style> each.");
            #endregion

            #region Achievements
            LanguageAPI.Add(prefix + "MONSOONUNLOCKABLE_UNLOCKABLE_NAME", "Mechorilla: Mastery");
            LanguageAPI.Add(prefix + "MONSOONUNLOCKABLE_ACHIEVEMENT_NAME", "Mechorilla: Mastery");
            LanguageAPI.Add(prefix + "MONSOONUNLOCKABLE_ACHIEVEMENT_DESC", "As Steel Mechorilla, beat the game or obliterate on Monsoon.");
            #endregion
        }
    }
}