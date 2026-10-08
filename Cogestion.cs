using System.Collections.Generic;
using FileSystemOS.Graphique;
using FileSystemOS.Processus;

namespace FileSystemOS.Noyau
{
    /// <summary>
    /// COGESTION threads / processus / buffers de l'interface graphique.
    ///
    /// Chaque fenetre garde son processus et son thread, mais ceux-ci ne dessinent plus
    /// eux-memes : ils deposent une DEMANDE DE RENDU dans un buffer commun.
    /// Le thread "rendus" (processus "cogestion", priorite haute) traite au plus
    /// Budget demandes par tour d'ordonnanceur, la fenetre active en premier.
    /// Resultat : meme avec beaucoup de fenetres, un tour reste court -> pas de lag.
    /// </summary>
    public static class Cogestion
    {
        public static int Budget = 2;          // rendus maximum par tour (interface.cfg)
        public static uint Requests, Renders;

        private static readonly List<Window> queue = new List<Window>();

        public static int Pending => queue.Count;

        public static void Request(Window w)
        {
            if (w.Queued) return;
            w.Queued = true;
            queue.Add(w);
            Requests++;
        }

        public static void Forget(Window w)
        {
            if (!w.Queued) return;
            queue.Remove(w);
            w.Queued = false;
        }

        public static void Work()
        {
            if (queue.Count == 0) return;
            Window focused = Services.Windows.Focused;

            // La fenetre active passe devant les autres
            int fi = focused == null ? -1 : queue.IndexOf(focused);
            if (fi > 0) { queue.RemoveAt(fi); queue.Insert(0, focused); }

            int done = 0;
            while (done < Budget && queue.Count > 0)
            {
                Window w = queue[0];
                queue.RemoveAt(0);
                w.Queued = false;
                if (w.Hidden) continue;           // en attente / reduite : inutile de dessiner
                w.Dirty = false;
                try { w.Render(w == focused); }
                catch (System.Exception e) { Diagnostic.Report("dessin de " + w.Title, e.Message); }
                Renders++;
                done++;
                Services.ScreenDirty = true;
            }
        }
    }

    public class CogestionThread : KThread
    {
        public CogestionThread() : base("rendus") { }
        public override void Step() => Cogestion.Work();
    }
}
