using System;
using FileSystemOS.Processus;
using FileSystemOS.Utils;

namespace FileSystemOS.Fsl
{
    /// <summary>Compile et lance les programmes FSL, chacun dans son propre processus.</summary>
    public static class Launcher
    {
        public static FslProgram CompileFile(string path) =>
            Compiler.Compile(TextCodec.Join(TextCodec.ReadLines(path)));

        /// <summary>fichier.fsl -> fichier.fsc (bytecode)</summary>
        public static string CompileToFile(string path)
        {
            FslProgram prog = CompileFile(path);
            int dot = path.LastIndexOf('.');
            string outPath = (dot > path.LastIndexOf('\\') ? path.Substring(0, dot) : path) + ".fsc";
            prog.Save(outPath);
            return TextCodec.FileName(outPath) + " (" + prog.Code.Count + " mots de bytecode)";
        }

        public static bool Run(string path, out string message)
        {
            string name = TextCodec.FileName(path);
            var outWin = Services.FslWin;
            Services.Windows.Open(outWin);

            FslProgram prog;
            try
            {
                prog = path.ToLower().EndsWith(".fsc") ? FslProgram.Load(path) : CompileFile(path);
            }
            catch (Exception e)
            {
                message = "Erreur de compilation : " + e.Message;
                outWin.Print(message);
                return false;
            }

            outWin.Print("== " + name + " ==");
            Process proc = Services.Scheduler.Spawn("fsl-" + name.ToLower(), false);
            proc.Start(new FslThread(new Vm(prog, outWin), name));
            message = name + " lance (PID " + proc.Pid + ")";
            return true;
        }
    }

    /// <summary>Thread d'un programme FSL : 300 instructions par tour d'ordonnanceur.</summary>
    public class FslThread : KThread
    {
        private readonly Vm vm;
        private readonly string name;

        public FslThread(Vm machine, string programName) : base("vm")
        {
            vm = machine;
            name = programName;
        }

        public override void Step()
        {
            if (vm.Done) return;
            vm.Run(300);
            if (!vm.Done) return;

            if (vm.Error != null) Services.FslWin.Print("Erreur d'execution - " + vm.Error);
            Services.FslWin.Print("-- " + name + " termine (" + vm.Steps + " instructions) --");
            Owner.Kill();
        }
    }
}
