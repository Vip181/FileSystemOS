using System.Collections.Generic;
using System.IO;
using Cosmos.System.FileSystem.Listing;
using Cosmos.System.FileSystem.VFS;

namespace FileSystemOS.Utils
{
    /// <summary>Outils de suppression recursive (dossiers, contenu complet d'un lecteur).</summary>
    public static class FsTools
    {
        /// <summary>
        /// Liste tout ce que contient 'dir', le contenu d'un dossier AVANT le dossier lui-meme
        /// (ordre de suppression sur). 'dir' n'est pas inclus.
        /// </summary>
        public static void Collect(string dir, List<string> paths, List<bool> isDir)
        {
            var list = VFSManager.GetDirectoryListing(dir);
            for (int i = 0; i < list.Count; i++)
            {
                string full = list[i].mFullPath;
                if (list[i].mEntryType == DirectoryEntryTypeEnum.Directory)
                {
                    Collect(full, paths, isDir);
                    paths.Add(full);
                    isDir.Add(true);
                }
                else if (list[i].mEntryType == DirectoryEntryTypeEnum.File)
                {
                    paths.Add(full);
                    isDir.Add(false);
                }
            }
        }

        public static void DeleteEntry(string path, bool dir)
        {
            if (dir) Directory.Delete(path);
            else File.Delete(path);
        }

        /// <summary>Supprime un dossier et tout son contenu. Renvoie le nombre d'elements supprimes.</summary>
        public static int DeleteTree(string dir)
        {
            var paths = new List<string>();
            var isDir = new List<bool>();
            Collect(dir, paths, isDir);
            for (int i = 0; i < paths.Count; i++) DeleteEntry(paths[i], isDir[i]);
            Directory.Delete(dir);
            return paths.Count + 1;
        }
    }
}
