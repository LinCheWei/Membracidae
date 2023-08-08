using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using TreeHopper.Deserialize;

namespace TreeHopperViewer
{   
    // Main form class inherits from Form
    public partial class MainForm : Form
    {
        public GhxDocument ghxParser;
        // Variables to plot
        private List<string> names;
        private List<Guid> iGuids;
        private List<PointF> pivots;
        private List<RectangleF> rectanglesToDraw;
        public bool rainbowEnabled = false;
        public ToolStripMenuItem rainbowMenuItem;
    }
    // Partial class to hold the InitializeComponents() method
    public partial class MainForm
    { 
        // Initialize the form
        public void InitializeComponent()
        {
            // Set the form's title
            this.Text = "TreeHopper Viewer";
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
            openMenuItem.Click += (sender, e) => OpenFile();

            // Add the "Open" menu item to the "File" menu
            fileMenu.DropDownItems.Add(openMenuItem);
            gitMenu.DropDownItems.Add(gitVersionMenuItem);
            viewMenu.DropDownItems.Add(rainbowMenuItem);

            // Add the menus to the MenuStrip control
            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(gitMenu);
            menuStrip.Items.Add(viewMenu);

            // Set the MenuStrip as the form's menu
            this.Controls.Add(menuStrip);
            rainbowMenuItem.Click += rainbowMenuItem_Click;
        }
        
        // Event handler for the "Rainbow" menu item to toggle the rainbowEnabled flag which sets the color mode of document
        public void rainbowMenuItem_Click(object sender, EventArgs e)
        {
            // Toggle the rainbowEnabled flag when the "Rainbow" menu item is clicked
            rainbowEnabled = !rainbowEnabled;
            rainbowMenuItem.Checked = rainbowEnabled;
            // Redraw the rectangles with the new color settings
            this.Invalidate();
        }

        // Event handler for the "Open" menu item to open a Grasshopper file
        public void OpenFile()
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

        // Method to parse the Grasshopper file and store the information to draw
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
    }
}




