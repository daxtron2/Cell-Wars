using CellWars.BreedableTypes;
using KGySoft.CoreLibraries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ultraviolet;
using Ultraviolet.Graphics;
using Ultraviolet.Graphics.Graphics2D;
using Ultraviolet.Platform;


namespace CellWars
{
    class Cell
    {
        public static int cellSize { get; set; }

        private static Texture2D texture, selectTexture;
        //private static IUltravioletWindow window;
        private static Random rng;
        private static CellManager manager;



        private Vector2 position;
        private Cell[] neighbors;

        //nonbreedable stuff
        public bool IsAlive { get; private set; } = false;
        private bool hasCorpse = false;
        private Cell mother, father;

        private Guid m_primogenitorID;
        public Guid PrimogenitorID
        {
            get { return m_primogenitorID; }
            private set { m_primogenitorID = value; }
        }

        private double m_health;
        public double Health
        {
            get { return m_health; }
            private set { m_health = value; }
        }
        public double TimeAlive { get; private set; } = 0;

        public bool Selected { get; set; } = false;
        public bool Hostile { get; private set; }


        //breedable stats private
        private double m_maxHealth;
        private double m_breedChance;
        private double m_maxHueDistance;
        private double m_damage;

        //breedable stats publics
        public CellColor CellColor { get; private set; }
        public double MaxHealth
        {
            get
            {
                return m_maxHealth;
            }
            private set
            {
                m_maxHealth = Math.Max(value, 10d);
            }
        }
        public double BreedChance
        {
            get
            {
                return Math.Clamp(m_breedChance, 0.05, 0.35);
            }
            private set { m_breedChance = Math.Clamp(value, 0.05, 0.35); }
        }
        public double MaxHueDistance
        {
            get
            {
                return m_maxHueDistance;
            }
            private set
            {
                m_maxHueDistance = value;
            }
        }
        public double Damage
        {
            get { return m_damage; }
            set { m_damage = Math.Clamp(value, 0.25, 9d); }
        }


        public Cell(Texture2D _texture, Texture2D _hostileTex, /*IUltravioletWindow _window,*/ Vector2 _position, CellManager _manager)
        {
            if (rng == null)
                rng = new Random();
            if (texture == null)
                texture = _texture;
            if (selectTexture == null)
                selectTexture = _hostileTex;
            //if (window == null)
            //    window = _window;
            if (manager == null)
                manager = _manager;
            neighbors = new Cell[8];

            position = _position;
            Initialize();
        }

        private void Initialize()
        {
            hasCorpse = false;
            IsAlive = false;
            Hostile = false;
            CellColor = new CellColor(Color.White);
        }

        internal void Draw(UltravioletTime time, ref SpriteBatch spriteBatch)
        {
            if (Selected)
                spriteBatch.Draw(selectTexture, position * cellSize, CellColor.Color);
            else
                spriteBatch.Draw(texture, position * cellSize, CellColor.Color);

            Selected = false;
        }

        internal void Update(double deltaTime)
        {
            //if (position.X > window.DrawableSize.Width ||
            //    position.Y > window.DrawableSize.Height ||
            //    position.X * cellSize < 0 ||
            //    position.Y * cellSize < 0)
            //{
            //    return;
            //}       

            if (!IsAlive) return;

            TimeAlive += deltaTime;
            Health -= deltaTime;

            if (Health <= 0)
            {
                Die();
                return;
            }
            else
            {
                if (Hostile)
                {
                    AttackNearestEnemy();
                }

                if (Health >= (MaxHealth * .75d) && rng.NextDouble(0d, 1d) <= BreedChance)
                {
                    Breed();
                }

                Hostile = HasEnemyNeighbor();

                var deadNeighbors = GetDeadNeighbors();
                if (deadNeighbors.Count > 0)
                {
                    Eat(deadNeighbors[0]);
                }
            }
        }

        private void AttackNearestEnemy()
        {
            List<Cell> shuffled = GetEnemyNeighbors();
            if (shuffled.Count == 0) return;

            shuffled.Shuffle();
            var enemy = shuffled[0];

            if (enemy.TakeDamage(Damage))
                Eat(enemy);

            return;

        }

        /// <summary>
        /// Makes the cell take damage and returns true if it dies
        /// </summary>
        /// <param name="damageTaken"></param>
        /// <returns>true if damage killed cell, false if not</returns>
        private bool TakeDamage(double damageTaken)
        {
            Health -= damageTaken;
            if (Health <= 0)
            {
                Die();
                return true;
            }
            return false;
        }

        private bool HasEnemyNeighbor()
        {
            foreach (Cell neighbor in neighbors)
            {
                if (neighbor == null) continue;

                if (!IsCloseFamily(neighbor))
                {
                    if (IsExtendedFamily(neighbor))
                    {
                        continue;
                    }

                    return true;
                }
            }
            return false;
        }
        private List<Cell> GetEnemyNeighbors()
        {
            List<Cell> enemyNeighbors = new List<Cell>();
            foreach (Cell neighbor in neighbors)
            {
                if (neighbor == null) continue;

                if (!IsCloseFamily(neighbor))
                {
                    enemyNeighbors.Add(neighbor);
                }
            }
            return enemyNeighbors;
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

            neighbors[0] = manager.GetCell(i - 1, j - 1);
            neighbors[1] = manager.GetCell(i - 1, j);
            neighbors[2] = manager.GetCell(i - 1, j + 1);

            neighbors[3] = manager.GetCell(i, j - 1);
            neighbors[4] = manager.GetCell(i, j + 1);

            neighbors[5] = manager.GetCell(i + 1, j - 1);
            neighbors[6] = manager.GetCell(i + 1, j);
            neighbors[7] = manager.GetCell(i + 1, j + 1);
        }

