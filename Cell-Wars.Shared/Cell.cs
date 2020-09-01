using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ultraviolet;
using Ultraviolet.Graphics;
using Ultraviolet.Graphics.Graphics2D;
using Ultraviolet.Platform;


namespace ultraviolettesting
{
    class Cell
    {
        public static int cellSize { get; set; }

        private static Texture2D texture;
        private static IUltravioletWindow window;
        private static Random rng;
        private static Cell[][] allCells;

        public double Hue
        {
            get
            {
                return Extensions.HueFromColor(color);
            }
            set
            {
                color = Extensions.ColorFromHSV(value, 1.0f, 1.0f);
            }
        }

        private Vector2 position;
        private Cell[] neighbors;
        private Color color;

        //life stuff
        public bool IsAlive { get; private set; } = false;
        private bool hasCorpse = false;
        private Cell mother, father;
        //life stats
        public double MaxLifeTime { get; private set; }
        public double CurrentLifeTime { get; private set; }
        public double BreedChance { get; private set; }
        public double MaxHueDistance { get; private set; }

        public Cell(Texture2D _texture, IUltravioletWindow _window, Vector2 _position, ref Cell[][] _allCells)
        {
            if (rng == null)
                rng = new Random();
            if (texture == null)
                texture = _texture;
            if (window == null)
                window = _window;
            if (allCells == null)
                allCells = _allCells;

            neighbors = new Cell[8];
            position = _position;

            color = Color.White;
        }

        internal void Draw(UltravioletTime time, ref SpriteBatch spriteBatch)
        {
            if (position.X > window.DrawableSize.Width || position.Y > window.DrawableSize.Height || position.X * cellSize < 0 || position.Y * cellSize < 0)
            {
                return;
            }

            spriteBatch.Draw(texture, position * cellSize, color);
        }

        internal void Update(UltravioletTime time)
        {
            if (position.X > window.DrawableSize.Width || position.Y > window.DrawableSize.Height || position.X * cellSize < 0 || position.Y * cellSize < 0)
            {
                return;
            }

            if (IsAlive)
            {
                if (CurrentLifeTime > MaxLifeTime)
                {
                    Die();
                    return;
                }
                else
                {
                    CurrentLifeTime += time.ElapsedTime.TotalSeconds;
                    if (CurrentLifeTime >= (MaxLifeTime * .75) && rng.NextDouble() <= BreedChance)
                    {
                        Breed();
                    }
                }
            }
            else if (hasCorpse)
            {
                CurrentLifeTime -= time.ElapsedTime.TotalSeconds;
                if (CurrentLifeTime <= MaxLifeTime * .75)
                {
                    hasCorpse = false;
                    CurrentLifeTime = 0f;
                    color = Color.White;
                }
            }


            //random color cycling, cool but slow and not really useful for anything
            //color = ColorExtensions.ColorFromHSV(hue % 360f, 1f, 1f);
            //hue += (float)(colorSpeed * time.ElapsedTime.TotalSeconds);
        }

        internal void GetNeighbors()
        {
            int i = (int)position.X;
            int j = (int)position.Y;

            neighbors[0] = allCells.Get2DValueOrNull(i - 1, j - 1);
            neighbors[1] = allCells.Get2DValueOrNull(i - 1, j);
            neighbors[2] = allCells.Get2DValueOrNull(i - 1, j + 1);

            neighbors[3] = allCells.Get2DValueOrNull(i, j - 1);
            neighbors[4] = allCells.Get2DValueOrNull(i, j + 1);

            neighbors[5] = allCells.Get2DValueOrNull(i + 1, j - 1);
            neighbors[6] = allCells.Get2DValueOrNull(i + 1, j);
            neighbors[7] = allCells.Get2DValueOrNull(i + 1, j + 1);
        }

        internal void Clicked(bool spawnNeighbors = false)
        {
            Born(null, null);
            if (spawnNeighbors)
            {
                foreach(Cell startingMate in neighbors)
                {
                    if (startingMate == null) continue;
                    startingMate.Clicked(false);
                    startingMate.Hue = Hue;
                }
            }
        }

        internal void Born(Cell parentA, Cell parentB)
        {
            IsAlive = true;
            CurrentLifeTime = 0;

            if (parentA != null && parentB != null)
            {
                mother = parentA;
                father = parentB;

                double randomGeneticMultiplier = rng.GetRandomSignedDouble();

                double avgHue = (mother.Hue + father.Hue) / 2;
                Hue = avgHue + rng.GetRandomSignedDouble(2 + randomGeneticMultiplier);

                double avgMaxLifeTime = (mother.MaxLifeTime + father.MaxLifeTime) / 2;
                MaxLifeTime = avgMaxLifeTime + rng.GetRandomSignedDouble(randomGeneticMultiplier);

                double avgBreedChance = (mother.BreedChance + father.BreedChance) / 2;
                BreedChance = avgBreedChance + rng.GetRandomSignedDouble(randomGeneticMultiplier);

                double avgMaxHueDistance = (mother.MaxHueDistance + father.MaxHueDistance) / 2;
                MaxHueDistance = avgMaxHueDistance + rng.GetRandomSignedDouble(randomGeneticMultiplier);
            }
            else
            {
                Hue = rng.NextDouble() * 360.0;
                MaxLifeTime = rng.NextDouble() * 5.0;
                BreedChance = rng.NextDouble() * .5;
                MaxHueDistance = rng.NextDouble() * 10;
            }
        }

        internal void Die()
        {
            IsAlive = false;
            hasCorpse = true;
            mother = null;
            father = null;
            color = Extensions.ColorFromHSV(Hue, 1, .5);
        }

        private void Breed()
        {
            var mate = neighbors.GetFirstViableMate(this, MaxHueDistance);
            if (mate != null)
            {
                int numOfChildrenAttempts = rng.Next(1, 9);
                for (int i = 0; i < numOfChildrenAttempts; i++)
                {
                    var child = GetAndCheckChild();
                    if (child != null)
                    {
                        if (child.neighbors.CheckIfNoUnwantedNeighbors(this, MaxHueDistance))
                        {
                            child.Born(this, mate);
                        }
                    }
                }
            }
        }

        private Cell GetAndCheckChild()
        {
            var child = neighbors[rng.Next(0, neighbors.Length)];
            if (child != null && !child.IsAlive && !child.hasCorpse)
            {
                return child;
            }
            return null;
        }

        public override string ToString()
        {
            return String.Format("Cur Lifespan: {0:0.000}    Cur Hue Distance: {1:0.000}    Cur Breed Chance: {2:0.000}    Cur cell life: {3:0.000}    Cur Hue: {4:0.000}\n",
                                   MaxLifeTime, MaxHueDistance, BreedChance, CurrentLifeTime, Hue);
        }
    }
}
