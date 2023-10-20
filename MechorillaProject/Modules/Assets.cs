using System.Reflection;
using R2API;
using UnityEngine;
using UnityEngine.Networking;
using RoR2;
using System.IO;
using RoR2.Audio;
using System.Collections.Generic;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;

namespace SteelMechorilla.Modules
{
    internal static class Assets
    {
        internal static AssetBundle mainAssetBundle;

        internal static Shader hotpoo = Resources.Load<Shader>("Shaders/Deferred/HGStandard");
        internal static Material commandoMat;

        internal static Material matBlueLightningLong;
        internal static Material matJellyfishLightningLarge;
        internal static Material matMageMatrixDirectionalLightning;
        internal static Material matClaySwing;
        internal static Material matDistortion;
        internal static Material matMercSwipe;
        internal static Material matMercSwipeRed;
        internal static Material matLunarGolem;
        internal static Material matMoonElevator;
        internal static Material matLunarMinigunTracer;
        internal static Material matBrotherArcaneFlare;
        internal static Material helfireFlareMat;

        //internal static GameObject drainPunchChargeEffect;

        //internal static NetworkSoundEventDef punchSoundDef;

        internal static List<EffectDef> effectDefs = new List<EffectDef>();
        internal static List<NetworkSoundEventDef> networkSoundEventDefs = new List<NetworkSoundEventDef>();

