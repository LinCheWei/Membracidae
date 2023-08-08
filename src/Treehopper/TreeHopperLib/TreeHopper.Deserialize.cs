using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using TreeHopper.Utility;
using System.Drawing;

namespace TreeHopper.Deserialize
{
    public class GhxDocument
    {
        private XmlNode srcNodes; // the original xmlnode  
        private List<Component> components; // list of GH components within this file 
        private List<ComponentIO> inputIOs; // list of input param for all the components in this file
        private Dictionary<string, object> parameters; // Dictionary for all the parameters
        private Guid guid; // guid of this GH file

        public List<Component> Components { get { return components; } }
        public XmlNode SrcNodes { get { return srcNodes; } }
        public Guid Guid { get { return guid; } }
        public Dictionary<string, object> Parameters { get { return parameters; } }
        public List<ComponentIO> InputIOs { get { return inputIOs; } }

        public GhxDocument(Stream stream)
        {
            // initialize ghxDocument from stream, when getting files from git 
            using (var content = new StreamReader(stream, Encoding.UTF8))
            {
                this.parameters = new Dictionary<string, object>();
                this.components = new List<Component>();
                this.guid = Guid.Empty;
                XmlReader reader = XmlReader.Create(content.ReadToEnd());
                XmlDocument document = new XmlDocument();
                document.Load(reader);
                this.deserialize(document);
                this.InitializeConnections();
            }
        }
        public GhxDocument(string filepath)
        {
            // initialize ghxDocument from file path, when getting the current state of the file
            this.parameters = new Dictionary<string, object>();
            this.components = new List<Component>();
            this.guid = Guid.Empty;
            XmlReader reader = XmlReader.Create(filepath);
            XmlDocument document = new XmlDocument();
            document.Load(reader);
            this.deserialize(document);
            this.InitializeConnections();
        }

        private void deserialize(XmlNode node)
        {
            // parsing through the ghx file

            this.srcNodes = node.SelectSingleNode("/Archive/chunks/chunk[@name='Definition']");

            // Adding param in parameter dictionay
            // Plugin version
            //Helper.AddValue(src.SelectNodes("items/item[@name='plugin_version']"), dict);
            // Document Id  
            Helper.GetGuid(srcNodes.SelectNodes("chunks/chunk[@name='DocumentHeader']/items/item[@name='DocumentID']"), out this.guid);
            // Doc Name
            Helper.AddParam(srcNodes.SelectNodes("chunks/chunk[@name='DefinitionProperties']/items/item[@name='Name']"), this.parameters, typeof(string));
            // Object Count
            Helper.AddParam(srcNodes.SelectNodes("chunks/chunk[@name='DefinitionObjects']/items/item[@name='ObjectCount']"), this.parameters, typeof(int));

            XmlNodeList src_components = srcNodes.SelectNodes("chunks/chunk[@name='DefinitionObjects']/chunks/chunk");

            for (int i = 0; i < src_components.Count; i++)
            {
                this.components.Add(new Component(src_components[i], i));
            }
        }

        public dynamic Parameter(string key)
        {
            dynamic obj = null;
            this.parameters.TryGetValue(key, out obj);
            return obj;
        }

        private void InitializeConnections()
        {
            // initialize the relationship between components, call after all the components are parsed
            this.inputIOs = new List<ComponentIO>();
            List<Component> comps = this.components.FindAll(component => component.HasInputIO);

            foreach (Component c in comps)
            {
                List<ComponentIO> cios = c.IO.FindAll(cio => cio.Type == "Input" && cio.IsConnected);
                this.inputIOs.AddRange(cios);

                foreach (ComponentIO io in c.IO)
                {
                    foreach (Guid guid in io.Sources)
                    {
                        dynamic r;
                        this.SearchGuid(guid, out r);
                        if (r.GetType() == typeof(Component))
                        {
                            // somehow a boolean toggle doesn't have a pivot
                            RectangleF bounds = r.Parameter("Bounds");
                            io.AddConnection(new Connection(r.Index, new PointF(bounds.X + bounds.Width, bounds.Y + bounds.Height / 2)));
                        }
                        else
                        {
                            RectangleF bounds = r.Parameter("Bounds");
                            io.AddConnection(new Connection(r.ComponentIndex, r.Index, new PointF(bounds.X + bounds.Width, bounds.Y + bounds.Height / 2)));
                            r.IsConnected = true;
                        }
                    }
                }
            }
        }

