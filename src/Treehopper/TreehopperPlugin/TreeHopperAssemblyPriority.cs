using Grasshopper.Kernel;
using Grasshopper;
using System;
using System.Collections.Generic;
using System.Timers;
using System.Windows.Forms;
using System.Windows.Forms.Layout;

namespace TreeHopperPlugin
{
    public class TreeHopperMenuItem : GH_AssemblyPriority
    {
        private System.Timers.Timer delayedLoadTimer;
        private ToolStripMenuItem treeHopperMenuItem; // New ToolStripMenuItem to be added to the menu
        private bool isSubMenuAdded; // A flag to track if the sub-menu items are already added

        public override GH_LoadingInstruction PriorityLoad()
        {
            // Delayed loading of the menu item
            DelayedLoadMenuItems();
            return GH_LoadingInstruction.Proceed;
        }

        private void DelayedLoadMenuItems()
        {
            delayedLoadTimer = new System.Timers.Timer(100.0);
            delayedLoadTimer.Elapsed += DelayedLoadCallback;
            delayedLoadTimer.Start();
        }

        private void DelayedLoadCallback(object sender, ElapsedEventArgs e)
        {
            if (Instances.DocumentEditor == null || !delayedLoadTimer.Enabled)
                return;

            delayedLoadTimer.Stop();
            delayedLoadTimer.Elapsed -= DelayedLoadCallback;
            MenuStrip mainMenuStrip = Instances.DocumentEditor.MainMenuStrip;

            lock (mainMenuStrip)
            {
                foreach (ToolStripMenuItem parent in mainMenuStrip.Items)
                {
                    if (parent.Name == "TreeHopperMenu") // Check if the menu item already exists
                        return;
                }

                // Create a new ToolStripMenuItem and add it to the menu bar
                treeHopperMenuItem = new ToolStripMenuItem("Treehopper");
                treeHopperMenuItem.Name = "TreeHopperMenu";
                treeHopperMenuItem.DropDownOpening += TreeHopperMenuItem_DropDownOpening;
                mainMenuStrip.Items.Add(treeHopperMenuItem);
            }
        }

        private void TreeHopperMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            // Check if the sub-menu items are already added
            if (!isSubMenuAdded)
            {
                // Add sub-menu items or other functionality here
                // For example:
                ToolStripMenuItem subMenuItem1 = new ToolStripMenuItem("Cool things");
                subMenuItem1.Click += SubMenuItem_Click;
                treeHopperMenuItem.DropDownItems.Add(subMenuItem1);

                ToolStripMenuItem subMenuItem2 = new ToolStripMenuItem("Cooler things");
                subMenuItem2.Click += SubMenuItem_Click;
                treeHopperMenuItem.DropDownItems.Add(subMenuItem2);

                // Set the flag to true to indicate that sub-menu items are added
                isSubMenuAdded = true;
            }
        }

        private void SubMenuItem_Click(object sender, EventArgs e)
        {
            // Handle the click event of the sub-menu item here
            // For example:
            ToolStripMenuItem clickedItem = (ToolStripMenuItem)sender;
            MessageBox.Show($"Clicked: {clickedItem.Text}");
        }
    }
}