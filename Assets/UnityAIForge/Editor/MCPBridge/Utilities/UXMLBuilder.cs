using System.Collections.Generic;
using System.Xml.Linq;

namespace MCP.Editor.Utilities
{
    /// <summary>
    /// Programmatic UXML builder using System.Xml.Linq for safe XML construction.
    /// Handles attribute escaping automatically, eliminating manual EscapeXml() calls.
    /// </summary>
    internal class UXMLBuilder
    {
        private static readonly XNamespace UiNs = "UnityEngine.UIElements";
        private static readonly XNamespace UieNs = "UnityEditor.UIElements";

        private readonly XElement _root;
        private readonly XDocument _doc;

        public UXMLBuilder()
        {
            _root = new XElement(UiNs + "UXML",
                new XAttribute(XNamespace.Xmlns + "ui", UiNs.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "uie", UieNs.NamespaceName));
            _doc = new XDocument(_root);
        }

        /// <summary>
        /// Adds a &lt;Style src="..." /&gt; element referencing a USS file.
        /// </summary>
        public UXMLBuilder AddStyleSheet(string ussFileName)
        {
            _root.Add(new XElement("Style", new XAttribute("src", ussFileName)));
            return this;
        }

        /// <summary>
        /// Creates a ui:VisualElement with the given name and CSS classes.
        /// Returns the XElement so children can be added.
        /// </summary>
        public XElement AddVisualElement(string name = null, params string[] cssClasses)
        {
            return AddVisualElement(_root, name, cssClasses);
        }

        /// <summary>
        /// Creates a ui:VisualElement under a parent element.
        /// </summary>
        public XElement AddVisualElement(XElement parent, string name = null, params string[] cssClasses)
        {
            var elem = new XElement(UiNs + "VisualElement");
            if (!string.IsNullOrEmpty(name))
                elem.SetAttributeValue("name", name);
            if (cssClasses.Length > 0)
                elem.SetAttributeValue("class", string.Join(" ", cssClasses));
            parent.Add(elem);
            return elem;
        }

        /// <summary>
        /// Creates a ui:Button under a parent element. Text is automatically XML-escaped.
        /// </summary>
        public XElement AddButton(XElement parent, string name, string text, params string[] cssClasses)
        {
            var elem = new XElement(UiNs + "Button");
            if (!string.IsNullOrEmpty(name))
                elem.SetAttributeValue("name", name);
            if (text != null)
                elem.SetAttributeValue("text", text);
            if (cssClasses.Length > 0)
                elem.SetAttributeValue("class", string.Join(" ", cssClasses));
            parent.Add(elem);
            return elem;
        }

        /// <summary>
        /// Creates a ui:Label under a parent element.
        /// </summary>
        public XElement AddLabel(XElement parent, string name, string text, params string[] cssClasses)
        {
            var elem = new XElement(UiNs + "Label");
            if (!string.IsNullOrEmpty(name))
                elem.SetAttributeValue("name", name);
            if (text != null)
                elem.SetAttributeValue("text", text);
            if (cssClasses.Length > 0)
                elem.SetAttributeValue("class", string.Join(" ", cssClasses));
            parent.Add(elem);
            return elem;
        }

        /// <summary>
        /// Creates a generic UI element under a parent.
        /// </summary>
        public XElement AddElement(XElement parent, string elementType, string name = null, string text = null,
            params string[] cssClasses)
        {
            var elem = new XElement(UiNs + elementType);
            if (!string.IsNullOrEmpty(name))
                elem.SetAttributeValue("name", name);
            if (text != null)
                elem.SetAttributeValue("text", text);
            if (cssClasses.Length > 0)
                elem.SetAttributeValue("class", string.Join(" ", cssClasses));
            parent.Add(elem);
            return elem;
        }

        /// <summary>
        /// Sets arbitrary attributes on an element.
        /// </summary>
        public static void SetAttributes(XElement element, Dictionary<string, string> attributes)
        {
            if (attributes == null) return;
            foreach (var kvp in attributes)
            {
                element.SetAttributeValue(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Renders the UXML document as a string.
        /// </summary>
        public override string ToString()
        {
            return _doc.Declaration != null
                ? _doc.Declaration + "\n" + _root.ToString()
                : _root.ToString();
        }

        /// <summary>
        /// Renders with SaveOptions for formatting control.
        /// </summary>
        public string ToString(SaveOptions options)
        {
            return _root.ToString(options);
        }
    }
}
