using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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
        private GhxDocument ghxParser;
        private List<string> names;
        private List<PointF> pivots;
        private List<RectangleF> rectanglesToDraw;
        private ToolStripMenuItem rainbowMenuItem;
        private bool rainbowEnabled = false;
        //Transforms
        private float scaleX;
        private float scaleY;
        private float translateX;
        private float translateY;
        Matrix transformationMatrix;
        //zoomies
        private float zoomLevel = 1.0f; // Initial zoom level (1.0 means 100%)


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

            // Create menu items
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
            ToolStripMenuItem gitMenu = new ToolStripMenuItem("Git");
            ToolStripMenuItem viewMenu = new ToolStripMenuItem("View");
            // Create sub menu items
            ToolStripMenuItem openMenuItem = new ToolStripMenuItem("Open");
            ToolStripMenuItem gitVersionMenuItem = new ToolStripMenuItem("Version");
            rainbowMenuItem = new ToolStripMenuItem("Rainbow");
            // compareMenuItem = new ToolStripMenuItem("Compare");

            openMenuItem.Click += (sender, e) =>
            {
                OpenFile();
            };

            // Add the "Open" menu item to the "File" menu
            fileMenu.DropDownItems.Add(openMenuItem);
            gitMenu.DropDownItems.Add(gitVersionMenuItem);
            viewMenu.DropDownItems.Add(rainbowMenuItem);
            //viewMenu.DropDownItems.Add(compareMenuItem);

            // Add the menus to the MenuStrip control
            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(gitMenu);
            menuStrip.Items.Add(viewMenu);

            // Set the MenuStrip as the form's menu
            this.Controls.Add(menuStrip);
            rainbowMenuItem.Click += rainbowMenuItem_Click;
        }

        private void rainbowMenuItem_Click(object sender, EventArgs e)
        {
            // Toggle the rainbowEnabled flag when the "Rainbow" menu item is clicked
            rainbowEnabled = !rainbowEnabled;
            rainbowMenuItem.Checked = rainbowEnabled;
            // Redraw the rectangles with the new color settings
            this.Invalidate();
        }


        private void OpenFile()
        {
            // Create an instance of OpenFileDialog
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // Set initial directory and file filter (if needed)
            openFileDialog.InitialDirectory = "..\\Membracidae";
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
            ghxParser = new GhxDocument(filePath);

            rectanglesToDraw = new List<RectangleF>(); // Initialize the rectanglesToDraw list
            names = new List<string>();
            pivots = new List<PointF>();
            foreach (Component c in ghxParser.Components)
            {
                var value = c.Parameter("Bounds");
                if (value != null)
                {
                    // Add the rectangle to the list
                    rectanglesToDraw.Add(value);

                    // if bounds then find name
                    var name = c.Parameter("Name");
                    if (name != null)
                    {
                        names.Add(name);
                    }
                    //pivots.Add(c.Parameter("Pivot"));
                    var IOs = c.IO;
                    foreach (var o in IOs)
                    {
                        pivots.Add(o.Parameter("Pivot"));
                    }
                }
            }
            // Trigger the drawing of rectangles after parsing all components
            this.Invalidate();
            this.WindowState = FormWindowState.Maximized;
        }

        private Color GetRectangleColor(int index, int totalRectangles)
        {
            if (rainbowEnabled)
            {
                // Calculate the hue value based on the index and total number of rectangles
                float hue = (120f / totalRectangles) * index;

                // Calculate the brightness value based on the index
                float brightness = 0.5f + (float)index / (totalRectangles - 1) * 0.5f;

                return ColorFromAhsb(255, hue, 1, brightness);
            }
            else
            {
                // Default pink color (you can change this to any other color you prefer)
                return Color.DeepPink;
            }
        }

        private Color ColorFromAhsb(int alpha, float hue, float saturation, float brightness)
        {
            // Calculate the RGB values from HSB (Hue, Saturation, Brightness)
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            float f = hue / 60 - (int)(hue / 60);

            brightness *= 255;
            int v = (int)brightness;
            int p = (int)(brightness * (1 - saturation));
            int q = (int)(brightness * (1 - f * saturation));
            int t = (int)(brightness * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(alpha, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(alpha, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(alpha, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(alpha, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(alpha, t, p, v);
            else
                return Color.FromArgb(alpha, v, p, q);
        }
    
     //Calculate transformations

    private float CalculateScaleX()
    {
        float boundingBoxWidth = CalculateBoundingBoxWidth();
        float clientAreaWidth = this.ClientSize.Width;
        return (float)clientAreaWidth / boundingBoxWidth;
    }

    private float CalculateScaleY()
    {
        float boundingBoxHeight = CalculateBoundingBoxHeight();
        float clientAreaHeight = this.ClientSize.Height;
        return (float)clientAreaHeight / boundingBoxHeight;
    }

    private float CalculateBoundingBoxWidth()
    {
        float minX = rectanglesToDraw.Min(rect => rect.Left);
        float maxX = rectanglesToDraw.Max(rect => rect.Right);
        return maxX - minX;
    }

    private float CalculateBoundingBoxHeight()
    {
        float minY = rectanglesToDraw.Min(rect => rect.Top);
        float maxY = rectanglesToDraw.Max(rect => rect.Bottom);
        return maxY - minY;
    }
        private Matrix CalculateTransformValues(float zoomLevel)
        {
            float minX = rectanglesToDraw.Min(rect => rect.Left);
            float minY = rectanglesToDraw.Min(rect => rect.Top);
            float maxX = rectanglesToDraw.Max(rect => rect.Right);
            float maxY = rectanglesToDraw.Max(rect => rect.Bottom);

            // Calculate the size of the bounding box
            float boundingBoxWidth = maxX - minX;
            float boundingBoxHeight = maxY - minY;

            // Calculate the size of the form's client area
            float clientAreaWidth = this.ClientSize.Width;
            float clientAreaHeight = this.ClientSize.Height;

            // Calculate the scaling factor to fit the bounding box to the client area with the given zoom level
            scaleX = (float)clientAreaWidth / (boundingBoxWidth * zoomLevel);
            scaleY = (float)clientAreaHeight / (boundingBoxHeight * zoomLevel);

            // Calculate the translation needed to center the scaled bounding box on the form
            translateX = (int)((clientAreaWidth - boundingBoxWidth * scaleX * zoomLevel) / 2);
            translateY = (int)((clientAreaHeight - boundingBoxHeight * scaleY * zoomLevel) / 2);

            // Create and return the transformation matrix with the given zoom level applied
            Matrix transformationMatrix = new Matrix();
            transformationMatrix.Scale(scaleX * zoomLevel, scaleY * zoomLevel);
            transformationMatrix.Translate(translateX, translateY);

            return transformationMatrix;
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (rectanglesToDraw != null && rectanglesToDraw.Count > 0)
            {
                CalculateTransformValues(zoomLevel);
                // Create a transformation matrix
                transformationMatrix = new Matrix();
                transformationMatrix.Scale(scaleX, scaleY);
                transformationMatrix.Translate(translateX, translateY);
                PointF[] points = {};
                if (pivots.Count > 0)
                {
                    points = pivots.ToArray();
                    transformationMatrix.TransformPoints(points);
                }
                // Draw all rectangles from the list
                using (Font font = new Font("Calibri", 10*1/zoomLevel))
                //using (SolidBrush fillBrush = new SolidBrush(Color.DeepPink))
                using (Pen pen = new Pen(Color.Black, 1))
                {
                    for (int i = 0; i < rectanglesToDraw.Count; i++)
                    {
                        RectangleF rect = rectanglesToDraw[i];
                        string name = names[i];
        
                        // Apply the transformation to the rectangle
                        GraphicsPath path = new GraphicsPath();
                        path.AddRectangle(rect);
                        path.Transform(transformationMatrix);
                        RectangleF scaledRect = path.GetBounds();
                        Color customColor = GetRectangleColor(i, rectanglesToDraw.Count);

                        // Draw the scaled rectangle with the custom gradient color
                        using (SolidBrush fillBrush = new SolidBrush(customColor))
                        {
                            e.Graphics.FillRectangle(fillBrush, scaledRect);
                            //e.Graphics.DrawRectangle(pen, scaledRect.Left, scaledRect.Top, scaledRect.Width, scaledRect.Height);
                        }


                        // Add rectangle text
                        int textX = (int)(scaledRect.Left + 5); // Adjust the X position of the text
                        int textY = (int)(scaledRect.Top + 5);  // Adjust the Y position of the text
                        e.Graphics.DrawString(name, font, Brushes.Black, textX, textY);
                    }
                    if (points.Length > 0)
                    {
                        foreach (PointF pivot in points)
                        {
                            e.Graphics.DrawEllipse(pen, pivot.X, pivot.Y, 2, 2);
                        }
                    }
                }
            }
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            //CalculateTransformValues();
            // Apply the stored transformation matrix to the mouse click coordinates
            PointF[] mouseClick = { new PointF(e.X, e.Y) };
            transformationMatrix.Invert();
            transformationMatrix.TransformPoints(mouseClick);
            transformationMatrix.Invert();

            // Check if the mouse click occurred within any of the rectangles
            for (int i = 0; i < rectanglesToDraw.Count; i++)
            {
                RectangleF rect = rectanglesToDraw[i];
                if (rect.Contains(mouseClick[0]))
                {
                    // Perform action when the rectangle is clicked
                    string name = names[i];
                    PointF pivot = pivots[i];

                    Component component = ghxParser.Components.FirstOrDefault(c => c.Parameter("Name") == name);
                    //Save component GUID
                    //Save component Input Param or param_input if any
                    //Output if any
                    //All GUID
                    //Save Script if any
                    //Otherwise save source if any
                    //if no pivot create in pivot and out pivot
                    //Don't really know what else

                    // For example, display a message box showing the name and pivot point
                    Guid instanceGuid = component.InstanceGuid;
                    string message = $"Component GUID = {instanceGuid}\n" + $"ComponentName = {name}";
                    MessageBox.Show(message, "Rectangle Clicked");
                }
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            // Check if the Ctrl key is pressed to enable zooming
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                // Increment or decrement the zoom level based on the mouse wheel delta
                const float zoomStep = 0.1f;
                zoomLevel += e.Delta > 0 ? zoomStep : -zoomStep;

                // Limit the zoom level to a reasonable range (e.g., 10% to 300%)
                zoomLevel = Math.Max(0.1f, Math.Min(3.0f, zoomLevel));

                // Redraw the rectangles with the new zoom level
                this.Invalidate();
            }
        }

    }
}
