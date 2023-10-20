using BepInEx.Configuration;
using SteelMechorilla.SkillStates.Mechorilla;
using R2API;
using RoR2;
using RoR2.Skills;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using RoR2.CharacterAI;
using RoR2.Navigation;
//using SteelMechorilla.Modules.Misc;
using RoR2.Orbs;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;

namespace SteelMechorilla.Modules.Enemies
{
    internal class Mechorilla
    {
        internal static Mechorilla instance;

        internal static GameObject characterPrefab;
        internal static GameObject survivorPrefab;
        internal static GameObject displayPrefab;

        internal static GameObject bossMaster;
        internal static GameObject umbraMaster;

        internal static ConfigEntry<bool> characterEnabled;
        internal static ConfigEntry<bool> enemyEnabled;
        internal static ConfigEntry<int> minimumStageCount;
        internal static ConfigEntry<int> spawnCost;

        public const string bodyName = "MechorillaBody";

        public static int bodyRendererIndex; // use this to store the rendererinfo index containing our character's body
                                             // keep it last in the rendererinfos because teleporter particles for some reason require this. hopoo pls

        // item display stuffs
        internal static ItemDisplayRuleSet itemDisplayRuleSet;
        internal static List<ItemDisplayRuleSet.KeyAssetRuleGroup> itemDisplayRules;

        internal static UnlockableDef masteryUnlockableDef;

        internal void CreateCharacter()
        {
            instance = this;

            enemyEnabled = Modules.Config.EnemyEnableConfig("Steel Mechorilla");
            characterEnabled = Modules.Config.CharacterEnableConfig("Steel Mechorilla (Playable)");

            if (enemyEnabled.Value || characterEnabled.Value)
            {
                if (characterEnabled.Value) masteryUnlockableDef = R2API.UnlockableAPI.AddUnlockable<Achievements.MasteryAchievement>();

                characterPrefab = CreateBodyPrefab(false);
                survivorPrefab = CreateBodyPrefab(true);

                CharacterBody survivorBody = survivorPrefab.GetComponent<CharacterBody>();
                survivorBody.baseMaxHealth = 480f;
                survivorBody.levelMaxHealth = 144f;

                displayPrefab = Modules.Prefabs.CreateDisplayPrefab("MechorillaDisplay", characterPrefab);

                if (characterEnabled.Value) Modules.Prefabs.RegisterNewSurvivor(survivorPrefab, displayPrefab, "MECHORILLA");

                bossMaster = CreateMaster(characterPrefab, "MechorillaMaster");
                umbraMaster = CreateMaster(survivorPrefab, "MechorillaMonsterMaster");

                if (enemyEnabled.Value) CreateSpawnCard();
            }

            Hook();
        }

        private static GameObject CreateBodyPrefab(bool isPlayer)
        {
            string name = bodyName;
            if (isPlayer) name = "MechorillaPlayerBody";

            string iconName = "Mechorilla";
            if (isPlayer) iconName = "MechorillaPlayer";

            #region Body
            GameObject newPrefab = Modules.Prefabs.CreatePrefab(name, "mdlMechorilla", new BodyInfo
            {
                armor = 20f,
                armorGrowth = 0f,
                bodyName = name,
                bodyNameToken = MechorillaPlugin.developerPrefix + "_MECHORILLA_BODY_NAME",
                bodyColor = Color.grey,
                characterPortrait = Modules.Assets.LoadCharacterIcon(iconName),
                crosshair = Modules.Assets.LoadCrosshair("SimpleDot"),
                damage = 40f,
                healthGrowth = 630f,
                healthRegen = 0f,
                jumpCount = 1,
                maxHealth = 2100f,
                subtitleNameToken = MechorillaPlugin.developerPrefix + "_MECHORILLA_BODY_SUBTITLE",
                podPrefab = null,
                moveSpeed = 6f,
                jumpPower = 35f,
                attackSpeed = 2f
            });

            CharacterBody body = newPrefab.GetComponent<CharacterBody>();
            body.hideCrosshair = true;
            body.hullClassification = HullClassification.Golem;
            body.bodyFlags = CharacterBody.BodyFlags.None;
            body.isChampion = true;
            body.preferredInitialStateType = new EntityStates.SerializableEntityStateType(typeof(SpawnState));

            CharacterMotor motor = newPrefab.GetComponent<CharacterMotor>();
            motor.mass = 10000f;

            SfxLocator sfx = newPrefab.GetComponent<SfxLocator>();
            sfx.barkSound = "";
            sfx.landingSound = "Play_gravekeeper_land";
            sfx.deathSound = "";
            sfx.fallDamageSound = "";

            FootstepHandler footstep = newPrefab.GetComponentInChildren<FootstepHandler>();
            footstep.footstepDustPrefab = Resources.Load<GameObject>("Prefabs/GenericHugeFootstepDust");
            footstep.baseFootstepString = "Play_moonBrother_step";
            footstep.sprintFootstepOverrideString = "Play_moonBrother_sprint";

            CharacterCameraParams regiParams = Modules.CameraParams.defaultCameraParams;

            KinematicCharacterMotor characterController = newPrefab.GetComponent<KinematicCharacterMotor>();
            characterController.CapsuleRadius = 4f;
            characterController.CapsuleHeight = 9f;

            CharacterDirection direction = newPrefab.GetComponent<CharacterDirection>();
            direction.turnSpeed = 135f;

            Interactor interactor = newPrefab.GetComponent<Interactor>();
            interactor.maxInteractionDistance = 8f;

            newPrefab.GetComponent<CameraTargetParams>().cameraParams = regiParams;

            newPrefab.GetComponent<EntityStateMachine>().mainStateType = new EntityStates.SerializableEntityStateType(typeof(MainState));

            var state = isPlayer ? typeof(EntityStates.SpawnTeleporterState) : typeof(SpawnState);
            newPrefab.GetComponent<EntityStateMachine>().initialStateType = new EntityStates.SerializableEntityStateType(state);

            newPrefab.GetComponent<CharacterDeathBehavior>().deathState = new EntityStates.SerializableEntityStateType(typeof(DeathState));

            newPrefab.GetComponentInChildren<ModelLocator>().normalizeToFloor = false;

            MechorillaPlugin.Destroy(newPrefab.GetComponent<SetStateOnHurt>());

            if (!isPlayer)
            {
                DeathRewards deathRewards = newPrefab.AddComponent<DeathRewards>();
                deathRewards.logUnlockableDef = Resources.Load<UnlockableDef>("UnlockableDefs/Logs.Parent.0");
                deathRewards.bossPickup = new SerializablePickupIndex
                {
                    pickupName = "ItemIndex.Pearl"
                };
            }
            #endregion
            
            #region Model
            Material bodyMat = Modules.Assets.CreateMaterial("matMechorilla", 5f, Color.white);

            bodyRendererIndex = 0;

            Modules.Prefabs.SetupCharacterModel(newPrefab, new CustomRendererInfo[] {
                new CustomRendererInfo
                {
                    childName = "Model",
                    material = bodyMat
                }}, bodyRendererIndex);
            #endregion

            CreateHitboxes(newPrefab);
            SetupHurtboxes(newPrefab);
            CreateSkills(newPrefab);
            CreateSkins(newPrefab);
            InitializeItemDisplays(newPrefab);

            return newPrefab;
        }

