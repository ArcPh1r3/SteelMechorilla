using UnityEngine;
using EntityStates;
using RoR2;

namespace SteelMechorilla.SkillStates.Mechorilla.Cannon
{
    public class EndBlasts : BaseSkillState
    {
        public static float baseDuration = 2f;

        private float duration;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = PrepBlasts.baseDuration;

            this.PlayCrossfade("FullBody, Override", "StopBlasts", "Action.playbackRate", this.duration, 0.1f);
            Util.PlaySound("sfx_mechorilla_blast_end", this.gameObject);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= this.duration)
            {
                this.outer.SetNextStateToMain();
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Pain;
        }
    }
}