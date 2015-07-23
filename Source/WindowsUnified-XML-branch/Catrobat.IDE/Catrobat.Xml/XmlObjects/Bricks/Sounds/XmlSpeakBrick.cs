﻿using System.Xml.Linq;

namespace Catrobat.IDE.Core.Xml.XmlObjects.Bricks.Sounds
{
    public partial class XmlSpeakBrick : XmlBrick
    {
        public string Text { get; set; }

        public XmlSpeakBrick() {}

        public XmlSpeakBrick(XElement xElement) : base(xElement) {}

        internal override void LoadFromXml(XElement xRoot)
        {
            if (xRoot.Element(XmlConstants.Text) != null)
            {
                Text = xRoot.Element(XmlConstants.Text).Value;
            }
        }

        internal override XElement CreateXml()
        {
            var xRoot = new XElement(XmlConstants.Brick);
            xRoot.SetAttributeValue(XmlConstants.Type, XmlConstants.XmlSpeakBrickType);

            if (Text != null)
            {
                xRoot.Add(new XElement(XmlConstants.Text)
                {
                    Value = Text
                });
            }

            //CreateCommonXML(xRoot);

            return xRoot;
        }
    }
}