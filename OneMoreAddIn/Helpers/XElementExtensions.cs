﻿//************************************************************************************************
// Copyright © 2020 Steven M Cohn.  All rights reserved.
//************************************************************************************************

namespace River.OneMoreAddIn
{
    using System.ComponentModel;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;


	internal static class XElementExtensions
	{

		/// <summary>
		/// Returns the CData node from the current element
		/// </summary>
		/// <param name="element">A one:T element</param>
		/// <returns>An XCData node or null if none</returns>
		public static XCData GetCData(this XElement element)
		{
			return element.DescendantNodes()
				.Where(e => e.NodeType == XmlNodeType.CDATA)
				.FirstOrDefault() as XCData;
		}


		/// <summary>
		/// Returns the InnerXml of the given element
		/// </summary>
		/// <param name="element">The element to interogate</param>
		/// <returns>A string specifying the inner XML of the element.</returns>
		public static string GetInnerXml(this XElement element)
		{
			string xml = null;

			// fastest way to get XElement inner XML
			using (var reader = element.CreateReader())
			{
				reader.MoveToContent();
				xml = reader.ReadInnerXml();
			}

			return xml;
		}


		/// <summary>
		/// Remove and return the first textual word from the element content
		/// </summary>
		/// <param name="element">The element to modify</param>
		/// <returns>A string that can be appended to a CData's raw content</returns>
		public static string ExtractFirstWord(this XElement element)
		{
			var cdata = element.GetCData();

			if (cdata.IsEmpty())
			{
				return string.Empty;
			}

			// copy the first word and remove it from the element

			var wrapper = cdata.GetWrapper();

			// OneNote is very conservative with styles and excludes prior and following
			// whitespace when applying css to words; so we don't need to worry about whitespace

			// get text node or span element but not others like <br/>
			var node = wrapper.Nodes().Where(
				n => n.NodeType == XmlNodeType.Text ||
				(n.NodeType == XmlNodeType.Element && (n as XElement).Name.LocalName.Equals("span")))
				.FirstOrDefault();

			if (node == null)
			{
				return null;
			}

			// remove first word and update element's CData
			node.Remove();
			cdata.Value = wrapper.GetInnerXml();

			// return first word
			return node.NodeType == XmlNodeType.Text
				? node.ToString()
				: (node as XElement).ToString(SaveOptions.DisableFormatting);
		}


		/// <summary>
		/// Remove and return the last textual word from the element content
		/// </summary>
		/// <param name="element">The element to modify</param>
		/// <returns>A string that can be appended to a CData's raw content</returns>
		public static string ExtractLastWord(this XElement element)
		{
			var cdata = element.GetCData();

			if (cdata.IsEmpty())
			{
				return string.Empty;
			}

			// copy the last word and remove it from the element

			var wrapper = cdata.GetWrapper();

			// OneNote is very conservative with styles and excludes prior and following
			// whitespace when applying css to words; so we don't need to worry about whitespace

			// get text node or span element but not others like <br/>
			// Note the use of Reverse() here so we get the last node with content
			var node = wrapper.Nodes().Reverse().Where(
				n => n.NodeType == XmlNodeType.Text ||
				(n.NodeType == XmlNodeType.Element && (n as XElement).Name.LocalName.Equals("span")))
				.FirstOrDefault();

			if (node == null)
			{
				return null;
			}

			// remove first word and update element's CData
			node.Remove();
			cdata.Value = wrapper.GetInnerXml();

			// return first word
			return node.NodeType == XmlNodeType.Text
				? node.ToString()
				: (node as XElement).ToString(SaveOptions.DisableFormatting);
		}


		public static bool ReadAttributeValue(
			this XElement element, string name, out string value, string defaultV = null)
		{
			var attr = element.Attribute(name)?.Value;
			if (attr != null)
			{
				value = attr;
				return true;
			}

			value = defaultV;
			return false;
		}

		public static bool ReadAttributeValue<T>(
			this XElement element, string name, out T value, T defaultV)
		{
			var attr = element.Attribute(name);
			if (attr != null)
			{
				try
				{
					value = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(attr.Value);
				}
				catch
				{
					Logger.Current.WriteLine($"Error translating {name}:{typeof(T).Name} '{attr.Value}'");
					value = defaultV;
					return false;
				}

				return true;
			}

			value = defaultV;
			return false;
		}
	}
}