        private static void CreateSpawnCard()
        {
            minimumStageCount = MechorillaPlugin.instance.Config.Bind<int>(new ConfigDefinition("Steel Mechorilla", "Minimum Stage Clear Count"), 0, new ConfigDescription("Number of stages that must be completed before this boss can spawn"));
            spawnCost = MechorillaPlugin.instance.Config.Bind<int>(new ConfigDefinition("Steel Mechorilla", "Spawn Cost"), 600, new ConfigDescription("How many director credits does this boss cost"));

            CharacterSpawnCard characterSpawnCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
            characterSpawnCard.name = "cscMechorilla";
            characterSpawnCard.prefab = bossMaster;
            characterSpawnCard.sendOverNetwork = true;
            characterSpawnCard.hullSize = HullClassification.BeetleQueen;
            characterSpawnCard.nodeGraphType = MapNodeGroup.GraphType.Ground;
            characterSpawnCard.requiredFlags = NodeFlags.None;
            characterSpawnCard.forbiddenFlags = NodeFlags.TeleporterOK;
            characterSpawnCard.directorCreditCost = spawnCost.Value;
            characterSpawnCard.occupyPosition = false;
            characterSpawnCard.loadout = new SerializableLoadout();
            characterSpawnCard.noElites = false;
            characterSpawnCard.forbiddenAsBoss = false;

            DirectorCard card = new DirectorCard
            {
                spawnCard = characterSpawnCard,
                selectionWeight = 1,
                preventOverhead = false,
                minimumStageCompletions = minimumStageCount.Value,
                spawnDistance = DirectorCore.MonsterSpawnDistance.Close
            };

            DirectorAPI.DirectorCardHolder spawnCard = new DirectorAPI.DirectorCardHolder
            {
                Card = card,
                MonsterCategory = DirectorAPI.MonsterCategory.Champions,
            };

            //DirectorCard cardGrove = new DirectorCard
            //{
            //    spawnCard = characterSpawnCard,
            //    selectionWeight = 2,
            //    preventOverhead = false,
            //    minimumStageCompletions = minimumStageCount.Value,
            //    spawnDistance = DirectorCore.MonsterSpawnDistance.Close
            //};
            //DirectorAPI.DirectorCardHolder regigigasCardGrove = new DirectorAPI.DirectorCardHolder
            //{
            //    Card = cardGrove,
            //    MonsterCategory = DirectorAPI.MonsterCategory.Champions,
            //};

            DirectorCard cardLoop = new DirectorCard {
                spawnCard = characterSpawnCard,
                selectionWeight = 1,
                preventOverhead = false,
                minimumStageCompletions = 5,
                spawnDistance = DirectorCore.MonsterSpawnDistance.Close
            };

            DirectorAPI.DirectorCardHolder spawnCardLoop = new DirectorAPI.DirectorCardHolder {
                Card = cardLoop,
                MonsterCategory = DirectorAPI.MonsterCategory.Champions,
            };

            DirectorCardCategorySelection dissonanceSpawns = Addressables.LoadAssetAsync<DirectorCardCategorySelection>("RoR2/Base/MixEnemy/dccsMixEnemy.asset").WaitForCompletion();
            dissonanceSpawns.AddCard(0, card);  //0 is Champions

            foreach (StageSpawnInfo ssi in Config.StageList) {
                DirectorAPI.DirectorCardHolder toAdd = ssi.GetMinStages() == 0 ? spawnCard : spawnCardLoop;

                DirectorAPI.Helpers.AddNewMonsterToStage(toAdd, false, DirectorAPI.ParseInternalStageName(ssi.GetStageName()), ssi.GetStageName());
            }

            //DirectorAPI.MonsterActions += delegate (List<DirectorAPI.DirectorCardHolder> list, DirectorAPI.StageInfo stage)
            //{
            //    if (stage.stage == DirectorAPI.Stage.SirensCall 
            //    || stage.stage == DirectorAPI.Stage.RallypointDelta 
            //    || stage.stage == DirectorAPI.Stage.GildedCoast 
            //    || stage.stage == DirectorAPI.Stage.TitanicPlains 
            //    || stage.stage == DirectorAPI.Stage.VoidCell 
            //    || stage.stage == DirectorAPI.Stage.AbandonedAqueduct 
            //    || stage.stage == DirectorAPI.Stage.WetlandAspect)
            //    {
            //        if (!list.Contains(regigigasCard))
            //        {
            //            list.Add(regigigasCard);
            //        }
            //    }

            //    if (stage.stage == DirectorAPI.Stage.Custom && stage.CustomStageName == "rootjungle")
            //    {
            //        if (!list.Contains(regigigasCard))
            //        {
            //            list.Add(regigigasCardGrove);
            //        }
            //    }
            //};
        }

        private static void SetupHurtboxes(GameObject bodyPrefab)
        {
            HurtBoxGroup hurtboxGroup = bodyPrefab.GetComponentInChildren<HurtBoxGroup>();
            List<HurtBox> hurtboxes = new List<HurtBox>();

            hurtboxes.Add(bodyPrefab.GetComponentInChildren<ChildLocator>().FindChild("MainHurtbox").GetComponent<HurtBox>());

            HealthComponent healthComponent = bodyPrefab.GetComponent<HealthComponent>();

            foreach (Collider i in bodyPrefab.GetComponent<ModelLocator>().modelTransform.GetComponentsInChildren<Collider>())
            {
                if (i.gameObject.name != "MainHurtbox")
                {
                    HurtBox hurtbox = i.gameObject.AddComponent<HurtBox>();
                    hurtbox.gameObject.layer = LayerIndex.entityPrecise.intVal;
                    hurtbox.healthComponent = healthComponent;
                    hurtbox.isBullseye = false;
                    hurtbox.damageModifier = HurtBox.DamageModifier.Normal;
                    hurtbox.hurtBoxGroup = hurtboxGroup;

                    hurtboxes.Add(hurtbox);
                }
            }

            //creating weakpoint hitboxes from code becuase the unity project is lost
            Vector3[] eyeSpots = new Vector3[] 
            {
                new Vector3(0,  -0.0341f, 0.7026f),
                
                //all his eyes made it way too easy to hit
                //also these are for chest not head
                //new Vector3(0,  1.0314f, 0.7869f),//top
                //new Vector3(0,  0.6466f, 0.7848f),//bottom
                //new Vector3(0.1653f, 0.8316f, 0.6549f),//left
                //new Vector3(-0.1653f, 0.8316f, 0.6549f),//right

                //new Vector3(0,  0.3124f, 0.8173f),
                //new Vector3(0, -0.0335f, 0.7869f),
                //new Vector3(0, -0.3514f, 0.7100f),
            };

            Transform chest = bodyPrefab.GetComponentInChildren<ChildLocator>().FindChild("Head");

            HurtBox eyeHurtbox = UnityEngine.Object.Instantiate(hurtboxes[0], chest);
            eyeHurtbox.isSniperTarget = true;
            CapsuleCollider collider = eyeHurtbox.transform.GetComponent<CapsuleCollider>();
            collider.radius = 0.2f;
            collider.height = collider.radius * 2;

            eyeHurtbox.transform.localPosition = eyeSpots[0];

            hurtboxes.Add(eyeHurtbox);

            for (int i = 1; i < eyeSpots.Length; i++) {

                eyeHurtbox = UnityEngine.Object.Instantiate(eyeHurtbox, chest);
                eyeHurtbox.transform.localPosition = eyeSpots[i];

                hurtboxes.Add(eyeHurtbox);
            }

            hurtboxGroup.hurtBoxes = hurtboxes.ToArray();
        }