        internal void Clicked(bool spawnNeighbors = false)
        {
            Born(null, null);
            if (spawnNeighbors)
            {
                PrimogenitorID = Guid.NewGuid();
                foreach (Cell startingMate in neighbors)
                {
                    if (startingMate == null) continue;
                    startingMate.Clicked(false);
                    startingMate.CellColor.Hue = CellColor.Hue;
                    startingMate.PrimogenitorID = PrimogenitorID;
                }
            }
        }

        internal void Born(Cell mother, Cell father)
        {
            IsAlive = true;
            hasCorpse = false;
            Hostile = false;
            CellColor.Saturation = 1;
            CellColor.Value = 1;
            TimeAlive = 0;

            if (mother != null && father != null)
            {
                //sexual reproduction

                double avgHue = (mother.CellColor.Hue + father.CellColor.Hue) / 2;
                CellColor.Hue = avgHue + rng.NextDouble(-1, 1);

                double avgMaxLifeTime = (mother.MaxHealth + father.MaxHealth) / 2;
                MaxHealth = avgMaxLifeTime + rng.NextDouble(-1, 1);

                double avgBreedChance = (mother.BreedChance + father.BreedChance) / 2;
                BreedChance = avgBreedChance + rng.NextDouble(-1, 1);

                double avgMaxHueDistance = (mother.MaxHueDistance + father.MaxHueDistance) / 2;
                MaxHueDistance = avgMaxHueDistance + rng.NextDouble(-1, 1);

                double avgDamage = (mother.Damage + father.Damage) / 2;
                Damage = avgDamage + rng.NextDouble(-1, 1);

                PrimogenitorID = mother.PrimogenitorID;
            }
            else if (mother != null && father == null)
            {
                //asexual reproduction

                CellColor.Hue = mother.CellColor.Hue + rng.NextDouble(-1, 1);

                MaxHealth = mother.MaxHealth + rng.NextDouble(-1, 1);

                BreedChance = mother.BreedChance + rng.NextDouble(-1, 1);

                MaxHueDistance = mother.MaxHueDistance + rng.NextDouble(-1, 1);

                Damage = mother.Damage + rng.NextDouble(-1, 1);

                PrimogenitorID = mother.PrimogenitorID;

            }
            else
            {
                CellColor = new CellColor(rng.NextDouble(0, 360));
                MaxHealth = rng.NextDouble(1.5, 5);
                BreedChance = rng.NextDouble(0.1, 0.5);
                MaxHueDistance = rng.NextDouble(5, 15);
                Damage = rng.NextDouble(0.1, 1d);
                PrimogenitorID = Guid.NewGuid();
            }

            Health = MaxHealth;
        }

        internal void Die()
        {
            IsAlive = false;
            hasCorpse = true;
            Hostile = false;
            Selected = false;
            mother = null;
            father = null;
            CellColor.Value = .1;
            CellColor.Saturation = .1;
            TimeAlive = 0;
            if (manager.SelectedCell == this)
                manager.SelectedCell = null;
        }

        private void Eat(Cell toEat)
        {
            if (toEat.hasCorpse)
            {
                Health += toEat.MaxHealth * .5;

                CellColor.ShiftTowards(toEat.CellColor);

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

                    child.Born(this, mate);
                }
            }
        }

        private Cell GetAndCheckChild()
        {
            var shuffled = new List<Cell>(neighbors);
            shuffled.Shuffle();

            //check for dying neighbors to replace
            Cell firstCorpse = null;
            for (int i = 0; i < shuffled.Count; i++)
            {
                if (shuffled[i] != null && shuffled[i].hasCorpse)
                {
                    firstCorpse = shuffled[i];
                }
            }

            if (firstCorpse != null)
            {
                firstCorpse.Die();
                return firstCorpse;
            }
            else
            {
                //if none, do normal

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
            if (IsAlive)
                return $"-|------ {position}\n" +
                       $" |MaxHP: {MaxHealth:0.000} | MHD: {MaxHueDistance:0.000} | BC: {BreedChance:0.000}\n" +
                       $" |CurHP: {Health:0.000} | Hue: {CellColor.Hue:0.000} | Hostile: {Hostile}\n" +
                       $" |Damage: {Damage:0.000} | Alive: {TimeAlive:0.000}s | \n" +
                       $" |PID: {PrimogenitorID.ToString().Substring(0, 8)}\n";
            else
                return $"-|------ {position}\n" +
                       $" | Dead has corpse: {hasCorpse}";
        }

        public bool IsCloseFamily(Cell cellToCheck)
        {
            if (cellToCheck == null) return false;
            if (cellToCheck.IsAlive)
            {
                if (CellColor.Distance(CellColor, cellToCheck.CellColor) > MaxHueDistance)
                    return false;
                else
                    return true;
            }
            return false;
        }

        public bool IsExtendedFamily(Cell cellToCheck)
        {
            if (cellToCheck == null) return false;
            if (cellToCheck.IsAlive)
            {
                if (PrimogenitorID == cellToCheck.PrimogenitorID)
                {
                    if (CellColor.Distance(CellColor, cellToCheck.CellColor) <= MaxHueDistance * 2)
                    {
                        return true;
                    }                    
                }
            }
            return false;
        }
    }
}
