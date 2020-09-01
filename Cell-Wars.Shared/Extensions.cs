using System;
using System.Collections.Generic;
using System.Text;
using Ultraviolet;

namespace ultraviolettesting
{
    static class Extensions
    {
        public static Cell Get2DValueOrNull(this Cell[][] array, int indexX, int indexY)
        {
            if (indexX < 0 || indexY < 0) return null;
            return indexX < array.Length && indexY < array[indexX].Length ? array[indexX][indexY] : null;
        }

        public static Cell GetFirstViableMate(this Cell[] neighbors, Cell suitor, double maxHueDistance)
        {
            //FirstOrDefault(n => n?.isAsexual == false && n?.isAlive == true);
            //might be faster with some pointer arithmetic instead?
            for(int i = 0; i < neighbors.Length; i++)
            {
                ref Cell neighbor = ref neighbors[i];
                if (neighbor == null) continue;
                if (neighbor.IsAlive 
                    && FloatDistance(suitor.Hue, neighbor.Hue) <= maxHueDistance)
                {
                    return neighbor;
                }
            }
            return null;
        }

        public static bool CheckIfNoUnwantedNeighbors(this Cell[] neighbors, Cell wantsToKnow, double maxHueDistance)
        {
            for (int i = 0; i < neighbors.Length; i++)
            {
                ref Cell neighbor = ref neighbors[i];
                if (neighbor == null) continue;
                if (neighbor.IsAlive && FloatDistance(wantsToKnow.Hue, neighbor.Hue) > maxHueDistance)
                {
                    return false;
                }
            }
            return true;
        }

        public static double[] GetMaxes(this Cell[][] cells)
        {
            double[] maxes = new double[] { 0, 0, 0, 0 };
            for(int x = 0; x < cells.Length; x++)
            {
                for(int y = 0; y < cells[x].Length; y++)
                {
                    if (!cells[x][y].IsAlive) continue;
                    if (cells[x][y].MaxLifeTime > maxes[0]) maxes[0]    = cells[x][y].MaxLifeTime;
                    if (cells[x][y].MaxHueDistance > maxes[1]) maxes[1] = cells[x][y].MaxHueDistance;
                    if (cells[x][y].BreedChance > maxes[2]) maxes[2]    = cells[x][y].BreedChance;
                    if (cells[x][y].CurrentLifeTime > maxes[3]) maxes[3]= cells[x][y].CurrentLifeTime;
                }
            }
            return maxes;
        }

        public static double[] GetAverages(this Cell[][] cells)
        {
            double[] totals = new double[] { 0, 0, 0, 0 };
            int counter = 0;
            for (int x = 0; x < cells.Length; x++)
            {
                for (int y = 0; y < cells[x].Length; y++)
                {
                    if (!cells[x][y].IsAlive) continue;
                    totals[0] += cells[x][y].MaxLifeTime;
                    totals[1] += cells[x][y].MaxHueDistance;
                    totals[2] += cells[x][y].BreedChance;
                    totals[3] += cells[x][y].CurrentLifeTime;
                    counter++;
                }
            }

            for(int i = 0; i < totals.Length; i++)
            {
                totals[i] /= counter;
            }
            return totals;
        }

        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value *= 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb((uint)((((((255 << 8) + v) << 8) + t) << 8) + p));
            else if (hi == 1)
                return Color.FromArgb((uint)((((((255 << 8) + q) << 8) + v) << 8) + p));
            else if (hi == 2)
                return Color.FromArgb((uint)((((((255 << 8) + p) << 8) + v) << 8) + t));
            else if (hi == 3)
                return Color.FromArgb((uint)((((((255 << 8) + p) << 8) + q) << 8) + v));
            else if (hi == 4)
                return Color.FromArgb((uint)((((((255 << 8) + t) << 8) + p) << 8) + v));
            else
                return Color.FromArgb((uint)((((((255 << 8) + v) << 8) + p) << 8) + q));
        }

        public static double HueFromColor(Color color)
        {
            System.Drawing.Color tempSys = System.Drawing.Color.FromArgb(color.R, color.G, color.B);
            return tempSys.GetHue();
        }

        public static double FloatDistance(double f1, double f2)
        {
            return Math.Abs(f1 - f2);
        }

        public static double GetRandomSignedDouble(this Random rng, double modifier = 1.0)
        {
            return (rng.NextDouble() - .5) * modifier;
        }
    }
}
