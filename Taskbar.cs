using System;
using System.Collections.Generic;
using System.Drawing;
using Cosmos.System.FileSystem.Listing;
using Cosmos.System.FileSystem.VFS;
using FileSystemOS.Fsl;

namespace FileSystemOS.Graphique
{
    /// <summary>
    /// Barre des taches + menu. Chacun a son propre buffer et est redessine
    /// par son propre thread (processus "interface") uniquement quand il a change.
    /// </summary>
    public class Taskbar
    {
        public const int H = Desktop.TaskbarH;

        private const int StartX = 8, StartW = 36;
        private const int TabX0 = 58, TabW = 150, TabH = 22, TabGap = 6;
        public const int MenuW = 250;
        private const int MenuHeaderH = 30, ItemH = 26;

        private readonly HexBitmap iconNormal;
        private readonly HexBitmap iconHover;
        // Entrees fixes, puis les applications .fsl trouvees dans \FSOS\Applications
        private static readonly string[] BaseItems =
            { "Explorateur de fichiers", "Editeur de texte", "Images", "Lecteur PDF", "Navigateur internet", "TextWar", "Console",
              "Comptes", "Affichage", "Installer File System OS", "A propos",
              "Sauvegarder la session", "Verrouiller", "Alimentation" };
        private const int MaxApps = 4;
        private readonly List<string> items = new List<string>();
        private readonly List<string> appPaths = new List<string>();

        // Icones hexa des entrees du menu
        private readonly HexBitmap[] menuIcons =
        {
            new HexBitmap(Icons.Folder, Icons.Palette),
            new HexBitmap(Icons.EditorIcon, Icons.Palette),
            new HexBitmap(Icons.ImageIcon, Icons.Palette),
            new HexBitmap(Icons.PdfIcon, Icons.Palette),
            new HexBitmap(Icons.BrowserIcon, Icons.Palette),
            new HexBitmap(Icons.TextWarIcon, Icons.Palette),
            new HexBitmap(Icons.ConsoleIcon, Icons.Palette),
            new HexBitmap(Icons.AccountIcon, Icons.Palette),
            new HexBitmap(Icons.DisplayIcon, Icons.Palette),
            new HexBitmap(Icons.InstallerIcon, Icons.Palette),
            new HexBitmap(Icons.AboutIcon, Icons.Palette),
            new HexBitmap(Icons.Drive, Icons.Palette),
            new HexBitmap(Icons.LockIcon, Icons.Palette),
            new HexBitmap(Icons.PowerIcon, Icons.Palette),
        };
        private readonly HexBitmap appIcon = new HexBitmap(Icons.FslApp, Icons.Palette);

        public readonly PixelBuffer BarBuffer;
        public PixelBuffer MenuBuffer;
        public bool BarDirty = true;
        public bool MenuDirty = true;
        public bool MenuOpen;

        private bool startHover;
        private int hoverTab = -1;
        private int hoverItem = -1;

        public Taskbar()
        {
            iconNormal = new HexBitmap(Icons.StartNormal, Icons.Palette);
            iconHover = new HexBitmap(Icons.StartHover, Icons.Palette);
            BarBuffer = new PixelBuffer("barre des taches", Services.ScreenW, H);
            RebuildMenu();
        }

        /// <summary>Recharge la liste des applications et adapte le buffer du menu.</summary>
        public int RebuildMenu()
        {
            items.Clear();
            appPaths.Clear();
            for (int i = 0; i < BaseItems.Length; i++) items.Add(BaseItems[i]);

            string root = InstallerWindow.FindRoot();
            if (root != null)
            {
                try
                {
                    var list = VFSManager.GetDirectoryListing(root + "\\Applications");
                    for (int i = 0; i < list.Count && appPaths.Count < MaxApps; i++)
                    {
                        string n = list[i].mName;
                        if (list[i].mEntryType != DirectoryEntryTypeEnum.File) continue;
                        string low = n.ToLower();
                        if (!low.EndsWith(".fsl") && !low.EndsWith(".fsc")) continue;
                        items.Add("App : " + n.Substring(0, n.LastIndexOf('.')));
                        appPaths.Add(list[i].mFullPath);
                    }
                }
                catch (Exception) { }
            }

            if (MenuBuffer == null || MenuBuffer.H != MenuH)
            {
                if (MenuBuffer != null) MenuBuffer.Release();
                MenuBuffer = new PixelBuffer("menu", MenuW, MenuH);
            }
            return appPaths.Count;
        }

