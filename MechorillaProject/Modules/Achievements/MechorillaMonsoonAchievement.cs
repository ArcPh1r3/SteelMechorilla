using R2API;
using R2API.Utils;
using RoR2;
using System;
using UnityEngine;

namespace SteelMechorilla.Modules.Achievements
{
    internal class MasteryAchievement : ModdedUnlockable
    {
        public override string AchievementIdentifier { get; } = MechorillaPlugin.developerPrefix + "_MECHORILLA_BODY_MONSOONUNLOCKABLE_ACHIEVEMENT_ID";
        public override string UnlockableIdentifier { get; } = MechorillaPlugin.developerPrefix + "_MECHORILLA_BODY_MONSOONUNLOCKABLE_REWARD_ID";
        public override string AchievementNameToken { get; } = MechorillaPlugin.developerPrefix + "_MECHORILLA_BODY_MONSOONUNLOCKABLE_ACHIEVEMENT_NAME";
        public override string PrerequisiteUnlockableIdentifier { get; } = MechorillaPlugin.developerPrefix + "_MECHORILLA_BODY_UNLOCKABLE_REWARD_ID";
        public override string UnlockableNameToken { get; } = MechorillaPlugin.developerPrefix + "_MECHORILLA_BODY_MONSOONUNLOCKABLE_UNLOCKABLE_NAME";
        public override string AchievementDescToken { get; } = MechorillaPlugin.developerPrefix + "_MECHORILLA_BODY_MONSOONUNLOCKABLE_ACHIEVEMENT_DESC";
        public override Sprite Sprite { get; } = Modules.Assets.mainAssetBundle.LoadAsset<Sprite>("texMechorillaPlayerIcon");

        public override Func<string> GetHowToUnlock { get; } = (() => Language.GetStringFormatted("UNLOCK_VIA_ACHIEVEMENT_FORMAT", new object[]
                            {
                                Language.GetString(MechorillaPlugin.developerPrefix + "_MECHORILLA_BODY_MONSOONUNLOCKABLE_ACHIEVEMENT_NAME"),
                                Language.GetString(MechorillaPlugin.developerPrefix + "_MECHORILLA_BODY_MONSOONUNLOCKABLE_ACHIEVEMENT_DESC")
                            }));
        public override Func<string> GetUnlocked { get; } = (() => Language.GetStringFormatted("UNLOCKED_FORMAT", new object[]
                            {
                                Language.GetString(MechorillaPlugin.developerPrefix + "_MECHORILLA_BODY_MONSOONUNLOCKABLE_ACHIEVEMENT_NAME"),
                                Language.GetString(MechorillaPlugin.developerPrefix + "_MECHORILLA_BODY_MONSOONUNLOCKABLE_ACHIEVEMENT_DESC")
                            }));

        public override BodyIndex LookUpRequiredBodyIndex()
        {
            return BodyCatalog.FindBodyIndex("MechorillaPlayerBody");
        }
        
        public void ClearCheck(Run run, RunReport runReport)
        {
            if (run is null) return;
            if (runReport is null) return;

            if (!runReport.gameEnding) return;

            if (runReport.gameEnding.isWin)
            {
                DifficultyDef difficultyDef = DifficultyCatalog.GetDifficultyDef(runReport.ruleBook.FindDifficulty());

                if (difficultyDef != null && difficultyDef.countsAsHardMode)
                {
                    if (base.meetsBodyRequirement)
                    {
                        base.Grant();
                    }
                }
            }
        }

        public override void OnInstall()
        {
            base.OnInstall();

            Run.onClientGameOverGlobal += this.ClearCheck;
        }

        public override void OnUninstall()
        {
            base.OnUninstall();

            Run.onClientGameOverGlobal -= this.ClearCheck;
        }
    }
}