using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Xml;
using LibGit2Sharp;
using System.Drawing;
using System.IO;
using System.Text;
using Microsoft.XmlDiffPatch;
using TreeHopper.VersionControl;
using TreeHopper.Deserialize;
using System.Drawing.Printing;
using System.Linq;

namespace TreeHopperPlugin
{
    public class TreeHopperComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public TreeHopperComponent()
          : base("TreeHopper", "THZ",
            "test component",
            "Treehopper", "testing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("open", "open", "open git repo", GH_ParamAccess.item, false);
            pManager.AddTextParameter("version", "vers", "dadsad", GH_ParamAccess.list, new List<string>() {"0"});
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("output", "output", "test", GH_ParamAccess.list);
            pManager.AddTextParameter("ver", "ver_params", "test", GH_ParamAccess.list);
            pManager.AddTextParameter("comp", "comp_params", "test", GH_ParamAccess.list);
        }

        bool open;

        private void terminationCallback(GH_Document doc)
        {
            /// Remove created valueList
            List<IGH_DocumentObject> toDelete = new List<IGH_DocumentObject>();
            foreach (IGH_DocumentObject comp in doc.Objects)
            {
                if (comp.Description == "Treehopper_valuelist")
                {
                    toDelete.Add(comp);
                }
            }
            if (toDelete.Count > 0)
            {
                doc.RemoveObjects(toDelete, true);
            }
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Document doc = Instances.ActiveCanvas.Document;
            DA.GetData(0, ref open);
            List<String> versionId = new List<string>();

            if (open)
            {
                Hopper root = new Hopper(doc.FilePath);
                versionId = root.getVersionList(true);
                DA.SetDataList(0, versionId);

                List<string> param = new List<string>();
                GhxDocument local = new GhxDocument(doc.FilePath);
                
                DA.SetDataList(1, param);

                List<string> cp = new List<string>();
                /*
                foreach (Component c in local.Components)
                {
                    foreach (KeyValuePair<object, string> kvp in c.Parameters)
                    {
                        cp.Add(kvp.ToString());
                    }
                    
                    foreach (ComponentIO cio in c.IO)
                    {
                        foreach (KeyValuePair<string, object> pair in cio.Parameters)
                        {
                            cp.Add(pair.ToString());
                        }
                    }
                    

                    cp.Add("");
                }
                */

                DA.SetDataList(2, cp);




                /*
                using (var repo = new Repository(path))
                {
                    string message = "";
                    var fileHistory = repo.Commits.QueryBy(filename, new CommitFilter { SortBy = CommitSortStrategies.Time }); /// search through commit that contains the changes of this ghx

                    /// Acquire commit info
                    foreach (LogEntry e in fileHistory)
                    {
                        message += string.Format("id:{0}, author:{1}\r\n", e.Commit.Id, e.Commit.Author);
                        targets.Add(e.Commit.Tree);
                        foreach (TreeEntry te in e.Commit.Tree)
                        {
                            if (te.Name == filename)
                            {
                                message += te.Target.Sha.Substring(0, 7) + "\r\n";
                                versionId.Add(te.Target.Sha.Substring(0, 7));
                            }
                        }
                    }

                    /// Create value list
                    bool createVallist = true;
                    foreach (IGH_DocumentObject comp in doc.Objects)
                    {
                        if (comp.Description == "Treehopper_valuelist")
                        {
                            createVallist = false;
                        }
                    }
                    if (createVallist)
                    {
                        var valList = new Grasshopper.Kernel.Special.GH_ValueList();
                        valList.CreateAttributes();
                        valList.Name = "Versions";
                        valList.NickName = "vers";
                        valList.Description = "Treehopper_valuelist";
                        valList.ListMode = Grasshopper.Kernel.Special.GH_ValueListMode.DropDown;

                        valList.Attributes.Pivot = new PointF((float)this.Attributes.DocObject.Attributes.Bounds.Left - valList.Attributes.Bounds.Width - 15,
                            (float)this.Params.Input[1].Attributes.Bounds.Y);

                        valList.ListItems.Clear();

                        for (int i = 0; i < targets.Count; i++)
                        {
                            valList.ListItems.Add(new Grasshopper.Kernel.Special.GH_ValueListItem(versionId[i], i.ToString()));
                        }

                        doc.AddObject(valList, false);
                        this.Params.Input[1].RemoveAllSources();
                        this.Params.Input[1].AddSource(valList);
                        valList.ExpireSolution(true);
                    }
                    DA.GetDataList(1, diffID);

                    
                    // Read full string from selected blob
                    var tarBlob = repo.Lookup<Blob>(targets[int.Parse(diffID[0])] .Sha+ ":" + filename);

                    
                    XmlReader tarReader = XmlReader.Create(tarBlob.GetContentStream());
                    XmlReader srcReader = XmlReader.Create(doc.FilePath);
                   
                    XmlDocument tarDoc = new XmlDocument();
                    XmlDocument srcDoc = new XmlDocument();
                    tarDoc.ReadNode(tarReader);
                    srcDoc.ReadNode(srcReader);

                    message = "";
                    foreach (XmlNode node in srcDoc.ChildNodes)
                    {
                        message += node.FirstChild.Name + "\r\n";
                    }
                    DA.SetData(0, message);

                    

                    // Initialize xmldiff
                    XmlDiff diff = new XmlDiff();

                    // Initialize xmlwriter and settingss
                    var settings = new XmlWriterSettings();
                    settings.OmitXmlDeclaration = true;
                    settings.Indent = true;
                    XmlWriter diffGram = XmlWriter.Create("diffgram.xml");

                    using (var content = new StreamReader(tarBlob.GetContentStream(), Encoding.UTF8))
                    {
                        XmlReader tarReader = XmlReader.Create(content.ReadToEnd());
                        XmlReader srcReader = XmlReader.Create(doc.FilePath);

                        XmlDocument xdoc = new XmlDocument();
                        XmlNode abc = xdoc.ReadNode(tarReader);

                        diff.Compare(srcReader, tarReader, diffGram);
                        DA.SetData(0, diff.ToString());
                    }
                    //Patch difference = repo.Diff.Compare<Patch>(targets[int.Parse(diffID[0])], DiffTargets.WorkingDirectory, tt);
                    
                }
                */
            }
            else
            {
                doc.ScheduleSolution(5, terminationCallback);
            }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => null;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("ba896193-fe68-46f0-b568-796c88675d8a");
    }
}