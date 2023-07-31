using LibGit2Sharp;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using GH_IO.Serialization;
using TreeHopper.Utility;
using System.Security.Cryptography;

namespace TreeHopper.Deserialize
{
    public class GhxVersion
    {
        private XmlNode srcNodes;
        private List<Component> components;
        private Dictionary<string, string> parameters;

        public Dictionary<string, string> Parameters() { return parameters; }
        public List<Component> Components() { return components; }
        public XmlNode SrcNodes() { return srcNodes; } 

        public GhxVersion(Stream stream) 
        {
            using (var content = new StreamReader(stream, Encoding.UTF8))
            {
                this.parameters = new Dictionary<string, string>();
                this.components = new List<Component>();
                XmlReader reader = XmlReader.Create(content.ReadToEnd());
                XmlDocument document = new XmlDocument();
                document.Load(reader);
                this.srcNodes = this.deserialize(document, out components, out parameters);
            }
        }
        public GhxVersion(string filepath)
        {
            this.parameters = new Dictionary<string, string>();
            this.components = new List<Component>();
            XmlReader reader = XmlReader.Create(filepath);
            XmlDocument document = new XmlDocument();
            document.Load(reader);
            this.srcNodes = this.deserialize(document, out components, out parameters);
        }

        private XmlNode deserialize(XmlNode node, out List<Component> components, out Dictionary<string, string> dict)
        {
            components = new List<Component>();
            dict = new Dictionary<string, string>();
            XmlNode src = node.SelectSingleNode("/Archive/chunks/chunk[@name='Definition']");

            // Adding param in parameter dictionay
            // Plugin version
            Helper.AddValue(src.SelectNodes("items/item[@name='plugin_version']"), dict);
            // Document Id  
            Helper.AddValue(src.SelectNodes("chunks/chunk[@name='DocumentHeader']/items/item[@name='DocumentID']"), dict);
            // Doc Name
            Helper.AddValue(src.SelectNodes("chunks/chunk[@name='DefinitionProperties']/items/item[@name='Name']"), dict);
            // Object Count
            Helper.AddValue(src.SelectNodes("chunks/chunk[@name='DefinitionObjects']/items/item[@name='ObjectCount']"), dict);

            XmlNodeList src_components = src.SelectNodes("chunks/chunk[@name='DefinitionObjects']/chunks/chunk");

            foreach (XmlNode src_component in src_components)
            {
                components.Add(new Component(src_component));
            }

            return src;
        }
    }

    public class Component
    {
        private XmlNode srcNodes;
        private Dictionary<string, string> parameters;
        private List<ComponentIO> io;

        public Dictionary<string, string> Parameters() { return this.parameters; }
        public List<ComponentIO> IOs() { return this.io; }
        public XmlNode SrcNodes() { return this.srcNodes; }

        public Component(XmlNode src)
        {
            this.srcNodes = src;
            this.parameters = new Dictionary<string, string>();
            this.io = new List<ComponentIO>();

            // Adding param in parameter dictionay
            // Component Name
            Helper.AddValue(src.SelectNodes("items/item[@name='Name']"), this.parameters).ToString();
            // InstanceGuid
            Helper.AddValue(src.SelectNodes("chunks/chunk[@name='Container']/items/item[@name='InstanceGuid']"), this.parameters);
            // Source GUID for panel
            Helper.AddValue(src.SelectNodes("chunks/chunk[@name='Container']/items/item[@name='Source']"), this.parameters);
            // ScriptSource for C# IDE
            Helper.AddValue(src.SelectNodes("chunks/chunk[@name='Container']/items/item[@name='ScriptSource']"), this.parameters);
            // CodeInput for python IDE
            Helper.AddValue(src.SelectNodes("chunks/chunk[@name='Container']/items/item[@name='CodeInput']"), this.parameters);
            // Bounds
            Helper.AddValue(src.SelectNodes("chunks/chunk[@name='Container']/chunks/chunk[@name='Attributes']/items/item[@name='Bounds']"), this.parameters);
            // Pivot
            Helper.AddValue(src.SelectNodes("chunks/chunk[@name='Container']/chunks/chunk[@name='Attributes']/items/item[@name='Pivot']"), this.parameters);
            // Slider Value
            Helper.AddValue(src.SelectNodes("chunks/chunk[@name='Container']/chunks/chunk[@name='Slider']/items/item[@name='Value']"), this.parameters);

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
                foreach (XmlNode ioNode in param_output)
                {
                    this.io.Add(new ComponentIO(ioNode));
                }
            }
        }

    }

    public class ComponentIO
    {
        private XmlNode srcNodes;
        private Dictionary<string, string> parameters;
        private XmlNode pData;

        public Dictionary<string, string> Parameters() { return this.parameters; }
        public XmlNode PData() { return this.pData; }
        public XmlNode SrcNodes() { return this.srcNodes; }

        public ComponentIO(XmlNode src)
        {
            this.srcNodes = src;
            this.parameters = new Dictionary<string, string>();

            // check if it's normal GH component
            if (src.Attributes["name"].Value == "param_input" || src.Attributes["name"].Value == "param_output")
            {
                // IO Type
                this.parameters.Add("Type", src.Attributes["name"].Value);
                // IO Name
                Helper.AddValue(src.SelectNodes("items/item[@name='Name']"), this.parameters);
                // Instance Guid
                Helper.AddValue(src.SelectNodes("items/item[@name='InstanceGuid']"), this.parameters);
                // Source Guid
                Helper.AddValue(src.SelectNodes("items/item[@name='Source']"), this.parameters);
                // Source Count
                Helper.AddValue(src.SelectNodes("items/item[@name='SourceCount']"), this.parameters);
                // Bounds
                Helper.AddValue(src.SelectNodes("chunks/chunk[@name='Attributes']/items/item[@name='Bounds']"), this.parameters);
                // Pivot
                Helper.AddValue(src.SelectNodes("chunks/chunk[@name='Attributes']/items/item[@name='Pivot']"), this.parameters);
                // pData
                this.pData = src.SelectSingleNode("chunks/chunk[@name='PersistentData']");
            }
            else
            {
                // IO Type
                this.parameters.Add("Type", src.Attributes["name"].Value);
                // IO Name
                Helper.AddValue(src.SelectNodes("items/item[@name='Name']"), this.parameters);
                // Instance Guid
                Helper.AddValue(src.SelectNodes("items/item[@name='InstanceGuid']"), this.parameters);
                // Source Guid
                Helper.AddValue(src.SelectNodes("items/item[@name='Source']"), this.parameters);
                // Source Count
                Helper.AddValue(src.SelectNodes("items/item[@name='SourceCount']"), this.parameters);
                // Bounds
                Helper.AddValue(src.SelectNodes("chunks/chunk[@name='Attributes']/items/item[@name='Bounds']"), this.parameters);
                // Pivot
                Helper.AddValue(src.SelectNodes("chunks/chunk[@name='Attributes']/items/item[@name='Pivot']"), this.parameters);
                // pData
                this.pData = src.SelectSingleNode("chunks/chunk[@name='PersistentData']");
            }
        }
    }
}
