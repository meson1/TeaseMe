﻿using System;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

namespace TeaseMe.Common
{
    public class TeaseSerializer
    {
        public string ConvertToXmlString(Tease tease)
        {
            var xmldoc = new XmlDocument();

            // Normal XML serialization.
            using (var stream = new MemoryStream())
            {
                new XmlSerializer(typeof(Tease)).Serialize(stream, tease);
                stream.Position = 0;
                xmldoc.Load(stream);
            }

            // A little XML reordering to make a beautiful XML output format, easier than implementing IXmlSerializable
            // for all the tease classes.
            if (xmldoc.DocumentElement != null)
            {
                // Remove the nonused namespace definitions.
                xmldoc.DocumentElement.RemoveAttribute("xmlns:xsi");
                xmldoc.DocumentElement.RemoveAttribute("xmlns:xsd");

                // Make the XML more intuitive by moving the Pages after the genereal tease information.
                var pagesNode = xmldoc.DocumentElement["Pages"];
                if (pagesNode != null)
                {
                    var node = xmldoc.DocumentElement.RemoveChild(pagesNode);
                    xmldoc.DocumentElement.AppendChild(node);
                }

                // The same for the Buttons, put them at the end of a Page.
                var pageNodes = xmldoc.SelectNodes("/Tease/Pages/Page");
                if (pageNodes != null)
                {
                    foreach (XmlNode pageNode in pageNodes)
                    {
                        var buttonNodes = pageNode.SelectNodes("Button");
                        if (buttonNodes != null)
                        {
                            foreach (XmlNode buttonNode in buttonNodes)
                            {
                                pageNode.RemoveChild(buttonNode);
                            }
                            foreach (XmlNode buttonNode in buttonNodes)
                            {
                                pageNode.AppendChild(buttonNode);
                            }
                        }

                        var commentsNode = pageNode.SelectSingleNode("Comments");
                        if (commentsNode != null)
                        {
                            pageNode.RemoveChild(commentsNode);

                            // The replacement of -- is needed for the serializer to function correctly, an XML comment cannot contain --.
                            pageNode.InsertBefore(xmldoc.CreateComment(String.Format(" {0} ", HttpUtility.HtmlDecode(commentsNode.InnerXml).Replace("--","- - "))), pageNode.FirstChild);
                        }

                        var errorsNode = pageNode.SelectSingleNode("Errors");
                        if (errorsNode != null)
                        {
                            pageNode.RemoveChild(errorsNode);
                            pageNode.InsertBefore(errorsNode, pageNode.FirstChild);
                        }
                    }
                }
            }

            // Convert the XML document to a string.
            using (var stream = new MemoryStream())
            {
                using (var writer = new XmlTextWriter(stream, Encoding.UTF8))
                {
                    writer.Formatting = Formatting.Indented;
                    xmldoc.Save(writer);

                    stream.Position = 0;
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }
    }
}
