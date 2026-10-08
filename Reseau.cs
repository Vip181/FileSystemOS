using System;
using Cosmos.HAL;
using Cosmos.System.Network.Config;
using Cosmos.System.Network.IPv4.UDP.DHCP;
using FileSystemOS.Processus;

namespace FileSystemOS.Systeme
{
    /// <summary>Reseau : detection de la carte et adresse IP par DHCP.</summary>
    public static class Reseau
    {
        public static string Status = "non initialise";
        public static bool Connected;

        public static string Init(bool dhcp)
        {
            try
            {
                if (NetworkDevice.Devices.Count == 0) { Status = "aucune carte reseau compatible"; return Status; }
                string card = NetworkDevice.Devices[0].Name;
                if (!dhcp) { Status = card + " (DHCP desactive)"; return Status; }

                using (var client = new DHCPClient()) { client.SendDiscoverPacket(); }

                var ip = NetworkConfiguration.CurrentAddress;
                if (ip == null) { Status = card + " : pas d'adresse IP"; return Status; }
                Connected = true;
                Status = card + " - IP " + ip.ToString();
            }
            catch (Exception e) { Status = "erreur : " + e.Message; }
            return Status;
        }
    }

    /// <summary>Processus "reseau" (priorite basse) : garde l'etat du reseau a jour.</summary>
    public class ReseauThread : KThread
    {
        public ReseauThread() : base("etat") { }

        public override void Step()
        {
            if (!Reseau.Connected) return;
            try
            {
                var ip = NetworkConfiguration.CurrentAddress;
                if (ip == null) { Reseau.Connected = false; Reseau.Status = "connexion perdue"; }
            }
            catch (Exception) { }
        }
    }
}