        public bool SearchGuid(Guid key, out dynamic result)
        {
            // search for centain component or componentIO that has the search guid
            result = null;
            foreach (Component c in this.components)
            {
                if (c.InstanceGuid == key) { result = c; return true; }
                else if (c.HasIO)
                {
                    foreach (ComponentIO cio in c.IO)
                    {
                        if (cio.InstanceGuid == key) { result = cio; return true; }
                    }
                }
            }
            return false;
        }
    }

    public class Component
    {
        private int index; // component index
        private XmlNode srcNodes; // original node
        private Guid instanceGuid;
        private Dictionary<string, object> parameters;
        private List<ComponentIO> io;

        public List<ComponentIO> IO { get { return io; } }
        public XmlNode SrcNodes { get { return srcNodes; } }
        public int Index { get { return index; } }
        public Dictionary<string, object> Parameters { get { return parameters; } }
        public Guid InstanceGuid { get { return instanceGuid; } }
        public bool HasIO { get { return this.io.Count > 0; } }
        public bool HasInputIO { get { return this.io.FindAll(cio => cio.Type == "Input").Count > 0; } }
        public bool HasOutputIO { get { return this.io.FindAll(cio => cio.Type == "Output").Count > 0; } }

        public Component(XmlNode src, int id)
        {
            this.index = id;
            this.srcNodes = src;
            this.parameters = new Dictionary<string, object>();
            this.io = new List<ComponentIO>();
            this.instanceGuid = Guid.Empty;

            // Adding param in parameter dictionay
            // Component Name
            Helper.AddParam(src.SelectNodes("items/item[@name='Name']"), this.parameters, typeof(string));
            // InstanceGuid
            Helper.GetGuid(src.SelectNodes("chunks/chunk[@name='Container']/items/item[@name='InstanceGuid']"), out this.instanceGuid);
            // Source GUID for panel
            Helper.AddParam(src.SelectNodes("chunks/chunk[@name='Container']/items/item[@name='Source']"), this.parameters, typeof(Guid));
            // ScriptSource for C# IDE
            Helper.AddParam(src.SelectNodes("chunks/chunk[@name='Container']/items/item[@name='ScriptSource']"), this.parameters, typeof(string));
            // CodeInput for python IDE
            Helper.AddParam(src.SelectNodes("chunks/chunk[@name='Container']/items/item[@name='CodeInput']"), this.parameters, typeof(string));
            // Bounds
            Helper.AddBounds(src.SelectNodes("chunks/chunk[@name='Container']/chunks/chunk[@name='Attributes']/items/item[@name='Bounds']"), this.parameters);
            // Pivot
            Helper.AddPivot(src.SelectNodes("chunks/chunk[@name='Container']/chunks/chunk[@name='Attributes']/items/item[@name='Pivot']"), this.parameters);
            // Slider Value
            Helper.AddParam(src.SelectNodes("chunks/chunk[@name='Container']/chunks/chunk[@name='Slider']/items/item[@name='Value']"), this.parameters, typeof(double));

            // input for normal gh component
            XmlNodeList param_input = src.SelectNodes("chunks/chunk[@name='Container']/chunks/chunk[@name='param_input']");
            if (param_input != null)
            {
                for (int i = 0; i < param_input.Count; i++)
                {
                    this.io.Add(new ComponentIO(param_input[i], this.index, i));
                }
            }
            // output for normal gh component
            XmlNodeList param_output = src.SelectNodes("chunks/chunk[@name='Container']/chunks/chunk[@name='param_output']");
            if (param_output != null)
            {
                for (int i = 0; i < param_output.Count; i++)
                {
                    this.io.Add(new ComponentIO(param_output[i], this.index, i));
                }
            }
            // input for IDE
            XmlNodeList parameter_data = src.SelectNodes("chunks/chunk[@name='Container']/chunks/chunk[@name='ParameterData']/chunks/chunk");
            if (parameter_data != null)
            {
                for (int i = 0; i < parameter_data.Count; i++)
                {
                    this.io.Add(new ComponentIO(parameter_data[i], this.index, i));
                }
            }
        }
        public dynamic Parameter(string key)
        {
            dynamic obj = null;
            this.parameters.TryGetValue(key, out obj);
            return obj;
        }
    }

    public class ComponentIO
    {
        private Guid instanceGuid;
        private int componentIndex; // index of the component who owns this IO
        private int index; // index of this IO in the component.IO
        private string type; // type of the IO, can be Input or Output
        private XmlNode srcNodes; // original node
        private XmlNode pData; // persistance data, maybe it'll be helpful?
        private Dictionary<string, object> parameters;
        private List<Guid> sources; // list of source guid for the IO, in order
        private List<Connection> connections; // list of connections for this IO
        public bool IsConnected;

