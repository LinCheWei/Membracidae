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
using TreeHopperViewer.Utils;

namespace TreeHopperViewer
{   
    // This is how we run the whole application
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

    public partial class MainForm : Form
    {
        // Zoom and other stories
        private float zoomLevel = 1.0f; // Initial zoom level (1.0 means 100%)
        private bool isPanning = false;
        Point mouseDown;
        //Offset in x and y where the form is drawn
        int startx = 0;                         // offset of image when mouse was pressed
        int starty = 0;
        int imgx = 0;                           // current offset of image
        int imgy = 0;

        // Initialize the form and handle mouse events like pan/zoom
        public MainForm()
        {
            InitializeComponent();
            this.MouseDown += MainForm_MouseDown;
            this.MouseMove += MainForm_MouseMove;
            this.MouseUp += MainForm_MouseUp;

            DoubleBuffered = true;
        }

        //Does all the drawing hard work
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

        //Event handler for when user clicks on the rectangle(GH Component)
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

        //Event handler for when mouse wheel is scrolled
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

        //Event handler for when mouse is pressed
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

        //Event handler for when mouse is moved
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

        //Event handler for when mouse is released
        private void MainForm_MouseUp(object sender, MouseEventArgs e)
        {
            isPanning = false;
        }
    }

}
