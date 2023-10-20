using RoR2;
using UnityEngine;
using UnityEngine.Networking;

using R2API.Networking.Interfaces;

namespace SteelMechorilla.Modules
{
    internal static class NetMessages
    {
        public static void RegisterNetworkMessages()
        {
            //R2API.Networking.NetworkingAPI.RegisterMessageType<SyncLifeSteal>();
        }

        public class SyncLifeSteal : INetMessage
        {
            NetworkInstanceId netId;
            float healAmount;

            public SyncLifeSteal() { }
            public SyncLifeSteal(NetworkInstanceId netId_, float healAmount_) {
                netId = netId_;
                healAmount = healAmount_;
            }

            public void Serialize(NetworkWriter writer) {
                writer.Write(netId);
                writer.Write(healAmount);
            }

            public void Deserialize(NetworkReader reader) {
                netId = reader.ReadNetworkId();
                healAmount = reader.ReadSingle();
            }

            public void OnReceived() {
                if (!NetworkServer.active) {
                    //Debug.Log("SyncLifeSteal: Client ran this. Skip.");
                    return;
                }
                //Chat.AddMessage($"Client received SyncSomething. Position received is {position}. Number received is {number}.");
                GameObject bodyObject = Util.FindNetworkObject(netId);
                if (!bodyObject) {
                    Log.Warning("SyncLifeSteal: bodyObject is null.");
                    return;
                }
                bodyObject.GetComponent<HealthComponent>().Heal(healAmount, default(ProcChainMask));
            }
        }
    }
}