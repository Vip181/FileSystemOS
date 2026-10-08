using Cosmos.Core.Memory;

namespace FileSystemOS.Memoire
{
    /// <summary>
    /// Accelerateur memoire fait maison :
    ///  1) Un pool de blocs de taille fixe reserve une seule fois au demarrage.
    ///     Allouer / liberer = depiler / empiler un index : O(1), sans toucher au tas.
    ///  2) Copie et remplissage rapides : 4 octets par 4 octets au lieu de 1 par 1.
    /// </summary>
    public unsafe class MemoryAccelerator
    {
        public const uint BlockSize = 64;
        public const uint BlockCount = 256;

        private readonly byte* pool;
        private readonly uint[] freeStack = new uint[BlockCount];
        private uint freeTop;

        public uint Hits;   // allocations servies par le pool
        public uint Misses; // allocations trop grosses -> tas classique

        public uint PoolAddress => (uint)pool;
        public uint FreeBlocks => freeTop;

        public MemoryAccelerator()
        {
            pool = Heap.Alloc(BlockSize * BlockCount);
            Fill(pool, 0, BlockSize * BlockCount);

            for (uint i = 0; i < BlockCount; i++)
                freeStack[i] = BlockCount - 1 - i;
            freeTop = BlockCount;
        }

        public byte* Alloc(uint size)
        {
            if (size <= BlockSize && freeTop > 0)
            {
                Hits++;
                uint index = freeStack[--freeTop];
                return pool + index * BlockSize;
            }
            Misses++;
            return Heap.Alloc(size);
        }

        public void Free(byte* p)
        {
            if (p >= pool && p < pool + BlockSize * BlockCount)
            {
                uint index = (uint)(p - pool) / BlockSize;
                freeStack[freeTop++] = index;
            }
            else
            {
                Heap.Free(p);
            }
        }

        /// <summary>Copie rapide : par mots de 32 bits, puis le reste octet par octet.</summary>
        public static void Copy(byte* dst, byte* src, uint count)
        {
            uint words = count / 4;
            uint* d = (uint*)dst;
            uint* s = (uint*)src;
            for (uint i = 0; i < words; i++) d[i] = s[i];
            for (uint i = words * 4; i < count; i++) dst[i] = src[i];
        }

        /// <summary>Remplissage rapide : la valeur est repetee sur 32 bits.</summary>
        public static void Fill(byte* dst, byte value, uint count)
        {
            uint v = (uint)(value | (value << 8) | (value << 16) | (value << 24));
            uint words = count / 4;
            uint* d = (uint*)dst;
            for (uint i = 0; i < words; i++) d[i] = v;
            for (uint i = words * 4; i < count; i++) dst[i] = value;
        }

        // ---- Versions 32 bits pour les buffers de pixels (int[]) ----

        /// <summary>Copie de pixels deroulee par 4 (moins de tours de boucle).</summary>
        public static void Copy32(int[] dst, int dstIndex, int[] src, int srcIndex, int count)
        {
            fixed (int* d0 = dst)
            fixed (int* s0 = src)
            {
                int* d = d0 + dstIndex;
                int* s = s0 + srcIndex;
                int i = 0;
                for (; i + 4 <= count; i += 4)
                {
                    d[i] = s[i]; d[i + 1] = s[i + 1]; d[i + 2] = s[i + 2]; d[i + 3] = s[i + 3];
                }
                for (; i < count; i++) d[i] = s[i];
            }
        }

        public static void Fill32(int[] dst, int index, int count, int value)
        {
            fixed (int* d0 = dst)
            {
                int* d = d0 + index;
                int i = 0;
                for (; i + 4 <= count; i += 4)
                {
                    d[i] = value; d[i + 1] = value; d[i + 2] = value; d[i + 3] = value;
                }
                for (; i < count; i++) d[i] = value;
            }
        }
    }
}
