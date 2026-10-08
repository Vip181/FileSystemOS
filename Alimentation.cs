using FileSystemOS.Demarrage;
using FileSystemOS.Processus;
using Sys = Cosmos.System;

namespace FileSystemOS.Systeme
{
    /// <summary>
    /// Extinction et redemarrage. Les fenetres, la console et les commandes ne coupent
    /// plus la machine directement : elles DEMANDENT, et le processus "alimentation"
    /// affiche l'ecran d'arret (logo V) avant d'eteindre proprement.
    /// </summary>
    public static class Alimentation
    {
        public const int Aucune = 0, Arret = 1, Redemarrage = 2;
        public static int Demande;

        public static void Arreter() { if (Demande == Aucune) Demande = Arret; }
        public static void Redemarrer() { if (Demande == Aucune) Demande = Redemarrage; }
    }

    public class AlimentationThread : KThread
    {
        private BootScreen screen;
        private uint frame;
        private string liveMsg = "Fermeture des processus...";
        private const int Frames = 45;

        public AlimentationThread() : base("veille") { }

        public override void Step()
        {
            if (Alimentation.Demande == Alimentation.Aucune) return;

            if (screen == null)
            {
                Services.ShuttingDown = true;   // le compositeur s'arrete
                Services.Memory.Write(Alimentation.Demande == Alimentation.Arret ? "Arret du systeme" : "Redemarrage");
                screen = new BootScreen();
                screen.Begin(Alimentation.Demande == Alimentation.Arret ? "Arret en cours" : "Redemarrage en cours");
                if (Live.Active) liveMsg = "Session Live : " + Live.Save();
            }

            frame++;
            screen.Frame(frame, (int)frame, Frames, liveMsg, null);
            if (frame < Frames) return;

            if (Alimentation.Demande == Alimentation.Arret) Sys.Power.Shutdown();
            else Sys.Power.Reboot();
        }
    }
}
