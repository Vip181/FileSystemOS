using Cosmos.Core.Memory;
using Cosmos.System;
using FileSystemOS.Graphique;
using FileSystemOS.Utils;

namespace FileSystemOS.Processus
{
    // =========== Processus "systeme" ===========

    /// <summary>Lit la souris et le clavier, les envoie au gestionnaire de fenetres.</summary>
    public class InputThread : KThread
    {
        private int lastX = -1, lastY = -1;
        public InputThread() : base("entree") { }

        public override void Step()
        {
            int mx = (int)MouseManager.X;
            int my = (int)MouseManager.Y;
            if (mx != lastX || my != lastY)
            {
                lastX = mx; lastY = my;
                Services.CursorDirty = true; // seul le curseur a bouge : mise a jour legere
            }

            bool down = (MouseManager.MouseState & MouseState.Left) == MouseState.Left;
            Services.Windows.UpdateMouse(mx, my, down);

            while (KeyboardManager.TryReadKey(out KeyEvent key))
            {
                if (key.Key == ConsoleKeyEx.F12) { FileSystemOS.Noyau.Diagnostic.Toggle(); continue; }
                Services.Windows.DispatchKey(key);
            }
        }
    }

    public class GcThread : KThread
    {
        private int counter;
        public GcThread() : base("gc") { }

        public override void Step()
        {
            if (++counter >= 25) { counter = 0; Heap.Collect(); }
        }
    }

    // =========== Processus "affichage" ===========

    public class CompositorThread : KThread
    {
        public CompositorThread() : base("compositeur") { }
        public override void Step()
        {
            if (Services.Booting || Services.ShuttingDown) return;   // ecran de chargement / d'arret
            Services.Compositor.Frame();
        }
    }

    // =========== Processus "interface" ===========

    public class TaskbarThread : KThread
    {
        public TaskbarThread() : base("barre") { }

        public override void Step()
        {
            var t = Services.Taskbar;
            if (!t.BarDirty) return;
            t.BarDirty = false;
            t.RenderBar();
            Services.ScreenDirty = true;
        }
    }

    public class MenuThread : KThread
    {
        public MenuThread() : base("menu") { }

        public override void Step()
        {
            var t = Services.Taskbar;
            if (!t.MenuOpen || !t.MenuDirty) return;
            t.MenuDirty = false;
            t.RenderMenu();
            Services.ScreenDirty = true;
        }
    }

    // =========== Processus "attente" ===========

    /// <summary>Redessine la barre du haut quand elle a change.</summary>
    public class TopBarThread : KThread
    {
        public TopBarThread() : base("barre-haut") { }

        public override void Step()
        {
            var t = Services.TopBar;
            if (!t.Dirty) return;
            t.Dirty = false;
            t.Render();
            Services.ScreenDirty = true;
        }
    }

    /// <summary>Thread du buffer d'attente : calcule les miniatures petit a petit.</summary>
    public class WaitThread : KThread
    {
        private int counter;
        public WaitThread() : base("tampon") { }

        public override void Step()
        {
            if (Services.Wait.Work()) Services.TopBar.Dirty = true;
            if (++counter >= 120)
            {
                counter = 0;
                Services.Wait.Balance();            // evite de saturer le noyau
                Services.TopBar.Dirty = true;       // compteurs a jour
            }
        }
    }

    // =========== Un processus par fenetre ===========

    /// <summary>Redessine le buffer de SA fenetre quand elle a change.</summary>
    public class WindowThread : KThread
    {
        private readonly Window win;
        private bool lastFocus;

        public WindowThread(Window w) : base("rendu") { win = w; }

        public override void Step()
        {
            bool focus = Services.Windows.Focused == win;
            if (focus != lastFocus) { lastFocus = focus; win.Dirty = true; }

            win.Tick();

            // Cogestion : on ne dessine pas ici, on depose une demande de rendu
            if (win.Dirty && !win.Hidden) FileSystemOS.Noyau.Cogestion.Request(win);
        }
    }

    // =========== Processus "horloge" ===========

    public class ClockThread : KThread
    {
        private int counter;
        public ClockThread() : base("rtc") { }

        public override void Step()
        {
            if (++counter < 30) return;
            counter = 0;
            int h = ((Cosmos.HAL.RTC.Hour + Services.ClockOffset) % 24 + 24) % 24;
            string t = Fmt.Two(h) + ":" +
                       Fmt.Two(Cosmos.HAL.RTC.Minute) + ":" +
                       Fmt.Two(Cosmos.HAL.RTC.Second);
            if (t != Services.ClockText)
            {
                Services.ClockText = t;
                Services.Taskbar.Invalidate();
            }
        }
    }

    /// <summary>Thread de demonstration (commande 'demo').</summary>
    public class CounterThread : KThread
    {
        public uint Value;
        public CounterThread() : base("compteur") { }
        public override void Step() { Value++; }
    }
}
