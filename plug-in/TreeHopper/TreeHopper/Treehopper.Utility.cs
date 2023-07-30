using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using TreeHopper.Deserialize;

namespace TreeHopper.Utility
{
    public class Helper
    {
        public void CompareGhx(GhxVersion src, GhxVersion trg)
        {

        }

        public static bool AddValue(XmlNodeList nodes, Dictionary<string, string>dict)
        {   
            if (nodes.Count > 0)
            {
                foreach (XmlNode node in nodes)
                {
                    string name = node.Attributes["name"].Value;
                    if (nodes.Count > 1) { name += node.Attributes["index"].Value; }

                    string value = "";

                    if (node.FirstChild.NodeType == XmlNodeType.Element)
                    {
                        foreach (XmlNode childNode in node.ChildNodes)
                        {
                            value += childNode.Name + ";" + childNode.FirstChild.Value;
                        }
                    }
                    else
                    {
                        value = node.InnerText;
                    }

                    dict.Add(name, value);
                    
                }
                return true;
            }
            else { return false; }
        }
    }
}
