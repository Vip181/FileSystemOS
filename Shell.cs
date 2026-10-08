using System;
using System.Collections.Generic;
using Cosmos.System.FileSystem.Listing;
using Cosmos.System.FileSystem.VFS;
using Cosmos.Core.Memory;
using System.IO;
using FileSystemOS.Fsl;
using FileSystemOS.Graphique;
using FileSystemOS.Memoire;
using FileSystemOS.Processus;
using FileSystemOS.Systeme;
using FileSystemOS.Utils;
using Sys = Cosmos.System;

namespace FileSystemOS
{
    public static unsafe class Shell
    {
        public static void Run(string line, List<string> o)
        {
            string cmd = line, arg = "";
            int sp = line.IndexOf(' ');
            if (sp >= 0)
            {
                cmd = line.Substring(0, sp);
                arg = line.Substring(sp + 1).Trim();
            }
            cmd = cmd.ToLower();

            if (cmd == "aide") Help(o);
            else if (cmd == "coucou") WriteText("coucou", o);
            else if (cmd == "ecrire")
            {
                if (arg.Length == 0) o.Add("Erreur : usage -> ecrire <texte>");
                else WriteText(arg, o);
            }
            else if (cmd == "lire") Read(o);
            else if (cmd == "dump") Dump(o);
            else if (cmd == "mem") Mem(o);
            else if (cmd == "accel") Accel(o);
            else if (cmd == "ps") Ps(o);
            else if (cmd == "buffers") Buffers(o);
            else if (cmd == "disques") Disks(o);
            else if (cmd == "dir") Dir(arg, o);
            else if (cmd == "ouvrir") OpenFile(arg, o);
            else if (cmd == "creer") CreateFile(arg, o);
            else if (cmd == "fslc") CompileFsl(arg, o);
            else if (cmd == "run") RunFsl(arg, o);
            else if (cmd == "demo") Demo(o);
            else if (cmd == "kill") Kill(arg, o);
            else if (cmd == "curseur") Cursor(o);
            else if (cmd == "heure") o.Add("Il est " + Services.ClockText);
            else if (cmd == "live") o.Add("Mode Live : " + Live.Status);
            else if (cmd == "sauver") o.Add("Session : " + Live.Save());
            else if (cmd == "qui") o.Add("Connecte : " + (Comptes.Actuel == null ? "personne" : Comptes.Actuel.Nom + (Comptes.Actuel.Admin ? " (admin)" : "")));
            else if (cmd == "comptes")
            {
                for (int i = 0; i < Comptes.Liste.Count; i++)
                    o.Add("  " + Comptes.Liste[i].Nom + (Comptes.Liste[i].Admin ? "  (admin)" : ""));
                Services.Windows.Open(Services.AccountsWin);
            }
            else if (cmd == "verrouiller") Session.Lock();
            else if (cmd == "pdf")
            {
                if (arg.Length == 0) o.Add("Erreur : usage -> pdf 0:\\document.pdf");
                else { Services.PdfWin.Load(arg); Services.Windows.Open(Services.PdfWin); o.Add("PDF ouvert : " + arg); }
            }
            else if (cmd == "textwar")
            {
                Services.Windows.Open(Services.TextWarWin);
                o.Add("TextWar ouvert" + (arg.Length > 0 ? " (collez l'adresse : " + arg + ")" : ""));
            }
            else if (cmd == "telechargements")
            {
                string f = Downloads.Folder();
                o.Add("Dossier : " + (f ?? "aucun disque"));
                if (f != null) { Services.ExplorerWin.Navigate(f, true); Services.Windows.Open(Services.ExplorerWin); }
            }
            else if (cmd == "web")
            {
                Services.Windows.Open(Services.BrowserWin);
                if (arg.Length > 0) Services.BrowserWin.Navigate(arg, true);
                o.Add("Navigateur : " + (arg.Length > 0 ? arg : Http.Home));
            }
            else if (cmd == "materiel") { var r = Materiel.Report(); for (int i = 0; i < r.Count; i++) o.Add(r[i]); }
            else if (cmd == "resolution")
            {
                if (arg.Length == 0)
                {
                    o.Add("Actuelle : " + Services.ScreenW + "x" + Services.ScreenH + " (" + Materiel.Choix + ")");
                    o.Add("Changer : resolution 1280x720  ou  resolution auto  (puis redemarrer)");
                }
                else o.Add("Resolution " + arg + " : " + Config.SetAndSave("demarrage.cfg", "resolution", arg.ToLower()) +
                           " - redemarrez pour l'appliquer");
            }
            else if (cmd == "fond")
            {
                if (arg.Length == 0) o.Add("Fond actuel : " + Desktop.WallpaperInfo + "   (fond <image.bmp> | fond aucun)");
                else o.Add("Fond d'ecran : " + Desktop.SetWallpaper(arg == "aucun" ? "" : arg));
            }
            else if (cmd == "image")
            {
                if (arg.Length == 0) o.Add("Erreur : usage -> image 0:\\photo.bmp");
                else { Services.ImageWin.Load(arg); Services.Windows.Open(Services.ImageWin); o.Add("Image ouverte : " + arg); }
            }
            else if (cmd == "apropos" || cmd == "version")
            {
                Services.Windows.Open(Services.AboutWin);
                o.Add(Services.OsName + " version " + Services.Version);
                o.Add("Editeur : " + Services.Company);
                o.Add("Createur : " + Services.Author);
                o.Add("Copie interdite de l'OS.");
                o.Add("OS Open Source : veuillez mentionner la version et l'OS en cas de toute modification.");
                o.Add("Langage integre : File Syst Langage (FSL)");
            }
            else if (cmd == "arreter") Alimentation.Arreter();
            else if (cmd == "redemarrer") Alimentation.Redemarrer();
            else if (cmd == "reseau" || cmd == "ip") o.Add("Reseau : " + Reseau.Status);
            else if (cmd == "demarrage")
            {
                o.Add("Journal du chargement :");
                for (int i = 0; i < Services.BootLog.Count; i++) o.Add("  " + Services.BootLog[i]);
            }
            else if (cmd == "config")
            {
                var d = Config.Dump();
                o.Add("Fichiers internes (\\FSOS\\Systeme) : " + d.Count + " reglage(s)");
                for (int i = 0; i < d.Count; i++) o.Add("  " + d[i]);
            }
            else if (cmd == "perf")
            {
                o.Add("Tours d'ordonnanceur / s : " + Services.Tps);
                o.Add("Images / s               : " + Services.Fps);
                o.Add("Cogestion : " + FileSystemOS.Noyau.Cogestion.Renders + " rendus, " +
                      FileSystemOS.Noyau.Cogestion.Pending + " en attente, budget " + FileSystemOS.Noyau.Cogestion.Budget + "/tour");
            }
            else o.Add("Erreur : commande inconnue '" + cmd + "' (tapez aide)");
        }

