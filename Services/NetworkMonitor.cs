using System;
using CriticalCommonLib.Models;
using Dalamud.Game.Network;

namespace CriticalCommonLib.Services
{
    public class NetworkMonitor : IDisposable
    {
        
        public NetworkMonitor()
        {
            Service.Network.NetworkMessage +=OnNetworkMessage;
        }
        
        public delegate void RetainerInformationUpdatedDelegate(NetworkRetainerInformation retainerInformation);
        public event RetainerInformationUpdatedDelegate? OnRetainerInformationUpdated;

        private void OnNetworkMessage(IntPtr dataptr, ushort opcode, uint sourceactorid, uint targetactorid, NetworkMessageDirection direction)
        {
            if (opcode == Utils.GetOpcode("RetainerInformation") && direction == NetworkMessageDirection.ZoneDown)
            {
                var retainerInformation = NetworkDecoder.DecodeRetainerInformation(dataptr);
                OnRetainerInformationUpdated?.Invoke(retainerInformation);
            }
        }

        public void Dispose()
        {
            Service.Network.NetworkMessage -= OnNetworkMessage;
        }
    }
}