        internal static void PopulateAssets()
        {
            if (mainAssetBundle == null)
            {
                using (var assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SteelMechorilla.mechorilla"))
                {
                    mainAssetBundle = AssetBundle.LoadFromStream(assetStream);
                }
            }

            using (Stream manifestResourceStream2 = Assembly.GetExecutingAssembly().GetManifestResourceStream("SteelMechorilla.MechorillaBank.bnk"))
            {
                byte[] array = new byte[manifestResourceStream2.Length];
                manifestResourceStream2.Read(array, 0, array.Length);
                SoundAPI.SoundBanks.Add(array);
            }

            //drainPunchChargeEffect = LoadEffect("DrainPunchChargeEffect", true);

            //punchSoundDef = CreateNetworkSoundEventDef("RegigigasPunchImpact");
            Assets.GrabMaterials();
        }

        internal static void GrabMaterials()
        {
            Assets.matBlueLightningLong = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/VFX/matLightningLongBlue.mat").WaitForCompletion();
            /*Assets.matBlueLightningLong = Object.Instantiate<Material>(Resources.Load<GameObject>("Prefabs/Effects/OrbEffects/LightningStrikeOrbEffect").transform.Find("Ring").GetComponent<ParticleSystemRenderer>().material);
            Assets.matJellyfishLightningLarge = Object.Instantiate<Material>(Resources.Load<GameObject>("Prefabs/Effects/ImpactEffects/VagrantCannonExplosion").transform.Find("Lightning, Radial").GetComponent<ParticleSystemRenderer>().material);
            Assets.matMageMatrixDirectionalLightning = Object.Instantiate<Material>(Resources.Load<GameObject>("Prefabs/Effects/OmniEffect/OmniImpactVFXLightningMage").transform.Find("Matrix, Directional").GetComponent<ParticleSystemRenderer>().material);
            Assets.matClaySwing = Object.Instantiate<Material>(Resources.Load<GameObject>("prefabs/effects/ImpSwipeEffect").GetComponentInChildren<ParticleSystemRenderer>().material);
            Assets.matDistortion = Object.Instantiate<Material>(Resources.Load<GameObject>("Prefabs/Effects/ImpactEffects/LoaderGroundSlam").transform.Find("Sphere, Distortion").GetComponent<ParticleSystemRenderer>().material);
            Assets.matMercSwipe = Object.Instantiate<Material>(Resources.Load<GameObject>("Prefabs/Projectiles/EvisProjectile").GetComponent<ProjectileController>().ghostPrefab.transform.Find("Base").GetComponent<ParticleSystemRenderer>().material);
            Assets.matMercSwipeRed = Object.Instantiate<Material>(Resources.Load<GameObject>("Prefabs/Projectiles/EvisProjectile").GetComponent<ProjectileController>().ghostPrefab.transform.Find("Base").GetComponent<ParticleSystemRenderer>().material);
            Assets.matMercSwipeRed.SetColor("_TintColor", Color.red);
            Assets.matLunarGolem = Object.Instantiate<Material>(Resources.Load<GameObject>("Prefabs/CharacterBodies/LunarGolemBody").GetComponentInChildren<CharacterModel>().baseRendererInfos[0].defaultMaterial);*/
            Assets.matMoonElevator = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/VFX/matLightningLongBlue.mat").WaitForCompletion();
            Assets.matLunarMinigunTracer = Addressables.LoadAssetAsync<Material>("RoR2/Base/LunarWisp/matLunarWispMinigunTracer.mat").WaitForCompletion();
            Assets.matBrotherArcaneFlare = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/VFX/matArcaneCircleBrother.mat").WaitForCompletion();
            Assets.helfireFlareMat = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/BurnNearby/HelfireIgniteEffect.prefab").WaitForCompletion().transform.Find("Flare").GetComponent<ParticleSystemRenderer>().sharedMaterial;
        }

        internal static NetworkSoundEventDef CreateNetworkSoundEventDef(string eventName)
        {
            NetworkSoundEventDef networkSoundEventDef = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            networkSoundEventDef.akId = AkSoundEngine.GetIDFromString(eventName);
            networkSoundEventDef.eventName = eventName;

            networkSoundEventDefs.Add(networkSoundEventDef);

            return networkSoundEventDef;
        }

        internal static void ConvertAllRenderersToHopooShader(GameObject objectToConvert)
        {
            foreach (Renderer i in objectToConvert.GetComponentsInChildren<Renderer>())
            {
                if (i)
                {
                    if (i.material)
                    {
                        i.material.shader = hotpoo;
                    }
                }
            }
        }

        internal static CharacterModel.RendererInfo[] SetupRendererInfos(GameObject obj)
        {
            MeshRenderer[] meshes = obj.GetComponentsInChildren<MeshRenderer>();
            CharacterModel.RendererInfo[] rendererInfos = new CharacterModel.RendererInfo[meshes.Length];

            for (int i = 0; i < meshes.Length; i++)
            {
                rendererInfos[i] = new CharacterModel.RendererInfo
                {
                    defaultMaterial = meshes[i].material,
                    renderer = meshes[i],
                    defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                    ignoreOverlays = false
                };
            }

            return rendererInfos;
        }

        public static GameObject LoadSurvivorModel(string modelName) {
            GameObject model = mainAssetBundle.LoadAsset<GameObject>(modelName);
            if (model == null) {
                Log.Error("Trying to load a null model- check to see if the name in your code matches the name of the object in Unity");
                return null;
            }

            return PrefabAPI.InstantiateClone(model, model.name, false);
        }

        internal static Texture LoadCharacterIcon(string characterName)
        {
            return mainAssetBundle.LoadAsset<Texture>("tex" + characterName + "Icon");
        }

        internal static GameObject LoadCrosshair(string crosshairName)
        {
            return Resources.Load<GameObject>("Prefabs/Crosshair/" + crosshairName + "Crosshair");
        }

        private static GameObject LoadEffect(string resourceName)
        {
            return LoadEffect(resourceName, "", false);
        }

        private static GameObject LoadEffect(string resourceName, string soundName)
        {
            return LoadEffect(resourceName, soundName, false);
        }

        private static GameObject LoadEffect(string resourceName, bool parentToTransform)
        {
            return LoadEffect(resourceName, "", parentToTransform);
        }

        private static GameObject LoadEffect(string resourceName, string soundName, bool parentToTransform)
        {
            GameObject newEffect = mainAssetBundle.LoadAsset<GameObject>(resourceName);

            newEffect.AddComponent<DestroyOnTimer>().duration = 12;
            newEffect.AddComponent<NetworkIdentity>();
            newEffect.AddComponent<VFXAttributes>().vfxPriority = VFXAttributes.VFXPriority.Always;
            var effect = newEffect.AddComponent<EffectComponent>();
            effect.applyScale = false;
            effect.effectIndex = EffectIndex.Invalid;
            effect.parentToReferencedTransform = parentToTransform;
            effect.positionAtReferencedTransform = true;
            effect.soundName = soundName;

            AddNewEffectDef(newEffect, soundName);

            return newEffect;
        }

        internal static void AddNewEffectDef(GameObject effectPrefab)
        {
            AddNewEffectDef(effectPrefab, "");
        }

        internal static void AddNewEffectDef(GameObject effectPrefab, string soundName)
        {
            EffectDef newEffectDef = new EffectDef();
            newEffectDef.prefab = effectPrefab;
            newEffectDef.prefabEffectComponent = effectPrefab.GetComponent<EffectComponent>();
            newEffectDef.prefabName = effectPrefab.name;
            newEffectDef.prefabVfxAttributes = effectPrefab.GetComponent<VFXAttributes>();
            newEffectDef.spawnSoundEventName = soundName;

            effectDefs.Add(newEffectDef);
        }

        public static Material CreateMaterial(string materialName, float emission, Color emissionColor, float normalStrength)
        {
            if (!commandoMat) commandoMat = Resources.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody").GetComponentInChildren<CharacterModel>().baseRendererInfos[0].defaultMaterial;

            Material mat = UnityEngine.Object.Instantiate<Material>(commandoMat);
            Material tempMat = Assets.mainAssetBundle.LoadAsset<Material>(materialName);

            if (!tempMat) return commandoMat;

            mat.name = materialName;
            mat.SetColor("_Color", tempMat.GetColor("_Color"));
            mat.SetTexture("_MainTex", tempMat.GetTexture("_MainTex"));
            mat.SetColor("_EmColor", emissionColor);
            mat.SetFloat("_EmPower", emission);
            mat.SetTexture("_EmTex", tempMat.GetTexture("_EmissionMap"));
            mat.SetFloat("_NormalStrength", normalStrength);

            return mat;
        }

        public static Material CreateMaterial(string materialName)
        {
            return Assets.CreateMaterial(materialName, 0f);
        }

        public static Material CreateMaterial(string materialName, float emission)
        {
            return Assets.CreateMaterial(materialName, emission, Color.black);
        }

        public static Material CreateMaterial(string materialName, float emission, Color emissionColor)
        {
            return Assets.CreateMaterial(materialName, emission, emissionColor, 0f);
        }
    }
}