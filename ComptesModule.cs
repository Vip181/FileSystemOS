using FileSystemOS.Systeme;

namespace FileSystemOS.Demarrage.Modules
{
    public class ComptesModule : BootStage
    {
        public override string Name => "Comptes utilisateurs";

        public override string Run()
        {
            string r = Comptes.Load();
            return Comptes.Liste.Count == 0 ? "aucun compte : creation de l'administrateur" : r;
        }
    }
}
