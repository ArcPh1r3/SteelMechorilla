using System;
using BepInEx;
using R2API.Utils;
using RoR2;
using System.Security;
using System.Security.Permissions;
using UnityEngine;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace SteelMechorilla
{
    [BepInDependency("com.Moffein.RiskyArtifacts", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin(MODUID, MODNAME, MODVERSION)]
    [R2APISubmoduleDependency(new string[]
    {
        "PrefabAPI",
        "LanguageAPI",
        "SoundAPI",
        "DirectorAPI",
        "LoadoutAPI",
        "UnlockableAPI",
        "NetworkingAPI",
        "RecalculateStatsAPI",
    })]

    public class MechorillaPlugin : BaseUnityPlugin
    {
        public const string MODUID = "com.rob.SteelMechorilla";
        public const string MODNAME = "SteelMechorilla";
        public const string MODVERSION = "1.0.2";

        public const string developerPrefix = "ROB";

        public static MechorillaPlugin instance;

        public static bool riskyArtifactsInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.Moffein.RiskyArtifacts");

        private void Awake()
        {
            instance = this;

            Log.Init(Logger);
            Modules.Config.ReadConfig();
            Modules.Assets.PopulateAssets();
            //Modules.Config.ReadConfig();
            Modules.CameraParams.InitializeParams();
            Modules.States.RegisterStates();
            Modules.Buffs.RegisterBuffs();
            Modules.Projectiles.RegisterProjectiles();
            Modules.Tokens.AddTokens();
            Modules.ItemDisplays.PopulateDisplays();
            Modules.NetMessages.RegisterNetworkMessages();

            new Modules.Enemies.Mechorilla().CreateCharacter();

            Hook();

            new Modules.ContentPacks().Initialize();

            RoR2.ContentManagement.ContentManager.onContentPacksAssigned += LateSetup;
        }

        private void LateSetup(global::HG.ReadOnlyArray<RoR2.ContentManagement.ReadOnlyContentPack> obj)
        {
            Modules.Enemies.Mechorilla.SetItemDisplays();
        }

        private void Hook()
        {
            //On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
            R2API.RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, R2API.RecalculateStatsAPI.StatHookEventArgs args) {

            /*if (sender.HasBuff(Modules.Buffs.armorBuff)) {

                args.armorAdd += 500f;
            }

            if (sender.HasBuff(Modules.Buffs.slowStartBuff)) {

                args.armorAdd += 20f;
                args.moveSpeedReductionMultAdd += 1f; //movespeed *= 0.5f // 1 + 1 = divide by 2?
                args.attackSpeedMultAdd -= 0.5f; //attackSpeed *= 0.5f;
                args.damageMultAdd -= 0.5f; //damage *= 0.5f;
            }*/
        }

        private void CharacterBody_RecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            //if (self)
            //{
            //    if (self.HasBuff(Modules.Buffs.armorBuff))
            //    {
            //        self.armor += 500f;
            //    }

            //    if (self.HasBuff(Modules.Buffs.slowStartBuff))
            //    {
            //        self.armor += 20f;
            //        self.moveSpeed *= 0.5f;
            //        self.attackSpeed *= 0.5f;
            //        self.damage *= 0.5f;
            //    }
            //}
        }
    }
}