        private static void Help(List<string> o)
        {
            o.Add("Commandes disponibles :");
            o.Add("  coucou         ecrit \"coucou\" dans la memoire");
            o.Add("  ecrire <txt>   ecrit un texte dans la memoire");
            o.Add("  lire           relit tout ce qui a ete ecrit");
            o.Add("  dump           affiche la zone memoire en hexa");
            o.Add("  mem            etat de la memoire");
            o.Add("  accel          teste l'accelerateur memoire");
            o.Add("  ps             processus, threads et priorites");
            o.Add("  buffers        buffers graphiques en memoire");
            o.Add("  disques        lecteurs detectes");
            o.Add("  dir <chemin>   liste un dossier (ex: dir 0:\\)");
            o.Add("  ouvrir <fich>  ouvre un fichier dans l'editeur");
            o.Add("  creer <fich>   cree un fichier texte et l'ouvre");
            o.Add("  fslc <f.fsl>   compile un programme FSL en .fsc");
            o.Add("  run <fichier>  execute un programme .fsl ou .fsc");
            o.Add("  demo           lance un processus de demo");
            o.Add("  kill <pid>     arrete un processus (ou une fenetre)");
            o.Add("  curseur        le curseur en hexadecimal");
            o.Add("  heure          affiche l'heure");
            o.Add("  apropos        version, editeur et auteur");
            o.Add("  demarrage      journal du chargement de l'OS");
            o.Add("  config         reglages des fichiers internes");
            o.Add("  perf           vitesse du systeme et cogestion");
            o.Add("  reseau         etat du reseau et adresse IP");
            o.Add("  materiel       processeur, memoire, ecran detectes");
            o.Add("  web [adresse|recherche]  navigateur internet");
            o.Add("  pdf <fichier>  lecteur PDF        textwar  telecharger des textes");
            o.Add("  telechargements  ouvre le dossier des telechargements");
            o.Add("  qui / comptes  utilisateur connecte / comptes");
            o.Add("  verrouiller    verrouille la session");
            o.Add("  live / sauver  mode Live et sauvegarde de la session");
            o.Add("  resolution [LxH|auto]  taille de l'ecran (au redemarrage)");
            o.Add("  fond [image.bmp|aucun] fond d'ecran");
            o.Add("  image <f.bmp>  ouvre une image");
            o.Add("  effacer        vide la console");
            o.Add("  arreter / redemarrer");
            o.Add("Fleches, PageUp/PageDown ou la barre pour defiler.");
        }

