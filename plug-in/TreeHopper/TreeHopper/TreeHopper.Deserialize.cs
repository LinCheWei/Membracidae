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

namespace TreeHopper.Deserialize
{
    public class GhxVersion
    {
        public List<Component> Components;

        public GhxVersion(Stream stream) 
        {
            using (var content = new StreamReader(stream, Encoding.UTF8))
            {
                XmlReader reader = XmlReader.Create(content.ReadToEnd());
                XmlDocument document = new XmlDocument();
                this.serialize(document.ReadNode(reader), out Components);
            }
        }

        public GhxVersion(string filepath)
        {
            XmlReader reader = XmlReader.Create(filepath);
            XmlDocument document = new XmlDocument();
            this.serialize(document.ReadNode(reader), out Components);
        }

        private void serialize(XmlNode node, out List<Component> components)
        {
            components = new List<Component>();
        }
    }

    public class Component
    {
        public Component() { }
    }
}
