/*
	TUIO C# Demo - part of the reacTIVision project
	Copyright (c) 2005-2016 Martin Kaltenbrunner <martin@tuio.org>

	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*/

using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using System.Diagnostics;

using System.Threading;
using TUIO;
using System.IO;
using NAudio.Wave;
using System.Drawing.Drawing2D;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
//using Newtonsoft.Json;
using System.Windows.Forms.Integration;
using WpfApp1;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using System.Collections.Concurrent;
using System.Media;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Contexts;

public class TuioDemo : Form, TuioListener
{
    private TcpClient serverClient;
    private NetworkStream serverStream;
    private string serverHost = "127.0.0.1";
    private int serverPort = 65432;


    private TuioClient client;
    private Dictionary<long, TuioObject> objectList;
    private Dictionary<long, TuioCursor> cursorList;
    private Dictionary<long, TuioBlob> blobList;
    private AxisAngleRotation3D modelRotationAngle;
    private RotateTransform3D modelRotation;
    private bool isDelayActive = false;

    public static int width, height, selectedIndex = -1;
    private int window_width = Screen.PrimaryScreen.Bounds.Width;
    private int window_height = Screen.PrimaryScreen.Bounds.Height;
    private int window_left = 0;
    private int window_top = 0;
    private int screen_width = Screen.PrimaryScreen.Bounds.Width;
    private int screen_height = Screen.PrimaryScreen.Bounds.Height;


    private bool fullscreen;
    private bool verbose;
    private bool isAdmin = false;

    private Image adminImage = Image.FromFile(@"admin.png");
    private Image backgroundImage = Image.FromFile(@"BG_3.jpg");
    private Image backgroundImage2 = Image.FromFile(@"bg.jpg");
    Font font = new Font("Times New Roman", 30.0f);
    SolidBrush fntBrush = new SolidBrush(Color.Black);
    SolidBrush bgrBrush = new SolidBrush(Color.FromArgb(255, 255, 64));
    SolidBrush curBrush = new SolidBrush(Color.FromArgb(192, 0, 192));
    SolidBrush SelectedItemBrush = new SolidBrush(Color.SeaGreen);
    SolidBrush MenuItemBrush = new SolidBrush(Color.White);
    SolidBrush objBrush = new SolidBrush(Color.Silver);
    SolidBrush blbBrush = new SolidBrush(Color.FromArgb(64, 64, 64));
    Pen curPen = new Pen(new SolidBrush(Color.Blue), 1);
    string message = string.Empty;
    private bool hand_gesture = false;

    List<Point> mymenupoints = new List<Point>();


    //elementHost1.Child = viewerControl;

    Bitmap off;


    // --------------------- BEGIN JOHN WORK ---------------------
    // Fields for emotion server
    private TcpClient emotionClient;
    private Thread emotionClientThread;
    private bool emotionClientConnected = false;
    private Label lblEmotionStatus;
    private Label lblEmotion;
    // --------------------- END JOHN WORK ---------------------
    // --------------------- START AKL WORK ---------------------
    List<int> context  = new List<int>();
    List<String> devices = new List<String>();
    string progress = "";
    string device_name = "";
    string device_mac = "";
    string user_role = "";
    string Action = "";
    void HandleClient(Socket clientSocket)
    {
        try
        {
            Console.WriteLine("Client connected.");

            // Buffer to store the received data
            byte[] buffer = new byte[1024];
            int receivedBytes;

            // Loop to handle communication
            while ((receivedBytes = clientSocket.Receive(buffer)) > 0)
            {
                // Decode message from client
                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                string[] parts = receivedMessage.Split(',');
                string action = parts[0];
                //List<string> restOfParts = parts.Skip(1).ToList();
                switch (action)
                {
                    case "devices":
                        devices = parts.Skip(1).ToList();
                        break;
                    case "logged":
                        device_name = parts[1];
                        device_mac = parts[2];
                        user_role = parts[3];
                        progress = parts[4];
                        mainmenuflag = checkmainmenu();
                        if (mainmenuflag == 2)
                        {
                            this.Controls.Remove(mainMenuButton);
                            this.mainMenuButton.Dispose();
                        }
                        break;
                }
                Console.WriteLine($"Received: {receivedMessage}");

                // Example processing: Echo the message back to the client
                string response = $"Server received: {receivedMessage}";
                clientSocket.Send(Encoding.UTF8.GetBytes(response));
                Console.WriteLine($"Response sent: {response}");
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"SocketException: {ex.Message}");
        }
        finally
        {
            clientSocket.Close();
            Console.WriteLine("Client disconnected.");
        }
    }
    void in_Main(string[] args)
    {
        // Server configuration
        string ipAddress = "127.0.0.1"; // Localhost
        int port = 5000;

        // Create a TCP socket
        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            // Bind the socket to the IP and port
            serverSocket.Bind(new IPEndPoint(IPAddress.Parse(ipAddress), port));
            serverSocket.Listen(5); // Max pending connections

            Console.WriteLine($"Server started on {ipAddress}:{port}");
            Console.WriteLine("Waiting for connections...");

            // Accept and handle clients in a loop
            while (true)
            {
                Socket clientSocket = serverSocket.Accept();
                Thread clientThread = new Thread(() => HandleClient(clientSocket));
                clientThread.Start();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
        }
        finally
        {
            serverSocket.Close();
            Console.WriteLine("Server stopped.");
        }
    }

