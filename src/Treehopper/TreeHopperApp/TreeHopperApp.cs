using Microsoft.Win32;
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
        PointF mouseDown;
        //Offset in x and y where the form is drawn
        float startx = 0;                         // offset of image when mouse was pressed
        float starty = 0;
        float imgx = 0;                           // current offset of image
        float imgy = 0;

        // Initialize the form and handle mouse events like pan/zoom
        public MainForm()
        {
            InitializeComponent();
            this.MouseDown += MainForm_MouseDown;
            this.MouseMove += MainForm_MouseMove;
            this.MouseUp += MainForm_MouseUp;

            this.WindowState = FormWindowState.Maximized;
            DoubleBuffered = true;

            this.AutoScaleMode = AutoScaleMode.Dpi;
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
                using (Pen pen = new Pen(Color.Black, 1 * zoomLevel))
                {
                    // Draw Bezier Curves
                    DrawWires(e.Graphics, pen);

                    // Draw Rectangles
                    DrawRectangles(e.Graphics, font, pen);

                    // Draw Points
                    DrawPivots(e.Graphics, pen, 2*zoomLevel);
                }
            }
        }

        private void DrawWires(Graphics g, Pen pen)
        {
            if (pointPair != null)
            {
                foreach (List<PointF> plist in pointPair)
                {
                    List<PointF> transformedPairs = new List<PointF>(transformPoints(plist, imgx, imgy, zoomLevel));
                    g.DrawBezier(pen, transformedPairs[0], transformedPairs[1], transformedPairs[2], transformedPairs[3]);
                }
            }
        }

        private void DrawRectangles(Graphics g, Font font, Pen pen)
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
                    g.FillRectangle(fillBrush, scaledRect);
                }

                // Add rectangle text
                float textX = (scaledRect.Left + 5); // Adjust the X position of the text
                float textY = (scaledRect.Top + 5);  // Adjust the Y position of the text
                g.DrawString(name, font, Brushes.Black, textX, textY);
            }
        }

        private void DrawPivots(Graphics g, Pen pen, float dotSize)
        {
            List<PointF> transformedPivots = new List<PointF>(transformPoints(pivots, imgx, imgy, zoomLevel));
            foreach (PointF pivot in transformedPivots)
            {
                g.DrawEllipse(pen, pivot.X, pivot.Y, dotSize, dotSize);
            }
        }


        public List<PointF> transformPoints(List<PointF> points, float imgx, float imgy, float zoom)
        {
            List<PointF> tPoints = new List<PointF> ();
            foreach (PointF p in points)
            {
                PointF transformedPt = new PointF(
                    (p.X + imgx) * zoomLevel,
                    (p.Y + imgy) * zoomLevel
                );

                tPoints.Add(transformedPt);

            }
            return tPoints;
        }
        //Event handler for when user clicks on the rectangle(GH Component)
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button == MouseButtons.Left)
            {
                // Check if the mouse click occurred within any of the rectangles
                foreach (Component c in ghxParser.Components)
                {

                    var rect = c.Parameter("Bounds");

                    if (rect != null)
                    {

                        RectangleF scaledRect = new RectangleF(
                        (rect.Left + imgx) * zoomLevel,
                        (rect.Top + imgy) * zoomLevel,
                        rect.Width * zoomLevel,
                        rect.Height * zoomLevel
                    );

                        if (scaledRect.Contains(e.Location))
                        {
                            // Perform action when the rectangle is clicked
                            Guid instanceGuid = c.InstanceGuid;

                            // For example, display a message box showing the name and pivot point
                            string name = c.Parameter("Name");
                            string code;
                            if (c.Parameter("CodeInput") != null) code = c.Parameter("CodeInput");
                            else if (c.Parameter("ScriptSource") != null) code = c.Parameter("ScriptSource");
                            else code = null;
                            string message = $"Component Name = {name}\n" + $"Component GUID = {instanceGuid}\n " +
                                $"Value = {code}";
                            MessageBox.Show(message, "Rectangle Clicked");
                        }
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

            float x = mousePosNow.X - this.Location.X;
            float y = mousePosNow .Y - this.Location.Y;

            float oldx = (x / oldZoom);
            float oldy = (y / oldZoom);

            float newx = (x/ zoomLevel);
            float newy = (y/ zoomLevel);

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

                float deltaX = mousePosition.X - mouseDown.X;
                float deltaY = mousePosition.Y - mouseDown.Y;

                imgx = (startx + (deltaX / zoomLevel));  // calculate new offset of image based on the current zoom factor
                imgy = (starty + (deltaY / zoomLevel));
            }
            this.Invalidate();
        }

        //Event handler for when mouse is released
        private void MainForm_MouseUp(object sender, MouseEventArgs e)
        {
            isPanning = false;
        }

    }

}
