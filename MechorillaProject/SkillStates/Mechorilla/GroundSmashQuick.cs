namespace SteelMechorilla.SkillStates.Mechorilla
{
    public class GroundSmashQuick : GroundSmash
    {
        protected override void PlayAnimation()
        {
            this.duration = 4f / this.attackSpeedStat;
            this.smashTime = 0.18f * this.duration;
            this.meleeDamageCoefficient = 1.5f;

            base.PlayAnimation("FullBody, Override", "GroundSlamQuick", "Action.playbackRate", this.duration);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= 0.75f * this.duration)
            {
                if (base.IsKeyDownAuthority())
                {
                    this.outer.SetNextState(new GroundSmashQuick());
                    return;
                }
            }
        }

        protected override void FireProjectile()
        {
        }
    }
}
