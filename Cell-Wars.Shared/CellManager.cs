using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Ultraviolet;
using Ultraviolet.Graphics;
using Ultraviolet.Graphics.Graphics2D;

namespace CellWars
{
    internal class CellManager
    {
        public enum SelectionType
        {
            None,
            Single,
            Familial,
            Primogenitor
        }
        private Cell[,] cells;

        public Cell SelectedCell;
        public SelectionType CurrentSelectionType = SelectionType.None;

        public static bool DrawDebug { get; set; } = false;

        public CellManager(Texture2D cellImg, Texture2D selectedCellImg, int width, int height)
        {
            Cell.cellSize = cellImg.Width;

            this.cells = new Cell[width / Cell.cellSize, height /Cell.cellSize];

            for (int x = 0; x < width / Cell.cellSize; x++)
            {
                for (int y = 0; y < height / Cell.cellSize; y++)
                {
                    cells[x,y] = new Cell(cellImg, selectedCellImg, /*window,*/ new Vector2(x, y), this);
                }
            }

            for (int x = 0; x < cells.GetLength(0); x++)
            {
                for (int y = 0; y < cells.GetLength(1); y++)
                {
                    cells[x,y].GetNeighbors();
                }
            }
        }

        public void ProcessTimeStep(UltravioletTime time, bool _pause = false)
        {            
            if(SelectedCell != null)
            {
                switch (CurrentSelectionType)
                {
                    case SelectionType.None:
                        break;
                    case SelectionType.Single:
                        break;
                    case SelectionType.Familial:
                        SelectAllFamilialCells();
                        break;
                    case SelectionType.Primogenitor:
                        SelectPrimogenitorLine();
                        break;
                }
            }
            
            if (_pause)            
                return;
            

            for (int x = 0; x < cells.GetLength(0); x++)
            {
                for (int y = 0; y < cells.GetLength(1); y++)
                {
                    cells[x,y].Update(time.ElapsedTime.TotalSeconds);
                }
            }
        }

        internal void Draw(UltravioletTime time, ref SpriteBatch spriteBatch)
        {
            for (int x = 0; x < cells.GetLength(0); x++)
            {
                for (int y = 0; y < cells.GetLength(1); y++)
                {
                    cells[x,y].Draw(time, ref spriteBatch);
                }
            }
        }

        internal void SpawnCells(Point2 cellPos, bool spawnMultiple)
        {
            cells[cellPos.X, cellPos.Y].Clicked(spawnMultiple);
        }

        internal Cell GetCell(Point2 cellPos)
        {
            return cells.Get2DValueOrNull(cellPos.X, cellPos.Y);
        }
        internal Cell GetCell(int x, int y)
        {
            return cells.Get2DValueOrNull(x, y);
        }        
        private List<Cell> GetCellList()
        {
 
            int size0 = cells.GetLength(0);
            int size1 = cells.GetLength(1);
            List<Cell> list = new List<Cell>(size0 * size1);
            for (int i = 0; i < size0; i++)
            {
                for (int j = 0; j < size1; j++)
                    list.Add(cells[i, j]);
            }
            return list;
        }
        
        internal string GetCellInfo(Point2 cellPos)
        {
            return cells[cellPos.X, cellPos.Y].ToString();
        }

        internal void Clear()
        {
            Console.Clear();
            for (int x = 0; x < cells.GetLength(0); x++)
            {
                for (int y = 0; y < cells.GetLength(1); y++)
                {
                    cells[x, y].Die();
                }
            }
        }

        internal void RandomSpawns()
        {
            for (int x = 0; x < cells.GetLength(0); x++)
            {
                for (int y = 0; y < cells.GetLength(1); y++)
                {
                    SpawnCells(new Point2(x, y), false);
                }
            }
        }

        public double[] GetMaxes()
        {
            double[] maxes = new double[] { 0, 0, 0, 0 };
            for (int x = 0; x < cells.GetLength(0); x++)
            {
                for (int y = 0; y < cells.GetLength(1); y++)
                {
                    if (!cells[x,y].IsAlive) continue;
                    if (cells[x,y].MaxHealth > maxes[0]) maxes[0] = cells[x,y].MaxHealth;
                    if (cells[x,y].MaxHueDistance > maxes[1]) maxes[1] = cells[x,y].MaxHueDistance;
                    if (cells[x,y].BreedChance > maxes[2]) maxes[2] = cells[x,y].BreedChance;
                    if (cells[x,y].Health > maxes[3]) maxes[3] = cells[x,y].Health;
                }
            }
            return maxes;
        }

        internal void SelectPrimogenitorLine(Point2 cellPos)
        {
            SelectedCell = GetCell(cellPos);
            SelectPrimogenitorLine();
        }
        private void SelectPrimogenitorLine()
        {
            Guid primoID = SelectedCell.PrimogenitorID;
            var allCells = GetCellList();
            allCells.Where(x => x.PrimogenitorID == primoID).ToList().ForEach(x => x.Selected = true);
        }

        internal void SelectAllFamilialCells(Point2 cellPos)
        {
            SelectedCell = GetCell(cellPos);
            SelectAllFamilialCells();
        }
        
        private void SelectAllFamilialCells()
        {
            var allCells = GetCellList();
            var family = allCells.Where(c => SelectedCell.IsCloseFamily(c));
            family.ToList().ForEach(x => x.Selected = true);
        }

        public double[] GetAverages()
        {
            double[] totals = new double[] { 0, 0, 0, 0 };
            int counter = 0;
            for (int x = 0; x < cells.GetLength(0); x++)
            {
                for (int y = 0; y < cells.GetLength(1); y++)
                {
                    if (!cells[x,y].IsAlive) continue;
                    totals[0] += cells[x,y].MaxHealth;
                    totals[1] += cells[x,y].MaxHueDistance;
                    totals[2] += cells[x,y].BreedChance;
                    totals[3] += cells[x,y].Health;
                    counter++;
                }
            }

            for (int i = 0; i < totals.Length; i++)
            {
                totals[i] /= counter;
            }
            return totals;
        }

        
    }
}
