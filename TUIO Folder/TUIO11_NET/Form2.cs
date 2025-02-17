using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static TuioDemo;

namespace mohsen
{
    public partial class Form2 : Form
    {
        Bitmap off;
        Timer T = new Timer();
        private Form parentForm;

        SolidBrush SelectedItemBrush = new SolidBrush(Color.SeaGreen);
        SolidBrush MenuItemBrush = new SolidBrush(Color.White);

        public List<Point> mymenupoints = new List<Point>();

        private Image adminImage = Image.FromFile(@"admin.png");
        private Image backgroundImage = Image.FromFile(@"BG_3.jpg");
        private Image backgroundImage2 = Image.FromFile(@"bg.jpg");
        Font font = new Font("Times New Roman", 30.0f);
        private ConcurrentQueue<string> yoloCommands = new ConcurrentQueue<string>();

        public Form2(Form parentForm, ConcurrentQueue<string> queue)
        {

            InitializeComponent();
            this.yoloCommands = queue;
            Console.WriteLine($"Form2 queue hash: {this.yoloCommands.GetHashCode()}");

            // Maximize and remove borders more comprehensively
            this.FormBorderStyle = FormBorderStyle.Sizable; // Removes all borders
            this.WindowState = FormWindowState.Maximized;

            // Ensure form covers entire screen
            this.Bounds = Screen.PrimaryScreen.Bounds;


            // Double buffering to reduce flickering
            this.DoubleBuffered = true;

            // Event handlers
            this.Paint += new PaintEventHandler(Form1_Paint);
            this.Load += new EventHandler(Form1_Load);
            this.MouseDown += Form1_MouseDown;
            this.KeyDown += Form2_KeyDown;

            // Timer setup
            Timer T = new Timer();
            T.Interval = 10;
            T.Tick += new EventHandler(T_Tick);
            T.Start();
            T.Interval = 100;
            this.parentForm = parentForm;
            this.FormClosing += Form2_FormClosing;
        }
        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Stop the prediction processing timer
            // Close the parent form as well
            if (parentForm != null && !parentForm.IsDisposed)
            {
                parentForm.Close();
            }
        }

        public static List<string> imagePaths = new List<string>();

        public List<CActor> menuobjs = new List<CActor>();
        public int SelectedMenuFlag = 0;

        public int flagisMenuShown = 0;
        public int CountMenuItems = 2;
        public int MenuSelectedIndex = 0;
        public int MenuItemIndex = 0;

        public int MenuIconWidth = 100;
        public int MenuIconHeight = 150;

        public List<CActor> CreateMenuObjects(List<Point> points)
        {
            int padding = 20;
            int availableWidth = ClientSize.Width - (padding * 4);

            int maxWidth = ((availableWidth / CountMenuItems) - 400);
            if (SelectedMenuFlag == 2) // 2 items
            {
                maxWidth = (availableWidth / CountMenuItems) - 550;
                MenuIconHeight = 250;
            }
            if (SelectedMenuFlag == 4) // 2 items
            {
                maxWidth = (availableWidth / CountMenuItems) - 200;
                MenuIconHeight = 250;
            }

            List<CActor> objs = new List<CActor>();

            for (int i = 0; i < points.Count; i++)
            {
                CActor obj = new CActor();

                // Adjust width based on the number of items

                obj.W = maxWidth;
                obj.H = MenuIconHeight;

                // Center the rectangle horizontally with padding on each side
                obj.X = ClientSize.Width / 2 - (CountMenuItems * maxWidth) / 2 + i * (maxWidth + padding);
                obj.Y = ClientSize.Height / 2 - MenuIconHeight / 2;

                // Highlight selected menu item
                obj.color = (i == MenuSelectedIndex) ? 1 : 0;
                objs.Add(obj);
            }
            return objs;
        }
        public void DrawRoundedRectangle(Graphics g, bool isSelected, Rectangle rect, int radius, int index)
        {
            using (GraphicsPath path = CreateRoundedRectanglePath(rect, radius))
            {
                g.FillPath(MenuItemBrush, path);
                // Draw the rounded rectangle background
                if (isSelected)
                {
                    using (Pen redPen = new Pen(Color.Red, 5)) // Adjust thickness as needed
                    {
                        g.DrawPath(redPen, path);
                    }
                }

                DrawImageIfSelected(g, index, path, rect);
            }
        }

        private GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = radius * 2f;
            SizeF sizeF = new SizeF(diameter, diameter);
            RectangleF arc = new RectangleF(rect.Location, sizeF);

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();

