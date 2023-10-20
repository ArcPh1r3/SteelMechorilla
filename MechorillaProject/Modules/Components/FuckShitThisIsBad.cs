using UnityEngine;
using RoR2;

namespace SteelMechorilla.Modules.Components
{
    public class FuckShitThisIsBad : MonoBehaviour
    {
        private float stopwatch;
        private GameObject effectPrefab;
        // cant be fucked making an actual ghost for this.

        private void Awake()
        {
            this.effectPrefab = Resources.Load<GameObject>("Prefabs/Effects/ImpactEffects/PodGroundImpact");
            this.stopwatch = 0.2f;
        }

        private void FixedUpdate()
        {
            this.stopwatch -= Time.fixedDeltaTime;

            if (this.stopwatch <= 0f)
            {
                this.stopwatch = 0.2f;
                EffectManager.SimpleEffect(this.effectPrefab, this.transform.position, Quaternion.identity, false);
                Util.PlaySound("sfx_mechorilla_smash", this.gameObject);
            }
        }
    }
}