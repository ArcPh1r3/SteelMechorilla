using EntityStates;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace SteelMechorilla.SkillStates.Mechorilla
{
    public class GroundSmash : BaseSkillState
    {
        public static float baseDuration = 9f;
        public float damageCoefficient = 2.5f;
        public float meleeDamageCoefficient = 4f;

        protected float smashTime;
        private bool hasSmashed;
        protected float duration;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = GroundSmash.baseDuration / this.attackSpeedStat;
            this.smashTime = 0.15f * this.duration;
            this.hasSmashed = false;

            this.characterDirection.moveVector = this.characterDirection.forward;

            Util.PlaySound("sfx_mechorilla_grunt", this.gameObject);
            this.PlayAnimation();
        }

        protected virtual void PlayAnimation()
        {
            base.PlayAnimation("FullBody, Override", "GroundSlam", "Action.playbackRate", this.duration);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= this.smashTime && !this.hasSmashed)
            {
                this.Smash();
            }

            if (base.isAuthority && base.fixedAge >= this.duration)
            {
                this.outer.SetNextStateToMain();
            }
        }

        private void Smash()
        {
            this.hasSmashed = true;

            Util.PlaySound("sfx_mechorilla_smash", base.gameObject);
            Util.PlaySound("sfx_mechorilla_bigsmash", base.gameObject);

            EffectManager.SimpleMuzzleFlash(Resources.Load<GameObject>("Prefabs/Effects/ImpactEffects/PodGroundImpact"), base.gameObject, "GroundSmashMuzzle", false);

            if (base.isAuthority)
            {
                Transform shockwaveTransform = base.FindModelChild("GroundSmashMuzzle");
                Vector3 shockwavePosition = shockwaveTransform.position;
                //Vector3 forward = base.characterDirection.forward;
                this.FireProjectile();

                new BlastAttack
                {
                    attacker = this.gameObject,
                    attackerFiltering = AttackerFiltering.NeverHitSelf,
                    baseDamage = this.meleeDamageCoefficient * this.damageStat,
                    baseForce = 800f,
                    bonusForce = Vector3.up * 2000f,
                    crit = base.RollCrit(),
                    damageColorIndex = DamageColorIndex.WeakPoint,
                    damageType = DamageType.AOE,
                    falloffModel = BlastAttack.FalloffModel.None,
                    inflictor = this.gameObject,
                    losType = BlastAttack.LoSType.None,
                    position = shockwavePosition,
                    procChainMask = default(ProcChainMask),
                    procCoefficient = 1.0f,
                    radius = 12f,
                    teamIndex = this.GetTeam()
                }.Fire();
            }
        }

        protected virtual void FireProjectile()
        {
            Transform muzzle = this.FindModelChild("GroundSmashMuzzleA");
            ProjectileManager.instance.FireProjectile(Modules.Projectiles.shockwave, muzzle.position, Util.QuaternionSafeLookRotation(muzzle.forward), base.gameObject, base.characterBody.damage * this.damageCoefficient, EntityStates.BrotherMonster.WeaponSlam.waveProjectileForce, base.RollCrit(), DamageColorIndex.Default, null, -1f);

            muzzle = this.FindModelChild("GroundSmashMuzzleB");
            ProjectileManager.instance.FireProjectile(Modules.Projectiles.shockwave, muzzle.position, Util.QuaternionSafeLookRotation(muzzle.forward), base.gameObject, base.characterBody.damage * this.damageCoefficient, EntityStates.BrotherMonster.WeaponSlam.waveProjectileForce, base.RollCrit(), DamageColorIndex.Default, null, -1f);
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}