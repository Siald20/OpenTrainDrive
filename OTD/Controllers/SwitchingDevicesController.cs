
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using System.IO;
using Microsoft.Extensions.Hosting;

namespace OpenTrainDrive.Engines
{
	/// <summary>
	/// Controller for switching devices (turnouts and signals).
	/// </summary>
	public static class SwitchingDevicesController
	{
		// Reference to switchingdevices.xml in application path
		private static string _xmlPath;
		private static List<SwitchingDevice> _devices = new();

		/// <summary>
		/// Loading all device data from switchingdevices.xml
		/// </summary>
		public static void Initialize()
		{
			_xmlPath = GetDefaultXmlPath();
			_devices.AddRange(LoadDevices());
		}

		/// <summary>
		/// Gets the default path to switchingdevices.xml in the application directory.
		/// </summary>
		private static string GetDefaultXmlPath()
		{
			// Use AppContext.BaseDirectory to get the application path
			var appPath = AppContext.BaseDirectory;
			var xmlFile = Path.Combine(appPath, "switchingdevices.xml");
			if (File.Exists(xmlFile))
				return xmlFile;
			// Fallback: try project root (for development)
			var devPath = Path.Combine(appPath, "..", "..", "..", "switchingdevices.xml");
			devPath = Path.GetFullPath(devPath);
			if (File.Exists(devPath))
				return devPath;
			return xmlFile; // Default, even if not found
		}

		private static List<SwitchingDevice> LoadDevices()
		{
			var devices = new List<SwitchingDevice>();
			if (string.IsNullOrEmpty(_xmlPath))
				return devices;
			var doc = XDocument.Load(_xmlPath);
			foreach (var elem in doc.Descendants("switchingdevice"))
			{
				var device = new SwitchingDevice
				{
					UID = (string)elem.Attribute("uid"),
					Name = (string)elem.Attribute("name"),
					Type = (string)elem.Attribute("type"),
					Index = (int?)elem.Attribute("index") ?? 0,
					Positions = elem.Element("positions")?.Elements("position").Select(pos => new DevicePosition
					{
						Name = (string)pos.Attribute("name"),
						Description = (string)pos.Attribute("description"),
						Decoders = pos.Elements("decoder").Select(dec => new DecoderCommand
						{
							Address = (int?)dec.Attribute("address") ?? 0,
							Output = (int?)dec.Attribute("output") ?? 0
						}).ToList()
					}).ToList() ?? new List<DevicePosition>()
				};
				devices.Add(device);
			}
			return devices;
		}

		/// <summary>
		/// Set turnout or signal to a given state (as defined in switchingdevices.xml; e.g., "gerade", "abzweigend").
		/// </summary>
		public static bool SetState(string uid, string state)
		{
			var _commandStation = "1"; // Example command station ID

			var turnout = _devices.FirstOrDefault(d => d.Type == "turnout" && d.UID == uid);
			if (turnout == null) return false;
			var pos = turnout.Positions.FirstOrDefault(p => p.Name == state);
			if (pos == null) return false;
			// Here you would send the decoder commands to hardware
			foreach (var cmd in pos.Decoders)
			{
				SendDecoderCommand(_commandStation, cmd.Address, cmd.Output);
			}
			return true;
		}

		private static void SendDecoderCommand(string commandStation, int address, int output)
		{
			// TODO: Implement actual command to hardware/command station
			Console.WriteLine($"Send to {commandStation}: Address={address}, Output={output}");
		}

		/// <summary>
		/// Disposes static resources used by the controller.
		/// </summary>
		public static void Dispose()
		{
			_devices.Clear();
			_xmlPath = null;
		}
	}

	public class SwitchingDevice
	{
		public string UID { get; set; }
		public string Name { get; set; }
		public string Type { get; set; }
		public int Index { get; set; }
		public List<DevicePosition> Positions { get; set; } = new();
	}

	public class DevicePosition
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public List<DecoderCommand> Decoders { get; set; } = new();
	}

	public class DecoderCommand
	{
		public int Address { get; set; }
		public int Output { get; set; }
	}
}