        private static GameObject CreateMaster(GameObject bodyPrefab, string masterName)
        {
            GameObject newMaster = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterMasters/LemurianMaster"), masterName, true);
            newMaster.GetComponent<CharacterMaster>().bodyPrefab = bodyPrefab;

            #region AI
            foreach (AISkillDriver ai in newMaster.GetComponentsInChildren<AISkillDriver>())
            {
                MechorillaPlugin.DestroyImmediate(ai);
            }

            newMaster.GetComponent<BaseAI>().fullVision = true;

            AISkillDriver blastDriver = newMaster.AddComponent<AISkillDriver>();
            blastDriver.customName = "Blast";
            blastDriver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
            blastDriver.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
            blastDriver.activationRequiresAimConfirmation = true;
            blastDriver.activationRequiresTargetLoS = false;
            blastDriver.selectionRequiresTargetLoS = true;
            blastDriver.maxDistance = 80f;
            blastDriver.minDistance = 8f;
            blastDriver.requireSkillReady = true;
            blastDriver.aimType = AISkillDriver.AimType.AtCurrentEnemy;
            blastDriver.ignoreNodeGraph = true;
            blastDriver.moveInputScale = 1f;
            blastDriver.driverUpdateTimerOverride = 2.5f;
            blastDriver.buttonPressType = AISkillDriver.ButtonPressType.Hold;
            blastDriver.minTargetHealthFraction = Mathf.NegativeInfinity;
            blastDriver.maxTargetHealthFraction = Mathf.Infinity;
            blastDriver.minUserHealthFraction = Mathf.NegativeInfinity;
            blastDriver.maxUserHealthFraction = 0.5f;
            blastDriver.skillSlot = SkillSlot.Special;

            AISkillDriver revengeDriver = newMaster.AddComponent<AISkillDriver>();
            revengeDriver.customName = "Beam";
            revengeDriver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
            revengeDriver.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
            revengeDriver.activationRequiresAimConfirmation = true;
            revengeDriver.activationRequiresTargetLoS = false;
            revengeDriver.selectionRequiresTargetLoS = true;
            revengeDriver.maxDistance = 24f;
            revengeDriver.minDistance = 0f;
            revengeDriver.requireSkillReady = true;
            revengeDriver.aimType = AISkillDriver.AimType.AtCurrentEnemy;
            revengeDriver.ignoreNodeGraph = true;
            revengeDriver.moveInputScale = 1f;
            revengeDriver.driverUpdateTimerOverride = 2.5f;
            revengeDriver.buttonPressType = AISkillDriver.ButtonPressType.Hold;
            revengeDriver.minTargetHealthFraction = Mathf.NegativeInfinity;
            revengeDriver.maxTargetHealthFraction = Mathf.Infinity;
            revengeDriver.minUserHealthFraction = Mathf.NegativeInfinity;
            revengeDriver.maxUserHealthFraction = 1f;
            revengeDriver.skillSlot = SkillSlot.Utility;

            AISkillDriver grabDriver = newMaster.AddComponent<AISkillDriver>();
            grabDriver.customName = "QuickSmash";
            grabDriver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
            grabDriver.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
            grabDriver.activationRequiresAimConfirmation = true;
            grabDriver.activationRequiresTargetLoS = false;
            grabDriver.selectionRequiresTargetLoS = true;
            grabDriver.maxDistance = 8f;
            grabDriver.minDistance = 0f;
            grabDriver.requireSkillReady = true;
            grabDriver.aimType = AISkillDriver.AimType.AtCurrentEnemy;
            grabDriver.ignoreNodeGraph = true;
            grabDriver.moveInputScale = 1f;
            grabDriver.driverUpdateTimerOverride = 0.5f;
            grabDriver.buttonPressType = AISkillDriver.ButtonPressType.Hold;
            grabDriver.minTargetHealthFraction = Mathf.NegativeInfinity;
            grabDriver.maxTargetHealthFraction = Mathf.Infinity;
            grabDriver.minUserHealthFraction = Mathf.NegativeInfinity;
            grabDriver.maxUserHealthFraction = Mathf.Infinity;
            grabDriver.skillSlot = SkillSlot.Primary;

            AISkillDriver stompDriver = newMaster.AddComponent<AISkillDriver>();
            stompDriver.customName = "Smash";
            stompDriver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
            stompDriver.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
            stompDriver.activationRequiresAimConfirmation = true;
            stompDriver.activationRequiresTargetLoS = false;
            stompDriver.selectionRequiresTargetLoS = true;
            stompDriver.maxDistance = 32f;
            stompDriver.minDistance = 0f;
            stompDriver.requireSkillReady = true;
            stompDriver.aimType = AISkillDriver.AimType.AtCurrentEnemy;
            stompDriver.ignoreNodeGraph = true;
            stompDriver.moveInputScale = 0.4f;
            stompDriver.driverUpdateTimerOverride = 0.5f;
            stompDriver.buttonPressType = AISkillDriver.ButtonPressType.Hold;
            stompDriver.minTargetHealthFraction = Mathf.NegativeInfinity;
            stompDriver.maxTargetHealthFraction = Mathf.Infinity;
            stompDriver.minUserHealthFraction = Mathf.NegativeInfinity;
            stompDriver.maxUserHealthFraction = Mathf.Infinity;
            stompDriver.skillSlot = SkillSlot.Secondary;

            AISkillDriver followCloseDriver = newMaster.AddComponent<AISkillDriver>();
            followCloseDriver.customName = "ChaseClose";
            followCloseDriver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
            followCloseDriver.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
            followCloseDriver.activationRequiresAimConfirmation = false;
            followCloseDriver.activationRequiresTargetLoS = false;
            followCloseDriver.maxDistance = 32f;
            followCloseDriver.minDistance = 0f;
            followCloseDriver.aimType = AISkillDriver.AimType.AtMoveTarget;
            followCloseDriver.ignoreNodeGraph = false;
            followCloseDriver.moveInputScale = 1f;
            followCloseDriver.driverUpdateTimerOverride = -1f;
            followCloseDriver.buttonPressType = AISkillDriver.ButtonPressType.Hold;
            followCloseDriver.minTargetHealthFraction = Mathf.NegativeInfinity;
            followCloseDriver.maxTargetHealthFraction = Mathf.Infinity;
            followCloseDriver.minUserHealthFraction = Mathf.NegativeInfinity;
            followCloseDriver.maxUserHealthFraction = Mathf.Infinity;
            followCloseDriver.skillSlot = SkillSlot.None;

            AISkillDriver followDriver = newMaster.AddComponent<AISkillDriver>();
            followDriver.customName = "Chase";
            followDriver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
            followDriver.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
            followDriver.activationRequiresAimConfirmation = false;
            followDriver.activationRequiresTargetLoS = false;
            followDriver.maxDistance = Mathf.Infinity;
            followDriver.minDistance = 0f;
            followDriver.aimType = AISkillDriver.AimType.AtMoveTarget;
            followDriver.ignoreNodeGraph = false;
            followDriver.moveInputScale = 1f;
            followDriver.driverUpdateTimerOverride = -1f;
            followDriver.buttonPressType = AISkillDriver.ButtonPressType.Hold;
            followDriver.minTargetHealthFraction = Mathf.NegativeInfinity;
            followDriver.maxTargetHealthFraction = Mathf.Infinity;
            followDriver.minUserHealthFraction = Mathf.NegativeInfinity;
            followDriver.maxUserHealthFraction = Mathf.Infinity;
            followDriver.skillSlot = SkillSlot.None;
            followDriver.shouldSprint = true;
            #endregion

            Modules.Prefabs.masterPrefabs.Add(newMaster);

            return newMaster;
        }

        private static void CreateHitboxes(GameObject prefab)
        {
            ChildLocator childLocator = prefab.GetComponentInChildren<ChildLocator>();
            GameObject model = childLocator.gameObject;

            //Transform hitboxTransform = childLocator.FindChild("PunchHitbox");
            //Modules.Prefabs.SetupHitbox(model, hitboxTransform, "Punch");
        }

        private static void CreateSkills(GameObject prefab)
        {
            Modules.Skills.CreateSkillFamilies(prefab);

            string prefix = MechorillaPlugin.developerPrefix;
            SkillLocator skillLocator = prefab.GetComponent<SkillLocator>();

            skillLocator.passiveSkill.enabled = false;
            skillLocator.passiveSkill.skillNameToken = prefix + "_MECHORILLA_BODY_PASSIVE_NAME";
            skillLocator.passiveSkill.skillDescriptionToken = prefix + "_MECHORILLA_BODY_PASSIVE_DESCRIPTION";

            #region Primary
            Modules.Skills.AddPrimarySkills(prefab, Modules.Skills.CreatePrimarySkillDef(new EntityStates.SerializableEntityStateType(typeof(GroundSmashQuick)), "Body", prefix + "_MECHORILLA_BODY_PRIMARY_GRAB_NAME", prefix + "_MECHORILLA_BODY_PRIMARY_GRAB_DESCRIPTION", Modules.Assets.mainAssetBundle.LoadAsset<Sprite>("texCrushGripIcon"), false));
            #endregion

            #region Secondary
            SkillDef groundSmashSkillDef = Modules.Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = prefix + "_MECHORILLA_BODY_SECONDARY_GROUNDSMASH_NAME",
                skillNameToken = prefix + "_MECHORILLA_BODY_SECONDARY_GROUNDSMASH_NAME",
                skillDescriptionToken = prefix + "_MECHORILLA_BODY_SECONDARY_GROUNDSMASH_DESCRIPTION",
                skillIcon = Modules.Assets.mainAssetBundle.LoadAsset<Sprite>("texEarthPowerIcon"),
                activationState = new EntityStates.SerializableEntityStateType(typeof(GroundSmash)),
                activationStateMachineName = "Body",
                baseMaxStock = 1,
                baseRechargeInterval = 8f,
                beginSkillCooldownOnSkillEnd = true,
                canceledFromSprinting = false,
                forceSprintDuringState = false,
                fullRestockOnAssign = true,
                interruptPriority = EntityStates.InterruptPriority.Any,
                resetCooldownTimerOnUse = false,
                isCombatSkill = true,
                mustKeyPress = false,
                cancelSprintingOnActivation = true,
                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,
            });

