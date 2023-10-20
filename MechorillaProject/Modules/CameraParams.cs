using RoR2;
using UnityEngine;

namespace SteelMechorilla.Modules
{
    internal static class CameraParams
    {
        internal static CharacterCameraParams defaultCameraParams;
        //internal static CharacterCameraParams aimCameraParams;
        //internal static CharacterCameraParams chargeCameraParams;

        internal static void InitializeParams()
        {
            defaultCameraParams = NewCameraParams("ccpMechorilla", 70f, 1.37f, new Vector3(0f, 10f, -30f));
            //aimCameraParams = NewCameraParams("ccpRegigigasAim", 70f, 1.37f, new Vector3(3f, 10f, -20f));
            //chargeCameraParams = NewCameraParams("ccpRegigigasCharge", 70f, 1.37f, new Vector3(0f, 15f, -50f));
        }

        private static CharacterCameraParams NewCameraParams(string name, float pitch, float pivotVerticalOffset, Vector3 standardPosition)
        {
            return NewCameraParams(name, pitch, pivotVerticalOffset, standardPosition, 0.1f);
        }

        private static CharacterCameraParams NewCameraParams(string name, float pitch, float pivotVerticalOffset, Vector3 standardPosition, float wallCushion)
        {
            CharacterCameraParams newParams = ScriptableObject.CreateInstance<CharacterCameraParams>();

            newParams.data.maxPitch = pitch;
            newParams.data.minPitch = -pitch;
            newParams.data.pivotVerticalOffset = pivotVerticalOffset;
            newParams.data.idealLocalCameraPos = standardPosition;
            newParams.data.wallCushion = wallCushion;

            return newParams;
        }
    }
}