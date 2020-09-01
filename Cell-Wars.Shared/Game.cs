using System;
using System.Collections.Generic;
using System.Linq;
using Ultraviolet;
using Ultraviolet.BASS;
using Ultraviolet.Content;
using Ultraviolet.FreeType2;
using Ultraviolet.Graphics;
using Ultraviolet.Graphics.Graphics2D;
using Ultraviolet.Input;
using Ultraviolet.OpenGL;
using Ultraviolet.Platform;

namespace ultraviolettesting
{
    public partial class Game : UltravioletApplication
    {
        public Game()
            : base("Daxtron2", "Cell Wars")
        { }

        protected override UltravioletContext OnCreatingUltravioletContext()
        {
            var configuration = new OpenGLUltravioletConfiguration();
            configuration.Plugins.Add(new BASSAudioPlugin());
            configuration.Plugins.Add(new FreeTypeFontPlugin());

#if DEBUG
            configuration.Debug = true;
            configuration.DebugLevels = DebugLevels.Error | DebugLevels.Warning;
            configuration.DebugCallback = (uv, level, message) =>
            {
                System.Diagnostics.Debug.WriteLine(message);
            };
#endif

            return new OpenGLUltravioletContext(this, configuration);
        }

        protected override void OnInitialized()
        {
            UsePlatformSpecificFileSource();
            kb = Ultraviolet.GetInput().GetKeyboard();
            mouse = Ultraviolet.GetInput().GetMouse();
            window = Ultraviolet.GetPlatform().Windows.GetPrimary();
            window.ClientSize = new Size2(500, 500);
            window.MaximumClientSize = window.ClientSize;
            base.OnInitialized();
        }

        protected override void OnLoadingContent()
        {
            this.contentManager = ContentManager.Create("Content");
            Texture2D cellImg = contentManager.Load<Texture2D>("1x1.bmp");
            Cell.cellSize = cellImg.Width;
            this.spriteBatch = SpriteBatch.Create();

            this.cells = new Cell[window.DrawableSize.Width / Cell.cellSize][];
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = new Cell[window.DrawableSize.Height / Cell.cellSize];
            }

            for (int x = 0; x < window.DrawableSize.Width / Cell.cellSize; x++)
            {
                for (int y = 0; y < window.DrawableSize.Height / Cell.cellSize; y++)
                {
                    cells[x][y] = new Cell(cellImg, window, new Vector2(x, y), ref cells);
                }
            }

            for (int x = 0; x < cells.Length; x++)
            {
                for (int y = 0; y < cells[x].Length; y++)
                {
                    cells[x][y].GetNeighbors();
                }
            }
            base.OnLoadingContent();
        }

        protected override void OnUpdating(UltravioletTime time)
        {
            Controls(time);

            if (!paused)
            {
                for (int x = 0; x < cells.Length; x++)
                {
                    for (int y = 0; y < cells[x].Length; y++)
                    {
                        cells[x][y].Update(time);
                    }
                }
            }
            //Console.WriteLine(window.DrawableSize.Width);
            base.OnUpdating(time);
        }

        private void Controls(UltravioletTime time)
        {
            if (kb.IsKeyPressed(Key.Escape))
            {
                Exit();
            }
            if (mouse.IsButtonClicked(MouseButton.Left))
            {
                //Console.WriteLine("click at {0}!", mouse.Position);
                var cellPos = mouse.Position / Cell.cellSize;
                //cells[cellPos.X][cellPos.Y].Clicked(true);
                Console.WriteLine("~~ Cell at {0}", cellPos);
                Console.WriteLine(cells[cellPos.X][cellPos.Y]);
            }
            else if (mouse.IsButtonClicked(MouseButton.Right))
            {
                var cellPos = mouse.Position / Cell.cellSize;
                cells[cellPos.X][cellPos.Y].Clicked(true);
            }
            if (kb.IsKeyPressed(Key.I))
            {
                double[] maxes = cells.GetMaxes();
                double maxLifeTime = maxes[0];
                double maxHueDistance = maxes[1];
                double maxBreedChance = maxes[2];
                double maxCurrentLifeTime = maxes[3];

                double[] avgs = cells.GetAverages();
                double avgLifeTime = avgs[0];
                double avgHueDistance = avgs[1];
                double avgBreedChance = avgs[2];
                double avgCurrentLifeTime = avgs[3];

                Console.WriteLine("~~{0}~~", time.TotalTime.TotalSeconds);
                Console.WriteLine("Max Lifespan: {0:0.000}    Max Hue Distance: {1:0.000}    Max Breed Chance: {2:0.000}    Max cell life: {3:0.000}",
                                   maxLifeTime, maxHueDistance, maxBreedChance, maxCurrentLifeTime);
                Console.WriteLine("Avg Lifespan: {0:0.000}    Avg Hue Distance: {1:0.000}    Avg Breed Chance: {2:0.000}    Avg cell life: {3:0.000}\n",
                                   avgLifeTime, avgHueDistance, avgBreedChance, avgCurrentLifeTime);

            }
            if (kb.IsKeyPressed(Key.R))
            {
                Console.Clear();
                for (int x = 0; x < cells.Length; x++)
                {
                    for (int y = 0; y < cells[x].Length; y++)
                    {
                        cells[x][y].Die();
                        cells[x][y].Clicked();
                    }
                }
            }
            if (kb.IsKeyPressed(Key.Space))
            {
                paused = !paused;
            }

            if (kb.IsKeyPressed(Key.H)) displayOff = true;

            if (kb.IsKeyReleased(Key.H)) displayOff = false;
        }

        protected override void OnDrawing(UltravioletTime time)
        {
            this.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            if (!displayOff)
            {
                for (int x = 0; x < cells.Length; x++)
                {
                    for (int y = 0; y < cells[x].Length; y++)
                    {
                        cells[x][y].Draw(time, ref this.spriteBatch);
                    }
                }
            }
            this.spriteBatch.End();
            //Console.WriteLine(time.ElapsedTime.TotalMilliseconds);

            base.OnDrawing(time);
        }

        protected override void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                if (this.contentManager != null)
                    this.contentManager.Dispose();

                if (this.spriteBatch != null)
                    this.spriteBatch.Dispose();
            }
            base.Dispose(disposing);
        }

        private ContentManager contentManager;
        private SpriteBatch spriteBatch;
        private Cell[][] cells;
        private KeyboardDevice kb;
        private IUltravioletWindow window;
        private MouseDevice mouse;
        private bool paused = false, displayOff = false;

    }
}
