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

namespace TreeHopper.Deserialize
{
    public class GhxVersion
    {
        private XmlNode SrcNodes;
        private List<Component> Components;
        private Dictionary<string, string> Parameters;

        public GhxVersion(Stream stream) 
        {
            using (var content = new StreamReader(stream, Encoding.UTF8))
            {
                this.Parameters = new Dictionary<string, string>();
                this.Components = new List<Component>();
                XmlReader reader = XmlReader.Create(content.ReadToEnd());
                XmlDocument document = new XmlDocument();
                document.Load(reader);
                this.SrcNodes = this.deserialize(document, out Components, out Parameters);
            }
        }
        public GhxVersion(string filepath)
        {
            this.Parameters = new Dictionary<string, string>();
            this.Components = new List<Component>();
            XmlReader reader = XmlReader.Create(filepath);
            XmlDocument document = new XmlDocument();
            document.Load(reader);
            this.SrcNodes = this.deserialize(document, out Components, out Parameters);
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

            XmlNodeList src_components = node.SelectNodes("chunks/chunk[@name='DefinitionObjects']/chunks/chunk");

            foreach (XmlNode src_component in src_components)
            {
                components.Add(new Component(src_component));
            }

            return src;
        }
    }

    public class Component
    {
        private XmlNode SrcNodes;
        private Dictionary<string, string> Parameters;
        private List<ComponentIO> IO;

        public Component(XmlNode src) 
        {
            this.SrcNodes = src;
            this.Parameters = new Dictionary<string, string>();
            this.IO = new List<ComponentIO>();

            // Adding param in parameter dictionay
            // Component Name
            Helper.AddValue(src.SelectNodes("items/item[@name='Name']"), this.Parameters);
            // InstanceGuid
            Helper.AddValue(src.SelectNodes("chunks/chunk[@name='Container']/items/item[@name='InstanceGuid']"), this.Parameters);
            // Source GUID for panel
            Helper.AddValue(src.SelectNodes("chunks/chunk[@name='Container']/items/item[@name='Source']"), this.Parameters);
            // ScriptSource for C# IDE
            Helper.AddValue(src.SelectNodes("chunks/chunk[@name='Container']/items/item[@name='ScriptSource']"), this.Parameters);
            // CodeInput for python IDE
            Helper.AddValue(src.SelectNodes("chunks/chunk[@name='Container']/items/item[@name='CodeInput']"), this.Parameters);
            // Bounds
            Helper.AddValue(src.SelectNodes("chunks/chunk[@name='Container']/chunks/chunk[@name='Attributes']/items/item[@name='Bounds']"), this.Parameters);
            // Pivot
            Helper.AddValue(src.SelectNodes("chunks/chunk[@name='Container']/chunks/chunk[@name='Attributes']/items/item[@name='Pivot']"), this.Parameters);
            // Slider Value
            Helper.AddValue(src.SelectNodes("chunks/chunk[@name='Container']/chunks/chunk[@name='Slider']/items/item[@name='Value']"), this.Parameters);

            // input for normal gh component
            XmlNodeList param_input = src.SelectNodes("chunks/chunk[@name='Container']/chunks/chunk[@name='param_input']");
            if (param_input != null)
            {   
                foreach (XmlNode ioNode in param_input)
                {
                    this.IO.Add(new ComponentIO(ioNode));
                }
            }
            // output for normal gh component
            XmlNodeList param_output = src.SelectNodes("chunks/chunk[@name='Container']/chunks/chunk[@name='param_output']");
            if (param_output != null)
            {
                foreach (XmlNode ioNode in param_output)
                {
                    this.IO.Add(new ComponentIO(ioNode));
                }
            }
            // input for IDE
            XmlNodeList parameter_data = src.SelectNodes("chunks/chunk[@name='Container']/chunks/chunk[@name='ParameterData']/chunks/chunk");
            if (parameter_data != null)
            {
                foreach (XmlNode ioNode in param_output)
                {
                    this.IO.Add(new ComponentIO(ioNode));
                }
            }
        }
    }

    public class ComponentIO
    {
        private XmlNode SrcNodes;
        private Dictionary<string, string> Parameters;
        private XmlNode PData;

        public ComponentIO(XmlNode src)
        {
            this.SrcNodes = src;
            this.Parameters = new Dictionary<string, string>();

            // check if it's normal GH component
            if (src.Attributes["name"].Value == "param_input" || src.Attributes["name"].Value == "param_output")
            {
                // IO Type
                this.Parameters.Add("Type", src.Attributes["name"].Value);
                // IO Name
                Helper.AddValue(src.SelectNodes("items/item[@name='Name']"), this.Parameters);
                // Instance Guid
                Helper.AddValue(src.SelectNodes("items/item[@name='InstanceGuid']"), this.Parameters);
                // Source Guid
                Helper.AddValue(src.SelectNodes("items/item[@name='Source']"), this.Parameters);
                // Source Count
                Helper.AddValue(src.SelectNodes("items/item[@name='SourceCount']"), this.Parameters);
                // Bounds
                Helper.AddValue(src.SelectNodes("chunks/chunk[@name='Attributes']/items/item[@name='Bounds']"), this.Parameters);
                // Pivot
                Helper.AddValue(src.SelectNodes("chunks/chunk[@name='Attributes']/items/item[@name='Pivot']"), this.Parameters);
                // pData
                this.PData = src.SelectSingleNode("chunks/chunk[@name='PersistentData']");
            }
            else
            {
                // IO Type
                this.Parameters.Add("Type", src.Attributes["name"].Value);
                // IO Name
                Helper.AddValue(src.SelectNodes("items/item[@name='Name']"), this.Parameters);
                // Instance Guid
                Helper.AddValue(src.SelectNodes("items/item[@name='InstanceGuid']"), this.Parameters);
                // Source Guid
                Helper.AddValue(src.SelectNodes("items/item[@name='Source']"), this.Parameters);
                // Source Count
                Helper.AddValue(src.SelectNodes("items/item[@name='SourceCount']"), this.Parameters);
                // Bounds
                Helper.AddValue(src.SelectNodes("chunks/chunk[@name='Attributes']/items/item[@name='Bounds']"), this.Parameters);
                // Pivot
                Helper.AddValue(src.SelectNodes("chunks/chunk[@name='Attributes']/items/item[@name='Pivot']"), this.Parameters);
                // pData
                this.PData = src.SelectSingleNode("chunks/chunk[@name='PersistentData']");
            }

        }
    }
}
