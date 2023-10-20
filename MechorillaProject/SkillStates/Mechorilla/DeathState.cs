using EntityStates;
using UnityEngine;
using RoR2;

namespace SteelMechorilla.SkillStates.Mechorilla
{
    public class DeathState : GenericCharacterDeath
    {
        private bool p;

        public override void OnEnter()
        {
            Animator anim = base.GetModelAnimator();
            base.PlayAnimation("FullBody, Override", "BufferEmpty");

            Util.PlaySound("sfx_mechorilla_zap", this.gameObject);

            base.OnEnter();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= 1f && !this.p)
            {
                this.p = true;
                Util.PlaySound("sfx_mechorilla_death", this.gameObject);
            }
        }
    }
}