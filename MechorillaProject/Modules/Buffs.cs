using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace SteelMechorilla.Modules
{
    public static class Buffs
    {
        internal static List<BuffDef> buffDefs = new List<BuffDef>();

        internal static void RegisterBuffs()
        {
            //armorBuff = AddNewBuff("RegigigasArmorBuff", Resources.Load<Sprite>("Textures/BuffIcons/texBuffGenericShield"), Color.grey, false, false);
            //slowStartBuff = AddNewBuff("RegigigasSlowStartDebuff", Resources.Load<Sprite>("Textures/BuffIcons/texBuffSlow50Icon"), Color.grey, true, false);
        }

        // simple helper method
        internal static BuffDef AddNewBuff(string buffName, Sprite buffIcon, Color buffColor, bool canStack, bool isDebuff)
        {
            BuffDef buffDef = ScriptableObject.CreateInstance<BuffDef>();
            buffDef.name = buffName;
            buffDef.buffColor = buffColor;
            buffDef.canStack = canStack;
            buffDef.isDebuff = isDebuff;
            buffDef.eliteDef = null;
            buffDef.iconSprite = buffIcon;

            buffDefs.Add(buffDef);

            return buffDef;
        }
    }
}