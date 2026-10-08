using System;
using System.Collections.Generic;

namespace FileSystemOS.Fsl
{
    /// <summary>
    /// Compilateur du File Syst Langage : analyse descendante recursive
    /// qui produit directement le bytecode (une seule passe).
    /// </summary>
    public class Compiler
    {
        private List<Token> toks;
        private int pos;
        private FslProgram p;

        public static FslProgram Compile(string source)
        {
            var c = new Compiler();
            c.toks = Lexer.Run(source);
            c.p = new FslProgram();
            while (c.Peek().Kind != TokKind.End) c.Statement();
            c.p.Emit(Op.Halt, c.Peek().Line);
            return c.p;
        }

        // ---- Outils ----
        private Token Peek() => toks[pos];
        private Token Next() => toks[pos++];
        private int Line => Peek().Line;
        private bool IsSym(string s) => Peek().Kind == TokKind.Symbol && Peek().Text == s;
        private bool IsWord(string s) => Peek().Kind == TokKind.Ident && Peek().Text == s;

        private void Err(string msg)
        {
            throw new Exception("Ligne " + Line + " : " + msg + " (trouve '" + Peek().Text + "')");
        }

        private void Expect(string s)
        {
            if (!IsSym(s)) Err("'" + s + "' attendu");
            pos++;
        }

        private int Find(string name)
        {
            for (int i = 0; i < p.VarNames.Count; i++) if (p.VarNames[i] == name) return i;
            return -1;
        }

        // ---- Instructions ----
        private void Statement()
        {
            if (IsWord("int") || IsWord("texte")) Declaration();
            else if (IsWord("afficher"))
            {
                int l = Line;
                pos++;
                Expect("(");
                Expr();
                Expect(")");
                Expect(";");
                p.Emit(Op.Print, l);
            }
            else if (IsWord("si")) If();
            else if (IsWord("tantque")) While();
            else if (IsSym("{")) Block();
            else if (Peek().Kind == TokKind.Ident) Assignment();
            else Err("instruction attendue");
        }

        // int x = 5;   texte t = "a";   int y = 0.nul;   int z;  (z vaut 0/nul)
        private void Declaration()
        {
            int l = Line;
            int type = Next().Text == "int" ? FslProgram.TypeInt : FslProgram.TypeTexte;
            if (Peek().Kind != TokKind.Ident || Lexer.IsKeyword(Peek().Text)) Err("nom de variable attendu");
            string name = Next().Text;
            if (Find(name) >= 0) throw new Exception("Ligne " + l + " : variable '" + name + "' deja declaree");

            int slot = p.VarNames.Count;
            p.VarNames.Add(name);
            p.VarTypes.Add(type);

            if (IsSym("=")) { pos++; Expr(); }
            else p.Emit(Op.PushNull, l);   // sans valeur -> variable nulle

            p.Emit(Op.Store, l);
            p.Emit(slot, l);
            Expect(";");
        }

        private void Assignment()
        {
            int l = Line;
            string name = Next().Text;
            int slot = Find(name);
            if (slot < 0) throw new Exception("Ligne " + l + " : variable inconnue '" + name + "'");
            Expect("=");
            Expr();
            p.Emit(Op.Store, l);
            p.Emit(slot, l);
            Expect(";");
        }

        private void If()
        {
            int l = Line;
            pos++;
            Expect("(");
            Expr();
            Expect(")");
            p.Emit(Op.Jz, l);
            int jz = p.Emit(0, l);
            Block();

            if (IsWord("sinon"))
            {
                pos++;
                p.Emit(Op.Jmp, l);
                int jmp = p.Emit(0, l);
                p.Code[jz] = p.Code.Count;
                if (IsWord("si")) If(); else Block();
                p.Code[jmp] = p.Code.Count;
            }
            else p.Code[jz] = p.Code.Count;
        }

        private void While()
        {
            int l = Line;
            pos++;
            int start = p.Code.Count;
            Expect("(");
            Expr();
            Expect(")");
            p.Emit(Op.Jz, l);
            int jz = p.Emit(0, l);
            Block();
            p.Emit(Op.Jmp, l);
            p.Emit(start, l);
            p.Code[jz] = p.Code.Count;
        }

        private void Block()
        {
            Expect("{");
            while (!IsSym("}"))
            {
                if (Peek().Kind == TokKind.End) Err("'}' manquant");
                Statement();
            }
            pos++;
        }

        // ---- Expressions (de la priorite la plus faible a la plus forte) ----
        private void Expr() => Or();

        private void Or()
        {
            And();
            while (IsSym("||")) { int l = Line; pos++; And(); p.Emit(Op.Or, l); }
        }

        private void And()
        {
            Equality();
            while (IsSym("&&")) { int l = Line; pos++; Equality(); p.Emit(Op.And, l); }
        }

        private void Equality()
        {
            Comparison();
            while (IsSym("==") || IsSym("!="))
            {
                int l = Line;
                int op = Next().Text == "==" ? Op.Eq : Op.Ne;
                Comparison();
                p.Emit(op, l);
            }
        }

        private void Comparison()
        {
            Additive();
            while (IsSym("<") || IsSym(">") || IsSym("<=") || IsSym(">="))
            {
                int l = Line;
                string s = Next().Text;
                int op = s == "<" ? Op.Lt : (s == ">" ? Op.Gt : (s == "<=" ? Op.Le : Op.Ge));
                Additive();
                p.Emit(op, l);
            }
        }

        private void Additive()
        {
            Term();
            while (IsSym("+") || IsSym("-"))
            {
                int l = Line;
                int op = Next().Text == "+" ? Op.Add : Op.Sub;
                Term();
                p.Emit(op, l);
            }
        }

        private void Term()
        {
            Unary();
            while (IsSym("*") || IsSym("/") || IsSym("%"))
            {
                int l = Line;
                string s = Next().Text;
                int op = s == "*" ? Op.Mul : (s == "/" ? Op.Div : Op.Mod);
                Unary();
                p.Emit(op, l);
            }
        }

        private void Unary()
        {
            if (IsSym("-")) { int l = Line; pos++; Unary(); p.Emit(Op.Neg, l); }
            else if (IsSym("!")) { int l = Line; pos++; Unary(); p.Emit(Op.Not, l); }
            else Primary();
        }

        private void Primary()
        {
            Token t = Peek();
            int l = t.Line;

            if (t.Kind == TokKind.Number) { pos++; p.Emit(Op.PushInt, l); p.Emit(t.Value, l); }
            else if (t.Kind == TokKind.String)
            {
                pos++;
                p.Strings.Add(t.Text);
                p.Emit(Op.PushStr, l);
                p.Emit(p.Strings.Count - 1, l);
            }
            else if (t.Kind == TokKind.Null) { pos++; p.Emit(Op.PushNull, l); }
            else if (t.Kind == TokKind.Ident && (t.Text == "vrai" || t.Text == "faux"))
            {
                pos++;
                p.Emit(Op.PushInt, l);
                p.Emit(t.Text == "vrai" ? 1 : 0, l);
            }
            else if (t.Kind == TokKind.Ident && !Lexer.IsKeyword(t.Text))
            {
                pos++;
                int slot = Find(t.Text);
                if (slot < 0) throw new Exception("Ligne " + l + " : variable inconnue '" + t.Text + "'");
                p.Emit(Op.Load, l);
                p.Emit(slot, l);
            }
            else if (IsSym("("))
            {
                pos++;
                Expr();
                Expect(")");
            }
            else Err("valeur attendue");
        }
    }
}