            return path;
        }

        private void DrawImageIfSelected(Graphics g, int index, GraphicsPath path, Rectangle rect)
        {
            if (SelectedMenuFlag != 0 && SelectedMenuFlag != 1 && imagePaths.Count > index)
            {
                try
                {
                    using (Image image = Image.FromFile(imagePaths[index]))
                    {
                        Rectangle destRect = CalculateImageDestinationRect(rect, image);
                        Region originalClip = g.Clip;
                        g.SetClip(path, CombineMode.Replace);
                        g.DrawImage(image, destRect);
                        g.Clip = originalClip;
                    }
                }
                catch (FileNotFoundException)
                {
                    MessageBox.Show($"Image file not found: {imagePaths[index]}");
                }
            }
        }

        private Rectangle CalculateImageDestinationRect(Rectangle rect, Image image)
        {
            float imageAspectRatio = (float)image.Width / image.Height;
            float rectAspectRatio = (float)rect.Width / rect.Height;
            int destWidth, destHeight;

            if (imageAspectRatio > rectAspectRatio)
            {
                destWidth = rect.Width;
                destHeight = (int)(rect.Width / imageAspectRatio);
            }
            else
            {
                destHeight = rect.Height;
                destWidth = (int)(rect.Height * imageAspectRatio);
            }

            int destX = rect.X + (rect.Width - destWidth) / 2;
            int destY = rect.Y + (rect.Height - destHeight) / 2;

            return new Rectangle(destX, destY, destWidth, destHeight);
        }
        public List<Point> generatemenu(int n)
        {
            List<Point> myIcons = new List<Point>();
            int centerX = ClientSize.Width / 2;
            int centerY = ClientSize.Height / 2;
            int radius = Math.Min(ClientSize.Width, ClientSize.Height) / 4;
            double angleIncrement = 360.0 / n;

            for (int i = 0; i < n; i++)
            {
                double angleInRadians = (angleIncrement * i) * (Math.PI / 180);
                int x = (int)(centerX + radius * Math.Cos(angleInRadians) - MenuIconWidth / 2);
                int y = (int)(centerY + radius * Math.Sin(angleInRadians) - MenuIconHeight / 2);
                myIcons.Add(new Point(x, y));
            }

            return myIcons;
        }
        private void check_menu(string prediction)
        {
            
            string pa = @"./Crown Dental APP/2d illustrations/Inlay.png";
            switch (SelectedMenuFlag) // which menu are you're at
            {
                case 0://if you're at the first menu 
                    if (MenuSelectedIndex == 0) //if you select the first option [EXTRACORONAL RESTORRATIONS]
                    {
                        CountMenuItems = 2;
                        SelectedMenuFlag = 1; // index of the new menu you're at

                    }
                    else if (MenuSelectedIndex == 1) //if you select the second option  [Interacrooanl RESTORRATIONS]
                    {

                        CountMenuItems = 1;
                        SelectedMenuFlag = 2;
                        imagePaths = new List<string>{
                                                    @"./Crown Dental APP/2d illustrations/Inlay.png",
                                                        };
                    }
                    break;
                case 1:
                    if (MenuSelectedIndex == 0) //if you select the first option  [FULL COVERGE]
                    {
                        CountMenuItems = 2;
                        SelectedMenuFlag = 3; // index of the new menu you're at
                        imagePaths = new List<string>{
                                                          @"./Crown Dental APP/2d illustrations/All ceramic crown preparation.png",
                                                        @"./Crown Dental APP/2d illustrations/Full veneer crown.png",
                                                        };
                    }
                    else if (MenuSelectedIndex == 1) //if you select the second option [Partial COVERGE]
                    {

                        CountMenuItems = 3;
                        SelectedMenuFlag = 4;
                        imagePaths = new List<string>{
                                                        @"./Crown Dental APP/2d illustrations/Anterior three quarter crown.png",
                                                        @"./Crown Dental APP/2d illustrations/Pin-Modified three quarter crown.png",
                                                        @"./Crown Dental APP/2d illustrations/Seven-eighth Crown.png",
                                                        };
                    }
                    break;

            }
        }
        public void CreateMenu()
        {
            if(menuobjs.Count != 0)
            {
                menuobjs.Clear();
            }
           menuobjs=  CreateMenuObjects(generatemenu(CountMenuItems));
           
        }
        private void Form2_KeyDown(object sender, KeyEventArgs e)
        {
           if(e.KeyValue.Equals(Keys.Escape))
            {
                Close();
            }
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
           
        }
        int cttick = 0;
        private DateTime lastProcessedTime = DateTime.MinValue; // Track last processed command time
        private readonly int cooldownMilliseconds = 500; // Cooldown time in milliseconds

        private string lastProcessedCommand = ""; // Tracks the last processed command

    
        private void ProcessYoloCommands()
        {
            while (yoloCommands.TryDequeue(out string command))
            {
                DateTime now = DateTime.Now;

                // Skip processing repeated commands within cooldown
                if (command == lastProcessedCommand && (now - lastProcessedTime).TotalMilliseconds < cooldownMilliseconds)
                {
                    Console.WriteLine($"Skipping repeated command within cooldown: {command}");
                    continue;
                }

                // Process the command
                  switch (command)
                  {
                      case "Right":
                        CreateMenu();
                        if (flagisMenuShown== 0)
                        {
                           
                            flagisMenuShown = 1;
                        }
                        else
                        {
                          MenuSelectedIndex = (MenuSelectedIndex + 1) % CountMenuItems;
                           

                        }
                        break;

                      case "Up":
                            //act as back option
                          Console.WriteLine("Processing 'Up' command");
                          break;

                      case "Down":
                        //choose menu item
                        if (flagisMenuShown ==1)
                        {
                            check_menu("Down");

                        }
                        Console.WriteLine("Processing 'Down' command");
                          break;

                      case "Left":
                        //navigate left
                        CreateMenu();
                        if (flagisMenuShown == 0)
                        {

                            flagisMenuShown = 1;
                        }
                        else
                        {
                            MenuSelectedIndex = (MenuSelectedIndex - 1) % CountMenuItems;


                        }
                        Console.WriteLine("Processing 'Left' command");
                          break;

                      default:
                          Console.WriteLine($"Unknown command: {command}");
                          break;
                  }
      
                // Update the last processed command and time
                lastProcessedCommand = command;
                lastProcessedTime = now;
            }
        }

        private void T_Tick(object sender, EventArgs e)
        {

            ProcessYoloCommands();
            cttick++;
            DrawDubb(this.CreateGraphics());
        }
       void Form1_Load(object sender, EventArgs e)
        {
            off = new Bitmap(ClientSize.Width, ClientSize.Height);

         

        }
        void Form1_Paint(object sender, PaintEventArgs e)
        {
            DrawDubb(e.Graphics);
        }

        void DrawScene(Graphics g)
        {
            // Fill entire client area
            g.Clear(Color.DarkKhaki);

            // Optional: Draw content that spans the entire screen
            Pen p = new Pen(Color.MintCream, 5);
            Pen pp = new Pen(Color.Black, 1);
            Pen ppp = new Pen(Color.Orange, 1);

            // Example of drawing across entire screen
            int width = ClientSize.Width;
            int height = ClientSize.Height;
            int cornerRadius = 10;
            int padding = 10;
            bool drawTextBelow = true;
            // Set a base font and adjust only once if needed
            Font subFont = new Font("Segoe UI", 16, FontStyle.Bold);
            SolidBrush textBrush = new SolidBrush(Color.Black);

            for (int i = 0; i < menuobjs.Count; i++) // go over each menu item
            {
                Rectangle rect = new Rectangle(menuobjs[i].X, menuobjs[i].Y, menuobjs[i].W, menuobjs[i].H);

                // Draw background of menu items


                // Define text based on menu item
                string itemText = "";
                DrawRoundedRectangle(g, (menuobjs[i].color == 0) ? false : true, rect, cornerRadius, i);
                switch (SelectedMenuFlag)
                {
                    case 0:

                        itemText = (i == 0) ? "Extracoronal \r\n restorations" : "Intracoronal \r\n restorations";
                        drawTextBelow = false;
                        break;
                    case 1:

                        itemText = (i == 0) ? "Full \r\n Coverage" : "Partial \r\n Coverage";
                        drawTextBelow = false;
                        break;
                    case 2:

                        itemText = "Inlay \r\n restoration";
                        break;
                    case 3:

                        itemText = (i == 0) ? "Full \r\n veneer" : "All \r\n Ceramic";
                        break;
                    case 4:

                        if (i == 0)
                        {
                            itemText = "Three Quarter";
                        }
                        else if (i == 1)
                        {
                            itemText = "Seven Eighth";
                        }
                        else
                        {
                            itemText = "Pin Moidified";

                        }
                        break;
                    default:
                        //DrawRoundedRectangle(g, (menuobjs[i].color == 0) ? MenuItemBrush : SelectedItemBrush, rect, cornerRadius, i);
                        break;
                }



                // Adjust the text position based on drawTextBelow flag
                StringFormat format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                if (drawTextBelow)
                {
                    // Draw text below the rectangle
                    Rectangle textRect = new Rectangle(
                        rect.X,
                        rect.Bottom - padding, // Position below the menu item with padding
                        rect.Width,
                        rect.Height // Set height as per need for text
                    );
                    Font BelowFont = new Font("Segoe UI", 26, FontStyle.Bold);

                    g.DrawString(itemText, BelowFont, textBrush, textRect, format);
                }
                else
                {
                    // Draw text inside the rectangle
                    g.DrawString(itemText, subFont, textBrush, rect, format);
                }
            }

        }


        void DrawDubb(Graphics g)
        {
            Graphics g2 = Graphics.FromImage(off);
            DrawScene(g2);
            g.DrawImage(off, 0, 0);
        }

    }

   
    
}
