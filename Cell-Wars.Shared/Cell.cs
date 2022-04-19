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
                int hueInt = (int)value;
                double fractional = value - hueInt;
                if (hueInt > 360)
                {
                    hueInt = hueInt % 360;
                }
                else if (hueInt < 0)
                {
                    hueInt = hueInt % 360;
                    hueInt = 360 + hueInt;
                }
                double hue = hueInt + fractional;
                color = Extensions.ColorFromHSV(hue, 1.0f, 1.0f);
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
        private double _maxLifeTime;
        private double _currentLifeTime;
        private double _breedChance;
        private double _maxHueDistance;

        public double MaxLifeTime
        {
            get
            {
                return Math.Clamp(_maxLifeTime, 1.5, 10.0);
            }
            private set
            {
                _maxLifeTime = value;
            }
        }
        public double CurrentLifeTime
        {
            get
            {
                return _currentLifeTime;
            }
            private set
            {
                _currentLifeTime = value;
            }
        }
        public double BreedChance
        {
            get
            {
                return Math.Clamp(_breedChance, 0.05, 0.35);
            }
            private set { _breedChance = Math.Clamp(value, 0.05, 0.35); }
        }
        public double MaxHueDistance
        {
            get
            {
                return _maxHueDistance;
            }
            private set
            {
                _maxHueDistance = value;
            }
        }

        private double _damagePercent;

        public double DamagePercent
        {
            get { return Math.Clamp(_damagePercent, 0.15, 0.90); }
            set { _damagePercent = Math.Clamp(value, 0.15, 0.90); }
        }

        public bool Hostile { get; private set; }

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
            Initialize();
        }

        private void Initialize()
        {
            hasCorpse = false;
            IsAlive = false;
            Hostile = false;
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
                    if (Hostile)
                    {
                        AttackNearestEnemy();
                    }
                    if (CurrentLifeTime >= (MaxLifeTime * .25) && rng.NextDouble() <= BreedChance)
                    {
                        Breed();
                    }
                    else if (CurrentLifeTime >= (MaxLifeTime * .65))
                    {
                        var deadNeighbors = GetDeadNeighbors();
                        if (deadNeighbors.Count > 0)
                        {
                            Eat(deadNeighbors[0]);
                        }
                    }
                }
            }


            //random color cycling, cool but slow and not really useful for anything
            //color = ColorExtensions.ColorFromHSV(hue % 360f, 1f, 1f);
            //hue += (float)(colorSpeed * time.ElapsedTime.TotalSeconds);
        }

        private void AttackNearestEnemy()
        {
            List<Cell> shuffled = new List<Cell>(neighbors);
            shuffled.Shuffle();
            foreach (Cell neighbor in shuffled)
            {
                if (neighbor != null
                    && neighbor.IsAlive
                    && Extensions.FloatDistance(this.Hue, neighbor.Hue) > MaxHueDistance)
                {
                    neighbor.CurrentLifeTime += neighbor.MaxLifeTime * (DamagePercent);
                    return;
                }
            }
        }

        private List<Cell> GetDeadNeighbors()
        {
            List<Cell> deadNeighbors = new List<Cell>();
            foreach (Cell neighbor in neighbors)
            {
                if (neighbor != null && !neighbor.IsAlive)
                {
                    deadNeighbors.Add(neighbor);
                }
            }
            return deadNeighbors;
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
                foreach (Cell startingMate in neighbors)
                {
                    if (startingMate == null) continue;
                    startingMate.Clicked(false);
                    startingMate.Hue = Hue;
                }
            }
        }

        internal void Born(Cell mother, Cell father)
        {
            IsAlive = true;
            hasCorpse = false;
            Hostile = false;
            CurrentLifeTime = 0;

            double randomGeneticMultiplier = rng.GetRandomSignedDouble();
            if (mother != null && father != null)
            {
                //sexual reproduction

                double avgHue = (mother.Hue + father.Hue) / 2;
                Hue = avgHue + rng.GetRandomSignedDouble(2 + randomGeneticMultiplier);

                double avgMaxLifeTime = (mother.MaxLifeTime + father.MaxLifeTime) / 2;
                MaxLifeTime = avgMaxLifeTime + rng.GetRandomSignedDouble(randomGeneticMultiplier);

                double avgBreedChance = (mother.BreedChance + father.BreedChance) / 2;
                BreedChance = avgBreedChance + rng.GetRandomSignedDouble(randomGeneticMultiplier);

                double avgMaxHueDistance = (mother.MaxHueDistance + father.MaxHueDistance) / 2;
                MaxHueDistance = avgMaxHueDistance + rng.GetRandomSignedDouble(randomGeneticMultiplier);

                double avgDamagePercent = (mother.DamagePercent + father.DamagePercent) / 2;
                DamagePercent = avgDamagePercent + rng.GetRandomSignedDouble(randomGeneticMultiplier);
            }
            else if (mother != null && father == null)
            {
                //asexual reproduction

                Hue = mother.Hue * rng.GetRandomSignedDouble(randomGeneticMultiplier);

                MaxLifeTime = mother.MaxLifeTime + rng.GetRandomSignedDouble(randomGeneticMultiplier);

                BreedChance = mother.BreedChance + rng.GetRandomSignedDouble(randomGeneticMultiplier);

                MaxHueDistance = mother.MaxHueDistance + rng.GetRandomSignedDouble(randomGeneticMultiplier);

                DamagePercent = mother.DamagePercent + rng.GetRandomSignedDouble(randomGeneticMultiplier);

            }
            else
            {
                Hue = rng.NextDouble() * 360.0;
                MaxLifeTime = rng.NextDouble() * 5.0;
                BreedChance = rng.NextDouble() * .5;
                MaxHueDistance = rng.NextDouble() * 10;
                DamagePercent = rng.NextDouble() * .5;
            }
        }

        internal void Die()
        {
            IsAlive = false;
            hasCorpse = true;
            Hostile = false;
            mother = null;
            father = null;
            color = Extensions.ColorFromHSV(Hue, 1, .5);
        }

        private void Eat(Cell toEat)
        {
            if (toEat.hasCorpse)
            {
                CurrentLifeTime -= toEat.MaxLifeTime * .5;
                if (CurrentLifeTime < 0)
                {
                    CurrentLifeTime = 0;
                }
                this.Hue += Math.Clamp((Hue - toEat.Hue) * 0.1, -2, 2);
                toEat.Initialize();
            }
        }

        private void Breed()
        {
            var mate = neighbors.GetFirstViableMate(this, MaxHueDistance);

            int numOfChildrenAttempts = rng.Next(1, 4);
            for (int i = 0; i < numOfChildrenAttempts; i++)
            {
                var child = GetAndCheckChild();
                if (child != null)
                {
                    //eat the corpse and gain energy
                    if (child.hasCorpse)
                    {
                        Eat(child);
                    }

                    if (child.neighbors.CheckIfNoUnwantedNeighbors(this, MaxHueDistance))
                    {
                        child.Born(this, mate);
                    }
                    else
                    {
                        Hostile = true;
                    }
                }
            }
        }

        private Cell GetAndCheckChild()
        {
            //check for dying neighbors to replace
            Cell firstCorpse = null;
            for (int i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i] != null && neighbors[i].hasCorpse)
                {
                    firstCorpse = neighbors[i];
                }
            }
            if (firstCorpse != null)
            {
                firstCorpse.Die();
                return firstCorpse;
            }
            //if none, do normal
            else
            {
                var shuffled = new List<Cell>(neighbors);
                shuffled.Shuffle();
                var child = shuffled[rng.Next(0, shuffled.Count)];
                if (child != null && !child.IsAlive && !child.hasCorpse)
                {
                    return child;
                }
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
