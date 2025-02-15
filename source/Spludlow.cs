using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Serialization;
using System.Xml;


namespace Spludlow
{
	public class Tools
	{
		public static string RandomString(int length)
		{
			Random random = new Random();
			return RandomString(random, length);
		}
		public static string RandomString(Random random, int length)
		{
			var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			var result = new char[length];

			for (int i = 0; i < result.Length; i++)
				result[i] = chars[random.Next(chars.Length)];

			return new string(result);
		}

		public static string TimeTook(DateTime startTime)
		{
			TimeSpan timeSpan = DateTime.Now - startTime;
			return TimeTook(timeSpan);
		}

		public static string TimeTook(TimeSpan span)
		{
			StringBuilder text = new StringBuilder();

			if (((int)span.TotalDays) > 0)
			{
				text.Append((int)span.TotalDays);
				text.Append("d");
				if (span.Hours > 0)
				{
					text.Append(" ");
					text.Append(span.Hours);
					text.Append("h");
				}
				if (span.Minutes > 0)
				{
					text.Append(" ");
					text.Append(span.Minutes);
					text.Append("m");
				}

				return text.ToString();
			}

			if (((int)span.TotalHours) > 0)
			{
				text.Append(span.Hours);
				text.Append("h");
				if (span.Minutes > 0)
				{
					text.Append(" ");
					text.Append(span.Minutes);
					text.Append("m");
				}

				return text.ToString();
			}

			if (((int)span.TotalMinutes) > 0)
			{
				text.Append(span.Minutes);
				text.Append("m");
				if (span.Seconds > 0)
				{
					text.Append(" ");
					text.Append(span.Seconds);
					text.Append("s");
				}

				return text.ToString();
			}

			if (((int)span.TotalSeconds) > 0)
			{
				text.Append(span.Seconds);
				text.Append("s");
				if (span.Milliseconds > 0)
				{
					text.Append(" ");
					text.Append(span.Milliseconds);
					text.Append("ms");
				}

				return text.ToString();
			}

			text.Append(span.Milliseconds);
			text.Append("ms");

			return text.ToString();
		}
	}

	public class Log
	{
		public static void Error(string message, Exception exception)
		{

		}

		public static void Error(string message)
		{

		}

		public static void Info(string message)
		{

		}

		public static void Warning(string message)
		{

		}

		public static void Finish(string message)
		{

		}
	}

	public class Serialization
	{

		public static string WriteDataSet(DataSet dataSet)
		{
			using (StringWriter writer = new StringWriter())
			{
				dataSet.WriteXml(writer, XmlWriteMode.WriteSchema);
				return writer.ToString();
			}
		}

		public static DataSet ReadDataSet(string xml)
		{
			DataSet dataSet = new DataSet();
			using (StringReader reader = new StringReader(xml))
			{
				dataSet.ReadXml(reader);
			}
			return dataSet;
		}


		public static string Write(object data)
		{
			Type type = data.GetType();

			XmlSerializer serializer = new XmlSerializer(type);


			using (StringWriter writer = new StringWriter())
			{
				serializer.Serialize(writer, data);
				return writer.ToString();
			}
		}

		public static void WriteFile(object data, string filename)
		{
			Type type = data.GetType();

			XmlSerializer serializer = new XmlSerializer(type);

			using (FileStream stream = new FileStream(filename, FileMode.Create))
				serializer.Serialize(stream, data);
		}

		public static void Write(object data, Stream stream)
		{
			Type type = data.GetType();

			XmlSerializer serializer = new XmlSerializer(type);

			serializer.Serialize(stream, data);
		}



		public static object Read(string xml, string typeName)
		{
			Type type = Type.GetType(typeName, true);
			return Read(xml, type);
		}

		public static object Read(string xml, Type type)
		{
			XmlSerializer serializer = new XmlSerializer(type);

			XmlReaderSettings settings = new XmlReaderSettings();       //	had problem with email containing 0x03 creating poisioned log message. It gets encoded fine '&#x3;' but throws exception when Deserialize
			settings.CheckCharacters = false;

			using (StringReader reader = new StringReader(xml))
			{
				XmlReader xmlReader = XmlReader.Create(reader, settings);

				return serializer.Deserialize(xmlReader);
			}
		}

		public static object Read(Stream stream, Type type)
		{
			XmlSerializer serializer = new XmlSerializer(type);

			XmlReaderSettings settings = new XmlReaderSettings();       //	had problem with email containing 0x03 creating poisioned log message. It gets encoded fine '&#x3;' but throws exception when Deserialize
			settings.CheckCharacters = false;

			using (XmlReader xmlReader = XmlReader.Create(stream, settings))
			{
				return serializer.Deserialize(xmlReader);
			}
		}

		public static object ReadCheckingCharacters(string xml, Type type)
		{
			XmlSerializer serializer = new XmlSerializer(type);

			using (StringReader reader = new StringReader(xml))
			{
				XmlReader xmlReader = XmlReader.Create(reader);

				return serializer.Deserialize(xmlReader);
			}
		}



		public static object ReadFile(string filename, Type type)
		{
			XmlSerializer serializer = new XmlSerializer(type);

			XmlReaderSettings settings = new XmlReaderSettings();
			settings.CheckCharacters = false;

			using (FileStream stream = new FileStream(filename, FileMode.Open))
			{
				XmlReader xmlReader = XmlReader.Create(stream, settings);

				return serializer.Deserialize(xmlReader);
			}
		}

		public static bool ValidateForXML(string text)
		{
			string xml = Spludlow.Serialization.Write(text);
			try
			{
				Spludlow.Serialization.ReadCheckingCharacters(xml, typeof(string));

				return true;
			}
			catch (InvalidOperationException ee)
			{
				if (ee.InnerException != null && !(ee.InnerException is System.Xml.XmlException))
					throw ee;

				System.Xml.XmlException xmlError = (System.Xml.XmlException)ee.InnerException;

				//Spludlow.Log.Warning("ValidateForXML", xmlError, text);

				return false;
			}
		}

		public static string FixString(string text)
		{
			StringBuilder result = new StringBuilder();

			foreach (char ch in text)
			{
				if (ValidCharacter(ch) == true)
					result.Append(ch);
				else
					result.Append('?');
			}

			return result.ToString();

		}

		public static bool ValidCharacter(char ch)
		{
			if (ch < 0x20)
			{
				if (ch == 0x9 || ch == 0xA || ch == 0xD)
					return true;

				return false;
			}

			if (ch >= 0xD800 && ch <= 0xDFFF)
				return false;

			if (ch == 0xFFFE || ch == 0xFFFF)
				return false;

			return true;
		}
	}
}

namespace Spludlow.Media
{
	public class Colour
	{
		public static Color FromHSV(double hue, double saturation, double value)
		{
			int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
			double f = hue / 60 - Math.Floor(hue / 60);

			value = value * 255;
			byte v = Convert.ToByte(value);
			byte p = Convert.ToByte(value * (1 - saturation));
			byte q = Convert.ToByte(value * (1 - f * saturation));
			byte t = Convert.ToByte(value * (1 - (1 - f) * saturation));

			if (hi == 0)
				return Color.FromArgb(255, v, t, p);
			else if (hi == 1)
				return Color.FromArgb(255, q, v, p);
			else if (hi == 2)
				return Color.FromArgb(255, p, v, t);
			else if (hi == 3)
				return Color.FromArgb(255, p, q, v);
			else if (hi == 4)
				return Color.FromArgb(255, t, p, v);
			else
				return Color.FromArgb(255, v, p, q);
		}
	}
}