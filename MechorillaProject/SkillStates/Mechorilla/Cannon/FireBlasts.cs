using UnityEngine;
using EntityStates;
using RoR2;
using UnityEngine.Networking;
using System.Linq;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;

namespace SteelMechorilla.SkillStates.Mechorilla.Cannon
{
    public class FireBlasts : BaseSkillState
    {
        public static int blastCount = 8;
        public static float baseShotDuration = 0.3f;
        public static float damageCoefficient = 4.5f;

        private int remainingBlasts;
        private float shotTimer;
        private float shotDuration;

        protected Vector3 predictedTargetPosition;
        private EntityStates.TitanMonster.FireFist.Predictor predictor;

        public override void OnEnter()
        {
            base.OnEnter();
            this.shotDuration = FireBlasts.baseShotDuration;
            this.remainingBlasts = FireBlasts.blastCount;

            if (NetworkServer.active)
            {
                BullseyeSearch bullseyeSearch = new BullseyeSearch();
                bullseyeSearch.teamMaskFilter = TeamMask.allButNeutral;
                if (base.teamComponent) bullseyeSearch.teamMaskFilter.RemoveTeam(base.teamComponent.teamIndex);

                bullseyeSearch.maxDistanceFilter = EntityStates.TitanMonster.FireFist.maxDistance;
                bullseyeSearch.maxAngleFilter = 90f;

                Ray aimRay = base.GetAimRay();
                bullseyeSearch.searchOrigin = aimRay.origin;
                bullseyeSearch.searchDirection = aimRay.direction;
                bullseyeSearch.filterByLoS = false;
                bullseyeSearch.sortMode = BullseyeSearch.SortMode.Angle;
                bullseyeSearch.RefreshCandidates();

                HurtBox hurtBox = bullseyeSearch.GetResults().FirstOrDefault<HurtBox>();
                if (hurtBox)
                {
                    this.predictor = new EntityStates.TitanMonster.FireFist.Predictor(base.transform);
                    this.predictor.SetTargetTransform(hurtBox.transform);
                }
            }

            this.Fire();
        }

        private void Fire()
        {
            this.shotTimer = this.shotDuration;
            this.remainingBlasts--;

            this.PlayAnimation("FullBody, Override", "FireBlast", "Action.playbackRate", this.shotDuration);

            EffectManager.SimpleMuzzleFlash(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LightningStrikeOnHit/SimpleLightningStrikeImpact.prefab").WaitForCompletion(), this.gameObject, "CannonMuzzle", false);

            if (base.isAuthority)
            {
                FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                fireProjectileInfo.projectilePrefab = Modules.Projectiles.blastProjectile;
                fireProjectileInfo.position = this.predictedTargetPosition;
                fireProjectileInfo.rotation = Quaternion.identity;
                fireProjectileInfo.owner = base.gameObject;
                fireProjectileInfo.damage = this.damageStat * FireBlasts.damageCoefficient;
                fireProjectileInfo.force = 2000f;
                fireProjectileInfo.crit = base.characterBody.RollCrit();
                fireProjectileInfo.fuseOverride = EntityStates.TitanMonster.FireFist.entryDuration - EntityStates.TitanMonster.FireFist.trackingDuration;
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
            }

            Util.PlaySound("sfx_mechorilla_blast", this.gameObject);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.shotTimer -= Time.fixedDeltaTime;

            if (this.predictor != null)
            {
                this.predictor.Update();
                this.predictor.GetPredictedTargetPosition(EntityStates.TitanMonster.FireFist.entryDuration - EntityStates.TitanMonster.FireFist.trackingDuration, out this.predictedTargetPosition);
            }

            if (this.shotTimer <= 0f)
            {
                if (this.remainingBlasts <= 0)
                {
                    this.outer.SetNextState(new EndBlasts());
                    return;
                }
                else
                {
                    this.Fire();
                }
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Pain;
        }
    }
}