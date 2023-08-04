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
        //Variables to plot
        private List<string> names;
        private List<Guid> iGuids;
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
        private PointF mousePosition = PointF.Empty;

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
            mousePosition = new PointF(this.ClientSize.Width / 2, this.ClientSize.Height / 2);
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
    
     //Calculate transformations
    private void CalculateTransformValues()
    {
        if (rectanglesToDraw == null || rectanglesToDraw.Count == 0)
            return;

        float minX = rectanglesToDraw.Min(rect => rect.Left);
        float minY = rectanglesToDraw.Min(rect => rect.Top);
        float maxX = rectanglesToDraw.Max(rect => rect.Right);
        float maxY = rectanglesToDraw.Max(rect => rect.Bottom);

        // Calculate the size of the bounding box
        float boundingBoxWidth = maxX - minX;
        float boundingBoxHeight = maxY - minY;

        // Calculate the new scaling factors based on the zoom level
        float scaleX = zoomLevel;
        float scaleY = zoomLevel;

        // Calculate the translation needed to center the scaled bounding box on the mouse position
        float translateX = mousePosition.X - scaleX * mousePosition.X;
        float translateY = mousePosition.Y - scaleY * mousePosition.Y;

        // Create a transformation matrix
        transformationMatrix = new Matrix();
        transformationMatrix.Translate(translateX, translateY);
        transformationMatrix.Scale(scaleX, scaleY);

        // Update the class-level variables
        this.scaleX = scaleX;
        this.scaleY = scaleY;
        this.translateX = translateX;
        this.translateY = translateY;
    }

    protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            CalculateTransformValues();
            if (rectanglesToDraw != null && rectanglesToDraw.Count > 0)
            {
                // Create a transformation matrix
                // Apply the stored transformation matrix to the mouse position
                PointF[] mousePos = { mousePosition };
                transformationMatrix.Invert();
                transformationMatrix.TransformPoints(mousePos);
                transformationMatrix.Invert();
                PointF[] points = {};
                if (pivots.Count > 0)
                {
                    points = pivots.ToArray();
                    transformationMatrix.TransformPoints(points);
                }
                // Draw all rectangles from the list
                using (Font font = new Font("Calibri", 10*scaleX))
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
                        Color customColor = AppUtils.GetRectangleColor(i, rectanglesToDraw.Count, rainbowEnabled);

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
                    Guid instanceGuid = iGuids[i];
                    PointF pivot = pivots[i];

                    Component component = ghxParser.Components.FirstOrDefault(c => c.InstanceGuid == instanceGuid);
                    //Save component GUID
                    //Save component Input Param or param_input if any
                    //Output if any
                    //All GUID
                    //Save Script if any
                    //Otherwise save source if any
                    //if no pivot create in pivot and out pivot
                    //Don't really know what else

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
    protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            // Recalculate the transformations
            CalculateTransformValues();

            // Determine the zoom direction (positive or negative)
            int direction = Math.Sign(e.Delta);

            // Adjust the zoom level based on the direction and a fixed factor (you can adjust this factor as needed)
            float zoomFactor = 1.2f;
            zoomLevel *= direction == 1 ? zoomFactor : 1.0f / zoomFactor;

            // Update the mouse position
            mousePosition = e.Location;

            // Redraw the form
            this.Invalidate();
        }

    }
}
