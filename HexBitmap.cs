namespace FileSystemOS.Graphique
{
    /// <summary>
    /// Image ecrite en HEXADECIMAL : chaque caractere (0-F) = 1 pixel = un index de palette.
    /// Le constructeur la reconvertit en bitmap ARGB 32 bits (0x00...... = transparent).
    /// </summary>
    public class HexBitmap
    {
        public readonly int W, H;
        public readonly uint[] Pixels;

        public HexBitmap(string[] rows, uint[] palette)
        {
            H = rows.Length;
            W = rows[0].Length;
            Pixels = new uint[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    Pixels[y * W + x] = palette[HexValue(rows[y][x])];
        }

        private static int HexValue(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            return 0;
        }

        public void Draw(PixelBuffer b, int x, int y) => b.DrawImage(Pixels, W, H, x, y);

        /// <summary>Dessine la forme d'une seule couleur (pour les glyphes : croix, tiret...).</summary>
        public void DrawTinted(PixelBuffer b, int x, int y, System.Drawing.Color c)
        {
            int v = PixelBuffer.Argb(c);
            for (int j = 0; j < H; j++)
                for (int i = 0; i < W; i++)
                    if ((Pixels[j * W + i] >> 24) != 0) b.SetPixel(x + i, y + j, v);
        }
    }

    /// <summary>Les deux bitmaps du bouton de la barre des taches (20x20).</summary>
    public static class Icons
    {
        // Palette : index hexa -> couleur ARGB
        public static readonly uint[] Palette =
        {
            0x00000000, // 0 transparent
            0xFF5865F2, // 1 indigo
            0xFF7C8BFF, // 2 indigo clair
            0xFFC8CEFF, // 3 lavande
            0xFFFFFFFF, // 4 blanc
            0xFF4DE1C1, // 5 menthe
            0xFFB4BEFF, // 6 halo
            0xFFF2C14E, // 7 jaune dossier
            0xFFC9962A, // 8 contour dossier
            0xFFF6F7FB, // 9 papier
            0xFF8C95A8, // A contour gris
            0xFF6B7385, // B corps disque
            0xFF4DE16A, // C voyant vert
            0xFF7C8BFF, // D lignes de texte
            0xFF22252F, // E fond sombre
            0xFFE25555, // F rouge
        };

        // ================= Icones d'applications 16x16 =================

        public static readonly string[] ConsoleIcon =
        {
            "0AAAAAAAAAAAAAA0",
            "AAAAAAAAAAAAAAAA",
            "AAAAAAAAAAAAAAAA",
            "EEEEEEEEEEEEEEEE",
            "EECEEEEEEEEEEEEE",
            "EECCEEEEEEEEEEEE",
            "EEECCEEEEEEEEEEE",
            "EEEECCEEEEEEEEEE",
            "EEECCEEEEEEEEEEE",
            "EECCEEEEEEEEEEEE",
            "EECEEEE44444EEEE",
            "EEEEEEEEEEEEEEEE",
            "EEEEEEEEEEEEEEEE",
            "EEEEEEEEEEEEEEEE",
            "EEEEEEEEEEEEEEEE",
            "0EEEEEEEEEEEEEE0",
        };

        public static readonly string[] EditorIcon =
        {
            "0011111111111100",
            "0011111111111100",
            "00A9999999999A00",
            "00A9999999999A00",
            "00A9DDDDDDDD9A00",
            "00A9999999999A00",
            "00A9DDDDDDDD9A00",
            "00A9999999999A00",
            "00A9DDDDDD999A00",
            "00A9999999999A00",
            "00A9DDDDDDDD9A00",
            "00A9999999999A00",
            "00A9DDDDD9999A00",
            "00A9999999999A00",
            "00AAAAAAAAAAAA00",
            "0000000000000000",
        };

        public static readonly string[] PowerIcon =
        {
            "0000000FF0000000",
            "0000000FF0000000",
            "000FF00FF00FF000",
            "00FF000FF000FF00",
            "0FF0000FF0000FF0",
            "0FF0000FF0000FF0",
            "FF00000FF00000FF",
            "FF000000000000FF",
            "FF000000000000FF",
            "FF000000000000FF",
            "0FF0000000000FF0",
            "0FF0000000000FF0",
            "00FF00000000FF00",
            "000FFF0000FFF000",
            "00000FFFFFF00000",
            "0000000000000000",
        };

        public static readonly string[] InstallerIcon =
        {
            "0000000CC0000000",
            "0000000CC0000000",
            "0000000CC0000000",
            "0000000CC0000000",
            "0000000CC0000000",
            "000CCCCCCCCCC000",
            "0000CCCCCCCC0000",
            "00000CCCCCC00000",
            "000000CCCC000000",
            "0000000CC0000000",
            "0000000000000000",
            "AA000000000000AA",
            "AA000000000000AA",
            "AAAAAAAAAAAAAAAA",
            "AAAAAAAAAAAAAAAA",
            "0000000000000000",
        };

        public static readonly string[] OutputIcon =
        {
            "0EEEEEEEEEEEEEE0",
            "EEEEEEEEEEEEEEEE",
            "EEEEEEEEEEEEEEEE",
            "EEEEE5EEEEEEEEEE",
            "EEEEE55EEEEEEEEE",
            "EEEEE555EEEEEEEE",
            "EEEEE5555EEEEEEE",
            "EEEEE55555EEEEEE",
            "EEEEE55555EEEEEE",
            "EEEEE5555EEEEEEE",
            "EEEEE555EEEEEEEE",
            "EEEEE55EEEEEEEEE",
            "EEEEE5EEEEEEEEEE",
            "EEEEEEEEEEEEEEEE",
            "EEEEEEEEEEEEEEEE",
            "0EEEEEEEEEEEEEE0",
        };

        // ================= Glyphes 10x10 (masques, recolores a l'affichage) =================

        public static readonly string[] CloseGlyph =
        {
            "1000000001",
            "1100000011",
            "0110000110",
            "0011001100",
            "0001111000",
            "0001111000",
            "0011001100",
            "0110000110",
            "1100000011",
            "1000000001",
        };

        public static readonly string[] MinimizeGlyph =
        {
            "0000000000",
            "0000000000",
            "0000000000",
            "0000000000",
            "1111111111",
            "1111111111",
            "0000000000",
            "0000000000",
            "0000000000",
            "0000000000",
        };

        /// <summary>Mettre en attente (pause).</summary>
        public static readonly string[] WaitGlyph =
        {
            "0000000000",
            "0011001100",
            "0011001100",
            "0011001100",
            "0011001100",
            "0011001100",
            "0011001100",
            "0011001100",
            "0011001100",
            "0000000000",
        };

        public static readonly string[] CheckGlyph =
        {
            "0000000001",
            "0000000011",
            "0000000110",
            "0000001100",
            "1000011000",
            "1100110000",
            "0111100000",
            "0011000000",
            "0000000000",
            "0000000000",
        };

        // ---- Icones 16x16 de l'explorateur ----
        public static readonly string[] Folder =
        {
            "0000000000000000",
            "0888880000000000",
            "8777778888888800",
            "8777777777777780",
            "8888888888888888",
            "8777777777777778",
            "8777777777777778",
            "8777777777777778",
            "8777777777777778",
            "8777777777777778",
            "8777777777777778",
            "8777777777777778",
            "8777777777777778",
            "8777777777777778",
            "0888888888888880",
            "0000000000000000",
        };

        public static readonly string[] TextFile =
        {
            "00AAAAAAAAA00000",
            "00A9999999AA0000",
            "00A9999999A9A000",
            "00A9999999AAAA00",
            "00A9999999999A00",
            "00A9DDDDDDDD9A00",
            "00A9999999999A00",
            "00A9DDDDDDDD9A00",
            "00A9999999999A00",
            "00A9DDDDDDDD9A00",
            "00A9999999999A00",
            "00A9DDDDD9999A00",
            "00A9999999999A00",
            "00A9999999999A00",
            "00AAAAAAAAAAAA00",
            "0000000000000000",
        };

        public static readonly string[] Drive =
        {
            "0000000000000000",
            "0000000000000000",
            "0000000000000000",
            "0000000000000000",
            "0AAAAAAAAAAAAAA0",
            "0ABBBBBBBBBBBBA0",
            "0ABBBBBBBBBBBBA0",
            "0ABBBBBBBBBBBBA0",
            "0AAAAAAAAAAAAAA0",
            "0ABBBBBBBBBBCBA0",
            "0ABBBBBBBBBBBBA0",
            "0AAAAAAAAAAAAAA0",
            "0000000000000000",
            "0000000000000000",
            "0000000000000000",
            "0000000000000000",
        };

        /// <summary>Icone des programmes .fsl (un "F" blanc sur indigo).</summary>
        public static readonly string[] FslApp =
        {
            "0111111111111110",
            "1111111111111111",
            "1111111111111111",
            "1114444444441111",
            "1114444444441111",
            "1114411111111111",
            "1114411111111111",
            "1114444444111111",
            "1114444444111111",
            "1114411111111111",
            "1114411111111111",
            "1114411111111111",
            "1114411111111111",
            "1111111111111111",
            "1111111111111111",
            "0111111111111110",
        };

        /// <summary>Programme compile .fsc : meme "F" sur fond gris.</summary>
        public static string[] FslCompiled()
        {
            string[] r = new string[FslApp.Length];
            for (int i = 0; i < r.Length; i++) r[i] = FslApp[i].Replace('1', 'B');
            return r;
        }

        /// <summary>Fichier generique = fichier texte sans les lignes (D -> 9).</summary>
        public static string[] PlainFile()
        {
            string[] r = new string[TextFile.Length];
            for (int i = 0; i < r.Length; i++) r[i] = TextFile[i].Replace('D', '9');
            return r;
        }

        // Bitmap 1 : curseur EN DEHORS de l'icone
        public static readonly string[] StartNormal =
        {
            "00111111111111111100",
            "01111111111111111110",
            "11333311333311333311",
            "11333311333311333311",
            "11333311333311333311",
            "11333311333311333311",
            "11111111111111111111",
            "11111111111111111111",
            "11333311333311333311",
            "11333311333311333311",
            "11333311333311333311",
            "11333311333311333311",
            "11111111111111111111",
            "11111111111111111111",
            "11333311333311333311",
            "11333311333311333311",
            "11333311333311333311",
            "11333311333311333311",
            "01111111111111111110",
            "00111111111111111100",
        };

        // Bitmap 2 : curseur SUR l'icone (plus lumineux, halo, point central menthe)
        public static readonly string[] StartHover =
        {
            "00666666666666666600",
            "06222222222222222260",
            "62444422444422444426",
            "62444422444422444426",
            "62444422444422444426",
            "62444422444422444426",
            "62222222222222222226",
            "62222222222222222226",
            "62444422555522444426",
            "62444422555522444426",
            "62444422555522444426",
            "62444422555522444426",
            "62222222222222222226",
            "62222222222222222226",
            "62444422444422444426",
            "62444422444422444426",
            "62444422444422444426",
            "62444422444422444426",
            "06222222222222222260",
            "00666666666666666600",
        };

        // ================= Glyphes 16x16 des boutons (blanc = 4) =================
        public static readonly string[] BackGlyph =
        {
            "0000000000000000", "0000000000000000", "0000000440000000", "0000004440000000",
            "0000044400000000", "0000444000000000", "0004444444444440", "0044444444444440",
            "0044444444444440", "0004444444444440", "0000444000000000", "0000044400000000",
            "0000004440000000", "0000000440000000", "0000000000000000", "0000000000000000",
        };

        public static readonly string[] UpGlyph =
        {
            "0000000000000000", "0000000440000000", "0000004444000000", "0000044444400000",
            "0000444444440000", "0004440440444000", "0044400440044400", "0000000440000000",
            "0000000440000000", "0000000440000000", "0000000440000000", "0000000440000000",
            "0000000440000000", "0000000440000000", "0000000440000000", "0000000000000000",
        };

        public static readonly string[] RefreshGlyph =
        {
            "0000000000000000", "0000044444000000", "0004400000440400", "0040000000044400",
            "0400000000444400", "0400000000000000", "4000000000000000", "4000000000000000",
            "4000000000000004", "4000000000000004", "0000000000000040", "0044440000000040",
            "0044400000000400", "0040440000044000", "0000004444400000", "0000000000000000",
        };

        public static readonly string[] PlusGlyph =
        {
            "0000000000000000", "0000000000000000", "0000000440000000", "0000000440000000",
            "0000000440000000", "0000000440000000", "0000000440000000", "0044444444444400",
            "0044444444444400", "0000000440000000", "0000000440000000", "0000000440000000",
            "0000000440000000", "0000000440000000", "0000000000000000", "0000000000000000",
        };

        public static readonly string[] NewFolderGlyph =
        {
            "0000000000000000", "0000000000000000", "0444440000000000", "4000004444444440",
            "4444444444444444", "4000000000000004", "4000000440000004", "4000000440000004",
            "4000044444400004", "4000044444400004", "4000000440000004", "4000000440000004",
            "4000000000000004", "4444444444444444", "0000000000000000", "0000000000000000",
        };

        public static readonly string[] TrashGlyph =
        {
            "0000000000000000", "0000044444400000", "0444444444444440", "0000000000000000",
            "0044444444444400", "0044044044044400", "0044044044044400", "0044044044044400",
            "0044044044044400", "0044044044044400", "0044044044044400", "0044044044044400",
            "0044044044044400", "0044444444444400", "0004444444444000", "0000000000000000",
        };

        public static readonly string[] PlayGlyph =
        {
            "0000000000000000", "0000000000000000", "0004400000000000", "0004444000000000",
            "0004444440000000", "0004444444400000", "0004444444444000", "0004444444444400",
            "0004444444444400", "0004444444444000", "0004444444400000", "0004444440000000",
            "0004444000000000", "0004400000000000", "0000000000000000", "0000000000000000",
        };

        public static readonly string[] SaveGlyph =
        {
            "0000000000000000", "0444444444444000", "0440000000044400", "0440000000044440",
            "0440000000044440", "0444444444444440", "0444444444444440", "0444000000004440",
            "0444000000004440", "0444000000004440", "0444000000004440", "0444000000004440",
            "0444000000004440", "0444444444444440", "0000000000000000", "0000000000000000",
        };

        public static readonly string[] BracesGlyph =
        {
            "0000000000000000", "0000440000440000", "0004000000004000", "0004000000004000",
            "0004000000004000", "0004000000004000", "0004000000004000", "0040000000000400",
            "0040000000000400", "0004000000004000", "0004000000004000", "0004000000004000",
            "0004000000004000", "0004000000004000", "0000440000440000", "0000000000000000",
        };

        public static readonly string[] PowerGlyph =
        {
            "0000000440000000", "0000000440000000", "0004400440044000", "0044000440004400",
            "0440000440000440", "0440000440000440", "4400000000000044", "4400000000000044",
            "4400000000000044", "4400000000000044", "0440000000000440", "0440000000000440",
            "0044000000004400", "0004440000444000", "0000044444400000", "0000000000000000",
        };

        // ================= Icones 16x16 : images, a propos, affichage =================
        public static readonly string[] ImageIcon =
        {
            "AAAAAAAAAAAAAAAA", "A22222222222222A", "A22222222227722A", "A22222222277772A",
            "A22222222227722A", "A22222222222222A", "A22222C22222222A", "A2222CCC2222222A",
            "A222CCCCC222222A", "A22CCCCCCC2C222A", "A2CCCCCCCCCCC22A", "ACCCCCCCCCCCCCCA",
            "ACCCCCCCCCCCCCCA", "ACCCCCCCCCCCCCCA", "ACCCCCCCCCCCCCCA", "AAAAAAAAAAAAAAAA",
        };

        public static readonly string[] AboutIcon =
        {
            "0000011111100000", "0001111111111000", "0011111441111100", "0111111441111110",
            "0111111111111110", "1111114441111111", "1111111441111111", "1111111441111111",
            "1111111441111111", "1111111441111111", "0111111441111110", "0111114444111110",
            "0011111111111100", "0001111111111000", "0000011111100000", "0000000000000000",
        };

        public static readonly string[] DisplayIcon =
        {
            "0000000000000000", "0AAAAAAAAAAAAAA0", "0A222222222222A0", "0A222222222222A0",
            "0A222222222222A0", "0A222222222222A0", "0A222222222222A0", "0A222222222222A0",
            "0A222222222222A0", "0A222222222222A0", "0AAAAAAAAAAAAAA0", "000000AAAA000000",
            "000000AAAA000000", "0000AAAAAAAA0000", "0000000000000000", "0000000000000000",
        };

        // ================= Icones 16x16 : navigateur, comptes, verrou =================
        public static readonly string[] BrowserIcon =
        {
            "0000011111100000", "0001122112211000", "0012221221222100", "0122221221222210",
            "0122212222122210", "1111111111111111", "1222212222122221", "1222212222122221",
            "1222212222122221", "1222212222122221", "1111111111111111", "0122212222122210",
            "0122221221222210", "0012221221222100", "0001122112211000", "0000011111100000",
        };

        public static readonly string[] AccountIcon =
        {
            "0000000000000000", "0000011111100000", "0000133333310000", "0001333333331000",
            "0001333333331000", "0001333333331000", "0000133333310000", "0000011111100000",
            "0000000000000000", "0001111111111000", "0013333333333100", "0133333333333310",
            "0133333333333310", "0133333333333310", "0111111111111110", "0000000000000000",
        };

        public static readonly string[] LockIcon =
        {
            "0000000000000000", "00000AAAAAA00000", "0000AA0000AA0000", "000AA000000AA000",
            "000AA000000AA000", "000AA000000AA000", "0777777777777770", "0777777777777770",
            "0777777887777770", "0777777887777770", "0777777887777770", "0777777777777770",
            "0777777777777770", "0777777777777770", "0888888888888880", "0000000000000000",
        };

        // ================= Icones 16x16 : PDF et TextWar =================
        public static readonly string[] PdfIcon =
        {
            "00AAAAAAAAA00000", "00A9999999AA0000", "00A9999999A9A000", "00A9999999AAAA00",
            "00A9999999999A00", "FFFFFFFFFFFF9A00", "FFFFFFFFFFFF9A00", "F4F4F4F4F4FF9A00",
            "FFFFFFFFFFFF9A00", "FFFFFFFFFFFF9A00", "00A9999999999A00", "00A9DDDDDDD99A00",
            "00A9999999999A00", "00A9DDDDD9999A00", "00AAAAAAAAAAAA00", "0000000000000000",
        };

        public static readonly string[] TextWarIcon =
        {
            "0FFFFFFFFFFFFFF0", "FFFFFFFFFFFFFFFF", "FF444444444444FF", "FF444444444444FF",
            "FFFFFF4444FFFFFF", "FFFFFF4444FFFFFF", "FFFFFF4444FFFFFF", "FFFFFF4444FFFFFF",
            "FFFFFF4444FFFFFF", "FFFFFF4444FFFFFF", "FFF44F4444F44FFF", "FFFF44444444FFFF",
            "FFFFF444444FFFFF", "FFFFFF4444FFFFFF", "FFFFFFFFFFFFFFFF", "0FFFFFFFFFFFFFF0",
        };
    }

    /// <summary>Bitmaps des glyphes construites une seule fois.</summary>
    public static class Glyphs
    {
        public static readonly HexBitmap Close = new HexBitmap(Icons.CloseGlyph, Icons.Palette);
        public static readonly HexBitmap Minimize = new HexBitmap(Icons.MinimizeGlyph, Icons.Palette);
        public static readonly HexBitmap Wait = new HexBitmap(Icons.WaitGlyph, Icons.Palette);
        public static readonly HexBitmap Check = new HexBitmap(Icons.CheckGlyph, Icons.Palette);

        public static readonly HexBitmap Back = new HexBitmap(Icons.BackGlyph, Icons.Palette);
        public static readonly HexBitmap Up = new HexBitmap(Icons.UpGlyph, Icons.Palette);
        public static readonly HexBitmap Refresh = new HexBitmap(Icons.RefreshGlyph, Icons.Palette);
        public static readonly HexBitmap Plus = new HexBitmap(Icons.PlusGlyph, Icons.Palette);
        public static readonly HexBitmap NewFolder = new HexBitmap(Icons.NewFolderGlyph, Icons.Palette);
        public static readonly HexBitmap Trash = new HexBitmap(Icons.TrashGlyph, Icons.Palette);
        public static readonly HexBitmap Play = new HexBitmap(Icons.PlayGlyph, Icons.Palette);
        public static readonly HexBitmap Save = new HexBitmap(Icons.SaveGlyph, Icons.Palette);
        public static readonly HexBitmap Braces = new HexBitmap(Icons.BracesGlyph, Icons.Palette);
        public static readonly HexBitmap Power = new HexBitmap(Icons.PowerGlyph, Icons.Palette);
    }
}
