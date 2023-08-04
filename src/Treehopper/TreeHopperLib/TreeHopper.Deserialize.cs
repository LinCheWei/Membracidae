using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using TreeHopper.Utility;
using System.Drawing;
using System.Drawing.Printing;

namespace TreeHopper.Deserialize
{
    public class GhxDocument
    {
        private XmlNode srcNodes;
        private List<Component> components;
        private Dictionary<string, object> parameters;
        private Guid guid;

        public List<Component> Components { get { return components; } }
        public XmlNode SrcNodes { get { return srcNodes; } } 
        public Guid Guid { get { return guid; } }
        public Dictionary<string, object> Parameters { get { return parameters; } }

        public GhxDocument(Stream stream) 
        {
            using (var content = new StreamReader(stream, Encoding.UTF8))
            {
                this.parameters = new Dictionary<string, object>();
                this.components = new List<Component>();
                this.guid = Guid.Empty;
                XmlReader reader = XmlReader.Create(content.ReadToEnd());
                XmlDocument document = new XmlDocument();
                document.Load(reader);
                this.srcNodes = this.deserialize(document);
            }
        }
        public GhxDocument(string filepath)
        {
            this.parameters = new Dictionary<string, object>();
            this.components = new List<Component>();
            this.guid = Guid.Empty;
            XmlReader reader = XmlReader.Create(filepath);
            XmlDocument document = new XmlDocument();
            document.Load(reader);
            this.srcNodes = this.deserialize(document);
        }

        private XmlNode deserialize(XmlNode node)
        {         
            XmlNode src = node.SelectSingleNode("/Archive/chunks/chunk[@name='Definition']");

            // Adding param in parameter dictionay
            // Plugin version
            //Helper.AddValue(src.SelectNodes("items/item[@name='plugin_version']"), dict);
            // Document Id  
            Helper.GetGuid(src.SelectNodes("chunks/chunk[@name='DocumentHeader']/items/item[@name='DocumentID']"), out this.guid);
            // Doc Name
            Helper.AddParam(src.SelectNodes("chunks/chunk[@name='DefinitionProperties']/items/item[@name='Name']"), this.parameters, typeof(string));
            // Object Count
            Helper.AddParam(src.SelectNodes("chunks/chunk[@name='DefinitionObjects']/items/item[@name='ObjectCount']"), this.parameters, typeof(int));

            XmlNodeList src_components = src.SelectNodes("chunks/chunk[@name='DefinitionObjects']/chunks/chunk");

            for (int i = 0; i < src_components.Count; i++)
            {
                this.components.Add(new Component(src_components[i], i));
            }

            return src;
        }

        public dynamic Parameter(string key)
        {
            dynamic obj = null;
            this.parameters.TryGetValue(key, out obj);
            return obj;
        }
    }

    public class Component
    {
        private int index;
        private XmlNode srcNodes;
        private Guid instanceGuid;
        private Dictionary<string, object> parameters;
        private List<ComponentIO> io;

        public List<ComponentIO> IO { get { return io; } }
        public XmlNode SrcNodes { get { return srcNodes; } }
        public int Index { get { return index; } }
        public Dictionary<string, object> Parameters { get { return parameters; } }
        public Guid InstanceGuid { get { return instanceGuid; } } 

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
                foreach (XmlNode ioNode in param_input)
                {
                    this.io.Add(new ComponentIO(ioNode));
                }
            }
            // output for normal gh component
            XmlNodeList param_output = src.SelectNodes("chunks/chunk[@name='Container']/chunks/chunk[@name='param_output']");
            if (param_output != null)
            {
                foreach (XmlNode ioNode in param_output)
                {
                    this.io.Add(new ComponentIO(ioNode));
                }
            }
            // input for IDE
            XmlNodeList parameter_data = src.SelectNodes("chunks/chunk[@name='Container']/chunks/chunk[@name='ParameterData']/chunks/chunk");
            if (parameter_data != null)
            {
                foreach (XmlNode ioNode in parameter_data)
                {
                    this.io.Add(new ComponentIO(ioNode));
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
        private string type;
        private XmlNode srcNodes;
        private XmlNode pData;
        private Dictionary<string, object> parameters;
        private List<Guid> source;

        public XmlNode PData { get { return this.pData; } }
        public XmlNode SrcNodes { get { return this.srcNodes; } }
        public List<Guid> Source { get { return this.source; } }
        public string Type { get { return this.type; } }
        public Guid InstanceGuid { get { return this.instanceGuid; } }
        public Dictionary<string, object> Parameters { get { return parameters; } }

        public ComponentIO(XmlNode src)
        {
            this.srcNodes = src;
            this.parameters = new Dictionary<string, object>();
            this.instanceGuid = Guid.Empty;

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
                Helper.GetSource(src.SelectNodes("items/item[@name='Source']"), out this.source);

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
                Helper.GetSource(src.SelectNodes("items/item[@name='Source']"), out this.source);
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
}
