using System.Collections.Generic;
using System.Drawing;
using Cosmos.System.Graphics.Fonts;
using FileSystemOS.Memoire;

namespace FileSystemOS.Graphique
{
    /// <summary>
    /// Buffer de pixels ARGB 32 bits en memoire.
    /// Chaque fenetre, la barre des taches, le menu, le bureau et l'ecran ont le leur.
    /// Tous les buffers sont enregistres dans Registry (commande 'buffers').
    /// </summary>
    public unsafe class PixelBuffer
    {
        public static readonly List<PixelBuffer> Registry = new List<PixelBuffer>();

        public readonly string Name;
        public readonly int W, H;
        public readonly int[] Data;

        public PixelBuffer(string name, int w, int h) : this(name, w, h, new int[w * h]) { }

        public PixelBuffer(string name, int w, int h, int[] data)
        {
            Name = name; W = w; H = h; Data = data;
            Registry.Add(this);
        }

        /// <summary>Retire le buffer de la liste (quand il est remplace).</summary>
        public void Release() => Registry.Remove(this);

        public uint Address { get { fixed (int* p = Data) { return (uint)p; } } }
        public uint Bytes => (uint)(W * H * 4);

        public static int Argb(Color c) =>
            (int)(0xFF000000u | ((uint)c.R << 16) | ((uint)c.G << 8) | c.B);

        // ---- Primitives ----
        public void Clear(Color c) => MemoryAccelerator.Fill32(Data, 0, Data.Length, Argb(c));

        public void SetPixel(int x, int y, int argb)
        {
            if ((uint)x < (uint)W && (uint)y < (uint)H) Data[y * W + x] = argb;
        }

        public void FillRect(Color c, int x, int y, int w, int h)
        {
            int x0 = x < 0 ? 0 : x, y0 = y < 0 ? 0 : y;
            int x1 = x + w > W ? W : x + w, y1 = y + h > H ? H : y + h;
            if (x0 >= x1 || y0 >= y1) return;
            int v = Argb(c);
            for (int yy = y0; yy < y1; yy++)
                MemoryAccelerator.Fill32(Data, yy * W + x0, x1 - x0, v);
        }

        public void DrawRect(Color c, int x, int y, int w, int h)
        {
            FillRect(c, x, y, w, 1);
            FillRect(c, x, y + h - 1, w, 1);
            FillRect(c, x, y, 1, h);
            FillRect(c, x + w - 1, y, 1, h);
        }

        public void FillCircle(Color c, int cx, int cy, int r)
        {
            int r2 = r * r;
            for (int dy = -r; dy <= r; dy++)
            {
                int dx = 0;
                while ((dx + 1) * (dx + 1) + dy * dy <= r2) dx++;
                FillRect(c, cx - dx, cy + dy, dx * 2 + 1, 1);
            }
        }

        /// <summary>Texte dessine pixel par pixel a partir des glyphes de la police PSF.</summary>
        public void DrawText(string s, Color c, int x, int y)
        {
            Font f = Services.Font;
            int fw = f.Width, fh = f.Height;
            int v = Argb(c);

            // Chemin rapide : pixels de chaque caractere precalcules au demarrage
            if (FontCache.Ready)
            {
                for (int i = 0; i < s.Length; i++)
                {
                    int ch = s[i] > 255 ? '?' : s[i];
                    int gx = x + i * fw;
                    if (gx >= W) break;
                    if (gx + fw <= 0) continue;
                    short[] g = FontCache.Pixels[ch];
                    bool inside = gx >= 0 && gx + fw <= W && y >= 0 && y + fh <= H;
                    for (int k = 0; k < g.Length; k++)
                    {
                        int px = gx + (g[k] & 0xFF), py = y + (g[k] >> 8);
                        if (inside) Data[py * W + px] = v;
                        else SetPixel(px, py, v);
                    }
                }
                return;
            }

            byte[] glyphs = f.Data;
            int bpr = (fw + 7) / 8; // octets par ligne de glyphe

            for (int i = 0; i < s.Length; i++)
            {
                int ch = s[i];
                if (ch > 255) ch = '?';
                int g = ch * fh * bpr;
                int gx = x + i * fw;
                if (gx >= W) break;

                for (int row = 0; row < fh; row++)
                {
                    int py = y + row;
                    if ((uint)py >= (uint)H) continue;
                    for (int col = 0; col < fw; col++)
                    {
                        byte b = glyphs[g + row * bpr + col / 8];
                        if ((b & (0x80 >> (col & 7))) != 0) SetPixel(gx + col, py, v);
                    }
                }
            }
        }

        /// <summary>Dessine une bitmap ARGB (alpha 0 = transparent).</summary>
        public void DrawImage(uint[] px, int bw, int bh, int x, int y)
        {
            for (int j = 0; j < bh; j++)
                for (int i = 0; i < bw; i++)
                {
                    uint p = px[j * bw + i];
                    if ((p >> 24) != 0) SetPixel(x + i, y + j, (int)p);
                }
        }

        // ---- Effets modernes ----

        /// <summary>Retrait horizontal de la ligne i d'un coin arrondi de rayon r.</summary>
        public static int CornerInset(int r, int i)
        {
            int d = r - i, dx = 0;
            while ((dx + 1) * (dx + 1) + d * d <= r * r) dx++;
            return r - dx;
        }

