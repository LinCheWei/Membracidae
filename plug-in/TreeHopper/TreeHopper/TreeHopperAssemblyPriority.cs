using Grasshopper.Kernel;
using Grasshopper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms.Layout;
using System.Windows.Forms;

namespace TreeHopper
{
    public abstract class TreeHopperMenuItem : GH_AssemblyPriority
    {
        private System.Timers.Timer delayedLoadTimer;
        //private static List<Tuple<string, TreeHopperMenuItem>> LoadQueue = new List<Tuple<string, TreeHopperMenuItem>>();

        public override GH_LoadingInstruction PriorityLoad()
        {
            //this.DelayedLoadMenuItems();
            return GH_LoadingInstruction.Abort;
        }


        private void DelayedLoadMenuItems()
        {
            this.delayedLoadTimer = new System.Timers.Timer(100.0);
            this.delayedLoadTimer.Elapsed += new ElapsedEventHandler(this.DelayedLoadCallback);
            this.delayedLoadTimer.Start();
        }

        private void DelayedLoadCallback(object sender, ElapsedEventArgs e)
        {
            if (Instances.DocumentEditor == null || !this.delayedLoadTimer.Enabled)
                return;
            this.delayedLoadTimer.Stop();
            this.delayedLoadTimer.Elapsed -= new ElapsedEventHandler(this.DelayedLoadCallback);
            MenuStrip mainMenuStrip = ((Form)Instances.DocumentEditor).MainMenuStrip;
            lock (mainMenuStrip)
            {
                foreach (ToolStripMenuItem parent in (ArrangedElementCollection)mainMenuStrip.Items)
                {
                    if (parent.Name == "Treehopper")
                    {
                        return;
                    }
                }
                ToolStripMenuItem parent1 = new ToolStripMenuItem("Treehopper");
                parent1.Name = "Treehopper";
                mainMenuStrip.Items.Add(parent1);
            }
        }
    }
}
