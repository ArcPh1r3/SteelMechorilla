using RoR2;
using UnityEngine;
using EntityStates;
using EntityStates.TitanMonster;
using UnityEngine.Networking;

namespace SteelMechorilla.SkillStates.Mechorilla
{
    public class SweepingBeam : BaseState
    {
		public float baseDuration = 16f;
		
		private float duration;
		private bool hasFired;
		private float fireStopwatch;
		private float stopwatch;
		private Transform modelTransform;
		private GameObject laserEffect;
		private ChildLocator laserChildLocator;
		private Transform laserEffectEnd;
		protected Transform muzzleTransform;

		public override void OnEnter()
		{
			base.OnEnter();
			this.duration = this.baseDuration / this.attackSpeedStat;
			this.muzzleTransform = this.FindModelChild("BeamMuzzle");
			this.characterBody.SetAimTimer(this.duration);

			base.PlayCrossfade("FullBody, Override", "SweepingBeam", "Action.playbackRate", this.duration, 0.1f);

			this.modelTransform = base.GetModelTransform();

			Util.PlaySound("sfx_mechorilla_foley_01", this.gameObject);
		}

		public override void OnExit()
		{
			this.StopBeam();
			base.characterBody.SetAimTimer(2f);

			base.OnExit();
		}

		private void StartBeam()
        {
			Util.PlaySound(FireMegaLaser.playAttackSoundString, base.gameObject);
			Util.PlaySound(FireMegaLaser.playLoopSoundString, base.gameObject);

			if (this.modelTransform)
			{
				if (this.muzzleTransform && new FireMegaLaser().effectPrefab)
				{
					this.laserEffect = UnityEngine.Object.Instantiate<GameObject>(new FireMegaLaser().laserPrefab, this.muzzleTransform.position, this.muzzleTransform.rotation);
					this.laserEffect.transform.parent = this.muzzleTransform;
					this.laserChildLocator = this.laserEffect.GetComponent<ChildLocator>();
					this.laserEffectEnd = this.laserChildLocator.FindChild("LaserEnd");

					LineRenderer line = this.laserEffect.GetComponent<LineRenderer>();
					line.startWidth = 20f;
					line.endWidth = 20f;
					line.sharedMaterial = Modules.Assets.matBlueLightningLong;

					this.laserEffect.GetComponent<LineBetweenTransforms>().transformNodes[0] = this.muzzleTransform;

					this.laserEffect.transform.Find("Start").Find("Flare").GetComponent<ParticleSystemRenderer>().sharedMaterial = Modules.Assets.helfireFlareMat;
					this.laserEffect.transform.Find("Start").Find("ArcaneFlare").GetComponent<ParticleSystemRenderer>().sharedMaterial = Modules.Assets.matBrotherArcaneFlare;
				}
			}
		}

		private void StopBeam()
        {
			if (this.laserEffect)
            {
				EntityState.Destroy(this.laserEffect);
				Util.PlaySound(FireMegaLaser.stopLoopSoundString, base.gameObject);
			}
        }

