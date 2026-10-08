using Cosmos.System.Graphics;
using Cosmos.System.Graphics.Fonts;
using FileSystemOS.Demarrage;
using FileSystemOS.Graphique;
using FileSystemOS.Processus;
using Sys = Cosmos.System;

namespace FileSystemOS
{
    /// <summary>
    /// File System OS - File System OS Company - Vincent Senut
    ///
    /// Le noyau ne prepare que le strict minimum (ecran, souris, ordonnanceur),
    /// puis lance le processus "demarrage" : il affiche le logo V anime et charge
    /// un par un les modules du systeme (Demarrage/Modules), chacun avec ses
    /// processus et ses threads.
    /// </summary>
    public class Kernel : Sys.Kernel
    {
        protected override void BeforeRun()
        {
            // Disques et fichiers internes AVANT l'ecran : la resolution voulue est dans demarrage.cfg
            Systeme.Live.Create();                        // disque en memoire (mode Live), avant le montage
            Demarrage.Modules.FichiersModule.Mount();
            Systeme.Live.Prepare();                       // \FSOS en memoire si l'OS n'est pas installe
            Systeme.Config.LoadAll();

            // Detection du materiel : resolution adaptee a CET ordinateur (avec replis)
            Services.Font = PCScreenFont.Default;
            Services.Canvas = Systeme.Materiel.OpenScreen(Systeme.Config.Get("resolution", "auto"));
            Services.Compositor = new Compositor();   // buffer ecran, utilise aussi par l'ecran de demarrage

            // Souris
            Sys.MouseManager.ScreenWidth = (uint)Services.ScreenW;
            Sys.MouseManager.ScreenHeight = (uint)Services.ScreenH;
            Sys.MouseManager.X = (uint)(Services.ScreenW / 2);
            Sys.MouseManager.Y = (uint)(Services.ScreenH / 2);
            MouseCursor.Build();

            // Ordonnanceur + processus de demarrage
            Services.Scheduler = new Scheduler();
            Process boot = Services.Scheduler.Spawn("demarrage", true, Prio.Haute);
            boot.Start(new BootThread(BootSequence.Stages()));
        }

        protected override void Run()
        {
            Services.Scheduler.Tick();
        }
    }
}
