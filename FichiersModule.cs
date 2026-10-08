using System;
using Cosmos.System.FileSystem;
using Cosmos.System.FileSystem.VFS;

namespace FileSystemOS.Demarrage.Modules
{
    public class FichiersModule : BootStage
    {
        public override string Name => "Systeme de fichiers";

        /// <summary>Monte les disques FAT (appele des le debut par le noyau).</summary>
        public static void Mount()
        {
            if (Services.FileSystem != null) return;
            try
            {
                Services.FileSystem = new CosmosVFS();
                VFSManager.RegisterVFS(Services.FileSystem);
                Services.FsReady = true;
            }
            catch (Exception) { Services.FsReady = false; }
        }

        public override string Run()
        {
            Mount();
            if (!Services.FsReady) return "aucun disque (l'OS fonctionne quand meme)";
            try { return VFSManager.GetVolumes().Count + " lecteur(s) FAT"; }
            catch (Exception) { return "monte"; }
        }
    }
}