        private static void WriteText(string text, List<string> o)
        {
            uint addr = Services.Memory.Write(text);
            if (addr == 0)
            {
                o.Add("Erreur : zone memoire pleine (4 Ko).");
                return;
            }
            o.Add("\"" + text + "\" ecrit a l'adresse 0x" + Fmt.Hex(addr));
            o.Add("Format : [longueur][octets ASCII]");
            var d = Services.Memory.DumpAt(addr, (uint)text.Length + 1);
            for (int i = 0; i < d.Count; i++) o.Add(d[i]);
        }

        private static void Read(List<string> o)
        {
            var all = Services.Memory.ReadAll();
            o.Add("Textes en memoire : " + all.Count);
            for (int i = 0; i < all.Count; i++) o.Add("  " + all[i]);
        }

        private static void Dump(List<string> o)
        {
            var m = Services.Memory;
            if (m.Used == 0) { o.Add("Zone vide."); return; }
            uint n = m.Used < 128 ? m.Used : 128;
            var d = m.DumpAt(m.BaseAddress, n);
            for (int i = 0; i < d.Count; i++) o.Add(d[i]);
        }

        private static void Mem(List<string> o)
        {
            var m = Services.Memory;
            var a = Services.Accelerator;
            o.Add("RAM detectee      : " + Cosmos.Core.CPU.GetAmountOfRAM() + " Mo");
            o.Add("Zone de textes    : 0x" + Fmt.Hex(m.BaseAddress) + "  " + m.Used + "/" + MemoryManager.ZoneSize + " octets");
            o.Add("Pool accelerateur : 0x" + Fmt.Hex(a.PoolAddress) + "  " + a.FreeBlocks + "/" + MemoryAccelerator.BlockCount + " blocs libres");
            o.Add("Pool              : hits=" + a.Hits + "  misses=" + a.Misses);
        }

        private static void Accel(List<string> o)
        {
            var a = Services.Accelerator;

            uint before = a.Hits;
            uint[] blocks = new uint[32];
            for (int i = 0; i < 32; i++) blocks[i] = (uint)a.Alloc(48);
            for (int i = 0; i < 32; i++) a.Free((byte*)blocks[i]);
            o.Add("32 allocations de 48 octets : " + (a.Hits - before) + " servies par le pool");

            byte* src = Heap.Alloc(4096);
            byte* dst = Heap.Alloc(4096);
            for (uint i = 0; i < 4096; i++) src[i] = (byte)(i * 7);
            MemoryAccelerator.Copy(dst, src, 4096);
            bool ok = true;
            for (uint i = 0; i < 4096; i++) if (dst[i] != src[i]) { ok = false; break; }
            o.Add("Copie rapide 4096 octets (32 bits) : " + (ok ? "OK" : "ECHEC"));

            MemoryAccelerator.Fill(dst, 0xAB, 4096);
            ok = true;
            for (uint i = 0; i < 4096; i++) if (dst[i] != 0xAB) { ok = false; break; }
            o.Add("Remplissage rapide 4096 octets     : " + (ok ? "OK" : "ECHEC"));

            Heap.Free(src);
            Heap.Free(dst);
        }

        private static void Ps(List<string> o)
        {
            var procs = Services.Scheduler.Processes;
            for (int i = 0; i < procs.Count; i++)
            {
                Process p = procs[i];
                o.Add("PID " + Fmt.Pad(p.Pid.ToString(), 3) + " " + Fmt.Pad(p.Name, 10) +
                      (p.Critical ? "[systeme]" : "[utilisateur]") + " " + Prio.Name(p.Priority) + (p.Suspended ? " [en attente]" : ""));
                for (int j = 0; j < p.Threads.Count; j++)
                {
                    KThread t = p.Threads[j];
                    o.Add("    TID " + Fmt.Pad(t.Tid.ToString(), 3) + " " + Fmt.Pad(t.Name, 10) + " ticks=" + t.Ticks);
                }
            }
            o.Add("Ordonnanceur : " + Services.Scheduler.TotalTicks + " tours");
        }

        private static void Buffers(List<string> o)
        {
            var reg = PixelBuffer.Registry;
            uint total = 0;
            for (int i = 0; i < reg.Count; i++)
            {
                PixelBuffer b = reg[i];
                total += b.Bytes;
                o.Add("0x" + Fmt.Hex(b.Address) + "  " + Fmt.Pad(b.W + "x" + b.H, 9) + " " +
                      Fmt.Pad((b.Bytes / 1024).ToString() + " Ko", 8) + " " + b.Name);
            }
            o.Add("Total : " + reg.Count + " buffers, " + (total / 1024) + " Ko");
        }