        public int Y0 => Services.ScreenH - H;
        public int MenuH => MenuHeaderH + items.Count * ItemH + 8;
        public int MenuX => 6;
        public int MenuY => Y0 - MenuH - 4;

        public void Invalidate() { BarDirty = true; }

        // ---- Tests de position (coordonnees ecran) ----
        private bool InStart(int mx, int my) => mx >= StartX && mx < StartX + StartW && my >= Y0;

        private bool InMenu(int mx, int my) =>
            MenuOpen && mx >= MenuX && mx < MenuX + MenuW && my >= MenuY && my < MenuY + MenuH;

        public bool IsOver(int mx, int my) => my >= Y0 || InMenu(mx, my);

        private int TabAt(int mx, int my)
        {
            if (my < Y0 + 5 || my >= Y0 + 5 + TabH || mx < TabX0) return -1;
            int idx = (mx - TabX0) / (TabW + TabGap);
            if ((mx - TabX0) % (TabW + TabGap) >= TabW) return -1;
            return idx < Services.Windows.All.Count ? idx : -1;
        }

        private int ItemAt(int mx, int my)
        {
            if (!InMenu(mx, my)) return -1;
            int rel = my - (MenuY + MenuHeaderH + 4);
            if (rel < 0) return -1;
            int idx = rel / ItemH;
            return idx < items.Count ? idx : -1;
        }

        // ---- Souris ----
        public void UpdateHover(int mx, int my)
        {
            bool s = InStart(mx, my);
            int t = TabAt(mx, my);
            int it = ItemAt(mx, my);
            if (s != startHover || t != hoverTab) { startHover = s; hoverTab = t; BarDirty = true; }
            if (it != hoverItem) { hoverItem = it; MenuDirty = true; }
        }

        public bool HandleClick(int mx, int my)
        {
            if (!Services.LoggedIn) return my >= Y0;   // rien n'est accessible avant la connexion
            if (MenuOpen)
            {
                if (InMenu(mx, my))
                {
                    int i = ItemAt(mx, my);
                    if (i >= 0) { Activate(i); SetMenu(false); }
                    return true;
                }
                SetMenu(false);
                if (InStart(mx, my) || my < Y0) return true;
            }

            if (my < Y0) return false;

            if (InStart(mx, my)) { SetMenu(true); return true; }

            int t = TabAt(mx, my);
            if (t >= 0) Services.Windows.ToggleFromTaskbar(Services.Windows.All[t]);
            return true;
        }

        private void SetMenu(bool open)
        {
            if (open) RebuildMenu();
            MenuOpen = open;
            MenuDirty = true;
            BarDirty = true;
            Services.ScreenDirty = true;
        }

        private void Activate(int item)
        {
            var wm = Services.Windows;
            switch (item)
            {
                case 0: wm.Open(Services.ExplorerWin); break;
                case 1: wm.Open(Services.EditorWin); break;
                case 2: wm.Open(Services.ImageWin); break;
                case 3: wm.Open(Services.PdfWin); break;
                case 4: wm.Open(Services.BrowserWin); break;
                case 5: wm.Open(Services.TextWarWin); break;
                case 6: wm.Open(Services.ConsoleWin); break;
                case 7: wm.Open(Services.AccountsWin); break;
                case 8: wm.Open(Services.DisplayWin); break;
                case 9: wm.Open(Services.InstallWin); break;
                case 10: wm.Open(Services.AboutWin); break;
                case 11:
                    Services.ConsoleWin.Print("Session : " + FileSystemOS.Systeme.Live.Save());
                    wm.Open(Services.ConsoleWin);
                    break;
                case 12: FileSystemOS.Systeme.Session.Lock(); break;
                case 13: wm.Open(Services.PowerWin); break;
                default: Launcher.Run(appPaths[item - BaseItems.Length], out string msg); break;
            }
        }

