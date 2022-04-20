using System;
using System.Collections.Generic;
using System.Text;
using Ultraviolet;
using System.Linq;
using System.Threading;
using CellWars.BreedableTypes;
using KGySoft.CoreLibraries;

namespace CellWars
{
    static class Extensions
    {
        public static Cell Get2DValueOrNull(this Cell[,] array, int indexX, int indexY)
        {
            if (indexX < 0 || indexY < 0) return null;
            return indexX < array.GetLength(0) && indexY < array.GetLength(1) ? array[indexX, indexY] : null;
        }

        public static Cell GetFirstViableMate(this Cell[] neighbors, Cell suitor, double maxHueDistance)
        {
            //FirstOrDefault(n => n?.isAsexual == false && n?.isAlive == true);
            //might be faster with some pointer arithmetic instead?
            for (int i = 0; i < neighbors.Length; i++)
            {
                Cell neighbor = neighbors[i];
                if (neighbor == null) continue;
                if (neighbor.IsAlive 
                    && CellColor.Distance(suitor.CellColor, neighbor.CellColor) <= maxHueDistance)
                {
                    return neighbor;
                }
            }
            return null;
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.Next(n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }
        }

        

    }

    internal class ThreadSafeRandom
    {
        private static readonly Random _global = new Random();
        private static readonly ThreadLocal<Random> _local = new ThreadLocal<Random>(() =>
        {
            int seed;
            lock (_global)
            {
                seed = _global.Next();
            }
            return new Random(seed);
        });

        public static int Next(int maxValue)
        {
            return _local.Value.Next(maxValue);
        }
    }
}
