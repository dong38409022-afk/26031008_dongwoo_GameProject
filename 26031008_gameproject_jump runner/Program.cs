using System.Drawing.Drawing2D;
using System.Media;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new GameForm());
    }
}

public class GameForm : Form
{
    const int Ground = 400;
    System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
    Random random = new Random();
    Image background = LoadImage("background.png");
    Image run = LoadImage("run_sheet.png");
    Image jump = LoadImage("jump.png");

    Image obstacle = LoadImage("obstacle.png");
    Image startScreen = LoadImage("start.png");
    Image endScreen = LoadImage("gameover.png");
    
    long lastTime;
    float frameTime = 1;
    SoundPlayer jumpSound = LoadSound("jump.wav");
    SoundPlayer scoreSound = LoadSound("score.wav");
    SoundPlayer hitSound = LoadSound("hit.wav");
    Font titleFont = new Font("맑은 고딕", 30, FontStyle.Bold);

    Font scoreFont = new Font("Consolas", 18, FontStyle.Bold);

    int state, score, nextSound = 1000;
    float height, jumpSpeed, speed = 8, distance, scroll, runFrame;
    bool spaceDown;
    RectangleF[] obstacles = new RectangleF[8];
    int obstacleCount = 0;

    static Image LoadImage(string name)
    {
        return Image.FromFile(Path.Combine(AppContext.BaseDirectory, "resource", "tx_ui", name));
    }

    static SoundPlayer LoadSound(string name)
    {
        return new SoundPlayer(Path.Combine(AppContext.BaseDirectory, "resource", "sound", name));
    }

    public GameForm()
    {
        Text = "Jump Runner · 점프 러너";
        ClientSize = new Size(960, 540);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;
        KeyPreview = true;
        BackColor = Color.White;
        
        jumpSound.Load(); scoreSound.Load(); hitSound.Load();
        
        timer.Interval = 16;
        timer.Tick += TimerTick;
        KeyDown += OnKeyDown;
        KeyUp += ReleaseKey;
        Deactivate += LeaveWindow;
        Paint += DrawScreen;
        FormClosed += CloseGame;
        timer.Start();
    }