		private void EvaluateBeam()
        {
			if (!this.laserEffect) return;

			Vector3 point = this.muzzleTransform.position;

			Ray ray = new Ray();
			ray.direction = this.muzzleTransform.forward;

			/*RaycastHit raycastHit;
			if (Physics.Raycast(this.muzzleTransform.position, this.muzzleTransform.forward, out raycastHit, FireMegaLaser.maxDistance, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore))
			{
				point = raycastHit.point;
			}
			else
			{
				point = this.muzzleTransform.position + (this.muzzleTransform.forward * FireMegaLaser.maxDistance);
			}*/
			point = this.muzzleTransform.position + (this.muzzleTransform.forward * FireMegaLaser.maxDistance);


			ray = new Ray(this.muzzleTransform.position, point - this.muzzleTransform.position);
			bool flag = false;
			if (this.laserEffect && this.laserChildLocator)
			{
				/*RaycastHit raycastHit2;
				if (Physics.Raycast(this.muzzleTransform.position, this.muzzleTransform.forward, out raycastHit2, (point - this.muzzleTransform.position).magnitude, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.UseGlobal))
				{
					point = raycastHit2.point;
					RaycastHit raycastHit3;
					if (Physics.Raycast(this.muzzleTransform.position, this.muzzleTransform.forward, out raycastHit3, raycastHit2.distance, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.UseGlobal))
					{
						point = ray.GetPoint(0.1f);
						flag = true;
					}
				}*/
				this.laserEffect.transform.rotation = Util.QuaternionSafeLookRotation(point - this.muzzleTransform.position);
				this.laserEffectEnd.transform.position = point;
			}

			if (this.fireStopwatch > 1f / FireMegaLaser.fireFrequency)
			{
				if (!flag) this.FireBullet(this.modelTransform, ray, "BeamMuzzle", (point - ray.origin).magnitude + 0.1f);
				this.fireStopwatch -= 1f / FireMegaLaser.fireFrequency;
			}
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			this.fireStopwatch += Time.fixedDeltaTime;
			this.stopwatch += Time.fixedDeltaTime;

			if (this.hasFired)
            {
				if (this.stopwatch >= 0.53 * this.duration)
				{
					this.StopBeam();
				}
				else this.EvaluateBeam();
            }
			else
            {
				if (this.stopwatch >= 0.16f * this.duration)
                {
					this.hasFired = true;
					this.StartBeam();
                }
            }

			if (base.isAuthority && base.fixedAge >= this.duration)
			{
				this.outer.SetNextStateToMain();
				return;
			}
		}

		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return InterruptPriority.Skill;
		}

		private void FireBullet(Transform modelTransform, Ray aimRay, string targetMuzzle, float maxDistance)
		{
			/*if (this.effectPrefab)
			{
				EffectManager.SimpleMuzzleFlash(this.effectPrefab, base.gameObject, targetMuzzle, false);
			}*/

			if (base.isAuthority)
			{
				new BulletAttack
				{
					owner = base.gameObject,
					weapon = base.gameObject,
					origin = this.muzzleTransform.position,
					aimVector = this.muzzleTransform.forward,
					minSpread = 0f,
					maxSpread = 0f,
					bulletCount = 1U,
					damage = (3f * FireMegaLaser.damageCoefficient) * this.damageStat / FireMegaLaser.fireFrequency,
					force = FireMegaLaser.force,
					muzzleName = targetMuzzle,
					hitEffectPrefab = null,
					isCrit = this.RollCrit(),
					procCoefficient = FireMegaLaser.procCoefficientPerTick,
					HitEffectNormal = false,
					radius = 8f,
					procChainMask = default,
					falloffModel = BulletAttack.FalloffModel.None,
					hitMask = LayerIndex.CommonMasks.bullet,
					stopperMask = LayerIndex.ui.intVal,
					maxDistance = maxDistance
				}.Fire();

				new BlastAttack
				{
					attacker = this.gameObject,
					attackerFiltering = AttackerFiltering.NeverHitSelf,
					baseDamage = (1.5f * FireMegaLaser.damageCoefficient) * this.damageStat / FireMegaLaser.fireFrequency,
					baseForce = 400f,
					bonusForce = Vector3.up * 100f,
					crit = this.RollCrit(),
					damageType = DamageType.AOE,
					falloffModel = BlastAttack.FalloffModel.Linear,
					inflictor = this.gameObject,
					losType = BlastAttack.LoSType.None,
					position = this.muzzleTransform.position,
					procChainMask = default,
					procCoefficient = 1f,
					radius = 2.5f,
					teamIndex = this.teamComponent.teamIndex
				}.Fire();
			}
		}

		public override void OnSerialize(NetworkWriter writer)
		{
			base.OnSerialize(writer);
			writer.Write(this.stopwatch);
		}

		public override void OnDeserialize(NetworkReader reader)
		{
			base.OnDeserialize(reader);
			this.stopwatch = reader.ReadSingle();
		}
	}
}