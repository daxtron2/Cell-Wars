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

namespace CellWars
{
    public partial class Game : UltravioletApplication
    {
        private ContentManager contentManager;
        private SpriteBatch spriteBatch;
        private KeyboardDevice kb;
        private IUltravioletWindow window;
        private MouseDevice mouse;
        private bool paused = false, displayOff = false;
        private CellManager cells;
        
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
            window.ClientSize = new Size2(700, 700);
            window.MaximumClientSize = window.ClientSize;
            base.OnInitialized();
        }

        protected override void OnLoadingContent()
        {
            this.contentManager = ContentManager.Create("Content");
            Texture2D cellImg = contentManager.Load<Texture2D>("1x1.bmp");
            Texture2D selectedCellImg = contentManager.Load<Texture2D>("hostile.bmp");
            this.spriteBatch = SpriteBatch.Create();

            cells = new CellManager(cellImg, selectedCellImg, window.DrawableSize.Width, window.DrawableSize.Height);

            base.OnLoadingContent();
        }

        protected override void OnUpdating(UltravioletTime time)
        {
            Controls(time);

            ProcessTimeStep(time, paused);
            
            base.OnUpdating(time);
        }

        protected void ProcessTimeStep(UltravioletTime time, bool _pause = false)
        {
            cells.ProcessTimeStep(time, _pause);
        }

        private void Controls(UltravioletTime time)
        {
            if (kb.IsKeyPressed(Key.Escape))
            {
                Exit();
            }
            if (mouse.IsButtonClicked(MouseButton.Left))
            {
                var cellPos = mouse.Position / Cell.cellSize;
                if (kb.IsKeyDown(Key.LeftShift))
                {
                    cells.CurrentSelectionType = CellManager.SelectionType.Primogenitor;
                    cells.SelectPrimogenitorLine(cellPos);
                }
                else if (kb.IsKeyDown(Key.LeftControl))
                {
                    cells.CurrentSelectionType = CellManager.SelectionType.Familial;
                    cells.SelectAllFamilialCells(cellPos);
                }
                else
                {
                    cells.CurrentSelectionType = CellManager.SelectionType.Single;
                    Console.WriteLine(cells.GetCellInfo(cellPos));
                }
            }
            else if (mouse.IsButtonClicked(MouseButton.Right))
            {
                var cellPos = mouse.Position / Cell.cellSize;
                cells.SpawnCells(cellPos, true);
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
                cells.Clear();
                cells.RandomSpawns();                
            }
            if (kb.IsKeyPressed(Key.Space))
            {
                paused = !paused;
            }

            if(paused && kb.IsKeyPressed(Key.Period))
            {
                ProcessTimeStep(time);
            }

            if (kb.IsKeyPressed(Key.Q)) displayOff = true;
            
            if (kb.IsKeyReleased(Key.Q)) displayOff = false;

            if (kb.IsKeyPressed(Key.H))
                CellManager.DrawDebug = !CellManager.DrawDebug;
        }

        protected override void OnDrawing(UltravioletTime time)
        {
            this.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            if (!displayOff)
            {
                cells.Draw(time, ref this.spriteBatch);
            }
            this.spriteBatch.End();
            
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
    }
}
