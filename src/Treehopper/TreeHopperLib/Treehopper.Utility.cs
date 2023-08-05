using System;
using System.Collections.Generic;
using System.Drawing;
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
            //This method specialize to parse all source guid into a list of guid that follows the order from a single input param
            guids = new List<Guid>();
            if (nodes.Count > 0)
            {
                foreach (XmlNode node in nodes)
                {
                    guids.Add(Guid.Parse(node.FirstChild.Value));
                }
                int test = guids.Count;
                return true;
            }
            else { return false; }

        }

        public static bool GetGuid(XmlNodeList nodes, out Guid guid)
        {
            //This method specialize to parse GUID into independent class properties
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

        public static bool AddParam(XmlNodeList nodes, Dictionary<string, object> dict, Type type)
        {
            //This method only works for nodes than have one childnode, for parameters like bound and pivot see AddBounds() & AddPivot()
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
                        //get parse method from the given type
                        var parse = type.GetMethod("Parse", new[] { typeof(string) });
                        //Invoke the parse method, convert string to given type
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
            //this method specialize to parse the bounds of a component into System.Drawing.Rectangle
            if (nodes.Count == 1)
            {
                XmlNode node = nodes[0];
                string name = node.Attributes["name"].Value;
                float x = float.Parse(node.SelectSingleNode("X").FirstChild.Value);
                float y = float.Parse(node.SelectSingleNode("Y").FirstChild.Value);
                float width = float.Parse(node.SelectSingleNode("W").FirstChild.Value);
                float height = float.Parse(node.SelectSingleNode("H").FirstChild.Value);
                RectangleF rect = new RectangleF(x, y, width, height);
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
            //this method specialize to parse the pivot of a component into System.Drawing.PointF
            if (nodes.Count == 1)
            {
                XmlNode node = nodes[0];
                string name = node.Attributes["name"].Value;
                XmlNode test = node.SelectSingleNode("X");
                float x = float.Parse(node.SelectSingleNode("X").FirstChild.Value);
                float y = float.Parse(node.SelectSingleNode("Y").FirstChild.Value);
                PointF pt = new PointF(x, y);
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