        /// <summary>Rectangle plein aux coins arrondis.</summary>
        public void FillRoundRect(Color c, int x, int y, int w, int h, int r)
        {
            if (r * 2 > h) r = h / 2;
            for (int i = 0; i < h; i++)
            {
                int k = i < r ? i : (i >= h - r ? h - 1 - i : r);
                int inset = k < r ? CornerInset(r, k) : 0;
                FillRect(c, x + inset, y + i, w - inset * 2, 1);
            }
        }

        /// <summary>Assombrit une zone (ombre douce sans transparence) : -25 % par passage.</summary>
        public void Darken(int x, int y, int w, int h)
        {
            int x0 = x < 0 ? 0 : x, y0 = y < 0 ? 0 : y;
            int x1 = x + w > W ? W : x + w, y1 = y + h > H ? H : y + h;
            for (int yy = y0; yy < y1; yy++)
            {
                int row = yy * W;
                for (int xx = x0; xx < x1; xx++)
                {
                    uint v = (uint)Data[row + xx];
                    v = v - ((v >> 2) & 0x003F3F3Fu);
                    Data[row + xx] = (int)(v | 0xFF000000u);
                }
            }
        }

        /// <summary>Comme Blit, mais avec les coins arrondis (le fond reste visible aux coins).</summary>
        public void BlitRounded(PixelBuffer src, int dx, int dy, int r)
        {
            for (int row = 0; row < src.H; row++)
            {
                int ty = dy + row;
                if (ty < 0 || ty >= H) continue;
                int k = row < r ? row : (row >= src.H - r ? src.H - 1 - row : r);
                int inset = k < r ? CornerInset(r, k) : 0;

                int sx = inset, tx = dx + inset, w = src.W - inset * 2;
                if (tx < 0) { sx -= tx; w += tx; tx = 0; }
                if (tx + w > W) w = W - tx;
                if (w <= 0) continue;
                MemoryAccelerator.Copy32(Data, ty * W + tx, src.Data, row * src.W + sx, w);
            }
        }

        /// <summary>Copie une zone rectangulaire d'un buffer de meme taille (restauration sous le curseur).</summary>
        public void CopyRegion(PixelBuffer src, int x, int y, int w, int h)
        {
            int x0 = x < 0 ? 0 : x, y0 = y < 0 ? 0 : y;
            int x1 = x + w > W ? W : x + w, y1 = y + h > H ? H : y + h;
            if (x0 >= x1) return;
            for (int yy = y0; yy < y1; yy++)
                MemoryAccelerator.Copy32(Data, yy * W + x0, src.Data, yy * src.W + x0, x1 - x0);
        }

        /// <summary>
        /// Copie redimensionnee (plus proche voisin) de la zone source (sx, sy, sw, sh)
        /// vers le rectangle (dx, dy, dw, dh). Sert aux images et au fond d'ecran.
        /// </summary>
        public void BlitScaled(PixelBuffer src, int sx, int sy, int sw, int sh, int dx, int dy, int dw, int dh)
        {
            if (dw <= 0 || dh <= 0 || sw <= 0 || sh <= 0) return;
            for (int y = 0; y < dh; y++)
            {
                int ty = dy + y;
                if (ty < 0 || ty >= H) continue;
                int syy = sy + y * sh / dh;
                int srow = syy * src.W;
                int drow = ty * W;
                for (int x = 0; x < dw; x++)
                {
                    int tx = dx + x;
                    if (tx < 0 || tx >= W) continue;
                    Data[drow + tx] = src.Data[srow + sx + x * sw / dw];
                }
            }
        }

        /// <summary>Image entiere, agrandie/reduite pour TENIR dans le rectangle (sans deformation).</summary>
        public void DrawFit(PixelBuffer img, int x, int y, int w, int h)
        {
            int dw = w, dh = img.H * w / img.W;
            if (dh > h) { dh = h; dw = img.W * h / img.H; }
            BlitScaled(img, 0, 0, img.W, img.H, x + (w - dw) / 2, y + (h - dh) / 2, dw, dh);
        }

        /// <summary>Image qui COUVRE tout le buffer (recadree au centre) : fond d'ecran.</summary>
        public void DrawCover(PixelBuffer img)
        {
            int sw = img.W, sh = img.W * H / W;
            if (sh > img.H) { sh = img.H; sw = img.H * W / H; }
            BlitScaled(img, (img.W - sw) / 2, (img.H - sh) / 2, sw, sh, 0, 0, W, H);
        }

        /// <summary>Copie un autre buffer dedans, ligne par ligne, avec decoupage aux bords.</summary>
        public void Blit(PixelBuffer src, int dx, int dy)
        {
            int sx0 = 0, sy0 = 0, w = src.W, h = src.H;
            if (dx < 0) { sx0 = -dx; w += dx; dx = 0; }
            if (dy < 0) { sy0 = -dy; h += dy; dy = 0; }
            if (dx + w > W) w = W - dx;
            if (dy + h > H) h = H - dy;
            if (w <= 0 || h <= 0) return;

            for (int row = 0; row < h; row++)
                MemoryAccelerator.Copy32(Data, (dy + row) * W + dx,
                                         src.Data, (sy0 + row) * src.W + sx0, w);
        }
    }
}