    private void TestSocketConnection()
    {
        string serverIp = "127.0.0.1";
        int serverPort = 65434;

        while (true)
        {
            try
            {
                Console.WriteLine("Attempting to connect to server...");
                using (TcpClient client = new TcpClient(serverIp, serverPort))
                using (NetworkStream stream = client.GetStream())
                {
                    Console.WriteLine("Connected to server!");

                    while (true)
                    {
                        Console.Write("Enter message to send (type 'exit' to close): ");
                        //string message = Console.ReadLine();

                        String message = "test";
                        if (string.IsNullOrWhiteSpace(message))
                            continue;

                        if (message.ToLower() == "exit")
                        {
                            Console.WriteLine("Closing connection...");
                            break;
                        }
                        // Send message
                        byte[] data = Encoding.UTF8.GetBytes(message);
                        stream.Write(data, 0, data.Length);
                        Console.WriteLine($"Sent: {message}");

                        // Receive response
                        byte[] buffer = new byte[1024];
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        string[] parts = response.Split(',');
                        string action = parts[0];
                        //List<string> restOfParts = parts.Skip(1).ToList();
                        switch (action)
                        {
                            case "devices":
                                devices = parts.Skip(1).ToList();
                                Console.WriteLine($"Sent: {devices[0]}");
                                break;
                            case "logged":
                                device_name = parts[1];
                                device_mac = parts[2];
                                user_role = parts[3];
                                progress = parts[4];
                                mainmenuflag = checkmainmenu();
                                if (mainmenuflag == 2)
                                {
                                    this.Controls.Remove(mainMenuButton);
                                    this.mainMenuButton.Dispose();
                                }
                                break;
                        }
                        Console.WriteLine($"Received: {response}");
                        Thread.Sleep(5000);
                    }
                    break; // Exit loop if connection was successful
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("Retrying in 5 seconds...");
                Thread.Sleep(5000); // Wait before retrying
            }
        }
    }





    // --------------------- END AKL WORK ---------------------


    public class CActor
    {
        //public int X, Y;
        public Rectangle rcDst;
        public int rowid, colid;
        public Rectangle rcSrc;
        public Bitmap img;
        public int color = 0;
        public int X, Y, W, H;

    }
    private ElementHost wpfHost;
    private dent3DviewerController viewerControl;
    public TuioDemo(int port)
    {

        verbose = true;
        fullscreen = false;
        width = window_width;
        height = window_height;

        this.ClientSize = new System.Drawing.Size(width, height);
        this.Name = "Crown Preparation Application";
        this.Name = "Crown Preparations Interactive App";
        //Button button = new Button();
        //button.Text = "START";
        //button.FlatAppearance.Equals(FlatStyle.Flat);
        //button.Location.X.Equals(this.ClientSize.Width / 2);
        //button.Location.Y.Equals(this.ClientSize.Height / 2);
        //button.Visible = true;
        //button.Enabled = true;

        this.Load += TuioDemo_Load;
        this.Closing += new CancelEventHandler(Form_Closing);
        this.KeyDown += new KeyEventHandler(Form_KeyDown);


        this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                        ControlStyles.UserPaint |
                        ControlStyles.DoubleBuffer, true);

        objectList = new Dictionary<long, TuioObject>(128);
        cursorList = new Dictionary<long, TuioCursor>(128);
        blobList = new Dictionary<long, TuioBlob>(128);
        //Task.Run(async () => await RunPythonScriptAsync(@"s"));

        client = new TuioClient(port);
        client.addTuioListener(this);

        client.connect();
        Task.Run(async () => await ReceivePredictionsAsync()); // need to be called
        // Start the YOLO thread
        //ConnectToServer();

        //StartYoloThread();
    }
    private void ConnectToServer()
    {
        try
        {
            serverClient = new TcpClient(serverHost, serverPort);
            serverStream = serverClient.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

            StringBuilder completeMessage = new StringBuilder();
            while ((bytesRead = serverStream.Read(buffer, 0, buffer.Length)) != 0)  // Using Read instead of ReadAsync
            {
                completeMessage.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
            }

            // Update UI with received data
            //if (InvokeRequired)
            //{
            //    Invoke(new MethodInvoker(delegate {
            //        MessageBox.Show("Received data:\n" + completeMessage.ToString());
            //    }));
            //}
            //else
            //{
            //    MessageBox.Show("Received data:\n" + completeMessage.ToString());
            //}
            String type = completeMessage.ToString();
            if (type.StartsWith("admin"))
            {
                // Extract the device name from the completeMessage
                string deviceName = type.Substring(6).Trim('\'');
                message = $"Welcome, Admin: {deviceName}";
                isAdmin = true;
            }
            else if (type.StartsWith("login"))
            {
                // Extract the device name from the completeMessage
                string deviceName = type.Substring(6).Trim('\'');
                message = $"welcome back, {deviceName}! You are logged in.";
            }
            else if (type.StartsWith("signup"))
            {
                // Extract the device name from the completeMessage
                string deviceName = type.Substring(7).Trim('\'');
                message = $"Hello, {deviceName}!";
            }
            else
            {
                message = "Unrecognized user type.";
            }
        }
        catch (SocketException e)
        {
            //MessageBox.Show("SocketException: " + e.Message);
        }
    }

    private object viewerLock = new object(); // Ensure thread safety
    private System.Windows.Window viewerWindow;
    private Thread viewerThread;

    private void Initialize3DViewer(string file_path, string image_path)
    {
        Console.WriteLine($"File path is {file_path} and the image path is {image_path}");
        lock (viewerLock)
        {
            if (viewerWindow != null)
            {
                viewerWindow.Dispatcher.Invoke(() =>
                {
                    // Instead of closing the window, update the existing viewer control
                    viewerControl.UpdateRowSource(file_path, image_path);
                });
            }
            else
            {
                if (viewerThread != null && viewerThread.IsAlive)
                {
                    viewerThread.Abort(); // Abort the previous thread if it's still running
                    viewerThread.Join();  // Wait for the thread to finish
                }

                // Create a new thread only if the window does not exist
                viewerThread = new Thread(() =>
                {
                    viewerControl = new WpfApp1.dent3DviewerController(file_path, image_path, 0);

                    viewerWindow = new System.Windows.Window
                    {
                        Title = "3D Viewer",
                        Content = viewerControl,
                        WindowState = System.Windows.WindowState.Maximized
                    };

                    viewerWindow.Closed += (sender, args) =>
                    {
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown();
                        FlagExecuted = 0;
                    };

                    viewerWindow.Show();
                    System.Windows.Threading.Dispatcher.Run();
                });

                viewerThread.SetApartmentState(ApartmentState.STA);
                viewerThread.IsBackground = true;
                viewerThread.Start();
            }
        }
    }

    async Task RunPythonScriptAsync(string scriptPath)
    {
        try
        {
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = @"C:\Users\Administrator\AppData\Local\Programs\Python\Python312\python.exe", // Ensure python is in PATH or specify full path
                Arguments = $"\"{@"C:\Users\Administrator\source\repos\Interactive-Dental-Application\test2.py"}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(start))
            {
                // Wrap synchronous call in Task to prevent UI blocking
                await Task.Run(() => process.WaitForExit());

                string output = await Task.Run(() => process.StandardOutput.ReadToEnd());
                string error = await Task.Run(() => process.StandardError.ReadToEnd());
                if (!string.IsNullOrEmpty(output))
                {
                    Console.WriteLine("Python Output: " + output);
                }
                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine("Python Error: " + error);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to run python script: " + ex.Message);
        }
        await ReceivePredictionsAsync();
    }


    private void TuioDemo_Load(object sender, EventArgs e)
    {
        context.Add(0);
        context.Add(0);
        context.Add(0);
        context.Add(0);
        context.Add(0);
        context.Add(0);

        /*        string audiofilepath = ("01 - Track 01.mp3");
                PlayBackgroundMusic(audiofilepath);*/
        off = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);
        //Task.Run(async () => await ReceivePredictionsAsync());
        this.InitializeComponent();
        //Initialize3DViewer(@"C:\Users\Administrator\source\repos\Interactive-Dental-Application\TUIO Folder\WpfApp1\obj\Debug\Seven-eighth crown preparation.stl", @"C:\Users\Administrator\source\repos\Interactive-Dental-Application\TUIO Folder\WpfApp1\obj\Debug\Seven-eighth Crown.png");

        // --------------------- BEGIN JOHN WORK ---------------------
        InitializeEmotionComponents(); // Initialize emotion UI
        ConnectToEmotionServer();     // Connect to the emotion server
        // --------------------- END JOHN WORK ---------------------
        Thread socketTestThread = new Thread(TestSocketConnection)
        {
            IsBackground = true
        };
        socketTestThread.Start();
    }

