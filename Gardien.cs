using FileSystemOS.Processus;

namespace FileSystemOS.Noyau
{
    /// <summary>
    /// Thread "gardien" du processus "noyau" : mesure chaque seconde la vitesse du systeme
    /// (tours d'ordonnanceur et images par seconde) et adapte le ramasse-miettes.
    /// </summary>
    public class GardienThread : KThread
    {
        private byte lastSec = 255;
        private uint lastTicks, lastFrames;

        public GardienThread() : base("gardien") { }

        public override void Step()
        {
            byte sec = Cosmos.HAL.RTC.Second;
            if (sec == lastSec) return;

            if (lastSec != 255)
            {
                Services.Tps = Services.Scheduler.TotalTicks - lastTicks;
                Services.Fps = Services.Frame - lastFrames;
            }
            lastSec = sec;
            lastTicks = Services.Scheduler.TotalTicks;
            lastFrames = Services.Frame;
            if (Diagnostic.Overlay) Services.ScreenDirty = true;   // panneau F12 mis a jour chaque seconde
        }
    }
}
