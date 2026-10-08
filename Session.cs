using System.Collections.Generic;
using FileSystemOS.Graphique;

namespace FileSystemOS.Systeme
{
    /// <summary>Ouverture / verrouillage de session : rien n'est accessible sans connexion.</summary>
    public static class Session
    {
        private static readonly List<Window> hidden = new List<Window>();
        private static bool firstLogin = true;

        /// <summary>Cache toutes les fenetres et affiche l'ecran de connexion.</summary>
        public static void ShowLogin()
        {
            Services.LoggedIn = false;
            hidden.Clear();
            var all = Services.Windows.All;
            for (int i = 0; i < all.Count; i++)
                if (!all[i].Hidden && all[i] != Services.LoginWin) { hidden.Add(all[i]); all[i].Minimized = true; }
            Services.Windows.Open(Services.LoginWin);
            Services.Taskbar.Invalidate();
            Services.ScreenDirty = true;
        }

        public static void Start(Compte c)
        {
            Comptes.Actuel = c;
            Services.LoggedIn = true;
            Services.Windows.Close(Services.LoginWin);
            if (Services.Memory != null) Services.Memory.Write("Connexion : " + c.Nom);

            var wm = Services.Windows;
            if (firstLogin)
            {
                firstLogin = false;
                wm.Open(Services.PowerWin);
                wm.Open(Services.ConsoleWin);
                wm.Open(Services.ExplorerWin);
                if (InstallerWindow.FindRoot() == null) wm.Open(Services.InstallWin);
            }
            else
            {
                for (int i = 0; i < hidden.Count; i++)
                    if (wm.All.Contains(hidden[i])) wm.ToggleFromTaskbar(hidden[i]);
                hidden.Clear();
            }
            Services.Taskbar.Invalidate();
            Services.ScreenDirty = true;
        }

        public static void Lock() => ShowLogin();
    }
}