        private static void Disks(List<string> o)
        {
            if (!Services.FsReady) { o.Add("Erreur : aucun systeme de fichiers."); return; }
            try
            {
                var vols = VFSManager.GetVolumes();
                if (vols.Count == 0) o.Add("Aucun lecteur detecte.");
                for (int i = 0; i < vols.Count; i++)
                {
                    string d = vols[i].mFullPath;
                    o.Add(Fmt.Pad(d, 6) + " total " + Fmt.Size(Services.FileSystem.GetTotalSize(d)) +
                          "   libre " + Fmt.Size(Services.FileSystem.GetAvailableFreeSpace(d)));
                }
            }
            catch (Exception e) { o.Add("Erreur : " + e.Message); }
        }

        private static void Dir(string arg, List<string> o)
        {
            if (!Services.FsReady) { o.Add("Erreur : aucun systeme de fichiers."); return; }
            string p = arg.Length == 0 ? "0:\\" : arg;
            try
            {
                var list = VFSManager.GetDirectoryListing(p);
                o.Add("Contenu de " + p + " (" + list.Count + ")");
                for (int i = 0; i < list.Count; i++)
                {
                    bool dir = list[i].mEntryType == DirectoryEntryTypeEnum.Directory;
                    o.Add((dir ? "[DOS] " : "      ") + Fmt.Pad(list[i].mName, 32) +
                          (dir ? "" : Fmt.Size(list[i].mSize)));
                }
            }
            catch (Exception e) { o.Add("Erreur : " + e.Message); }
        }

        private static void OpenFile(string arg, List<string> o)
        {
            if (arg.Length == 0) { o.Add("Erreur : usage -> ouvrir 0:\\fichier.txt"); return; }
            Services.EditorWin.Load(arg);
            Services.Windows.Open(Services.EditorWin);
            o.Add("Ouvert dans l'editeur : " + arg);
        }

        private static void CreateFile(string arg, List<string> o)
        {
            if (arg.Length == 0) { o.Add("Erreur : usage -> creer 0:\\notes.txt"); return; }
            try
            {
                if (!File.Exists(arg)) File.WriteAllText(arg, "");
                Services.EditorWin.Load(arg);
                Services.Windows.Open(Services.EditorWin);
                o.Add("Fichier pret : " + arg);
            }
            catch (Exception e) { o.Add("Erreur : " + e.Message); }
        }

        private static void CompileFsl(string arg, List<string> o)
        {
            if (arg.Length == 0) { o.Add("Erreur : usage -> fslc 0:\\prog.fsl"); return; }
            try { o.Add("Compile : " + Launcher.CompileToFile(arg)); }
            catch (Exception e) { o.Add("Erreur de compilation : " + e.Message); }
        }

        private static void RunFsl(string arg, List<string> o)
        {
            if (arg.Length == 0) { o.Add("Erreur : usage -> run 0:\\prog.fsl"); return; }
            Launcher.Run(arg, out string msg);
            o.Add(msg);
        }

        private static void Demo(List<string> o)
        {
            Process p = Services.Scheduler.Spawn("demo", false);
            p.Start(new CounterThread());
            o.Add("Processus 'demo' lance (PID " + p.Pid + "). Tapez ps.");
        }

        private static void Kill(string arg, List<string> o)
        {
            int pid = Fmt.ParseInt(arg);
            if (pid < 0) { o.Add("Erreur : usage -> kill <pid>"); return; }
            Services.Scheduler.Kill(pid, out string msg);
            o.Add(msg);
        }

        private static void Cursor(List<string> o)
        {
            o.Add("Curseur " + MouseCursor.W + "x" + MouseCursor.H + " : 1 chiffre hexa = 1 pixel");
            o.Add("0 = transparent  1 = 0xFF000000 (noir)  F = 0xFFFFFFFF (blanc)");
            for (int y = 0; y < MouseCursor.H; y++)
            {
                char[] row = new char[MouseCursor.W];
                for (int x = 0; x < MouseCursor.W; x++)
                {
                    uint px = MouseCursor.Bitmap[y * MouseCursor.W + x];
                    row[x] = px == 0 ? '0' : (px == MouseCursor.Black ? '1' : 'F');
                }
                o.Add(Fmt.Two(y) + " : " + new string(row) +
                      "   contour 0x" + Fmt.Hex(MouseCursor.Outline[y], 3) +
                      "  remplissage 0x" + Fmt.Hex(MouseCursor.Fill[y], 3));
            }
        }
    }
}
