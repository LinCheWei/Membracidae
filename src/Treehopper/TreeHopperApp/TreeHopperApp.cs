﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TreeHopper.Deserialize;
using TreeHopper.Utility;

namespace TreeHopperViewer
{
    internal class TreeHopperApp
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private GhxVersion ghxParser;
        private List<string> names;
        private List<Rectangle> rectanglesToDraw;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "TH Viewer";
            string iconFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "treehopper.ico");
            if (File.Exists(iconFilePath))
            {
                this.Icon = new Icon(iconFilePath); // Set the form's icon to the specified .ico file
            }

            // Create a MenuStrip control
            MenuStrip menuStrip = new MenuStrip();

            // Create a "File" menu
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");

            // Create a "Open" menu item under "File"
            ToolStripMenuItem openMenuItem = new ToolStripMenuItem("Open");
            openMenuItem.Click += (sender, e) =>
            {
                // Handle the click event for the "Open" menu item
                OpenFile();
            };

            // Add the "Open" menu item to the "File" menu
            fileMenu.DropDownItems.Add(openMenuItem);

            // Add the "File" menu to the MenuStrip control
            menuStrip.Items.Add(fileMenu);

            // Set the MenuStrip as the form's menu
            this.Controls.Add(menuStrip);
        }

        private void OpenFile()
        {
            // Create an instance of OpenFileDialog
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // Set initial directory and file filter (if needed)
            openFileDialog.InitialDirectory = "C:\\Users\\akango\\Documents\\github\\Membracidae";
            openFileDialog.Filter = "Grasshopper Files|*.ghx|All Files|*.*";

            // Show the dialog and check if the user clicked OK
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Process the selected file
                ProcessGrasshopperFile(openFileDialog.FileName);
            }
        }

        private void ProcessGrasshopperFile(string filePath)
        {
            ghxParser = new GhxVersion(filePath);
            Dictionary<string, string> dict = ghxParser.Parameters();

            rectanglesToDraw = new List<Rectangle>(); // Initialize the rectanglesToDraw list
            names = new List<string>();

            foreach (Component c in ghxParser.Components())
            {
                c.Parameters().TryGetValue("Bounds", out var value);
                if (value != null)
                {
                    string[] parameters = value.Split(';');
                    int x = (int)(float.Parse(parameters[1]));
                    int y = (int)(float.Parse(parameters[3]));
                    int width = (int)(float.Parse(parameters[5]));
                    int height = (int)(float.Parse(parameters[7]));

                    // Create a new rectangle object based on the bounds
                    Rectangle rect = new Rectangle(x, y, width, height);

                    // Add the rectangle to the list
                    rectanglesToDraw.Add(rect);

                    // if bounds then find name
                    c.Parameters().TryGetValue("Name", out var name);
                    if (name != null)
                    {
                        names.Add(name);
                    }
                }
            }

            // Trigger the drawing of rectangles after parsing all components
            this.Invalidate();
            this.WindowState = FormWindowState.Maximized;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (rectanglesToDraw != null && rectanglesToDraw.Count > 0)
            {
                // Find the bounding box that encloses all rectangles
                int minX = rectanglesToDraw.Min(rect => rect.Left);
                int minY = rectanglesToDraw.Min(rect => rect.Top);
                int maxX = rectanglesToDraw.Max(rect => rect.Right);
                int maxY = rectanglesToDraw.Max(rect => rect.Bottom);

                float margin = -0.25f;
                // Calculate the size of the bounding box
                int boundingBoxWidth = maxX - minX;
                int boundingBoxHeight = maxY - minY;

                // Calculate the size of the form's client area
                int clientAreaWidth = this.ClientSize.Width;
                int clientAreaHeight = this.ClientSize.Height;

                // Calculate the scaling factor to fit the bounding box to the client area
                float scaleX = (float)clientAreaWidth / boundingBoxWidth;
                float scaleY = (float)clientAreaHeight / boundingBoxHeight;
                float scale = Math.Min(scaleX, scaleY) + margin;

                // Calculate the translation needed to center the scaled bounding box on the form
                int translateX = (int)((clientAreaWidth - boundingBoxWidth * scale) / 2);
                int translateY = (int)((clientAreaHeight - boundingBoxHeight * scale) / 2);

                // Create a transformation matrix
                Matrix transformationMatrix = new Matrix();
                transformationMatrix.Translate(translateX - minX * scale, translateY - minY * scale);
                transformationMatrix.Scale(scale, scale);

                // Draw all rectangles from the list
                using (Font font = new Font("Calibri", 10 * scale))
                using (SolidBrush fillBrush = new SolidBrush(Color.DeepPink))
                using (Pen pen = new Pen(Color.Black, 1))
                {
                    for (int i = 0; i < rectanglesToDraw.Count; i++)
                    {
                        Rectangle rect = rectanglesToDraw[i];
                        string name = names[i];

                        // Apply the transformation to the rectangle
                        GraphicsPath path = new GraphicsPath();
                        path.AddRectangle(rect);
                        path.Transform(transformationMatrix);
                        RectangleF scaledRect = path.GetBounds();

                        // Draw the scaled rectangle
                        e.Graphics.FillRectangle(fillBrush, scaledRect);
                        e.Graphics.DrawRectangle(pen, scaledRect.Left, scaledRect.Top, scaledRect.Width, scaledRect.Height);

                        // Add rectangle text
                        int textX = (int)(scaledRect.Left + 5); // Adjust the X position of the text
                        int textY = (int)(scaledRect.Top + 5);  // Adjust the Y position of the text
                        e.Graphics.DrawString(name, font, Brushes.Black, textX, textY);
                    }
                }
            }
        }
    }
}
