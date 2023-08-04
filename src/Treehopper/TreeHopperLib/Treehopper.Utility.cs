using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using TreeHopper.Deserialize;

namespace TreeHopper.Utility
{
    public class Helper
    {
        public void CompareGhx(GhxDocument src, GhxDocument trg)
        {
        }

        public static bool GetSource(XmlNodeList nodes, out List<Guid> guids)
        {
            guids = new List<Guid>();
            if (nodes.Count == 1)
            {
                foreach(XmlNode node in nodes)
                {
                    guids.Add(Guid.Parse(node.FirstChild.Value));
                }
                return true;
            }
            else { return false; }

        }

        public static bool GetGuid(XmlNodeList nodes, out Guid guid)
        {   
            guid = Guid.Empty;
            if (nodes.Count == 1)
            {
                XmlNode node = nodes[0];
                if (node.HasChildNodes && node.FirstChild.NodeType == XmlNodeType.Text)
                {
                    guid = Guid.Parse(node.FirstChild.Value);
                }
                return true;
            }
            else { return false; }
        }

        public static bool AddParam(XmlNodeList nodes, Dictionary<string, object>dict, Type type)
        {
            if (nodes.Count == 1)
            {
                XmlNode node = nodes[0];
                string name = node.Attributes["name"].Value;
                dynamic value = "";

                if (node.HasChildNodes && node.FirstChild.NodeType == XmlNodeType.Text)
                {
                    value = node.FirstChild.Value;
                    if (type != typeof(string))
                    {
                        var parse = type.GetMethod("Parse", new[] { typeof(string) });
                        var result = parse.Invoke(null, new object[] { value });
                        if (result != null) { value = result; }
                    }
                }

                dict.Add(name, value);
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool AddBounds(XmlNodeList nodes, Dictionary<string, object> dict)
        {
            if (nodes.Count == 1)
            {
                XmlNode node = nodes[0];
                string name = node.Attributes["name"].Value;
                int x = int.Parse(node.SelectSingleNode("X").FirstChild.Value);
                int y = int.Parse(node.SelectSingleNode("Y").FirstChild.Value);
                int width = int.Parse(node.SelectSingleNode("W").FirstChild.Value);
                int height = int.Parse(node.SelectSingleNode("H").FirstChild.Value);
                Rectangle rect = new Rectangle(x, y, width, height);
                dict.Add(name, rect);
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool AddPivot(XmlNodeList nodes, Dictionary<string, object> dict)
        {
            if (nodes.Count == 1)
            {
                XmlNode node = nodes[0];
                string name = node.Attributes["name"].Value;
                XmlNode test = node.SelectSingleNode("X");
                float x = float.Parse(node.SelectSingleNode("X").FirstChild.Value);
                float y = float.Parse(node.SelectSingleNode("Y").FirstChild.Value);
                PointF pt = new PointF(x,y);
                dict.Add(name, pt);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
