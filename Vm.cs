using FileSystemOS.Graphique;

namespace FileSystemOS.Fsl
{
    /// <summary>Valeur FSL : nulle (0/nul), entier ou texte.</summary>
    public struct Value
    {
        public const int KNull = 0, KInt = 1, KText = 2;

        public int Kind;
        public int I;
        public string S;

        public static Value Int(int v) { Value x = new Value(); x.Kind = KInt; x.I = v; return x; }
        public static Value Text(string s) { Value x = new Value(); x.Kind = KText; x.S = s; return x; }
    }

    /// <summary>
    /// Machine virtuelle a pile. Run(budget) execute au plus 'budget' instructions
    /// puis rend la main : un programme ne peut donc jamais bloquer l'OS.
    /// </summary>
    public class Vm
    {
        private readonly FslProgram p;
        private readonly int[] code;
        private readonly int[] lines;
        private readonly Value[] stack = new Value[256];
        private readonly Value[] vars;
        private readonly OutputWindow output;
        private int sp, pc;

        public bool Done;
        public string Error;
        public uint Steps;

        public Vm(FslProgram prog, OutputWindow outWin)
        {
            p = prog;
            code = prog.Code.ToArray();
            lines = prog.Lines.ToArray();
            vars = new Value[prog.VarNames.Count]; // toutes les variables commencent a 0/nul
            output = outWin;
        }

        private void Push(Value v)
        {
            if (sp >= stack.Length) { Fail("pile pleine"); return; }
            stack[sp++] = v;
        }

        private Value Pop()
        {
            if (sp <= 0) { Fail("pile vide"); return new Value(); }
            return stack[--sp];
        }

        private void Fail(string msg)
        {
            if (Done) return;
            int ip = pc - 1 < 0 ? 0 : pc - 1;
            int line = ip < lines.Length ? lines[ip] : 0;
            Error = "Ligne " + line + " : " + msg;
            Done = true;
        }

        public static string ToText(Value v)
        {
            if (v.Kind == Value.KNull) return "0/nul";
            if (v.Kind == Value.KInt) return v.I.ToString();
            return v.S;
        }

        private static bool Truthy(Value v)
        {
            if (v.Kind == Value.KInt) return v.I != 0;
            if (v.Kind == Value.KText) return v.S.Length > 0;
            return false;
        }

        private static bool Equal(Value a, Value b)
        {
            if (a.Kind != b.Kind) return false;
            if (a.Kind == Value.KNull) return true;
            if (a.Kind == Value.KInt) return a.I == b.I;
            return a.S == b.S;
        }

        private bool Ints(Value a, Value b)
        {
            if (a.Kind == Value.KNull || b.Kind == Value.KNull)
            {
                Fail("calcul avec une variable nulle (0/nul)");
                return false;
            }
            if (a.Kind != Value.KInt || b.Kind != Value.KInt)
            {
                Fail("nombres attendus");
                return false;
            }
            return true;
        }

        public void Run(int budget)
        {
            while (budget-- > 0 && !Done)
            {
                if (pc >= code.Length) { Done = true; break; }
                int op = code[pc++];
                Steps++;
                Value a, b;

                switch (op)
                {
                    case Op.Halt: Done = true; break;
                    case Op.PushInt: Push(Value.Int(code[pc++])); break;
                    case Op.PushStr: Push(Value.Text(p.Strings[code[pc++]])); break;
                    case Op.PushNull: Push(new Value()); break;
                    case Op.Load: Push(vars[code[pc++]]); break;

                    case Op.Store:
                        {
                            int slot = code[pc++];
                            Value v = Pop();
                            if (v.Kind != Value.KNull && v.Kind != p.VarTypes[slot])
                            {
                                Fail("type incompatible pour '" + p.VarNames[slot] + "' (" +
                                     (p.VarTypes[slot] == FslProgram.TypeInt ? "int" : "texte") + " attendu)");
                                break;
                            }
                            vars[slot] = v;
                            break;
                        }

                    case Op.Add:
                        b = Pop(); a = Pop();
                        if (a.Kind == Value.KText || b.Kind == Value.KText) Push(Value.Text(ToText(a) + ToText(b)));
                        else if (Ints(a, b)) Push(Value.Int(a.I + b.I));
                        break;
                    case Op.Sub: b = Pop(); a = Pop(); if (Ints(a, b)) Push(Value.Int(a.I - b.I)); break;
                    case Op.Mul: b = Pop(); a = Pop(); if (Ints(a, b)) Push(Value.Int(a.I * b.I)); break;
                    case Op.Div:
                        b = Pop(); a = Pop();
                        if (Ints(a, b)) { if (b.I == 0) Fail("division par zero"); else Push(Value.Int(a.I / b.I)); }
                        break;
                    case Op.Mod:
                        b = Pop(); a = Pop();
                        if (Ints(a, b)) { if (b.I == 0) Fail("division par zero"); else Push(Value.Int(a.I % b.I)); }
                        break;

                    case Op.Eq: b = Pop(); a = Pop(); Push(Value.Int(Equal(a, b) ? 1 : 0)); break;
                    case Op.Ne: b = Pop(); a = Pop(); Push(Value.Int(Equal(a, b) ? 0 : 1)); break;
                    case Op.Lt: b = Pop(); a = Pop(); if (Ints(a, b)) Push(Value.Int(a.I < b.I ? 1 : 0)); break;
                    case Op.Gt: b = Pop(); a = Pop(); if (Ints(a, b)) Push(Value.Int(a.I > b.I ? 1 : 0)); break;
                    case Op.Le: b = Pop(); a = Pop(); if (Ints(a, b)) Push(Value.Int(a.I <= b.I ? 1 : 0)); break;
                    case Op.Ge: b = Pop(); a = Pop(); if (Ints(a, b)) Push(Value.Int(a.I >= b.I ? 1 : 0)); break;

                    case Op.And: b = Pop(); a = Pop(); Push(Value.Int(Truthy(a) && Truthy(b) ? 1 : 0)); break;
                    case Op.Or: b = Pop(); a = Pop(); Push(Value.Int(Truthy(a) || Truthy(b) ? 1 : 0)); break;
                    case Op.Not: a = Pop(); Push(Value.Int(Truthy(a) ? 0 : 1)); break;
                    case Op.Neg:
                        a = Pop();
                        if (a.Kind != Value.KInt) Fail("nombre attendu apres '-'");
                        else Push(Value.Int(-a.I));
                        break;

                    case Op.Jmp: pc = code[pc]; break;
                    case Op.Jz:
                        {
                            int target = code[pc++];
                            if (!Truthy(Pop())) pc = target;
                            break;
                        }

                    case Op.Print: output.Print(ToText(Pop())); break;

                    default: Fail("instruction inconnue " + op); break;
                }
            }
        }
    }
}
