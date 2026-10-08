using System.Collections.Generic;

namespace FileSystemOS.Processus
{
    /// <summary>
    /// Thread cooperatif : Step() fait une petite tranche de travail puis rend la main.
    /// (Cosmos n'a pas encore de threads preemptifs stables, d'ou ce modele.)
    /// </summary>
    public abstract class KThread
    {
        private static int nextTid = 1;

        public readonly int Tid;
        public readonly string Name;
        public bool Alive = true;
        public uint Ticks;
        public uint Errors;          // exceptions rattrapees par l'ordonnanceur
        public Process Owner;

        protected KThread(string name)
        {
            Tid = nextTid++;
            Name = name;
        }

        public abstract void Step();
    }

    /// <summary>Un processus = un nom, un PID et un ou plusieurs threads.</summary>
    public class Process
    {
        private static int nextPid = 1;

        public readonly int Pid;
        public readonly string Name;
        public readonly bool Critical;
        public readonly List<KThread> Threads = new List<KThread>();
        public bool Alive = true;
        public bool Suspended;   // gele : l'ordonnanceur ne l'execute plus (buffer d'attente)
        public int Priority = Prio.Normale;

        public Process(string name, bool critical)
        {
            Pid = nextPid++;
            Name = name;
            Critical = critical;
        }

        public KThread Start(KThread t)
        {
            t.Owner = this;
            Threads.Add(t);
            return t;
        }

        public void Kill()
        {
            Alive = false;
            for (int i = 0; i < Threads.Count; i++) Threads[i].Alive = false;
        }
    }

    /// <summary>Priorites : Haute et Normale a chaque tour, Basse un tour sur quatre.</summary>
    public static class Prio
    {
        public const int Haute = 0, Normale = 1, Basse = 2;
        public static string Name(int p) => p == Haute ? "haute" : (p == Basse ? "basse" : "normale");
    }

    /// <summary>Ordonnanceur round-robin a priorites : chaque thread vivant avance d'un pas par tour.</summary>
    public class Scheduler
    {
        public readonly List<Process> Processes = new List<Process>();
        public uint TotalTicks;

        public Process Spawn(string name, bool critical)
        {
            var p = new Process(name, critical);
            Processes.Add(p);
            return p;
        }

        public Process Spawn(string name, bool critical, int priority)
        {
            Process p = Spawn(name, critical);
            p.Priority = priority;
            return p;
        }

        public void Tick()
        {
            for (int i = 0; i < Processes.Count; i++)
            {
                Process p = Processes[i];
                if (!p.Alive || p.Suspended) continue;
                if (p.Priority == Prio.Basse && (TotalTicks & 3) != 0) continue;   // economise le noyau
                for (int j = 0; j < p.Threads.Count; j++)
                {
                    KThread t = p.Threads[j];
                    if (!t.Alive) continue;
                    // Un thread qui plante ne fait plus planter tout l'OS : l'erreur est notee
                    try { t.Step(); }
                    catch (System.Exception e)
                    {
                        t.Errors++;
                        FileSystemOS.Noyau.Diagnostic.Report(p.Name + "/" + t.Name, e.Message);
                        if (t.Errors >= 20 && !p.Critical) t.Alive = false;   // thread arrete s'il echoue en boucle
                    }
                    t.Ticks++;
                }
            }

            for (int i = Processes.Count - 1; i >= 0; i--)
                if (!Processes[i].Alive) Processes.RemoveAt(i);

            TotalTicks++;
        }

        public bool Kill(int pid, out string message)
        {
            for (int i = 0; i < Processes.Count; i++)
            {
                Process p = Processes[i];
                if (p.Pid != pid) continue;
                if (p.Critical)
                {
                    message = "Erreur : '" + p.Name + "' est un processus systeme.";
                    return false;
                }
                p.Kill();
                message = "Processus '" + p.Name + "' (PID " + pid + ") arrete.";
                return true;
            }
            message = "Erreur : aucun processus avec le PID " + pid;
            return false;
        }
    }
}