        public XmlNode PData { get { return this.pData; } }
        public XmlNode SrcNodes { get { return this.srcNodes; } }
        public List<Guid> Sources { get { return this.sources; } }
        public List<Connection> Connections { get { return this.connections; } }
        public string Type { get { return this.type; } }
        public Guid InstanceGuid { get { return this.instanceGuid; } }
        public Dictionary<string, object> Parameters { get { return parameters; } }
        public int Index { get { return index; } }
        public int ComponentIndex { get { return componentIndex; } }
        public void AddConnection(Connection conn) { this.connections.Add(conn); }

        public ComponentIO(XmlNode src, int cid, int id)
        {
            this.srcNodes = src;
            this.componentIndex = cid;
            this.index = id;
            this.parameters = new Dictionary<string, object>();
            this.instanceGuid = Guid.Empty;
            this.IsConnected = false;
            this.connections = new List<Connection>();

            // check if it's normal GH component
            if (src.Attributes["name"].Value == "param_input" || src.Attributes["name"].Value == "param_output")
            {
                // IO Type
                if (src.Attributes["name"].Value == "param_input") { this.type = "Input"; }
                else { this.type = "Output"; }

                // IO Name
                Helper.AddParam(src.SelectNodes("items/item[@name='Name']"), this.parameters, typeof(string));
                // Instance Guid
                Helper.GetGuid(src.SelectNodes("items/item[@name='InstanceGuid']"), out this.instanceGuid);
                // Source Count
                Helper.AddParam(src.SelectNodes("items/item[@name='SourceCount']"), this.parameters, typeof(int));
                // Bounds
                Helper.AddBounds(src.SelectNodes("chunks/chunk[@name='Attributes']/items/item[@name='Bounds']"), this.parameters);
                // Pivot
                Helper.AddPivot(src.SelectNodes("chunks/chunk[@name='Attributes']/items/item[@name='Pivot']"), this.parameters);
                // Source Guid
                this.IsConnected = Helper.GetSource(src.SelectNodes("items/item[@name='Source']"), out this.sources);

                // pData
                this.pData = src.SelectSingleNode("chunks/chunk[@name='PersistentData']");
            }
            else
            {
                // IO Type
                if (src.Attributes["name"].Value == "InputParam") { this.type = "Input"; }
                else { this.type = "Output"; }

                // IO Name
                Helper.AddParam(src.SelectNodes("items/item[@name='Name']"), this.parameters, typeof(string));
                // Instance Guid
                Helper.GetGuid(src.SelectNodes("items/item[@name='InstanceGuid']"), out this.instanceGuid);
                // Source Count
                Helper.AddParam(src.SelectNodes("items/item[@name='SourceCount']"), this.parameters, typeof(int));
                // Bounds
                Helper.AddBounds(src.SelectNodes("chunks/chunk[@name='Attributes']/items/item[@name='Bounds']"), this.parameters);
                // Pivot
                Helper.AddPivot(src.SelectNodes("chunks/chunk[@name='Attributes']/items/item[@name='Pivot']"), this.parameters);
                // Source Guid
                this.IsConnected = Helper.GetSource(src.SelectNodes("items/item[@name='Source']"), out this.sources);
                // pData
                this.pData = src.SelectSingleNode("chunks/chunk[@name='PersistentData']");
            }
        }
        public dynamic Parameter(string key)
        {
            dynamic obj = null;
            this.parameters.TryGetValue(key, out obj);
            return obj;
        }
    }

    public class Connection
    {
        // each connection represent a wire belongs to that IO, the target can be either another IO (normally) or another component (panels, slider)  
        private int componentIndex; // index for the target component, always valid
        private int ioIndex; // index for the target IO in the target component, null when the target is slider or panel ...etc.
        private PointF pivot; // pointf of the other side of the wire

        public int ComponentIndex { get { return this.componentIndex; } }
        public int IoIndex { get { return this.ioIndex; } }
        public PointF Pivot { get { return this.pivot; } }

        public Connection(int compnentId, int ioId, PointF pt)
        {
            this.componentIndex = compnentId;
            this.ioIndex = ioId;
            this.pivot = pt;
        }

        public Connection(int compnentId, PointF pt)
        {
            this.componentIndex = compnentId;
            this.pivot = pt;
        }
    }
}
