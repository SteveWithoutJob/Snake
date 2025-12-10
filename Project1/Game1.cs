using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Geometry_Snake
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // === Had ===
        private Texture2D snakeTexture, obstacleTexture, pointTexture;
        private Vector2 headPosition;
        private Texture2D triangleTexture;
        private Vector2 velocity;
        private float gravity = 0.4f;
        private bool isOnGround = true;

        private List<Vector2> trail = new List<Vector2>(); // pozice hlavy
        private int bodyLength = 7;
        private int trailSpacing = 2;

        // === Hra ===
        private List<Rectangle> obstacles = new List<Rectangle>();
        private List<Vector2> points = new List<Vector2>();
        private int score = 0;
        private SpriteFont font;
        private Texture2D outlineTexture;
        private Texture2D groundTexture;
        private bool spaceWasPressed = false; // pro detekci podržení
        private float jumpChargeTime = 0f; // jak dlouho je Space podržený
        private const float maxChargeTime = 1.0f; // maximální doba nabíjení (sekundy)


        private BasicEffect basicEffect;

        // Menu
        Rectangle playButton = new Rectangle(300, 200, 200, 80);

        enum GameState
        {
            Menu,
            Playing,
            GameOver
        }
        GameState gameState = GameState.Menu;
        private Vector2 obs;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 480;
            _graphics.ApplyChanges();

            headPosition = new Vector2(100, 350);


            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Jednobarevné textury
            snakeTexture = new Texture2D(GraphicsDevice, 1, 1);
            snakeTexture.SetData(new[] { Color.LimeGreen });

            obstacleTexture = new Texture2D(GraphicsDevice, 1, 1);
            obstacleTexture.SetData(new[] { Color.Red });

            outlineTexture = new Texture2D(GraphicsDevice, 1, 1);
            outlineTexture.SetData(new[] { Color.Black });

            groundTexture = new Texture2D(GraphicsDevice, 1, 1);
            groundTexture.SetData(new[] { Color.SaddleBrown });

            // Trojúhelníková textura (apex nahoře)
            int tw = 64; // velikost textury (můžeš upravit)
            triangleTexture = new Texture2D(GraphicsDevice, tw, tw);
            Color[] tdata = new Color[tw * tw];
            int half = tw / 2;
            for (int y = 0; y < tw; y++)
            {
                float frac = (float)y / (tw - 1);           // 0.0 (vršek) -> 1.0 (spodek)
                int halfWidth = (int)(half * frac);         // rozšiřování směrem dolů
                for (int x = 0; x < tw; x++)
                {
                    if (Math.Abs(x - half) <= halfWidth)
                        tdata[x + y * tw] = Color.Red;
                    else
                        tdata[x + y * tw] = Color.Transparent;
                }
            }
            triangleTexture.SetData(tdata);

            pointTexture = new Texture2D(GraphicsDevice, 1, 1);
            pointTexture.SetData(new[] { Color.Gold });

            // Překážky a body
            obstacles.Add(new Rectangle(400, 380, 40, 40));
            obstacles.Add(new Rectangle(700, 360, 50, 60));

            points.Add(new Vector2(500, 330));
            points.Add(new Vector2(850, 310));

            Texture2D smallTriangleTexture = new(GraphicsDevice, 20, 20);
            Color[] data = new Color[20 * 20];

            for (int y = 0; y < 20; y++)
            {
                for (int x = 0; x < 20; x++)
                {
                    if (x >= 10 - y && x <= 10 + y)
                        data[x + y * 20] = Color.Red;
                    else
                        data[x + y * 20] = Color.Transparent;
                }
            }

            smallTriangleTexture.SetData(data);



        }

        private void DrawRectangleOutline(Rectangle rect, int thickness = 2)
        {
            // Horní
            _spriteBatch.Draw(outlineTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), Color.Black);
            // Dolní
            _spriteBatch.Draw(outlineTexture, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), Color.Black);
            // Levý
            _spriteBatch.Draw(outlineTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), Color.Black);
            // Pravý
            _spriteBatch.Draw(outlineTexture, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), Color.Black);
        }

        private void DrawTriangleOutline(Rectangle rect, int thickness = 2)
        {
            int halfWidth = rect.Width / 2;
            int centerX = rect.X + halfWidth;

            // Levá strana trojúhelníku (od vrchu dolů)
            for (int y = 0; y < rect.Height; y++)
            {
                float frac = (float)y / rect.Height;
                int lineHalfWidth = (int)(halfWidth * frac);
                int leftX = centerX - lineHalfWidth;

                for (int t = 0; t < thickness; t++)
                {
                    _spriteBatch.Draw(outlineTexture, new Rectangle(leftX + t, rect.Y + y, 1, 1), Color.Black);
                }
            }

            // Pravá strana trojúhelníku (od vrchu dolů)
            for (int y = 0; y < rect.Height; y++)
            {
                float frac = (float)y / rect.Height;
                int lineHalfWidth = (int)(halfWidth * frac);
                int rightX = centerX + lineHalfWidth;

                for (int t = 0; t < thickness; t++)
                {
                    _spriteBatch.Draw(outlineTexture, new Rectangle(rightX - t, rect.Y + y, 1, 1), Color.Black);
                }
            }

            // Spodní strana trojúhelníku
            for (int t = 0; t < thickness; t++)
            {
                _spriteBatch.Draw(outlineTexture, new Rectangle(rect.X, rect.Y + rect.Height - t, rect.Width, 1), Color.Black);
            }
        }


        protected override void Update(GameTime gameTime)
        {


            var keyboard = Keyboard.GetState();
            if (keyboard.IsKeyDown(Keys.Escape))
                Exit();

            if (keyboard.IsKeyDown(Keys.Space) && isOnGround)
            {
                // Nabíjení skoku
                jumpChargeTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (jumpChargeTime > maxChargeTime)
                    jumpChargeTime = maxChargeTime;

                spaceWasPressed = true;
            }
            else if (spaceWasPressed && isOnGround)
            {
                // Uvolnění Space = skok
                float jumpPower = -10f - (jumpChargeTime * 5f);
                velocity.Y = jumpPower;
                isOnGround = false;

                // Reset
                spaceWasPressed = false;
                jumpChargeTime = 0f;

            }

            // Fyzika
            velocity.Y += gravity;
            headPosition.X += 0.4f;
            headPosition.Y += velocity.Y;

            // Podlaha
            if (headPosition.Y >= 380)
            {
                headPosition.Y = 380;
                velocity.Y = 0;
                isOnGround = true;

            }

            // Stopa hlavy
            trail.Insert(0, headPosition);
            if (trail.Count > (bodyLength + 1) * trailSpacing)
                trail.RemoveAt(trail.Count - 1);

            // Kolize s překážkami
            Rectangle headRect = new Rectangle((int)headPosition.X, (int)headPosition.Y, 20, 20);
            foreach (var obs in obstacles)
            {
                if (headRect.Intersects(obs))
                {
                    // Konec hry 
                    score = 0;
                    bodyLength = 10;
                    headPosition = new Vector2(100, 350);
                    velocity = Vector2.Zero;
                    trail.Clear();
                    break;
                }

            }

            // Sbírání bodů
            for (int i = points.Count - 1; i >= 0; i--)
            {
                Rectangle pointRect = new Rectangle((int)points[i].X, (int)points[i].Y, 25, 25);
                if (headRect.Intersects(pointRect))
                {
                    score++;
                    bodyLength++;
                    points.RemoveAt(i);
                }
            }

            // Posun překážek a bodů doleva
            for (int i = 0; i < obstacles.Count; i++)
            {
                var r = obstacles[i];
                r.X -= 3;
                if (r.Right < 0)
                    r.X = 800 + i * 300; // recykluj
                obstacles[i] = r;
            }
            for (int i = 0; i < points.Count; i++)
            {
                points[i] = new Vector2(points[i].X - 3, points[i].Y);
                if (points[i].X < -20)
                    points[i] = new Vector2(800 + i * 400, 330);
                base.Update(gameTime);
            }
        }





        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            _spriteBatch.Begin();

            // === ZEMĚ ===
            Rectangle ground = new Rectangle(0, 400, 800, 80);
            _spriteBatch.Draw(groundTexture, ground, Color.SaddleBrown);
            DrawRectangleOutline(ground, 3);

            // === HAD ===
            int size = 20;
            for (int i = 0; i < bodyLength && i * trailSpacing < trail.Count; i++)
            {
                Vector2 pos = trail[i * trailSpacing];

                // Když je had na zemi (Y >= 380), udělej ho širší a nižší
                int width = size;
                int height = size;
                int spacing = size; // mezera mezi čtverečky

                if (pos.Y >= 380)
                {
                    width = (int)(size * 1.5f);  // ŠÍŘKA NA ZEMI - změň 1.5f na jiné číslo (např. 2f pro dvojnásobek)
                    height = (int)(size * 0.7f); // výška na zemi - zmáčknutý tvar
                    spacing = (int)(size * 0.5f); // MENŠÍ MEZERA NA ZEMI = delší ocasek (0.5f = poloviční mezera, 0.3f = ještě delší)
                }

                // Vypočítej pozici s mezerou
                int actualIndex = 0;
                for (int j = 0; j <= i && actualIndex < trail.Count; j++)
                {
                    if (j == i) break;
                    actualIndex += (trail[actualIndex].Y >= 380) ? (int)(size * 0.5f) : size;
                }

                Rectangle snakeRect = new Rectangle((int)pos.X, (int)pos.Y, width, height);

                // Kreslení těla
                _spriteBatch.Draw(snakeTexture, snakeRect, Color.LimeGreen);
                // Kreslení obrysu
                DrawRectangleOutline(snakeRect, 2);
            }

            // === PŘEKÁŽKY ===
            foreach (var obs in obstacles)
            {
                // Kreslení trojúhelníku
                _spriteBatch.Draw(triangleTexture, obs, Color.Red);
                // Kreslení obrysu trojúhelníku
                DrawTriangleOutline(obs, 2);
            }

            // === BODY ===
            foreach (var p in points)
            {
                Rectangle pointRect = new Rectangle((int)p.X, (int)p.Y, 25, 25);

                // Kreslení bodu
                _spriteBatch.Draw(pointTexture, pointRect, Color.Yellow);
                // Kreslení obrysu
                DrawRectangleOutline(pointRect, 2);
            }

            // === SKÓRE ===
            if (font != null)
            {
                _spriteBatch.DrawString(font, $"Score: {score}", new Vector2(10, 10), Color.White);

                // Ukazatel nabíjení skoku
                if (isOnGround && jumpChargeTime > 0)
                {
                    int barWidth = (int)(200 * (jumpChargeTime / maxChargeTime));
                    Rectangle chargeBar = new Rectangle(10, 40, barWidth, 10);
                    _spriteBatch.Draw(pointTexture, chargeBar, Color.Yellow);
                    DrawRectangleOutline(new Rectangle(10, 40, 200, 10), 2);
                }
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}