        // ---- Rendu dans les buffers (appele par les threads de l'interface) ----
        public void RenderBar()
        {
            var b = BarBuffer;
            b.FillRect(Theme.Taskbar, 0, 0, b.W, H);
            b.FillRect(Theme.TitleActive, 0, 0, b.W, 2);

            bool lit = startHover || MenuOpen;
            if (lit) b.FillRect(Theme.TaskbarHover, StartX, 3, StartW, H - 5);
            (lit ? iconHover : iconNormal).Draw(b, StartX + 8, 7);

            b.FillRect(Theme.TaskbarSep, StartX + StartW + 6, 8, 1, 18);

            var all = Services.Windows.All;
            Window focused = Services.Windows.Focused;
            for (int i = 0; i < all.Count && Services.LoggedIn; i++)
            {
                int x = TabX0 + i * (TabW + TabGap);
                if (x + TabW > b.W - 100) break;

                Window w = all[i];
                bool active = w == focused;

                Color bg = w.Hidden ? Theme.TabMinimized : (active ? Theme.TabActive : Theme.TabNormal);
                if (i == hoverTab && !active) bg = Theme.TabHover;
                b.FillRoundRect(bg, x, 5, TabW, TabH, 6);

                // Icone hexa de l'application (+ glyphe pause si en attente)
                w.Icon.Draw(b, x + 7, 8);
                if (w.Suspended) Glyphs.Wait.DrawTinted(b, x + TabW - 16, 11, Theme.DotYellow);

                string title = w.Title;
                int maxChars = (TabW - 32 - (w.Suspended ? 16 : 0)) / 8;
                if (title.Length > maxChars) title = title.Substring(0, maxChars);
                b.DrawText(title, w.Hidden ? Theme.TabTextMuted : Theme.TitleText, x + 28, 8);

                // Indicateur : long = active, court = ouverte, rien = reduite / en attente
                if (active) b.FillRoundRect(Theme.DotGreen, x + TabW / 2 - 20, 5 + TabH - 3, 40, 3, 1);
                else if (!w.Hidden) b.FillRoundRect(Theme.TabTextMuted, x + TabW / 2 - 6, 5 + TabH - 3, 12, 3, 1);
            }

            b.DrawText(Services.ClockText, Theme.TaskbarText, b.W - 80, 9);
            string who = FileSystemOS.Systeme.Comptes.Actuel != null && Services.LoggedIn ? FileSystemOS.Systeme.Comptes.Actuel.Nom : "";
            if (FileSystemOS.Systeme.Live.Active) who = "LIVE  " + who;
            if (who.Length > 0) b.DrawText(who, Theme.DotYellow, b.W - 92 - who.Length * 8, 9);
        }

        public void RenderMenu()
        {
            var b = MenuBuffer;
            b.FillRect(Theme.MenuBg, 0, 0, MenuW, MenuH);
            b.FillRect(Theme.TitleActive, 0, 0, MenuW, MenuHeaderH);
            b.FillRect(Theme.TitleActiveLight, 0, 0, MenuW, MenuHeaderH / 2);
            b.DrawText(Services.OsName, Theme.TitleText, 12, 7);
            b.DrawText(Services.Version, Theme.TitleActiveLight, MenuW - Services.Version.Length * 8 - 12, 7);

            for (int i = 0; i < items.Count; i++)
            {
                int iy = MenuHeaderH + 4 + i * ItemH;
                if (i == BaseItems.Length) b.FillRect(Theme.MenuBorder, 10, iy - 2, MenuW - 20, 1);
                if (i == hoverItem) b.FillRoundRect(Theme.MenuHover, 4, iy, MenuW - 8, ItemH - 2, 6);
                HexBitmap ico = i < BaseItems.Length ? menuIcons[i] : appIcon;
                ico.Draw(b, 12, iy + (ItemH - 2 - 16) / 2);
                b.DrawText(items[i], Theme.TitleText, 36, iy + 4);
            }

            b.DrawRect(Theme.MenuBorder, 0, 0, MenuW, MenuH);
        }
    }
}
