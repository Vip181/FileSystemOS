using System;
using System.Collections.Generic;

namespace FileSystemOS.Fsl
{
    public enum TokKind { Number, String, Ident, Null, Symbol, End }

    public class Token
    {
        public TokKind Kind;
        public string Text;
        public int Value;
        public int Line;

        public Token(TokKind kind, string text, int value, int line)
        {
            Kind = kind; Text = text; Value = value; Line = line;
        }
    }

    /// <summary>Analyseur lexical du File Syst Langage : texte -> liste de jetons.</summary>
    public static class Lexer
    {
        private static readonly string[] Keywords =
            { "int", "texte", "afficher", "si", "sinon", "tantque", "vrai", "faux" };

        public static bool IsKeyword(string w)
        {
            for (int i = 0; i < Keywords.Length; i++) if (Keywords[i] == w) return true;
            return false;
        }

        public static bool IsDigit(char c) => c >= '0' && c <= '9';
        public static bool IsLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';

        /// <summary>Vrai si src[i..] commence par le litteral nul : 0/nul ou 0.nul</summary>
        public static bool IsNullAt(string s, int i)
        {
            if (i + 5 > s.Length || s[i] != '0') return false;
            if (s[i + 1] != '/' && s[i + 1] != '.') return false;
            if (s[i + 2] != 'n' || s[i + 3] != 'u' || s[i + 4] != 'l') return false;
            return i + 5 == s.Length || !(IsLetter(s[i + 5]) || IsDigit(s[i + 5]));
        }

        public static List<Token> Run(string src)
        {
            var t = new List<Token>();
            int i = 0, line = 1;

            while (i < src.Length)
            {
                char c = src[i];

                if (c == '\n') { line++; i++; continue; }
                if (c == ' ' || c == '\t' || c == '\r') { i++; continue; }

                // Commentaire
                if (c == '/' && i + 1 < src.Length && src[i + 1] == '/')
                {
                    while (i < src.Length && src[i] != '\n') i++;
                    continue;
                }

                // Litteral nul : 0/nul ou 0.nul
                if (IsNullAt(src, i))
                {
                    t.Add(new Token(TokKind.Null, "0/nul", 0, line));
                    i += 5;
                    continue;
                }

                if (IsDigit(c))
                {
                    int start = i, v = 0;
                    while (i < src.Length && IsDigit(src[i])) { v = v * 10 + (src[i] - '0'); i++; }
                    t.Add(new Token(TokKind.Number, src.Substring(start, i - start), v, line));
                    continue;
                }

                if (IsLetter(c))
                {
                    int start = i;
                    while (i < src.Length && (IsLetter(src[i]) || IsDigit(src[i]))) i++;
                    t.Add(new Token(TokKind.Ident, src.Substring(start, i - start), 0, line));
                    continue;
                }

                if (c == '"')
                {
                    i++;
                    int start = i;
                    while (i < src.Length && src[i] != '"' && src[i] != '\n') i++;
                    if (i >= src.Length || src[i] != '"')
                        throw new Exception("Ligne " + line + " : texte non termine (\" manquant)");
                    t.Add(new Token(TokKind.String, src.Substring(start, i - start), 0, line));
                    i++;
                    continue;
                }

                // Symboles a deux caracteres
                if (i + 1 < src.Length)
                {
                    string two = src.Substring(i, 2);
                    if (two == "==" || two == "!=" || two == "<=" || two == ">=" || two == "&&" || two == "||")
                    {
                        t.Add(new Token(TokKind.Symbol, two, 0, line));
                        i += 2;
                        continue;
                    }
                }

                if ("+-*/%<>=!(){};".IndexOf(c) >= 0)
                {
                    t.Add(new Token(TokKind.Symbol, c.ToString(), 0, line));
                    i++;
                    continue;
                }

                throw new Exception("Ligne " + line + " : caractere inattendu '" + c + "'");
            }

            t.Add(new Token(TokKind.End, "fin du fichier", 0, line));
            return t;
        }
    }
}
