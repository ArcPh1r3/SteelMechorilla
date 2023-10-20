using EntityStates;
using RoR2;

namespace SteelMechorilla.SkillStates.Mechorilla
{
    public class SpawnState : BaseState
    {
        public float duration = 2f;

        public override void OnEnter()
        {
            base.OnEnter();

            Util.PlaySound(EntityStates.LemurianBruiserMonster.SpawnState.spawnSoundString, base.gameObject);

            base.PlayAnimation("Body", "Spawn", "Spawn.playbackRate", this.duration);

            if (EntityStates.LemurianBruiserMonster.SpawnState.spawnEffectPrefab)
            {
                EffectManager.SimpleMuzzleFlash(EntityStates.LemurianBruiserMonster.SpawnState.spawnEffectPrefab, base.gameObject, "Model", false);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
    }
}