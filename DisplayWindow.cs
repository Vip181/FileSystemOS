using System.Collections.Generic;
using FileSystemOS.Systeme;

namespace FileSystemOS.Graphique
{
    /// <summary>Affichage : resolution (appliquee au redemarrage) et fond d'ecran.</summary>
    public class DisplayWindow : Window
    {
        private const int ListY = 40, RowH = 24, MaxRows = 7;
        private readonly List<string> choices = new List<string>();
        private readonly Button btnApply, btnNoWall;
        private int selected;
        private string status = "";

        public DisplayWindow(int x, int y) : base("Affichage", x, y, 460, 430, Icons.DisplayIcon)
        {
            btnApply = new Button(Glyphs.Refresh, "Appliquer et redemarrer", 20, ListY + MaxRows * RowH + 8, 230, 30, Theme.Blue, Theme.BlueHover);
            btnNoWall = new Button(Glyphs.Trash, "Retirer le fond", 20, ListY + MaxRows * RowH + 120, 170, 28, Theme.Red, Theme.RedHover);
        }

        public override void OnOpen()
        {
            choices.Clear();
            choices.Add("auto");
            for (int i = 0; i < Materiel.Modes.Count && choices.Count < MaxRows; i++)
            {
                string m = Materiel.Modes[i].Columns + "x" + Materiel.Modes[i].Rows;
                if (Materiel.Modes[i].Columns >= 800 && choices.IndexOf(m) < 0) choices.Add(m);
            }
            string cur = Config.Get("resolution", "auto");
            selected = choices.IndexOf(cur);
            if (selected < 0) selected = 0;
            status = "Actuelle : " + Services.ScreenW + "x" + Services.ScreenH + " (" + Materiel.Choix + ")";
        }

        protected override void DrawContent(PixelBuffer b, int cx, int cy, int cw, int ch)
        {
            b.DrawText("Resolution de l'ecran", Theme.Text, 20, cy + 14);
            for (int i = 0; i < choices.Count; i++)
            {
                int y = cy + ListY + i * RowH;
                if (i == selected) b.FillRect(Theme.RowSelected, 16, y, W - 32, RowH - 2);
                b.FillCircle(Theme.TextMuted, 30, y + 11, 6);
                b.FillCircle(Theme.WindowBg, 30, y + 11, 4);
                if (i == selected) b.FillCircle(Theme.TitleActive, 30, y + 11, 3);
                string label = choices[i] == "auto" ? "Automatique (adaptee a cet ordinateur)" : choices[i];
                b.DrawText(label, Theme.Text, 46, y + 3);
            }
            btnApply.Draw(b, 0, cy);

            int wy = cy + btnNoWall.RY - 30;
            b.DrawText("Fond d'ecran : " + Shorten(Desktop.WallpaperInfo, 36), Theme.Text, 20, wy);
            btnNoWall.Draw(b, 0, cy);
            b.DrawText("Pour en choisir un : ouvrez une image .bmp", Theme.TextMuted, 20, cy + btnNoWall.RY + 40);
            b.DrawText("puis cliquez sur \"Fond d'ecran\".", Theme.TextMuted, 20, cy + btnNoWall.RY + 58);

            b.DrawText(Shorten(status, (W - 40) / 8), Theme.TextMuted, 20, H - 26);
        }

        private static string Shorten(string s, int n) => s.Length > n ? s.Substring(0, n - 2) + ".." : s;

        private bool HoverBtn(Button bt, int mx, int my)
        {
            bool h = mx >= 0 && bt.Hit(X, Y + TitleH, mx, my);
            if (h == bt.Hover) return false;
            bt.Hover = h;
            return true;
        }

        public override void OnMouseMove(int mx, int my)
        {
            if (HoverBtn(btnApply, mx, my) | HoverBtn(btnNoWall, mx, my)) Dirty = true;
        }

        public override void OnMouseDown(int mx, int my)
        {
            int lx = mx - X, ly = my - Y - TitleH;
            btnApply.Pressed = btnApply.Hit(0, 0, lx, ly);
            btnNoWall.Pressed = btnNoWall.Hit(0, 0, lx, ly);
            if (ly >= ListY && ly < ListY + choices.Count * RowH && lx > 16 && lx < W - 16)
                selected = (ly - ListY) / RowH;
        }

        public override void OnMouseUp(int mx, int my)
        {
            int lx = mx - X, ly = my - Y - TitleH;
            if (btnApply.Pressed && btnApply.Hit(0, 0, lx, ly))
            {
                status = Config.SetAndSave("demarrage.cfg", "resolution", choices[selected]);
                Alimentation.Redemarrer();
            }
            if (btnNoWall.Pressed && btnNoWall.Hit(0, 0, lx, ly))
                status = "Fond retire : " + Desktop.SetWallpaper("");
            btnApply.Pressed = btnNoWall.Pressed = false;
        }
    }
}
