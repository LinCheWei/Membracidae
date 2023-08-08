using System;
using System.Collections;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Windows.Forms;
using TreeHopper.Deserialize;
using TreeHopper.Utility;
using TreeHopperApp.Utils;

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
        // Variables to plot
        private List<string> names;
        private List<Guid> iGuids;
        private List<PointF> pivots;
        private List<RectangleF> rectanglesToDraw;
        private ToolStripMenuItem rainbowMenuItem;
        private bool rainbowEnabled = false;
        // Transforms
        // Zoom
        private float zoomLevel = 1.0f; // Initial zoom level (1.0 means 100%)
        private bool isPanning = false;
        Point mouseDown;
        int startx = 0;                         // offset of image when mouse was pressed
        int starty = 0;
        int imgx = 0;                         // current offset of image
        int imgy = 0;


        public MainForm()
        {
            InitializeComponent();
            this.MouseDown += MainForm_MouseDown;
            this.MouseMove += MainForm_MouseMove;
            this.MouseUp += MainForm_MouseUp;

            DoubleBuffered = true;
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
            //mousePosition = new PointF(this.ClientSize.Width / 2, this.ClientSize.Height / 2);
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
            iGuids = new List<Guid>();
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
                    Guid instanceGuid = c.InstanceGuid;
                    string name = c.Parameter("Name");
                    if (name != null)
                    {
                        names.Add(name);
                    }
                    if (instanceGuid != null)
                    {
                        iGuids.Add(instanceGuid);
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            if (rectanglesToDraw != null && rectanglesToDraw.Count > 0)
            {
                using (Font font = new Font("Calibri", 10 * zoomLevel))
                using (Pen pen = new Pen(Color.Black, 1))
                {
                    foreach (var rectIndex in Enumerable.Range(0, rectanglesToDraw.Count))
                    {
                        RectangleF rect = rectanglesToDraw[rectIndex];
                        string name = names[rectIndex];

                        // Apply the transformation to the rectangle
                        RectangleF scaledRect = new RectangleF(
                            (rect.Left + imgx) * zoomLevel,
                            (rect.Top + imgy) * zoomLevel,
                            rect.Width * zoomLevel,
                            rect.Height * zoomLevel
                        );

                        Color customColor = AppUtils.GetRectangleColor(rectIndex, rectanglesToDraw.Count, rainbowEnabled);

                        // Draw the scaled rectangle with the custom gradient color
                        using (SolidBrush fillBrush = new SolidBrush(customColor))
                        {
                            e.Graphics.FillRectangle(fillBrush, scaledRect);
                        }

                        // Add rectangle text
                        int textX = (int)(scaledRect.Left + 5); // Adjust the X position of the text
                        int textY = (int)(scaledRect.Top + 5);  // Adjust the Y position of the text
                        e.Graphics.DrawString(name, font, Brushes.Black, textX, textY);
                    }

                    foreach (var pivot in pivots)
                    {
                        PointF transformedPivot = new PointF(
                            (pivot.X + imgx) * zoomLevel,
                            (pivot.Y + imgy) * zoomLevel
                        );
                        e.Graphics.DrawEllipse(pen, transformedPivot.X, transformedPivot.Y, 2, 2);
                    }
                }
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button == MouseButtons.Left)
            {
                PointF transformedMouseClick = new PointF(
                    (e.X - imgx) / zoomLevel,
                    (e.Y - imgy) / zoomLevel
                );

                // Check if the mouse click occurred within any of the rectangles
                for (int i = 0; i < rectanglesToDraw.Count; i++)
                {
                    RectangleF rect = rectanglesToDraw[i];
                    if (rect.Contains(transformedMouseClick))
                    {
                        // Perform action when the rectangle is clicked
                        Guid instanceGuid = iGuids[i];
                        PointF pivot = pivots[i];

                        Component component = ghxParser.Components.FirstOrDefault(c => c.InstanceGuid == instanceGuid);

                        // For example, display a message box showing the name and pivot point
                        string name = component.Parameter("Name");
                        string code;
                        if (component.Parameter("CodeInput") != null) code = component.Parameter("CodeInput");
                        else if (component.Parameter("ScriptSource") != null) code = component.Parameter("ScriptSource");
                        else code = null;
                        string message = $"Component Name = {name}\n" + $"Component GUID = {instanceGuid}\n " +
                            $"Value = {code}";
                        MessageBox.Show(message, "Rectangle Clicked");
                    }
                }
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            float oldZoom = zoomLevel;
            if(e.Delta > 0)
            {
                zoomLevel += 0.1F;
            }
            else if(e.Delta < 0)
            {
                zoomLevel = Math.Max(zoomLevel - 0.1F, 0.01F);
            }

            Point mousePosNow = e.Location;

            int x = mousePosNow.X - this.Location.X;
            int y = mousePosNow .Y - this.Location.Y;

            int oldx = (int)(x / oldZoom);
            int oldy = (int)(y / oldZoom);

            int newx = (int)(x/ zoomLevel);
            int newy = (int)(y/ zoomLevel);

            imgx = newx - oldx + imgx;
            imgy = newy - oldy + imgy;

            this.Invalidate();
        }
        private void MainForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (!isPanning)
                {
                    isPanning = true;
                    mouseDown = e.Location;
                    startx = imgx; starty = imgy;
                }
            }
        }
        private void MainForm_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Right)
            {
                Point mousePosition = e.Location;

                int deltaX = mousePosition.X - mouseDown.X;
                int deltaY = mousePosition.Y - mouseDown.Y;

                imgx = (int)(startx + (deltaX / zoomLevel));  // calculate new offset of image based on the current zoom factor
                imgy = (int)(starty + (deltaY / zoomLevel));

                this.Refresh();
            }
        }
        private void MainForm_MouseUp(object sender, MouseEventArgs e)
        {
            isPanning = false;
        }
    }

}
