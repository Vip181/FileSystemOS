using FileSystemOS.Graphique;

namespace FileSystemOS.Demarrage.Modules
{
    /// <summary>Cree les fenetres et ouvre celles du bureau de depart.</summary>
    public class ApplicationsModule : BootStage
    {
        public override string Name => "Applications";

        public override string Run()
        {
            Services.PowerWin = new ControlWindow(30, 40);
            Services.ExplorerWin = new ExplorerWindow(200, 60);
            Services.EditorWin = new EditorWindow(300, 90);
            Services.FslWin = new OutputWindow(470, 400);
            Services.InstallWin = new InstallerWindow(280, 120);
            Services.ImageWin = new ImageWindow(240, 80);
            Services.AboutWin = new AboutWindow(260, 90);
            Services.DisplayWin = new DisplayWindow(300, 70);
            Services.BrowserWin = new BrowserWindow(160, 50);
            Services.PdfWin = new PdfWindow(220, 60);
            Services.TextWarWin = new TextWarWindow(260, 110);
            Services.AccountsWin = new AccountsWindow(280, 60);
            Services.LoginWin = new LoginWindow((Services.ScreenW - 440) / 2, (Services.ScreenH - 400) / 2);

            // Rien ne s'ouvre avant la connexion : ecran de connexion d'abord
            FileSystemOS.Systeme.Session.ShowLogin();
            return "ecran de connexion";
        }
    }
}
