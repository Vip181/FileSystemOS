using FileSystemOS.Processus;

namespace FileSystemOS.Demarrage.Modules
{
    public class EntreesModule : BootStage
    {
        public override string Name => "Clavier et souris";

        public override string Run()
        {
            Process p = Services.Scheduler.Spawn("systeme", true, Prio.Haute);
            p.Start(new InputThread());
            p.Start(new GcThread());
            return "souris PS/2 et clavier actifs";
        }
    }
}