    private void Form_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
    {

        if (e.KeyData == Keys.F1)
        {
            if (fullscreen == false)
            {

                width = screen_width;
                height = screen_height;

                window_left = this.Left;
                window_top = this.Top;

                this.FormBorderStyle = FormBorderStyle.None;
                this.Left = 0;
                this.Top = 0;
                this.Width = screen_width;
                this.Height = screen_height;

                fullscreen = true;
            }
            else
            {

                width = window_width;
                height = window_height;

                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.Left = window_left;
                this.Top = window_top;
                this.Width = window_width;
                this.Height = window_height;

                fullscreen = false;
            }
        }
        else if (e.KeyData == Keys.Escape)
        {
            this.Close();

        }
        else if (e.KeyData == Keys.V)
        {
            verbose = !verbose;
        }

    }

    private void Form_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        client.removeTuioListener(this);

        client.disconnect();

        // --------------------- BEGIN JOHN WORK ---------------------
        DisconnectEmotionClient(); // Ensure the emotion client disconnects
        // --------------------- END JOHN WORK ---------------------

        System.Environment.Exit(0);
    }




    // --------------------- BEGIN JOHN WORK ---------------------
    // Initialize emotion UI components
    private void InitializeEmotionComponents()
    {
        //lblEmotionStatus = new Label
        //{
        //    Location = new System.Drawing.Point(20, 30),
        //    Size = new System.Drawing.Size(360, 30),
        //    Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular),
        //    ForeColor = System.Drawing.Color.White,
        //    Text = "Emotion Status: Disconnected",
        //    BackColor = System.Drawing.Color.Transparent,
        //    TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        //};
        //this.Controls.Add(lblEmotionStatus);

        //lblEmotion = new Label
        //{
        //    Location = new System.Drawing.Point(20, 70),
        //    Size = new System.Drawing.Size(360, 30),
        //    Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular),
        //    ForeColor = System.Drawing.Color.White,
        //    Text = "Emotion: None",
        //    BackColor = System.Drawing.Color.Transparent,
        //    TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        //};
        //this.Controls.Add(lblEmotion);
    }

    private void ConnectToEmotionServer()
    {
        emotionClientThread = new Thread(() =>
        {
            try
            {
                emotionClient = new TcpClient("127.0.0.1", 5000);
                emotionClientConnected = true;
                UpdateEmotionStatus("Connected to the Emotion Server!");

                NetworkStream stream = emotionClient.GetStream();
                byte[] buffer = new byte[1024];

                while (emotionClientConnected)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        UpdateEmotionStatus("Connection closed by the Emotion server.");
                        break;
                    }

                    string receivedMessage = string.Empty; // Initialize as an empty string to avoid null
                    receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    UpdateEmotionLabel(receivedMessage);

                }
            }
            catch (Exception ex)
            {
                UpdateEmotionStatus($"Emotion Error: {ex.Message}");
            }
            finally
            {
                DisconnectEmotionClient();
            }
        })
        {
            IsBackground = true
        };
        emotionClientThread.Start();
    }

    private void UpdateEmotionStatus(string message)
    {
        //if (lblEmotionStatus.InvokeRequired)
        //{
        //    lblEmotionStatus.Invoke(new Action(() => lblEmotionStatus.Text = message));
        //}
        //else
        //{
        //    lblEmotionStatus.Text = message;
        //}
    }

    private void UpdateEmotionLabel(string emotion)
    {
        if (lblEmotion == null)
        {
            lblEmotion = new Label(); 
            lblEmotion.Text = string.Empty;
        }

        if (lblEmotion.InvokeRequired)
        {
            lblEmotion.Invoke(new Action(() => lblEmotion.Text = $"Emotion: {emotion}"));
        }
        else
        {
            lblEmotion.Text = $"Emotion: {emotion}";
        }
    }

    private void DisconnectEmotionClient()
    {
        emotionClientConnected = false;
        emotionClient?.Close();
        UpdateEmotionStatus("Disconnected from the Emotion Server.");
    }
    // --------------------- END JOHN WORK ---------------------


    public void addTuioObject(TuioObject o)
    {
        lock (objectList)
        {
            objectList.Add(o.SessionID, o);
        }
        if (verbose) Console.WriteLine("add obj " + o.SymbolID + " (" + o.SessionID + ") " + o.X + " " + o.Y + " " + o.Angle);
    }
    public AudioFileReader soundEffect;
    public WaveOutEvent soundEffectOutput;

    public async void PlaySoundEffect(string audioFilePath)
    {
        // Introduce a delay of 1 second before playing the sound
        await Task.Delay(0);

        // Initialize sound effect for playback
        soundEffect = new AudioFileReader(audioFilePath);
        soundEffectOutput = new WaveOutEvent
        {
            Volume = 0.8f // Set volume if needed
        };
        soundEffectOutput.Init(soundEffect);

        // Play the sound
        soundEffectOutput.Play();

        // Wait for the sound to finish playing
        while (soundEffectOutput.PlaybackState == PlaybackState.Playing)
        {
            await System.Threading.Tasks.Task.Delay(10); // Check playback state every 100 milliseconds
        }
    }

    public int menuMarker = 15;
    private bool hasPlayedSound = false;
    // Declare a threshold for rotation change
    private double rotationThreshold = 10f; // Example: 15-degree change
    private double previousRotationAngle = 0f;

    private bool markerVisible = false; // Track visibility state

    public void checkrotation(List<CActor> objs, Graphics g, TuioObject o)
    {
        if (o.SymbolID == menuMarker) // Assuming marker with SymbolID controls the menu
        {
            bool isNewMarker = !markerVisible; // Check if this is a new marker appearance
            markerVisible = true; // Update marker visibility

            // Convert the angle to degrees
            double angleDegrees = o.Angle * 180.0 / Math.PI;
            // Normalize the angle to be within 0 to 360 degrees
            if (angleDegrees < 0) angleDegrees += 360;
            // Reverse the angle direction for correct item selection
            angleDegrees = 360 - angleDegrees;

            // Calculate the rotation difference
            double rotationDifference = Math.Abs(angleDegrees - previousRotationAngle);

            // Introduce a small threshold to avoid minor rotations
            const double rotationThreshold = 5.0; // Adjust this value as needed

            // Update only if the rotation difference is greater than the threshold

            previousRotationAngle = angleDegrees;

            // Divide the full circle (360 degrees) into equal sections for each menu item
            double anglePerItem = 360.0 / CountMenuItems;
            // Calculate which menu item should be selected
            int newMenuIndex = (int)Math.Floor(angleDegrees / anglePerItem) % CountMenuItems;
            SoundPlayer player = new SoundPlayer("menusound_swipe.wav");
            // Update the menu selection only if the new index is different from the current one
            if (newMenuIndex != MenuSelectedIndex)
            {
                // Deselect previous menu item
                if (MenuSelectedIndex >= 0 && MenuSelectedIndex < CountMenuItems)
                {
                    MenuObjs[MenuSelectedIndex].color = 0; // Deselect previous menu item
                }
                // Select new menu item
                if (newMenuIndex >= 0 && newMenuIndex < CountMenuItems)
                {
                    MenuObjs[newMenuIndex].color = 1; // Select new menu item
                }
                if (rotationDifference > rotationThreshold)
                {
                    // Play sound effect
                    //layer.Play();
                }
                if (rotationDifference < -rotationThreshold)
                {
                    // Play sound effect
                    //player.Play();
                }
                // Update the selected menu index
                MenuSelectedIndex = newMenuIndex;

                // Trigger a repaint with the updated menu
                Invalidate();
            }

        }
        else
        {
            // Marker is no longer visible; you can set it to false to track disappearance
            markerVisible = false;
        }
    }

    // Call this method when detecting the marker's disappearance
    public void HandleMarkerDisappearance(TuioObject o)
    {
        if (o.SymbolID == menuMarker)
        {
            markerVisible = false; // Reset visibility tracking
        }
    }

    public void updateTuioObject(TuioObject o)
    {

        // Existing verbose logging for other object data
        if (verbose)
        {
            //Console.WriteLine("set obj " + o.SymbolID + " (" + o.SessionID + ") " + o.X + " " + o.Y + " " + o.Angle + " " + o.MotionSpeed + " " + o.RotationSpeed + " " + o.MotionAccel + " " + o.RotationAccel);
        }
    }

    /* public Graphics drawmenu(List<CActor> menuobjs, Graphics g)
     {
         int cornerRadius = 10;
         for (int i = 0; i < menuobjs.Count; i++)
         {
             Rectangle rect = new Rectangle(menuobjs[i].X, menuobjs[i].Y, menuobjs[i].W, menuobjs[i].H);
             if (menuobjs[i].color == 0)
             {
                 DrawRoundedRectangle(g, MenuItemBrush, rect, cornerRadius);
             }
             else
             {
                 DrawRoundedRectangle(g, SelectedItemBrush, rect, cornerRadius);
             }

         }
         return g;
     }
 */
    public void removeTuioObject(TuioObject o)
    {
        lock (objectList)
        {
            objectList.Remove(o.SessionID);
        }
        //if (verbose) //Console.WriteLine("del obj " + o.SymbolID + " (" + o.SessionID + ")");
    }

    public void addTuioCursor(TuioCursor c)
    {
        lock (cursorList)
        {
            cursorList.Add(c.SessionID, c);
        }
        //if (verbose) Console.WriteLine("add cur " + c.CursorID + " (" + c.SessionID + ") " + c.X + " " + c.Y);
    }

    public void updateTuioCursor(TuioCursor c)
    {
        if (verbose) Console.WriteLine("set cur " + c.CursorID + " (" + c.SessionID + ") " + c.X + " " + c.Y + " " + c.MotionSpeed + " " + c.MotionAccel);
    }

    public void removeTuioCursor(TuioCursor c)
    {
        lock (cursorList)
        {
            cursorList.Remove(c.SessionID);
        }
        if (verbose) Console.WriteLine("del cur " + c.CursorID + " (" + c.SessionID + ")");
    }

    public void addTuioBlob(TuioBlob b)
    {
        lock (blobList)
        {
            blobList.Add(b.SessionID, b);
        }
        if (verbose) Console.WriteLine("add blb " + b.BlobID + " (" + b.SessionID + ") " + b.X + " " + b.Y + " " + b.Angle + " " + b.Width + " " + b.Height + " " + b.Area);
    }

    public void updateTuioBlob(TuioBlob b)
    {

        if (verbose) Console.WriteLine("set blb " + b.BlobID + " (" + b.SessionID + ") " + b.X + " " + b.Y + " " + b.Angle + " " + b.Width + " " + b.Height + " " + b.Area + " " + b.MotionSpeed + " " + b.RotationSpeed + " " + b.MotionAccel + " " + b.RotationAccel);
    }

    public void removeTuioBlob(TuioBlob b)
    {
        lock (blobList)
        {
            blobList.Remove(b.SessionID);
        }
        if (verbose) Console.WriteLine("del blb " + b.BlobID + " (" + b.SessionID + ")");
    }
    public static List<string> imagePaths = new List<string>();
    public int SelectedMenuFlag = 0;
    public int save_selectedMenuFlag = 0;
    public void refresh(TuioTime frameTime)
    {
        Invalidate();
    }
    public int MenuIconWidth = 100;
    public int MenuIconHeight = 150;
    public int CountMenuItems = 2;
    public int MenuSelectedIndex = 0; //item selection
    public int FlagExecuted = 0;
    List<CActor> MenuObjs = new List<CActor>();
    public void DrawRoundedRectangle(Graphics g, int isSelected, Rectangle rect, int radius, int index)
    {
        using (GraphicsPath path = CreateRoundedRectanglePath(rect, radius))
        {
            g.FillPath(MenuItemBrush, path);
            // Draw the rounded rectangle background
            if (isSelected == 1)
            {
                using (Pen redPen = new Pen(Color.Red, 5)) // Adjust thickness as needed
                {
                    g.DrawPath(redPen, path);
                }
            }
            if (isSelected == 2)
            {
                using (Pen redPen = new Pen(Color.Blue, 5)) // Adjust thickness as needed
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

    public void RefreshMenu()
    {
        Invalidate();
    }

    public List<Point> generatemenu(int n, int ind = -1)
    {
        MenuSelectedIndex = (0 > ind) ? n - 1 : ind;
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

    public Graphics drawmenu(List<CActor> menuobjs, Graphics g)
    {
        //FlagExecuted = 0; // need to be moved to block where python file opens
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
            switch (SelectedMenuFlag)
            {
                case 0:

                    itemText = (i == 0) ? "Extracoronal \r\n restorations" : "Intracoronal \r\n restorations";
                    if(i==0)
                    {
                        menuobjs[i].color = (menuobjs[i].color != 1 && context[1] == 1 && context[2] == 1 && context[3] == 1 && context[4] == 1 && context[5] == 1) ? 2 : menuobjs[i].color;
                    }
                    else
                    {
                        menuobjs[i].color = (menuobjs[i].color != 1 && context[0]==1) ? 2 : menuobjs[i].color;
                    }
                    drawTextBelow = false;
                    break;
                case 1:

                    itemText = (i == 0) ? "Full \r\n Coverage" : "Partial \r\n Coverage";
                    if (i == 0)
                    {
                        menuobjs[i].color = (menuobjs[i].color != 1 && context[1] == 1 && context[2] == 1) ? 2 : menuobjs[i].color;
                    }
                    else
                    {
                        menuobjs[i].color = (menuobjs[i].color != 1 && context[3] == 1 && context[4] == 1 && context[5] == 1) ? 2 : menuobjs[i].color;
                    }
                    drawTextBelow = false;
                    break;
                case 2:

                    itemText = "Inlay \r\n restoration";
                    menuobjs[i].color = (context[0] == 1) ? 2 : menuobjs[i].color;
                    break;
                case 3:

                    itemText = (i == 0) ? "All \r\n Ceramic" : "Full \r\n veneer";
                    if (i == 0)
                    {
                        menuobjs[i].color = (menuobjs[i].color != 1 && context[1] == 1) ? 2 : menuobjs[i].color;
                    }
                    else
                    {
                        menuobjs[i].color = (menuobjs[i].color != 1 && context[2] == 1) ? 2 : menuobjs[i].color;
                    }
                    break;
                case 4:

                    if (i == 0)
                    {
                        itemText = "Three Quarter";
                        menuobjs[i].color = (menuobjs[i].color != 1 && context[3] == 1) ? 2 : menuobjs[i].color;
                    }
                    else if (i == 1)
                    {

                        itemText = "Pin Modified";
                        menuobjs[i].color = (menuobjs[i].color != 1 && context[4] == 1) ? 2 : menuobjs[i].color;
                    }
                    else
                    {
                        itemText = "Seven Eighth";
                        menuobjs[i].color = (menuobjs[i].color != 1 && context[5] == 1) ? 2 : menuobjs[i].color;
                    }
                    break;
                default:
                    //DrawRoundedRectangle(g, (menuobjs[i].color == 0) ? MenuItemBrush : SelectedItemBrush, rect, cornerRadius, i);
                    break;
            }
            DrawRoundedRectangle(g, menuobjs[i].color, rect, cornerRadius, i);



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

        return g;
    }

    private void check_menu()
    {
        string pa = @"C:\Users\Administrator\source\repos\Interactive-Dental-Application\TUIO Folder\WpfApp1\obj\Debug\All ceramic crown preparation.png";
        save_selectedMenuFlag = SelectedMenuFlag;
        Console.WriteLine($"Flag executed: {FlagExecuted}");
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
                ActivateDelay();
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
                ActivateDelay();
                break;
            case 2:
                if (FlagExecuted == 0)
                {
                    //"C:\Users\Administrator\source\repos\Interactive-Dental-Application\TUIO Folder\TUIO11_NET\bin\Debug\Crown Dental APP\2d illustrations\Anterior three quarter crown.png"
                    Initialize3DViewer(@"./3D_viewer/Inlay.stl", @"./Crown Dental APP/2d illustrations/Inlay.png");
                    FlagExecuted = 1;
                    context[0] = 1;
                }
                break;
            case 3:
                if (MenuSelectedIndex == 0 && FlagExecuted == 0)
                {
                    Initialize3DViewer(@"./3D_viewer/All ceramic crown preparation.stl", @"./Crown Dental APP/2d illustrations/All ceramic crown preparation.png");
                    FlagExecuted = 1;
                    context[1] = 1;
                }
                else if (MenuSelectedIndex == 1 && FlagExecuted == 0)
                {
                    Initialize3DViewer(@"./3D_viewer/Full veneer crown preparation.stl", @"./Crown Dental APP/2d illustrations/Full veneer crown.png");
                    FlagExecuted = 1;
                    context[2] = 1;
                }
                break;
            case 4:
                if (MenuSelectedIndex == 0 && FlagExecuted == 0)
                {
                    Initialize3DViewer(@"./3D_viewer/Anterior Three quarter crown preparation.stl", @"./Crown Dental APP/2d illustrations/Anterior three quarter crown.png");
                    FlagExecuted = 1;
                    context[3] = 1;
                }
                else if (MenuSelectedIndex == 1 && FlagExecuted == 0)
                {
                    Initialize3DViewer(@"./3D_viewer/Pin modified three-quarter crown preparation.stl", @"./Crown Dental APP/2d illustrations/Pin-Modified three quarter crown.png");
                    FlagExecuted = 1;
                    context[4] = 1;
                }
                else if (MenuSelectedIndex == 2 && FlagExecuted == 0)
                {
                    Initialize3DViewer(@"./3D_viewer/Seven-eighth crown preparation.stl", @"./Crown Dental APP/2d illustrations/Seven-eighth Crown.png");
                    FlagExecuted = 1;
                    context[5] = 1;
                }
                break;
        }
    }

    private void TuioDemo_Paint(object sender, PaintEventArgs e)
    {
        DrawDubb(this.CreateGraphics());
    }


    private void check_selection()
    {
        Console.WriteLine($"Selected menu flag = {SelectedMenuFlag}");
        Console.WriteLine($"Menu selected index = {MenuSelectedIndex}");
        switch (SelectedMenuFlag) // which menu are you're at
        {
            case 0://if you're at the first menu 

                break;
            case 1:
                CountMenuItems = 2;
                SelectedMenuFlag = 0;
                imagePaths = new List<string>{
                                                          @"./Crown Dental APP/2d illustrations/All ceramic crown preparation.png",
                                                        @"./Crown Dental APP/2d illustrations/Full veneer crown.png",
                                                        };
                ActivateDelay();
                break;
            case 2:
                CountMenuItems = 2;
                SelectedMenuFlag = 0;
                ActivateDelay();
                break;
            case 3:
                CountMenuItems = 2;
                SelectedMenuFlag = 1;
                ActivateDelay();
                break;
            case 4:
                CountMenuItems = 2;
                SelectedMenuFlag = 1;
                ActivateDelay();
                break;
        }
    }
    private bool flagFirst = false;
    private async Task ReceivePredictionsAsync()
    {
        //hand_gesture = true;
        try
        {
            //hand_gesture flag need to be set to 1 when opening python server
            Console.WriteLine("Trying to connect");
            using (TcpClient client = new TcpClient("localhost", 65434))
            {
                client.ReceiveTimeout = 2000;
                client.SendTimeout = 2000;

                using (NetworkStream stream = client.GetStream())
                {
                    byte[] dataToReceive = new byte[4096];
                    int bytesRead;

                    while ((bytesRead = await stream.ReadAsync(dataToReceive, 0, dataToReceive.Length)) != 0)
                    {
                        // Convert received bytes to string
                        string responseData = Encoding.ASCII.GetString(dataToReceive, 0, bytesRead);
                        Console.WriteLine("Received Prediction: " + responseData);
                        if (!flagFirst && hand_gesture)
                        {
                            SelectedMenuFlag = 0;
                            MenuSelectedIndex = 0;
                            mainmenuflag = 2;
                            flagFirst = true;

                        }
                        if (responseData == "Swipe up" && FlagExecuted == 1)
                        {
                            viewerControl.ChangeBasedOnCommand("Swipe up");
                        }
                        if (responseData == "Swipe Right" && FlagExecuted == 1)
                        {
                            viewerControl.ChangeBasedOnCommand("Swipe right");
                        }
                        if (responseData == "Swipe Right")
                        {
                            Console.WriteLine($"before menu selected index is {MenuSelectedIndex} and Count items is {CountMenuItems} right");
                            if (MenuSelectedIndex < CountMenuItems - 1)
                            {
                                MenuSelectedIndex++;
                            }
                            else
                            {
                                MenuSelectedIndex = 0;
                            }
                            Console.WriteLine($"after menu selected index is {MenuSelectedIndex} Count items is {CountMenuItems} right");
                        }
                        else if (responseData == "Swipe Left" && FlagExecuted == 1)
                        {
                            viewerControl.ChangeBasedOnCommand("Swipe left");
                        }
                        if (responseData == "Swipe Left")
                        {
                            Console.WriteLine($"before menu selected index is {MenuSelectedIndex} Count items is {CountMenuItems} left");
                            if (MenuSelectedIndex > 0)
                            {
                                MenuSelectedIndex--;
                            }
                            else
                            {
                                MenuSelectedIndex = CountMenuItems - 1;
                            }
                            Console.WriteLine($"after menu selected index is {MenuSelectedIndex} Count items is {CountMenuItems} left");
                        }
                        else if (responseData == "Zoom In" && FlagExecuted == 1)
                        {
                            viewerControl.ChangeBasedOnCommand("Zoom in");
                        }
                        else if (responseData == "Zoom out" && FlagExecuted == 1)
                        {
                            viewerControl.ChangeBasedOnCommand("Zoom out");
                        }
                        else if (responseData == "Select")
                        {
                            FlagExecuted = 0;
                            check_menu();
                        }
                        else if (responseData == "Back")
                        {
                            back();
                        }
                        using (Graphics g = this.CreateGraphics())
                        {
                            DrawDubb(g);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
    public int mainmenuflag = 1;

    public void back()
    {
        if (!hand_gesture)
        {
            FlagExecuted = 0;
        }
        Console.WriteLine(FlagExecuted);
        switch (SelectedMenuFlag)
        {
            case 1:
                SelectedMenuFlag = 0;
                break;
            case 2:

                SelectedMenuFlag = 0;
                CountMenuItems = 2;
                break;
            case 3:
                SelectedMenuFlag = 1;
                CountMenuItems = 2;
                break;
            case 4:
                SelectedMenuFlag = 1;
                CountMenuItems = 2;
                break;

        }
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        Graphics g = pevent.Graphics;
        //g.FillRectangle(bgrBrush, new Rectangle(0, 0, width, height));
        g.Clear(Color.WhiteSmoke);
        ////////////////////////////////////////////////////
        ///////////////////////
        // Define a rectangle for the title text
        Font titleFont = new Font("Segoe UI", 35, FontStyle.Bold);
        SolidBrush textBrush = new SolidBrush(Color.Black);

        // Define the dimensions and position for the semi-transparent rounded rectangle
        int boxWidth = 800; // Adjust width as needed
        int boxHeight = 300; // Adjust height as needed
        int boxX = (this.window_width / 2) - (boxWidth / 2);
        int boxY = this.ClientRectangle.Top + 20;

        // Create a semi-transparent white brush
        SolidBrush boxBrush = new SolidBrush(Color.FromArgb(150, Color.White));

        // Draw the rounded rectangle
        GraphicsPath roundedRectPath = new GraphicsPath();
        int cornerRadius = 20;
        roundedRectPath.AddArc(boxX, boxY, cornerRadius, cornerRadius, 180, 90);
        roundedRectPath.AddArc(boxX + boxWidth - cornerRadius, boxY, cornerRadius, cornerRadius, 270, 90);
        roundedRectPath.AddArc(boxX + boxWidth - cornerRadius, boxY + boxHeight - cornerRadius, cornerRadius, cornerRadius, 0, 90);
        roundedRectPath.AddArc(boxX, boxY + boxHeight - cornerRadius, cornerRadius, cornerRadius, 90, 90);
        roundedRectPath.CloseFigure();
        ///////////////////////
        ////////////////////////////////////////////////////

        SolidBrush brush = new SolidBrush(Color.White);
        System.Drawing.Pen mypen = new System.Drawing.Pen(Color.Black, 5);
        Font f = new Font("Calibri", 35, FontStyle.Bold);
        if (mainmenuflag == 1)
        {
            // g.FillRectangle(bgrBrush, new Rectangle(0, 0, width, height));

            g.DrawImage(backgroundImage, new Rectangle(0, 0, width, height));
            g.FillPath(boxBrush, roundedRectPath);
            RectangleF textRect = new RectangleF(boxX + 10, boxY - 50, boxWidth - 20, boxHeight - 20);

            // Create a StringFormat for centered alignment
            StringFormat format = new StringFormat
            {
                Alignment = StringAlignment.Center,      // Center horizontally
                LineAlignment = StringAlignment.Center   // Center vertically
            };
            g.DrawString("Interactive Application for Crown Preparation Learners", titleFont, textBrush, textRect, format);
            if (message.Length != 0)
            {
                textRect = new RectangleF(boxX + 10, boxY + 100, boxWidth - 20, boxHeight - 20);
                g.DrawString(message, titleFont, textBrush, textRect, format);
            }
            //textRect = new RectangleF(boxX + 10, boxY + 100, boxWidth - 20, boxHeight - 20);
            //g.DrawString("welcome akool", titleFont, textBrush, textRect, format);
            mainmenuflag = checkmainmenu();
            if (mainmenuflag == 2)
            {
                this.Controls.Remove(mainMenuButton);
                this.mainMenuButton.Dispose();
            }
        }
        else if (mainmenuflag == 2 && !hand_gesture)
        {
            g.DrawImage(backgroundImage2, new Rectangle(0, 0, width, height));
            g.DrawImage(adminImage, new Rectangle(10, 10, 100, 100));
            if (cursorList.Count > 0)
            {
                lock (cursorList)
                {
                    foreach (TuioCursor tcur in cursorList.Values)
                    {
                        List<TuioPoint> path = tcur.Path;
                        TuioPoint current_point = path[0];

                        for (int i = 0; i < path.Count; i++)
                        {
                            TuioPoint next_point = path[i];
                            g.DrawLine(curPen, current_point.getScreenX(width), current_point.getScreenY(height), next_point.getScreenX(width), next_point.getScreenY(height));
                            current_point = next_point;
                        }
                        g.FillEllipse(curBrush, current_point.getScreenX(width) - height / 100, current_point.getScreenY(height) - height / 100, height / 50, height / 50);
                        g.DrawString(tcur.CursorID + "", font, fntBrush, new PointF(tcur.getScreenX(width) - 10, tcur.getScreenY(height) - 10));
                    }
                }
            }


            // draw the objects
            if (objectList.Count > 0)
            {
                lock (objectList)
                {
                    foreach (TuioObject tobj in objectList.Values)
                    {
                        int ox = tobj.getScreenX(width);
                        int oy = tobj.getScreenY(height);
                        int size = height / 10;


                        string objectImagePath = "";
                        if (tobj.SymbolID == 15)
                        {
                            mymenupoints = generatemenu(CountMenuItems);
                            MenuObjs = CreateMenuObjects(mymenupoints);
                            checkrotation(MenuObjs, g, tobj);
                            g = drawmenu(MenuObjs, g);
                        }
                        foreach (TuioObject obj1 in objectList.Values)
                        {
                            foreach (TuioObject obj2 in objectList.Values)
                            {
                                if (obj1.SymbolID == 15 && obj2.SymbolID == 12 && AreObjectsIntersecting(obj1, obj2))
                                {
                                    check_menu();
                                }
                                if (FlagExecuted == 1 && (obj1.SymbolID == 5 || obj2.SymbolID == 5))
                                {
                                    viewerControl.ChangeBasedOnCommand("Zoom out");
                                }
                                else if (FlagExecuted == 1 && (obj1.SymbolID == 8 || obj2.SymbolID == 8))
                                {
                                    viewerControl.ChangeBasedOnCommand("Zoom in");
                                }
                                else if (FlagExecuted == 1 && (obj1.SymbolID == 3 || obj2.SymbolID == 3))
                                {
                                    viewerControl.ChangeBasedOnCommand("Swipe right");

                                }
                                else if (FlagExecuted == 1 && (obj1.SymbolID == 11 || obj2.SymbolID == 11))
                                {
                                    viewerControl.ChangeBasedOnCommand("Swipe left");
                                }
                                else if (FlagExecuted == 1 && (obj1.SymbolID == 1 || obj2.SymbolID == 1))
                                {
                                    viewerControl.ChangeBasedOnCommand("Swipe up");
                                }
                                else if (FlagExecuted == 1 && (obj1.SymbolID == 2 || obj2.SymbolID == 2))
                                {
                                    viewerControl.ChangeBasedOnCommand("Swipe down");
                                }
                                if (obj1.SymbolID == 15 && obj2.SymbolID == 11 && AreObjectsIntersecting(obj1, obj2))
                                {
                                    check_selection();
                                }
                                if (obj1.SymbolID == 6 && obj2.SymbolID == 6)
                                {
                                    back();
                                }
                            }
                        }
                    }
                }
            }


            // draw the blobs
            if (blobList.Count > 0)
            {
                lock (blobList)
                {
                    foreach (TuioBlob tblb in blobList.Values)
                    {
                        int bx = tblb.getScreenX(width);
                        int by = tblb.getScreenY(height);
                        float bw = tblb.Width * width;
                        float bh = tblb.Height * height;

                        g.TranslateTransform(bx, by);
                        g.RotateTransform((float)(tblb.Angle / Math.PI * 180.0f));
                        g.TranslateTransform(-bx, -by);

                        g.FillEllipse(blbBrush, bx - bw / 2, by - bh / 2, bw, bh);

                        g.TranslateTransform(bx, by);
                        g.RotateTransform(-1 * (float)(tblb.Angle / Math.PI * 180.0f));
                        g.TranslateTransform(-bx, -by);

                        g.DrawString(tblb.BlobID + "", font, fntBrush, new PointF(bx, by));
                    }
                }
            }
        }
        if (hand_gesture)
        {
            //Console.WriteLine($"selected index {MenuSelectedIndex}");
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(() => mainMenuButton.Visible = false));
            }
            else
            {
                mainMenuButton.Visible = false;
            }
            g.DrawImage(backgroundImage2, new Rectangle(0, 0, width, height));
            g.DrawImage(adminImage, new Rectangle(10, 10, 100, 100));
            mymenupoints = generatemenu(CountMenuItems, MenuSelectedIndex);
            MenuObjs = CreateMenuObjects(mymenupoints);
            g = drawmenu(MenuObjs, g);
        }
    }

    public int checkmainmenu()
    {
        if (objectList.Count > 0)
        {
            lock (objectList)
            {
                foreach (TuioObject tobj in objectList.Values)
                {
                    int ox = tobj.getScreenX(width);
                    int oy = tobj.getScreenY(height);
                    int size = height / 10;

                    /*  g.TranslateTransform(ox, oy);
                        g.RotateTransform((float)(tobj.Angle / Math.PI * 180.0f));
                        g.TranslateTransform(-ox, -oy);

                        g.FillRectangle(objBrush, new Rectangle(ox - size / 2, oy - size / 2, size, size));

                        g.TranslateTransform(ox, oy);
                        g.RotateTransform(-1 * (float)(tobj.Angle / Math.PI * 180.0f));
                        g.TranslateTransform(-ox, -oy);

                        g.DrawString(tobj.SymbolID + "", font, fntBrush, new PointF(ox - 10, oy - 10));*/
                    string objectImagePath = "";
                    if (tobj.SymbolID == 12)
                    {
                        return 2;
                    }
                }
            }
        }
        return 1;

    }

    void DrawDubb(Graphics g)
    {
        if (hand_gesture)
        {
            using (Graphics g2 = Graphics.FromImage(off))
            {
                // Draw on the off-screen buffer (bitmap)
                OnPaintBackground(new PaintEventArgs(g2, new Rectangle(0, 0, off.Width, off.Height)));

                // Draw the off-screen buffer to the on-screen Graphics object
                g.DrawImage(off, 0, 0);
            }
        }
        else
        {
            Graphics g2 = Graphics.FromImage(off);
            DrawScene(g2);
            g.DrawImage(off, 0, 0);
        }

    }
    private Button mainMenuButton;
    private void InitializeComponent()
    {
        this.DoubleBuffered = true;
        // 
        // buttonRJ1
        // 
        // 
        // TuioDemo
        // 
        //this.ClientSize = new System.Drawing.Size(1344, 709);


        this.Text = "Crown Preparations Interactive App";
        mainMenuButton = new Button();
        mainMenuButton.Size = new Size(350, 100);
        mainMenuButton.Text = "START"; mainMenuButton.Font = new Font("Calibri", 35, FontStyle.Bold);
        mainMenuButton.ForeColor = Color.White;

        mainMenuButton.FlatAppearance.Equals(FlatStyle.Flat);
        mainMenuButton.Location = new Point((screen_width / 2) - (350 / 2), (screen_height / 2) - (50));
        mainMenuButton.TabIndex = 1;
        mainMenuButton.BackColor = Color.Transparent; // Set the desired location
        mainMenuButton.Click += new EventHandler(MainMenuButton_Click);

        // Add the button to the form only if mainmenuflag == 1
        if (mainmenuflag == 1)
        {
            this.Controls.Add(mainMenuButton);
        }

    }

    private void MainMenuButton_Click(object sender, EventArgs e)
    {

        mainmenuflag = 2;

        this.Controls.Remove(mainMenuButton);
        this.mainMenuButton.Dispose();
    }
    private async void ActivateDelay()
    {
        isDelayActive = true;
        await Task.Delay(3000); // 3-second delay
        isDelayActive = false;
    }
    private Thread yoloThread;
    private bool stopYoloThread = false;
    private ConcurrentQueue<string> yoloCommands = new ConcurrentQueue<string>();
    public string prediction;
    int form2flag = 0;
    private void StartYoloThread()
    {
        yoloThread = new Thread(() =>
        {
            TcpListener listener = null;

            try
            {
                // Allow immediate reuse of the port
                listener = new TcpListener(System.Net.IPAddress.Parse("127.0.0.1"), 4001);
                listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                listener.Start();
                Console.WriteLine("Server started. Waiting for YOLO client to connect...");

                while (!stopYoloThread)
                {
                    // Accept a client connection
                    using (TcpClient client = listener.AcceptTcpClient())
                    {

                        yoloThread.IsBackground = true;
                        Console.WriteLine("YOLO client connected.");
                        if (form2flag == 0)
                        {
                            this.Invoke(new Action(() =>
                            {
                                this.Hide(); // Close the current form

                                Console.WriteLine($"Form1 queue hash: {yoloCommands.GetHashCode()}");
                                // Open the new form
                                //Form2 form2 = new Form2(this, yoloCommands);
                                //form2.Show();
                            }));
                            form2flag = 1;
                        }

                        // Close the current form
                        using (NetworkStream stream = client.GetStream())
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            while (!stopYoloThread)
                            {
                                try
                                {
                                    // Read commands sent by YOLO client
                                    string command = reader.ReadLine();
                                    if (!string.IsNullOrEmpty(command))
                                    {
                                        Console.WriteLine($"Received: {command}");
                                        yoloCommands.Enqueue(command);
                                    }
                                }
                                catch (IOException ioEx)
                                {
                                    Console.WriteLine($"Connection error: {ioEx.Message}");
                                    break; // Break if client disconnects
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server error: {ex.Message}");
            }
            finally
            {
                listener?.Stop();
                Console.WriteLine("Server stopped.");
            }
        });
        yoloThread.IsBackground = true;
        yoloThread.Start();
    }


    void DrawScene(Graphics g)
    {
        g.Clear(Color.DarkKhaki);
        Pen p = new Pen(Color.MintCream, 5);
        Pen pp = new Pen(Color.Black, 1);
        Pen ppp = new Pen(Color.Orange, 1);
        // g.DrawLine(p, c2.XC - c2.Rad, c2.YC, c1.XC + c1.Rad, c1.YC);
        // g.DrawLine(p, c.XC, c.YC - c.Rad, c3.XC, c3.YC + c3.Rad);
        g.DrawImage(backgroundImage2, new Rectangle(0, 0, width, height));
        g.DrawImage(adminImage, new Rectangle(10, 10, 100, 100));

    }

    System.Windows.Forms.Timer YOLOTimer = new System.Windows.Forms.Timer();
    public int cttick = 0;


    private void StopYoloThread()
    {
        stopYoloThread = true;
        yoloThread.Join();
    }
    public bool AreObjectsIntersecting(TuioObject obj1, TuioObject obj2)
    {
        if (isDelayActive) return false;
        int obj1X = obj1.getScreenX(width);
        int obj1Y = obj1.getScreenY(height);
        int obj1Size = 600;

        int obj2X = obj2.getScreenX(width);
        int obj2Y = obj2.getScreenY(height);
        int obj2Size = 600;

        //this.Text = "(" + obj1X + " , " + obj1Y + ") (" + obj2X + " , " + obj2Y + ") " + "Size: " + obj1Size;

        return obj1X < obj2X + obj2Size && obj1X + obj1Size > obj2X &&
               obj1Y < obj2Y + obj2Size && obj1Y + obj1Size > obj2Y && (obj1.SymbolID == 15 || obj2.SymbolID == 15);
    }
    [STAThread]

    public static void Main(String[] argv)
    {
        //try
        //{
        //    string exePath = @"C:\Users\Administrator\source\repos\Interactive-Dental-Application\TUIO Folder\reacTIVision-1.5.1-win64 (1)\reacTIVision-1.5.1-win64\reacTIVision.exe";

        //    // Get the process name (remove the file extension for comparison)
        //    string processName = System.IO.Path.GetFileNameWithoutExtension(exePath);

        //    // Check if the process is already running
        //    var runningProcesses = System.Diagnostics.Process.GetProcessesByName(processName);

        //    if (runningProcesses.Length > 0)
        //    {
        //        Console.WriteLine($"The process '{processName}' is already running.");
        //    }
        //    else
        //    {
        //        // Start the process if it's not running
        //        System.Diagnostics.Process.Start(exePath);
        //        Console.WriteLine($"Started process '{processName}'.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine($"Error starting reacTIVision.exe: {ex.Message}");
        //}

        int port = 0;
        switch (argv.Length)
        {
            case 1:
                port = int.Parse(argv[0], null);
                if (port == 0) goto default;
                break;
            case 0:
                port = 3333;
                break;
            default:
                Console.WriteLine("usage: mono TuioDemo [port]");
                System.Environment.Exit(0);
                break;
        }

        TuioDemo app = new TuioDemo(port);
        Application.Run(app);
    }
}