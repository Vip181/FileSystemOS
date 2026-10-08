using System;
using System.Collections.Generic;
using System.IO;

namespace FileSystemOS.Fsl
{
    /// <summary>Jeu d'instructions de la machine virtuelle FSL.</summary>
    public static class Op
    {
        public const int Halt = 0, PushInt = 1, PushStr = 2, PushNull = 3, Load = 4, Store = 5;
        public const int Add = 6, Sub = 7, Mul = 8, Div = 9, Mod = 10;
        public const int Eq = 11, Ne = 12, Lt = 13, Gt = 14, Le = 15, Ge = 16;
        public const int And = 17, Or = 18, Not = 19, Neg = 20;
        public const int Jmp = 21, Jz = 22, Print = 23;
    }

    /// <summary>
    /// Programme compile : bytecode + table des textes + table des variables.
    /// Peut etre enregistre dans un fichier .fsc (format "FSC1").
    /// </summary>
    public class FslProgram
    {
        public const int TypeInt = 1, TypeTexte = 2;

        public readonly List<int> Code = new List<int>();
        public readonly List<int> Lines = new List<int>();     // ligne source de chaque mot
        public readonly List<string> Strings = new List<string>();
        public readonly List<string> VarNames = new List<string>();
        public readonly List<int> VarTypes = new List<int>();

        public int Emit(int v, int line)
        {
            Code.Add(v);
            Lines.Add(line);
            return Code.Count - 1;
        }

        // ---- Fichier .fsc ----
        public void Save(string path)
        {
            var o = new List<byte>();
            o.Add((byte)'F'); o.Add((byte)'S'); o.Add((byte)'C'); o.Add((byte)'1');

            WriteInt(o, Strings.Count);
            for (int i = 0; i < Strings.Count; i++) WriteStr(o, Strings[i]);

            WriteInt(o, VarNames.Count);
            for (int i = 0; i < VarNames.Count; i++) { WriteStr(o, VarNames[i]); WriteInt(o, VarTypes[i]); }

            WriteInt(o, Code.Count);
            for (int i = 0; i < Code.Count; i++) { WriteInt(o, Code[i]); WriteInt(o, Lines[i]); }

            File.WriteAllBytes(path, o.ToArray());
        }

        public static FslProgram Load(string path)
        {
            byte[] d = File.ReadAllBytes(path);
            if (d.Length < 4 || d[0] != 'F' || d[1] != 'S' || d[2] != 'C' || d[3] != '1')
                throw new Exception("ce fichier n'est pas un programme FSL compile");

            var p = new FslProgram();
            int pos = 4;

            int ns = ReadInt(d, ref pos);
            for (int i = 0; i < ns; i++) p.Strings.Add(ReadStr(d, ref pos));

            int nv = ReadInt(d, ref pos);
            for (int i = 0; i < nv; i++) { p.VarNames.Add(ReadStr(d, ref pos)); p.VarTypes.Add(ReadInt(d, ref pos)); }

            int nc = ReadInt(d, ref pos);
            for (int i = 0; i < nc; i++) { p.Code.Add(ReadInt(d, ref pos)); p.Lines.Add(ReadInt(d, ref pos)); }
            return p;
        }

        private static void WriteInt(List<byte> o, int v)
        {
            o.Add((byte)v); o.Add((byte)(v >> 8)); o.Add((byte)(v >> 16)); o.Add((byte)(v >> 24));
        }

        private static void WriteStr(List<byte> o, string s)
        {
            WriteInt(o, s.Length);
            for (int i = 0; i < s.Length; i++) o.Add(s[i] < 128 ? (byte)s[i] : (byte)'?');
        }

        private static int ReadInt(byte[] d, ref int pos)
        {
            if (pos + 4 > d.Length) throw new Exception("fichier .fsc tronque");
            int v = d[pos] | (d[pos + 1] << 8) | (d[pos + 2] << 16) | (d[pos + 3] << 24);
            pos += 4;
            return v;
        }

        private static string ReadStr(byte[] d, ref int pos)
        {
            int n = ReadInt(d, ref pos);
            if (n < 0 || pos + n > d.Length) throw new Exception("fichier .fsc tronque");
            char[] c = new char[n];
            for (int i = 0; i < n; i++) c[i] = (char)d[pos + i];
            pos += n;
            return new string(c);
        }
    }
}