            Modules.Skills.AddSecondarySkills(prefab, groundSmashSkillDef);
            #endregion

            #region Utility
            SkillDef sweepingBeamSkillDef = Modules.Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = prefix + "_MECHORILLA_BODY_UTILITY_SWEEPINGBEAM_NAME",
                skillNameToken = prefix + "_MECHORILLA_BODY_UTILITY_SWEEPINGBEAM_NAME",
                skillDescriptionToken = prefix + "_MECHORILLA_BODY_UTILITY_SWEEPINGBEAM_DESCRIPTION",
                skillIcon = Modules.Assets.mainAssetBundle.LoadAsset<Sprite>("texRevengeIcon"),
                activationState = new EntityStates.SerializableEntityStateType(typeof(SweepingBeam)),
                activationStateMachineName = "Body",
                baseMaxStock = 1,
                baseRechargeInterval = 16f,
                beginSkillCooldownOnSkillEnd = true,
                canceledFromSprinting = false,
                forceSprintDuringState = false,
                fullRestockOnAssign = true,
                interruptPriority = EntityStates.InterruptPriority.Any,
                resetCooldownTimerOnUse = false,
                isCombatSkill = true,
                mustKeyPress = false,
                cancelSprintingOnActivation = true,
                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1
            });

            Modules.Skills.AddUtilitySkills(prefab, sweepingBeamSkillDef);
            #endregion

            #region Special
            SkillDef impactSkillDef = Modules.Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = prefix + "_MECHORILLA_BODY_SPECIAL_BLAST_NAME",
                skillNameToken = prefix + "_MECHORILLA_BODY_SPECIAL_BLAST_NAME",
                skillDescriptionToken = prefix + "_MECHORILLA_BODY_SPECIAL_BLAST_DESCRIPTION",
                skillIcon = Modules.Assets.mainAssetBundle.LoadAsset<Sprite>("texGigaImpactIcon"),
                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.Mechorilla.Cannon.PrepBlasts)),
                activationStateMachineName = "Body",
                baseMaxStock = 1,
                baseRechargeInterval = 24f,
                beginSkillCooldownOnSkillEnd = true,
                canceledFromSprinting = false,
                forceSprintDuringState = false,
                fullRestockOnAssign = true,
                interruptPriority = EntityStates.InterruptPriority.Skill,
                resetCooldownTimerOnUse = false,
                isCombatSkill = true,
                mustKeyPress = false,
                cancelSprintingOnActivation = true,
                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1
            });

            Modules.Skills.AddSpecialSkills(prefab, impactSkillDef);
            #endregion
        }

        private static void CreateSkins(GameObject prefab)
        {
            GameObject model = prefab.GetComponentInChildren<ModelLocator>().modelTransform.gameObject;
            CharacterModel characterModel = model.GetComponent<CharacterModel>();

            ModelSkinController skinController = model.AddComponent<ModelSkinController>();
            ChildLocator childLocator = model.GetComponent<ChildLocator>();

            SkinnedMeshRenderer mainRenderer = characterModel.mainSkinnedMeshRenderer;

            CharacterModel.RendererInfo[] defaultRenderers = characterModel.baseRendererInfos;

            List<SkinDef> skins = new List<SkinDef>();

            #region DefaultSkin
            SkinDef defaultSkin = Modules.Skins.CreateSkinDef(MechorillaPlugin.developerPrefix + "_MECHORILLA_BODY_DEFAULT_SKIN_NAME",
                Assets.mainAssetBundle.LoadAsset<Sprite>("texMainSkin"),
                defaultRenderers,
                mainRenderer,
                model);

            skins.Add(defaultSkin);
            #endregion

            #region MasterySkin
            SkinDef masterySkin = Modules.Skins.CreateSkinDef(MechorillaPlugin.developerPrefix + "_MECHORILLA_BODY_MONSOON_SKIN_NAME",
                Assets.mainAssetBundle.LoadAsset<Sprite>("texMainSkin"),
                SkinRendererInfos(defaultRenderers, new Material[]
                {
                    Modules.Assets.CreateMaterial("matMechorillaMonsoon", 0f, Color.white)
                }),
                mainRenderer,
                model,
                masteryUnlockableDef);

            skins.Add(masterySkin);
            #endregion

            skinController.skins = skins.ToArray();
        }

        private static void InitializeItemDisplays(GameObject prefab)
        {
            CharacterModel characterModel = prefab.GetComponentInChildren<CharacterModel>();

            if (itemDisplayRuleSet == null)
            {
                itemDisplayRuleSet = ScriptableObject.CreateInstance<ItemDisplayRuleSet>();
                itemDisplayRuleSet.name = "idrs" + bodyName;
            }

            characterModel.itemDisplayRuleSet = itemDisplayRuleSet;
        }

        internal static void SetItemDisplays()
        {
            itemDisplayRules = new List<ItemDisplayRuleSet.KeyAssetRuleGroup>();

            // add item displays here
            //  HIGHLY recommend using KingEnderBrine's ItemDisplayPlacementHelper mod for this
            #region Item Displays
            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.Jetpack,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBugWings"),
childName = "Chest",
localPos = new Vector3(0F, 1.04382F, -2.92125F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.GoldGat,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayGoldGat"),
childName = "Chest",
localPos = new Vector3(2.11878F, 4.159F, -0.48292F),
localAngles = new Vector3(38.10577F, 106.9518F, 290.605F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.BFG,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBFG"),
childName = "Chest",
localPos = new Vector3(1.15184F, 3.03362F, -2.54124F),
localAngles = new Vector3(4.67658F, 31.55978F, 331.4998F),
localScale = new Vector3(2F, 2F, 2F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.CritGlasses,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayGlasses"),
childName = "Head",
localPos = new Vector3(-0.00008F, 1.88765F, 1.51572F),
localAngles = new Vector3(350.0497F, 0F, 0F),
localScale = new Vector3(2.72044F, 2.74791F, 2.09006F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Syringe,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplaySyringeCluster"),
childName = "Chest",
localPos = new Vector3(-0.97271F, 3.61142F, -0.86999F),
localAngles = new Vector3(327.254F, 340.6512F, 6.41084F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Behemoth,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBehemoth"),
childName = "ElbowL",
localPos = new Vector3(0F, 0.2247F, -0.1174F),
localAngles = new Vector3(6.223F, 180F, 0F),
localScale = new Vector3(0.1F, 0.1F, 0.1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Missile,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayMissileLauncher"),
childName = "Chest",
localPos = new Vector3(-0.3075F, 0.5204F, -0.049F),
localAngles = new Vector3(0F, 0F, 51.9225F),
localScale = new Vector3(0.1F, 0.1F, 0.1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Dagger,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayDagger"),
childName = "Chest",
localPos = new Vector3(-0.0553F, 0.2856F, 0.0945F),
localAngles = new Vector3(334.8839F, 31.5284F, 34.6784F),
localScale = new Vector3(1.2428F, 1.2428F, 1.2299F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Hoof,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayHoof"),
childName = "CalfL",
localPos = new Vector3(0.2869F, 0.2642F, 0F),
localAngles = new Vector3(55.8093F, 270F, 0F),
localScale = new Vector3(0.4506F, 0.4001F, 0.1641F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.ChainLightning,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayUkulele"),
childName = "Chest",
localPos = new Vector3(-0.0011F, 0.1031F, -0.0901F),
localAngles = new Vector3(0F, 180F, 89.3997F),
localScale = new Vector3(0.4749F, 0.4749F, 0.4749F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.GhostOnKill,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayMask"),
childName = "Head",
localPos = new Vector3(0F, -0.0523F, 0.6298F),
localAngles = new Vector3(333.5951F, 0F, 0F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Mushroom,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayMushroom"),
childName = "FootR",
localPos = new Vector3(0.0812F, -0.0456F, 0.0773F),
localAngles = new Vector3(67.8714F, 226.4006F, 180F),
localScale = new Vector3(0.2363F, 0.2363F, 0.2363F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.AttackSpeedOnCrit,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayWolfPelt"),
childName = "Head",
localPos = new Vector3(0F, 0.2783F, -0.002F),
localAngles = new Vector3(358.4554F, 0F, 0F),
localScale = new Vector3(0.5666F, 0.5666F, 0.5666F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.BleedOnHit,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayTriTip"),
childName = "HandL",
localPos = new Vector3(-0.1194F, 0.4038F, -0.0871F),
localAngles = new Vector3(270F, 79.8952F, 0F),
localScale = new Vector3(2.3065F, 2.3065F, 0.881F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.WardOnLevel,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayWarbanner"),
childName = "Pelvis",
localPos = new Vector3(0F, 0.5767F, -0.8333F),
localAngles = new Vector3(0F, 0F, 90F),
localScale = new Vector3(1.4718F, 1.4718F, 1.4718F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.HealOnCrit,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayScythe"),
childName = "Chest",
localPos = new Vector3(-0.5369F, 1.5847F, 0.0983F),
localAngles = new Vector3(355.0806F, 8.1859F, 114.0644F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.HealWhileSafe,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplaySnail"),
childName = "FootR",
localPos = new Vector3(-0.076F, -0.2002F, 0.082F),
localAngles = new Vector3(81.6783F, 317.1524F, 180F),
localScale = new Vector3(0.357F, 0.357F, 0.357F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Clover,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayClover"),
childName = "FootR",
localPos = new Vector3(0.0004F, 0.1094F, -0.1329F),
localAngles = new Vector3(85.6192F, 0.0001F, 179.4897F),
localScale = new Vector3(0.2749F, 0.2749F, 0.2749F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.BarrierOnOverHeal,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayAegis"),
childName = "Chest",
localPos = new Vector3(1.89661F, 3.5622F, -0.25336F),
localAngles = new Vector3(343.5132F, 274.588F, 169.3314F),
localScale = new Vector3(1.69729F, 1.69729F, 1.69729F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.GoldOnHit,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBoneCrown"),
childName = "Head",
localPos = new Vector3(0F, -0.1538F, 0F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(2.7392F, 4.1238F, 3.9193F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.WarCryOnMultiKill,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayPauldron"),
childName = "UpperArmL",
localPos = new Vector3(0.1904F, 0.0241F, 0.0469F),
localAngles = new Vector3(77.2628F, 76.1643F, 0F),
localScale = new Vector3(3.231F, 3.231F, 3.231F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.SprintArmor,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBuckler"),
childName = "HandR",
localPos = new Vector3(-0.0702F, 0.2846F, 0.0001F),
localAngles = new Vector3(273.4401F, 270F, 90F),
localScale = new Vector3(0.618F, 0.618F, 0.618F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.IceRing,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayIceRing"),
childName = "LowerArmR",
localPos = new Vector3(0.0334F, 0.2587F, -0.1223F),
localAngles = new Vector3(274.3965F, 90F, 270F),
localScale = new Vector3(0.3627F, 0.3627F, 0.3627F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.FireRing,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayFireRing"),
childName = "LowerArmR",
localPos = new Vector3(0F, 0.387F, 0F),
localAngles = new Vector3(90F, 0F, 0F),
localScale = new Vector3(1.6394F, 1.6394F, 1.6394F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.UtilitySkillMagazine,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayAfterburnerShoulderRing"),
                            childName = "UpperArmL",
                            localPos = new Vector3(0, 0, -0.002f),
                            localAngles = new Vector3(-90, 0, 0),
                            localScale = new Vector3(0.01f, 0.01f, 0.01f),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayAfterburnerShoulderRing"),
                            childName = "UpperArmR",
                            localPos = new Vector3(0, 0, -0.002f),
                            localAngles = new Vector3(-90, 0, 0),
                            localScale = new Vector3(0.01f, 0.01f, 0.01f),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.JumpBoost,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayWaxBird"),
childName = "Head",
localPos = new Vector3(0F, 0.0529F, -0.1242F),
localAngles = new Vector3(24.419F, 0F, 0F),
localScale = new Vector3(0.5253F, 0.5253F, 0.5253F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.ArmorReductionOnHit,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayWarhammer"),
childName = "Chest",
localPos = new Vector3(0.0513F, 0.0652F, -0.0792F),
localAngles = new Vector3(64.189F, 90F, 90F),
localScale = new Vector3(0.1722F, 0.1722F, 0.1722F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.NearbyDamageBonus,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayDiamond"),
childName = "HandL",
localPos = new Vector3(-0.0984F, 0.3897F, 0F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.3879F, 0.3879F, 0.3879F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayDiamond"),
childName = "HandR",
localPos = new Vector3(1.22358F, -1.02336F, -0.31921F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.3879F, 0.3879F, 0.3879F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.ArmorPlate,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayRepulsionArmorPlate"),
childName = "ThighL",
localPos = new Vector3(0F, 0.4032F, -0.1655F),
localAngles = new Vector3(90F, 0F, 0F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.CommandMissile,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayMissileRack"),
childName = "Chest",
localPos = new Vector3(0.00003F, -0.14565F, -2.04463F),
localAngles = new Vector3(83.85653F, 0F, 180F),
localScale = new Vector3(5F, 5F, 5F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Feather,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayFeather"),
childName = "LowerArmL",
localPos = new Vector3(0.001F, 0.2755F, 0.0454F),
localAngles = new Vector3(270F, 91.2661F, 0F),
localScale = new Vector3(0.0285F, 0.0285F, 0.0285F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Crowbar,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayCrowbar"),
childName = "Chest",
localPos = new Vector3(0F, 0.1219F, -0.0764F),
localAngles = new Vector3(90F, 90F, 0F),
localScale = new Vector3(0.1936F, 0.1936F, 0.1936F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.FallBoots,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayGravBoots"),
childName = "CalfL",
localPos = new Vector3(-0.0038F, 0.3729F, -0.0046F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.1485F, 0.1485F, 0.1485F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayGravBoots"),
childName = "CalfR",
localPos = new Vector3(-0.0038F, 0.3729F, -0.0046F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.1485F, 0.1485F, 0.1485F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.ExecuteLowHealthElite,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayGuillotine"),
childName = "Chest",
localPos = new Vector3(0.8369F, 1.2494F, -0.7704F),
localAngles = new Vector3(330.2193F, 177.1101F, 267.1591F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.EquipmentMagazine,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBattery"),
childName = "Chest",
localPos = new Vector3(0.5581F, 0.7718F, 0.4092F),
localAngles = new Vector3(49.7878F, 9.4627F, 182.7204F),
localScale = new Vector3(0.2149F, 0.2149F, 0.2149F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.NovaOnHeal,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayDevilHorns"),
childName = "Head",
localPos = new Vector3(0.0949F, 0.0945F, 0.0654F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.5349F, 0.5349F, 0.5349F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayDevilHorns"),
childName = "Head",
localPos = new Vector3(-0.0949F, 0.0945F, 0.0105F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(-0.5349F, 0.5349F, 0.5349F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Infusion,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayInfusion"),
childName = "Pelvis",
localPos = new Vector3(-0.0703F, 0.0238F, -0.0366F),
localAngles = new Vector3(0F, 45F, 0F),
localScale = new Vector3(0.5253F, 0.5253F, 0.5253F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Medkit,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayMedkit"),
childName = "Chest",
localPos = new Vector3(0F, 2.14353F, -3.23435F),
localAngles = new Vector3(284.3671F, 0.00001F, 180F),
localScale = new Vector3(3F, 3F, 3F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Bandolier,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBandolier"),
childName = "Chest",
localPos = new Vector3(0.0035F, 0F, 0F),
localAngles = new Vector3(270F, 0F, 0F),
localScale = new Vector3(0.1684F, 0.242F, 0.242F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.BounceNearby,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayHook"),
childName = "Chest",
localPos = new Vector3(-0.0922F, 0.4106F, -0.0015F),
localAngles = new Vector3(290.3197F, 89F, 0F),
localScale = new Vector3(0.214F, 0.214F, 0.214F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.IgniteOnKill,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayGasoline"),
childName = "ThighL",
localPos = new Vector3(0.0494F, 0.0954F, 0.0015F),
localAngles = new Vector3(90F, 0F, 0F),
localScale = new Vector3(0.3165F, 0.3165F, 0.3165F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.StunChanceOnHit,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayStunGrenade"),
childName = "ThighR",
localPos = new Vector3(0.001F, 0.3609F, 0.0523F),
localAngles = new Vector3(90F, 0F, 0F),
localScale = new Vector3(0.5672F, 0.5672F, 0.5672F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Firework,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayFirework"),
childName = "Muzzle",
localPos = new Vector3(0.0086F, 0.0069F, 0.0565F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.1194F, 0.1194F, 0.1194F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.LunarDagger,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayLunarDagger"),
childName = "Chest",
localPos = new Vector3(-0.0015F, 0.2234F, -0.0655F),
localAngles = new Vector3(277.637F, 358.2474F, 1.4903F),
localScale = new Vector3(0.3385F, 0.3385F, 0.3385F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Knurl,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayKnurl"),
childName = "LowerArmL",
localPos = new Vector3(-0.0186F, 0.0405F, -0.0049F),
localAngles = new Vector3(78.8707F, 36.6722F, 105.8275F),
localScale = new Vector3(0.0848F, 0.0848F, 0.0848F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.BeetleGland,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBeetleGland"),
childName = "Chest",
localPos = new Vector3(0.0852F, 0.0577F, 0F),
localAngles = new Vector3(359.9584F, 0.1329F, 39.8304F),
localScale = new Vector3(0.0553F, 0.0553F, 0.0553F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.SprintBonus,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplaySoda"),
childName = "Pelvis",
localPos = new Vector3(-0.075F, 0.095F, 0F),
localAngles = new Vector3(270F, 251.0168F, 0F),
localScale = new Vector3(0.1655F, 0.1655F, 0.1655F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.SecondarySkillMagazine,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayDoubleMag"),
childName = "Chest",
localPos = new Vector3(-0.0018F, 0.0002F, 0.097F),
localAngles = new Vector3(84.2709F, 200.5981F, 25.0139F),
localScale = new Vector3(0.0441F, 0.0441F, 0.0441F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.StickyBomb,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayStickyBomb"),
childName = "Pelvis",
localPos = new Vector3(0.0594F, 0.0811F, 0.0487F),
localAngles = new Vector3(8.4958F, 176.5473F, 162.7601F),
localScale = new Vector3(0.0736F, 0.0736F, 0.0736F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.TreasureCache,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayKey"),
childName = "Pelvis",
localPos = new Vector3(0.0589F, 0.1056F, -0.0174F),
localAngles = new Vector3(0.2454F, 195.0205F, 89.0854F),
localScale = new Vector3(0.4092F, 0.4092F, 0.4092F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.BossDamageBonus,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayAPRound"),
childName = "Pelvis",
localPos = new Vector3(-0.0371F, 0.0675F, -0.052F),
localAngles = new Vector3(90F, 41.5689F, 0F),
localScale = new Vector3(0.2279F, 0.2279F, 0.2279F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.SlowOnHit,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBauble"),
childName = "Pelvis",
localPos = new Vector3(-0.0074F, 0.076F, -0.0864F),
localAngles = new Vector3(0F, 23.7651F, 0F),
localScale = new Vector3(0.0687F, 0.0687F, 0.0687F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.ExtraLife,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayHippo"),
childName = "Chest",
localPos = new Vector3(0F, 0.8946F, -0.543F),
localAngles = new Vector3(336.1F, 180F, 0F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.KillEliteFrenzy,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBrainstalk"),
childName = "Head",
localPos = new Vector3(0F, -0.2836F, 0F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.RepeatHeal,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayCorpseFlower"),
childName = "UpperArmR",
localPos = new Vector3(-0.0393F, 0.1484F, 0F),
localAngles = new Vector3(270F, 90F, 0F),
localScale = new Vector3(0.1511F, 0.1511F, 0.1511F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.AutoCastEquipment,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayFossil"),
childName = "Chest",
localPos = new Vector3(-0.0722F, 0.0921F, 0F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.4208F, 0.4208F, 0.4208F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.IncreaseHealing,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayAntler"),
childName = "Head",
localPos = new Vector3(0.1003F, 0.269F, 0F),
localAngles = new Vector3(0F, 90F, 0F),
localScale = new Vector3(0.3395F, 0.3395F, 0.3395F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayAntler"),
childName = "Head",
localPos = new Vector3(-0.1003F, 0.269F, 0F),
localAngles = new Vector3(0F, 90F, 0F),
localScale = new Vector3(0.3395F, 0.3395F, -0.3395F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.TitanGoldDuringTP,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayGoldHeart"),
childName = "Chest",
localPos = new Vector3(-0.0571F, 0.3027F, 0.0755F),
localAngles = new Vector3(335.0033F, 343.2951F, 0F),
localScale = new Vector3(0.1191F, 0.1191F, 0.1191F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.SprintWisp,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBrokenMask"),
childName = "UpperArmR",
localPos = new Vector3(-0.0283F, 0.0452F, -0.0271F),
localAngles = new Vector3(0F, 270F, 0F),
localScale = new Vector3(0.1385F, 0.1385F, 0.1385F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.BarrierOnKill,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBrooch"),
childName = "Head",
localPos = new Vector3(0F, -0.0341F, 0.7026F),
localAngles = new Vector3(60.0987F, 0F, 0F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.TPHealingNova,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayGlowFlower"),
childName = "UpperArmL",
localPos = new Vector3(0.0399F, 0.1684F, 0.0121F),
localAngles = new Vector3(0F, 73.1449F, 0F),
localScale = new Vector3(0.2731F, 0.2731F, 0.0273F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.LunarUtilityReplacement,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBirdFoot"),
childName = "Head",
localPos = new Vector3(0F, 0.2387F, -0.199F),
localAngles = new Vector3(0F, 270F, 0F),
localScale = new Vector3(0.2833F, 0.2833F, 0.2833F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Thorns,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayRazorwireLeft"),
childName = "UpperArmL",
localPos = new Vector3(0F, 0F, 0F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(1.8655F, 1.8655F, 1.8655F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.LunarPrimaryReplacement,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBirdEye"),
childName = "Head",
localPos = new Vector3(0.4958F, -0.6786F, 0.5263F),
localAngles = new Vector3(270F, 23.8331F, 0F),
localScale = new Vector3(0.6787F, 0.6787F, 0.6787F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBirdEye"),
childName = "Head",
localPos = new Vector3(0.462F, -1.0016F, 0.5412F),
localAngles = new Vector3(270F, 23.8331F, 0F),
localScale = new Vector3(0.6787F, 0.6787F, 0.6787F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBirdEye"),
childName = "Head",
localPos = new Vector3(0.4084F, -1.3391F, 0.5182F),
localAngles = new Vector3(279.9289F, 21.5984F, 355.7885F),
localScale = new Vector3(0.6787F, 0.6787F, 0.6787F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBirdEye"),
childName = "Head",
localPos = new Vector3(-0.462F, -1.0016F, 0.5412F),
localAngles = new Vector3(270F, 336.1669F, 0F),
localScale = new Vector3(0.6787F, 0.6787F, 0.6787F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBirdEye"),
childName = "Head",
localPos = new Vector3(-0.502F, -0.6753F, 0.541F),
localAngles = new Vector3(270F, 342.6888F, 0F),
localScale = new Vector3(0.6787F, 0.6787F, 0.6787F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBirdEye"),
childName = "Head",
localPos = new Vector3(-0.4089F, -1.3426F, 0.5184F),
localAngles = new Vector3(282.3309F, 315.1425F, 29.6489F),
localScale = new Vector3(0.6787F, 0.6787F, 0.6787F),
                            limbMask = LimbFlags.None
                        },
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.NovaOnLowHealth,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayJellyGuts"),
childName = "Head",
localPos = new Vector3(-0.0484F, -0.0116F, 0.0283F),
localAngles = new Vector3(316.2306F, 45.1087F, 303.6165F),
localScale = new Vector3(0.1035F, 0.1035F, 0.1035F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.LunarTrinket,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBeads"),
childName = "LowerArmL",
localPos = new Vector3(0F, 0.3249F, 0.0381F),
localAngles = new Vector3(0F, 0F, 90F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Plant,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayInterstellarDeskPlant"),
childName = "UpperArmR",
localPos = new Vector3(-0.0663F, 0.2266F, 0F),
localAngles = new Vector3(4.9717F, 270F, 54.4915F),
localScale = new Vector3(0.0429F, 0.0429F, 0.0429F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Bear,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBear"),
childName = "Chest",
localPos = new Vector3(0F, 0.3014F, 0.0662F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.2034F, 0.2034F, 0.2034F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.DeathMark,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayDeathMark"),
childName = "LowerArmR",
localPos = new Vector3(0F, 0.4099F, 0.0252F),
localAngles = new Vector3(277.5254F, 0F, 0F),
localScale = new Vector3(-0.0375F, -0.0341F, -0.0464F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.ExplodeOnDeath,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayWilloWisp"),
childName = "Pelvis",
localPos = new Vector3(0.0595F, 0.0696F, -0.0543F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.0283F, 0.0283F, 0.0283F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Seed,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplaySeed"),
childName = "Head",
localPos = new Vector3(-0.1702F, 0.1366F, -0.026F),
localAngles = new Vector3(344.0657F, 196.8238F, 275.5892F),
localScale = new Vector3(0.0275F, 0.0275F, 0.0275F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.SprintOutOfCombat,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayWhip"),
childName = "Pelvis",
localPos = new Vector3(0.1001F, -0.0132F, 0F),
localAngles = new Vector3(0F, 0F, 20.1526F),
localScale = new Vector3(0.2845F, 0.2845F, 0.2845F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = JunkContent.Items.CooldownOnCrit,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplaySkull"),
childName = "Chest",
localPos = new Vector3(0F, 0.4783F, 0.4991F),
localAngles = new Vector3(270F, 0F, 0F),
localScale = new Vector3(0.8005F, 0.8005F, 0.8005F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Phasing,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayStealthkit"),
childName = "CalfL",
localPos = new Vector3(-0.0063F, 0.2032F, -0.0507F),
localAngles = new Vector3(90F, 0F, 0F),
localScale = new Vector3(0.1454F, 0.2399F, 0.16F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.PersonalShield,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayShieldGenerator"),
childName = "Chest",
localPos = new Vector3(0F, 0.2649F, 0.0689F),
localAngles = new Vector3(304.1204F, 90F, 270F),
localScale = new Vector3(0.1057F, 0.1057F, 0.1057F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.ShockNearby,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayTeslaCoil"),
childName = "HandL",
localPos = new Vector3(-0.00011F, 0.00009F, -1.63089F),
localAngles = new Vector3(270F, 0F, 0F),
localScale = new Vector3(2F, 2F, 2F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.ShieldOnly,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayShieldBug"),
childName = "Head",
localPos = new Vector3(0.0868F, 0.3114F, 0F),
localAngles = new Vector3(348.1819F, 268.0985F, 0.3896F),
localScale = new Vector3(0.3521F, 0.3521F, 0.3521F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayShieldBug"),
childName = "Head",
localPos = new Vector3(-0.0868F, 0.3114F, 0F),
localAngles = new Vector3(11.8181F, 268.0985F, 359.6104F),
localScale = new Vector3(0.3521F, 0.3521F, -0.3521F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.AlienHead,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayAlienHead"),
childName = "Chest",
localPos = new Vector3(0.0417F, 0.2791F, -0.0493F),
localAngles = new Vector3(284.1172F, 243.7966F, 260.89F),
localScale = new Vector3(0.6701F, 0.6701F, 0.6701F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.HeadHunter,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplaySkullCrown"),
childName = "Head",
localPos = new Vector3(0F, 0.2556F, 0F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.4851F, 0.1617F, 0.1617F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.EnergizedOnEquipmentUse,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayWarHorn"),
childName = "Pelvis",
localPos = new Vector3(-0.1509F, 0.0659F, 0F),
localAngles = new Vector3(0F, 0F, 69.9659F),
localScale = new Vector3(0.2732F, 0.2732F, 0.2732F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.FlatHealth,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplaySteakCurved"),
childName = "Head",
localPos = new Vector3(0F, 0.3429F, -0.0671F),
localAngles = new Vector3(294.98F, 180F, 180F),
localScale = new Vector3(0.1245F, 0.1155F, 0.1155F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Tooth,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayToothMeshLarge"),
childName = "Head",
localPos = new Vector3(0F, 0.0687F, 0.0998F),
localAngles = new Vector3(344.9017F, 0F, 0F),
localScale = new Vector3(7.5452F, 7.5452F, 7.5452F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Pearl,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayPearl"),
childName = "LowerArmR",
localPos = new Vector3(0F, 0F, 0F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.1F, 0.1F, 0.1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.ShinyPearl,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayShinyPearl"),
childName = "LowerArmL",
localPos = new Vector3(0F, 0F, 0F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.1F, 0.1F, 0.1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.BonusGoldPackOnKill,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayTome"),
childName = "ThighR",
localPos = new Vector3(0.0155F, 0.2145F, 0.0615F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.0475F, 0.0475F, 0.0475F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Squid,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplaySquidTurret"),
childName = "Head",
localPos = new Vector3(-0.0164F, 0.1641F, -0.0005F),
localAngles = new Vector3(0F, 90F, 0F),
localScale = new Vector3(0.2235F, 0.3016F, 0.3528F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Icicle,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayFrostRelic"),
childName = "Base",
localPos = new Vector3(-0.658F, -1.0806F, 0.015F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Talisman,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayTalisman"),
childName = "Base",
localPos = new Vector3(0.8357F, -0.7042F, -0.2979F),
localAngles = new Vector3(270F, 0F, 0F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.LaserTurbine,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayLaserTurbine"),
childName = "Chest",
localPos = new Vector3(0F, 0.0622F, -0.0822F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.2159F, 0.2159F, 0.2159F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.FocusConvergence,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayFocusedConvergence"),
childName = "Base",
localPos = new Vector3(-0.0554F, -1.6605F, -0.3314F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.1F, 0.1F, 0.1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = JunkContent.Items.Incubator,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayAncestralIncubator"),
childName = "Chest",
localPos = new Vector3(0F, 0.3453F, 0F),
localAngles = new Vector3(353.0521F, 317.2421F, 69.6292F),
localScale = new Vector3(0.0528F, 0.0528F, 0.0528F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.FireballsOnHit,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayFireballsOnHit"),
childName = "LowerArmL",
localPos = new Vector3(0F, 0.3365F, -0.0878F),
localAngles = new Vector3(270F, 0F, 0F),
localScale = new Vector3(0.0761F, 0.0761F, 0.0761F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.SiphonOnLowHealth,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplaySiphonOnLowHealth"),
childName = "Pelvis",
localPos = new Vector3(0.0542F, 0.0206F, -0.0019F),
localAngles = new Vector3(0F, 303.4368F, 0F),
localScale = new Vector3(0.0385F, 0.0385F, 0.0385F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.BleedOnHitAndExplode,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBleedOnHitAndExplode"),
childName = "ThighR",
localPos = new Vector3(0F, 0.0575F, -0.0178F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.0486F, 0.0486F, 0.0486F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.MonstersOnShrineUse,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayMonstersOnShrineUse"),
childName = "ThighR",
localPos = new Vector3(0.0022F, 0.084F, 0.066F),
localAngles = new Vector3(352.4521F, 260.6884F, 341.5106F),
localScale = new Vector3(0.0246F, 0.0246F, 0.0246F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.RandomDamageZone,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayRandomDamageZone"),
childName = "LowerArmL",
localPos = new Vector3(0.0709F, 0.4398F, 0.0587F),
localAngles = new Vector3(349.218F, 235.9453F, 0F),
localScale = new Vector3(0.0465F, 0.0465F, 0.0465F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.Fruit,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayFruit"),
childName = "Chest",
localPos = new Vector3(-0.0513F, 0.2348F, -0.1839F),
localAngles = new Vector3(354.7403F, 305.3714F, 336.9526F),
localScale = new Vector3(0.2118F, 0.2118F, 0.2118F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.AffixRed,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayEliteHorn"),
childName = "Head",
localPos = new Vector3(0.45F, 2F, 0F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayEliteHorn"),
childName = "Head",
localPos = new Vector3(-0.45F, 2F, 0F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(-1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.AffixBlue,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayEliteRhinoHorn"),
childName = "Head",
localPos = new Vector3(0.04493F, 2.92693F, 0.83557F),
localAngles = new Vector3(271.6351F, 37.5755F, 321.8995F),
localScale = new Vector3(3F, 3F, 3F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayEliteRhinoHorn"),
childName = "Head",
localPos = new Vector3(0.01531F, 2.2415F, 1.06777F),
localAngles = new Vector3(300F, 0F, 0F),
localScale = new Vector3(2F, 2F, 2F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.AffixWhite,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayEliteIceCrown"),
childName = "Head",
localPos = new Vector3(0.00716F, 2.516F, -0.51942F),
localAngles = new Vector3(305.314F, 178.3078F, 181.9276F),
localScale = new Vector3(0.28315F, 0.28315F, 0.28315F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.AffixPoison,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayEliteUrchinCrown"),
childName = "Head",
localPos = new Vector3(0.0055F, 2.41879F, -0.1904F),
localAngles = new Vector3(313.6205F, 175.768F, 182.5143F),
localScale = new Vector3(0.54651F, 0.54651F, 0.78344F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.AffixHaunted,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayEliteStealthCrown"),
childName = "Head",
localPos = new Vector3(-0.00335F, 1.6045F, -0.32385F),
localAngles = new Vector3(316.9946F, 180.7612F, 178.4379F),
localScale = new Vector3(0.62047F, 0.62047F, 0.62047F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.CritOnUse,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayNeuralImplant"),
childName = "Head",
localPos = new Vector3(0F, 0.1861F, 0.2328F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.2326F, 0.2326F, 0.2326F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.DroneBackup,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayRadio"),
childName = "Pelvis",
localPos = new Vector3(0.0604F, 0.1269F, 0F),
localAngles = new Vector3(0F, 90F, 0F),
localScale = new Vector3(0.2641F, 0.2641F, 0.2641F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.Lightning,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayLightningArmRight"),
childName = "UpperArmR",
localPos = new Vector3(0F, 0F, 0F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(2.1862F, 2.1862F, 2.1862F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.BurnNearby,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayPotion"),
childName = "Pelvis",
localPos = new Vector3(0.078F, 0.065F, 0F),
localAngles = new Vector3(359.1402F, 0.1068F, 331.8908F),
localScale = new Vector3(0.0307F, 0.0307F, 0.0307F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.CrippleWard,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayEffigy"),
childName = "Pelvis",
localPos = new Vector3(0.0768F, -0.0002F, 0F),
localAngles = new Vector3(0F, 270F, 0F),
localScale = new Vector3(0.2812F, 0.2812F, 0.2812F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.QuestVolatileBattery,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBatteryArray"),
childName = "Chest",
localPos = new Vector3(0F, 0F, -0.9528F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.GainArmor,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayElephantFigure"),
childName = "CalfR",
localPos = new Vector3(0F, 0.3011F, 0.0764F),
localAngles = new Vector3(77.5634F, 0F, 0F),
localScale = new Vector3(0.6279F, 0.6279F, 0.6279F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.Recycle,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayRecycler"),
childName = "Chest",
localPos = new Vector3(0F, 0F, -0.726F),
localAngles = new Vector3(0F, 90F, 0F),
localScale = new Vector3(0.3304F, 0.3304F, 0.3304F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.FireBallDash,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayEgg"),
childName = "Pelvis",
localPos = new Vector3(0.0727F, 0.0252F, 0F),
localAngles = new Vector3(270F, 0F, 0F),
localScale = new Vector3(0.1891F, 0.1891F, 0.1891F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.Cleanse,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayWaterPack"),
childName = "Chest",
localPos = new Vector3(0F, 0.1996F, -0.0489F),
localAngles = new Vector3(0F, 180F, 0F),
localScale = new Vector3(0.0821F, 0.0821F, 0.0821F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.Tonic,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayTonic"),
childName = "Pelvis",
localPos = new Vector3(0.066F, 0.058F, 0F),
localAngles = new Vector3(0F, 90F, 0F),
localScale = new Vector3(0.1252F, 0.1252F, 0.1252F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.Gateway,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayVase"),
childName = "Pelvis",
localPos = new Vector3(0.0807F, 0.0877F, 0F),
localAngles = new Vector3(0F, 90F, 0F),
localScale = new Vector3(0.0982F, 0.0982F, 0.0982F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.Meteor,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayMeteor"),
childName = "Base",
localPos = new Vector3(0F, -1.7606F, -0.9431F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.Saw,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplaySawmerang"),
childName = "Base",
localPos = new Vector3(0F, -1.7606F, -0.9431F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.1F, 0.1F, 0.1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.Blackhole,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayGravCube"),
childName = "Base",
localPos = new Vector3(0F, -1.7606F, -0.9431F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.5F, 0.5F, 0.5F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.Scanner,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayScanner"),
childName = "Pelvis",
localPos = new Vector3(0.0857F, 0.0472F, 0.0195F),
localAngles = new Vector3(270F, 154.175F, 0F),
localScale = new Vector3(0.0861F, 0.0861F, 0.0861F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.DeathProjectile,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayDeathProjectile"),
childName = "Pelvis",
localPos = new Vector3(0F, 0.028F, -0.0977F),
localAngles = new Vector3(0F, 180F, 0F),
localScale = new Vector3(0.0596F, 0.0596F, 0.0596F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.LifestealOnHit,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayLifestealOnHit"),
childName = "Head",
localPos = new Vector3(-0.2175F, 0.4404F, -0.141F),
localAngles = new Vector3(44.0939F, 33.5151F, 43.5058F),
localScale = new Vector3(0.1246F, 0.1246F, 0.1246F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.TeamWarCry,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayTeamWarCry"),
childName = "Pelvis",
localPos = new Vector3(0F, 0F, 0.1866F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.1233F, 0.1233F, 0.1233F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });
            #endregion

            itemDisplayRuleSet.keyAssetRuleGroups = itemDisplayRules.ToArray();
            //itemDisplayRuleSet.GenerateRuntimeValues();
        }

        private static CharacterModel.RendererInfo[] SkinRendererInfos(CharacterModel.RendererInfo[] defaultRenderers, Material[] materials)
        {
            CharacterModel.RendererInfo[] newRendererInfos = new CharacterModel.RendererInfo[defaultRenderers.Length];
            defaultRenderers.CopyTo(newRendererInfos, 0);

            //newRendererInfos[1].defaultMaterial = materials[0];

            return newRendererInfos;
        }

        private static void Hook()
        {
        }
    }
}