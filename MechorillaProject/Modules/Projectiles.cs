using R2API;
using Rewired.ComponentControls.Effects;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.Rendering.PostProcessing;

namespace SteelMechorilla.Modules
{
    internal static class Projectiles
    {
        public static GameObject shockwave;

        public static GameObject blastProjectile;
        public static GameObject blastProjectileGhost;

        internal static void RegisterProjectiles()
        {
            shockwave = PrefabAPI.InstantiateClone(RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/BrotherSunderWave"), "MechorillaShockwave", true);
            shockwave.transform.GetChild(0).transform.localScale = new Vector3(10, 1.5f, 1);
            shockwave.GetComponent<ProjectileCharacterController>().lifetime = 2f;
            shockwave.GetComponent<ProjectileDamage>().damageType = DamageType.AOE;

            /*GameObject shockwaveGhost = PrefabAPI.InstantiateClone(shockwave.GetComponent<ProjectileController>().ghostPrefab, "PaladinShockwaveGhost", false);
            shockwaveGhost.transform.GetChild(0).transform.localScale = new Vector3(10, 1, 1);
            shockwaveGhost.transform.GetChild(1).transform.localScale = new Vector3(10, 1.5f, 1);
            MechorillaPlugin.Destroy(shockwaveGhost.transform.GetChild(0).Find("Infection, World").gameObject);
            MechorillaPlugin.Destroy(shockwaveGhost.transform.GetChild(0).Find("Water").gameObject);

            //Material shockwaveMat = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/LunarWispTrackingBomb").GetComponent<ProjectileController>().ghostPrefab.transform.GetChild(0).GetComponent<ParticleSystemRenderer>().material;
            Material matMeteorIndicator = UnityEngine.Object.Instantiate(RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/MeteorStrikePredictionEffect").transform.Find("GroundSlamIndicator").GetComponent<MeshRenderer>().material);
            shockwaveGhost.transform.GetChild(1).GetComponent<MeshRenderer>().material = matMeteorIndicator;*/

            GameObject shockwaveGhost = Projectiles.CreateGhostPrefab("MechorillaShockwaveGhost");

            shockwaveGhost.AddComponent<Modules.Components.FuckShitThisIsBad>();

            shockwave.GetComponent<ProjectileController>().ghostPrefab = shockwaveGhost;

            Modules.Prefabs.projectilePrefabs.Add(shockwave);

            blastProjectile = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Captain/CaptainAirstrikeProjectile1.prefab").WaitForCompletion(), "MechorillaBlastProjectile", true);

            blastProjectileGhost = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Captain/CaptainAirstrikeGhost1.prefab").WaitForCompletion(), "MechorillaBlastProjectileGhost", true);
            blastProjectileGhost.transform.Find("Expander").Find("Sphere, Inner Expanding").GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/matTeamAreaIndicatorIntersectionMonster.mat").WaitForCompletion();

            blastProjectile.GetComponent<ProjectileImpactExplosion>().impactEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Lightning/LightningStrikeImpact.prefab").WaitForCompletion();
            blastProjectile.GetComponent<ProjectileController>().ghostPrefab = blastProjectileGhost;

            Modules.Prefabs.projectilePrefabs.Add(blastProjectile);
        }

        /*private static void CreateEarthPowerWave()
        {
            earthPowerWave = CloneProjectilePrefab("BrotherUltLineProjectileRotateLeft", "EarthPowerProjectile");

            earthPowerWave.GetComponent<ProjectileDamage>().damageType = DamageType.IgniteOnHit;

            RegigigasPlugin.DestroyImmediate(earthPowerWave.GetComponent<RotateAroundAxis>());

            GameObject waveGhost = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/ProjectileGhosts/BrotherUltLineGhost"), "EarthPowerProjectileGhost", true);
            if (!waveGhost.GetComponent<NetworkIdentity>()) waveGhost.AddComponent<NetworkIdentity>();

            // gather materials and stuff
            PostProcessProfile magmaWormPP = Resources.Load<GameObject>("Prefabs/CharacterBodies/MagmaWormBody").GetComponentInChildren<PostProcessVolume>().sharedProfile;
            Material matMagmaOpaqueLarge = Resources.Load<GameObject>("Prefabs/ProjectileGhosts/MagmaOrbGhost").transform.Find("Particles").Find("SpitCore").GetComponent<ParticleSystemRenderer>().material;
            Material matMagmaOpaqueDirectional = Resources.Load<GameObject>("Prefabs/Effects/MagmaWormBurrow").transform.Find("ParticleLoop").Find("Magma, Directional").GetComponent<ParticleSystemRenderer>().material;
            Material titanPredictionEffect = Resources.Load<GameObject>("Prefabs/Projectiles/TitanPreFistProjectile").transform.Find("TeamAreaIndicator, GroundOnly").GetComponent<TeamAreaIndicator>().teamMaterialPairs[0].sharedMaterial;
            Material matSpiteBombPredictionEffect = Resources.Load<GameObject>("Prefabs/Effects/SpiteBombDelayEffect").transform.Find("Nova Sphere").GetComponent<ParticleSystemRenderer>().material;

            waveGhost.transform.Find("Size").Find("IndicatorFX").GetComponent<MeshRenderer>().material = titanPredictionEffect;
            waveGhost.transform.Find("Size").Find("IndicatorFX").Find("Edges").GetComponent<MeshRenderer>().material = matSpiteBombPredictionEffect;
            waveGhost.transform.Find("Size").Find("FireFX").Find("Dust, Directional").GetComponent<ParticleSystemRenderer>().material = matMagmaOpaqueDirectional;
            waveGhost.transform.Find("Size").Find("FireFX").Find("SparksUp").GetComponent<ParticleSystemRenderer>().material = matMagmaOpaqueLarge;

            waveGhost.transform.Find("Size").Find("FireFX").GetComponent<PostProcessVolume>().sharedProfile = magmaWormPP;

            earthPowerWave.GetComponent<ProjectileController>().ghostPrefab = waveGhost;
        }
        /*
        private static void CreateBomb()
        {
            bombPrefab = CloneProjectilePrefab("CommandoGrenadeProjectile", "HenryBombProjectile");

            ProjectileImpactExplosion bombImpactExplosion = bombPrefab.GetComponent<ProjectileImpactExplosion>();
            InitializeImpactExplosion(bombImpactExplosion);

            bombImpactExplosion.blastRadius = 16f;
            bombImpactExplosion.destroyOnEnemy = true;
            bombImpactExplosion.lifetime = 12f;
            bombImpactExplosion.impactEffect = Modules.Assets.bombExplosionEffect;
            bombImpactExplosion.explosionSoundString = "HenryBombExplosion";
            bombImpactExplosion.timerAfterImpact = true;
            bombImpactExplosion.lifetimeAfterImpact = 0.1f;

            ProjectileController bombController = bombPrefab.GetComponent<ProjectileController>();
            bombController.ghostPrefab = CreateGhostPrefab("HenryBombGhost");
            bombController.startSound = "";
        }
        */
        private static void InitializeImpactExplosion(ProjectileImpactExplosion projectileImpactExplosion)
        {
            projectileImpactExplosion.blastDamageCoefficient = 1f;
            projectileImpactExplosion.blastProcCoefficient = 1f;
            projectileImpactExplosion.blastRadius = 1f;
            projectileImpactExplosion.bonusBlastForce = Vector3.zero;
            projectileImpactExplosion.childrenCount = 0;
            projectileImpactExplosion.childrenDamageCoefficient = 0f;
            projectileImpactExplosion.childrenProjectilePrefab = null;
            projectileImpactExplosion.destroyOnEnemy = false;
            projectileImpactExplosion.destroyOnWorld = false;
            projectileImpactExplosion.explosionSoundString = "";
            projectileImpactExplosion.falloffModel = RoR2.BlastAttack.FalloffModel.None;
            projectileImpactExplosion.fireChildren = false;
            projectileImpactExplosion.impactEffect = null;
            projectileImpactExplosion.lifetime = 0f;
            projectileImpactExplosion.lifetimeAfterImpact = 0f;
            projectileImpactExplosion.lifetimeExpiredSoundString = "";
            projectileImpactExplosion.lifetimeRandomOffset = 0f;
            projectileImpactExplosion.offsetForLifetimeExpiredSound = 0f;
            projectileImpactExplosion.timerAfterImpact = false;

            projectileImpactExplosion.GetComponent<ProjectileDamage>().damageType = DamageType.Generic;
        }

        private static GameObject CreateGhostPrefab(string ghostName)
        {
            GameObject ghostPrefab = Modules.Assets.mainAssetBundle.LoadAsset<GameObject>(ghostName);
            ghostPrefab.AddComponent<NetworkIdentity>();
            ghostPrefab.AddComponent<ProjectileGhostController>();

            Modules.Assets.ConvertAllRenderersToHopooShader(ghostPrefab);

            return ghostPrefab;
        }

        private static GameObject CloneProjectilePrefab(string prefabName, string newPrefabName)
        {
            GameObject newPrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/Projectiles/" + prefabName), newPrefabName);
            return newPrefab;
        }
    }
}