    void ReleaseKey(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space)
        {
            spaceDown = false;
        }
    }

    void LeaveWindow(object sender, EventArgs e)
    {
        spaceDown = false;
    }

    void StartGame()
    {
        state = 1;
        score = 0;
        nextSound = 1000;
        height = 0;
        jumpSpeed = 0;
        distance = 0;
        scroll = 0;
        runFrame = 0;
        obstacleCount = 0;
        speed = 8;
        spaceDown = false;
        AddObstacle(1150);
        frameTime = 1;
        lastTime = Environment.TickCount64;
        Focus();
        Invalidate();
    }

    void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (state != 1)
        {
            if (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1) StartGame();
            else if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2) Close();
            return;
        }
        if (e.KeyCode == Keys.Space)
        {
            e.SuppressKeyPress = true;
            if (!spaceDown)
            {
                if (state == 1 && height == 0)
                {
                    jumpSpeed = 15;
                    jumpSound.Play();
                }
            }
            spaceDown = true;
        }
        Invalidate();
    }

    void TimerTick(object sender, EventArgs e)
    {
        long now = Environment.TickCount64;
        
        frameTime = (now - lastTime) / 16.6667f;
        lastTime = now;
        
        if (frameTime > 3)
        {
            frameTime = 3;
        }
        StepGame();
        Invalidate();
    }

    void StepGame()
    {
        if (state != 1) return;

        if (height > 0 || jumpSpeed > 0)
        {
            height += jumpSpeed * frameTime;
            jumpSpeed -= 0.7f * frameTime;
            
            if (!spaceDown && height >= 85 && jumpSpeed > 4) jumpSpeed = 4;
            if (height < 0)
            {
                height = 0;
                jumpSpeed = 0;
            }
        }
        speed = 8 + score / 1000f; 
        float movement = speed * frameTime;
        runFrame = (runFrame + movement / 36f) % 6;
        distance += movement;
        score = (int)(distance / 10);
        scroll = (scroll + movement * 0.3f) % 960;
        RectangleF playerBox = new RectangleF(145, Ground - 83 - height, 43, 77);
        for (int i = 0; i < obstacleCount; i++)
        {
            RectangleF box = obstacles[i];
            box.X -= movement;
            obstacles[i] = box;
            
            RectangleF hitBox = new RectangleF(box.X + 5, box.Y + 6, box.Width - 10 + movement, box.Height - 8);
            if (playerBox.IntersectsWith(hitBox))
            {
                state = 2;
                hitSound.Play();
                break;
            }
        }
        if (state == 1)
        {
            if (obstacleCount > 0 && obstacles[0].Right < 0)
            {
                
                for (int i = 0; i < obstacleCount - 1; i++)
                {
                    obstacles[i] = obstacles[i + 1];
                }
                obstacleCount--;
            }
            if (obstacleCount == 0) AddObstacle(960);
            RectangleF last = obstacles[obstacleCount - 1];
            if (last.Right < 960)
            {
                float nextX = last.Right + speed * random.Next(55, 86);
                if (nextX < 960) nextX = 960;
                AddObstacle(nextX);
            }
            if (score >= nextSound)
            {
                nextSound += 1000;
                scoreSound.Play();
            }
        }
        Invalidate();
    }

    void AddObstacle(float x)
    {
        if (obstacleCount == obstacles.Length) return;
        int count = 1;
        if (speed >= 9) count = random.Next(1, 4);
        int size = 60;
        if (random.Next(2) == 1) size = 76;
        obstacles[obstacleCount] = new RectangleF(x, Ground - size, count * 50, size);
        obstacleCount++;
    }

    void DrawScreen(object sender, PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        if (state != 1)
        {
            Image menuImage = startScreen;
            if (state == 2) menuImage = endScreen;
            g.DrawImage(menuImage, new Rectangle(0, 0, 960, 540));
            if (state == 2)
            {
                
                g.FillRectangle(Brushes.White, 280, 85, 400, 159);
                CenterText(g, "점수", titleFont, 90);
                CenterText(g, score.ToString(), titleFont, 166);
            }
            
            g.FillRectangle(Brushes.White, 314, 255, 331, 66);
            g.FillRectangle(Brushes.White, 314, 347, 331, 65);
            string menuText = "1. 게임 시작";
            if (state == 2) menuText = "1. 다시 시작";
            CenterText(g, menuText, titleFont, 259);
            CenterText(g, "2. 나가기", titleFont, 350);
            return;
        }
        
        Rectangle scenery = new Rectangle(0, 0, background.Width, 850);
        g.DrawImage(background, new Rectangle(-(int)scroll, 60, 960, Ground - 60), scenery, GraphicsUnit.Pixel);
        g.DrawImage(background, new Rectangle(960 - (int)scroll, 60, 960, Ground - 60), scenery, GraphicsUnit.Pixel);
        
        SolidBrush fade = new SolidBrush(Color.FromArgb(235, Color.White));
        g.FillRectangle(fade, 0, 60, 960, Ground - 60);
        fade.Dispose(); 
        g.FillRectangle(Brushes.White, 0, Ground, 960, 140);
        g.DrawLine(Pens.Black, 0, Ground, 960, Ground);

        Rectangle player = new Rectangle(100, Ground - 120 - (int)height, 128, 128);
        if (height > 0) g.DrawImage(jump, player);
        else g.DrawImage(run, player, new Rectangle((int)runFrame * 256, 0, 256, 256), GraphicsUnit.Pixel);
        for (int index = 0; index < obstacleCount; index++)
        {
            RectangleF box = obstacles[index];
            int count = (int)(box.Width / 50);
            for (int i = 0; i < count; i++)
            {
                float x = box.X + i * 50;
                
                g.DrawImage(obstacle, new RectangleF(x, box.Y, 50, box.Height),
                    new RectangleF(7, 13, 114, 108), GraphicsUnit.Pixel);
                
                Pen outline = new Pen(Color.Black, 2);
                g.DrawLine(outline, x + 5, box.Y + 6, x + 5, box.Bottom - 4);
                g.DrawLine(outline, x + 45, box.Y + 6, x + 45, box.Bottom - 4);
                g.DrawRectangle(outline, x + 4, box.Y + 8, 42, 10);
                outline.Dispose();
            }
        }

        g.DrawString("JUMP RUNNER", scoreFont, Brushes.Black, 24, 18);
        g.DrawString("SCORE " + score.ToString("00000"), scoreFont, Brushes.Black, 525, 18);
    }

    static void CenterText(Graphics g, string text, Font font, int y)
    {
        g.DrawString(text, font, Brushes.Black, (960 - g.MeasureString(text, font).Width) / 2, y);
    }

    void CloseGame(object sender, FormClosedEventArgs e)
    {
            timer.Dispose();
            background.Dispose(); run.Dispose(); jump.Dispose(); obstacle.Dispose();
            startScreen.Dispose(); endScreen.Dispose();
            jumpSound.Dispose(); scoreSound.Dispose(); hitSound.Dispose();
            titleFont.Dispose(); scoreFont.Dispose();
    }
}




