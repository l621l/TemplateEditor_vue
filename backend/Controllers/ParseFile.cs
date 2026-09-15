using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Linq;


public partial class FileService                 
{
	private string path;
	private int FirstPosX, SecondPosX;
	private int FirstPosY, SecondPosY;
	private int FirstPosLoop, SecondPosLoop;
	private int FirstPosPng, SecondPosPng;
	private int FirstPosTurnOffQueue, SecondPosTurnOffQueue;
	private int FirstPosKeepAlive, SecondPosKeepAlive;

	private static string Attr(XElement el, string attrName)
	{
		return el?.Attribute(attrName)?.Value ?? "";
	}

	private static string ApplyPresetMasks(string mask, PresetItem preset)
	{
		if (string.IsNullOrWhiteSpace(mask))
			return "";

		return mask
			.Replace("{channel}", preset.Channel ?? "", StringComparison.OrdinalIgnoreCase)
			.Replace("{preset}", preset.Folder ?? "", StringComparison.OrdinalIgnoreCase);
	}

	public LaunchInfo GetLaunchInfoByBroadcastAndBind(int broadcast, string bind)
	{
		var preset = presets.FirstOrDefault(p => p.Id == broadcast);
		if (preset == null || !binds.TryGetValue(bind, out var bindItem))
            return new LaunchInfo
            {
                File = "",
                Text = ""
            };

		string text = "";
		if (bindItem.TextByPresetId.TryGetValue(preset.Id, out string bindText))
			text = bindText;

		return new LaunchInfo
		{
			File = ApplyPresetMasks(bindItem.FileMask, preset),
			Text = text
		};
	}

	public Dictionary<int, BroadcastFrontendInfo> GetBroadcastsForFrontend()
	{
		var result = new Dictionary<int, BroadcastFrontendInfo>();

		foreach (var preset in presets)
		{
			var info = new BroadcastFrontendInfo
			{
				Id = preset.Id,
				Name = preset.Name,
				Folder = preset.Folder,
				Channel = preset.Channel,
				Caption = preset.Caption
			};

			foreach (var bindKv in binds)
			{
				info.FilesByBind[bindKv.Key] = ApplyPresetMasks(bindKv.Value.FileMask, preset);

				if (bindKv.Value.TextByPresetId.TryGetValue(preset.Id, out string text))
					info.TextByBind[bindKv.Key] = text;
				else
					info.TextByBind[bindKv.Key] = "";
			}

			result[preset.Id] = info;
		}

		return result;
	}

	public string GetXmlPathByIndexes(int broad, int xml)
	{
		var allXmlFolders = xmlNamesForBroadcasts.Keys.ToList();
		var orderedPresets = presets.OrderBy(p => p.Id).ToList();

		if (broad < 0 || broad >= orderedPresets.Count)
			throw new ArgumentOutOfRangeException(nameof(broad), "wrong broad");

		if (xml < 0 || xml >= allXmlFolders.Count)
			throw new ArgumentOutOfRangeException(nameof(xml), "wrong xml");

		var preset = orderedPresets[broad];
		string bind = allXmlFolders[xml];
		var launch = GetLaunchInfoByBroadcastAndBind(preset.Id, bind);

		if (string.IsNullOrWhiteSpace(launch.File))
			throw new FileNotFoundException("xml path is empty");

		return launch.File;
	}

	public string GetImagePathFromXml(string xmlPath)
	{
		if (string.IsNullOrWhiteSpace(xmlPath))
			throw new ArgumentException("xml path is empty", nameof(xmlPath));

		if (!File.Exists(xmlPath))
			throw new FileNotFoundException("xml file not found", xmlPath);

		string fileText = File.ReadAllText(xmlPath);
		XDocument doc = XDocument.Parse(fileText);
		var animationElement = doc.Descendants("animation").FirstOrDefault();
		if (animationElement == null)
			throw new InvalidOperationException("Animation element not found in the XML.");

		string folderPath = animationElement.Attribute("folder")?.Value;
		if (string.IsNullOrWhiteSpace(folderPath))
			throw new InvalidOperationException("Animation folder attribute is empty.");

		folderPath = ServerSettings.RequireAbsolutePath(folderPath, $"{xmlPath}: animation folder");

		return folderPath;
	}

	public string GetImagesFolderByXmlPath(string xmlPath)
	{
		string imagePath = GetImagePathFromXml(xmlPath);
		string imagesFolder = Path.GetDirectoryName(imagePath);

		if (string.IsNullOrWhiteSpace(imagesFolder))
			throw new InvalidOperationException("Failed to resolve images folder from XML.");

		return imagesFolder;
	}

	public string GetImagesFolderByIndexes(int broad, int xml)
	{
		string xmlPath = GetXmlPathByIndexes(broad, xml);
		return GetImagesFolderByXmlPath(xmlPath);
	}

	#region Парсинг
	public string[] Parse(string path)
	{
		if (!Path.Exists(path))
		{
			_logger.LogError("File not found: " + path);
			return new string[4];
        }
		var result = new string[4];
        try
        {
            string fileText = File.ReadAllText(path);

            XDocument doc = XDocument.Parse(fileText);
            var animationElement = doc.Descendants("animation").FirstOrDefault();
            if (animationElement != null)
            {
                result[2] = animationElement.Attribute("loop")?.Value;
                result[3] = Path.GetFileName(animationElement.Attribute("folder")?.Value);
                var offsetElement = animationElement.Element("offset");
                result[0] = offsetElement?.Attribute("x")?.Value;
                result[1] = offsetElement?.Attribute("y")?.Value;
            }

            _logger.LogInformation(path + ": xml has been parsed");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error parsing XML file: " + ex.Message);
            throw;
        }
        return result;
	}

	public void ParseMainXml(string path)
	{
        string xml = File.ReadAllText(path);
        XDocument doc = XDocument.Parse(xml);

		presets = doc
			.Descendants("preset")
			.Where(p => int.TryParse(Attr(p, "id"), out _))
			.Select(p => new PresetItem
			{
				Id = int.Parse(Attr(p, "id")),
				Name = Attr(p, "name"),
				Folder = Attr(p, "folder"),
				Channel = Attr(p, "channel"),
				Caption = Attr(p, "caption")
			})
			.OrderBy(p => p.Id)
			.ToList();

		binds = new Dictionary<string, BindItem>(StringComparer.OrdinalIgnoreCase);

		foreach (var template in doc.Descendants("template"))
		{
			string bindName = Attr(template, "bind");
			if (string.IsNullOrWhiteSpace(bindName))
				continue;

			string fileMask = Attr(template, "file");
			if (string.IsNullOrWhiteSpace(fileMask))
				continue;

			if (string.Equals(Path.GetExtension(fileMask), ".xml", StringComparison.OrdinalIgnoreCase))
				ServerSettings.RequireAbsolutePath(fileMask, $"{path}: template {bindName}");

			var item = new BindItem
			{
				Bind = bindName,
				FileMask = fileMask
			};

			var presetParameters = template.Element("preset_parameters");
			if (presetParameters != null)
			{
				foreach (var parameters in presetParameters.Elements("parameters"))
				{
					if (!int.TryParse(Attr(parameters, "id_preset"), out int idPreset))
						continue;

					item.TextByPresetId[idPreset] = Attr(parameters, "text");
				}
			}

			binds[bindName] = item;
		}

		broadcastsForFrontend = GetBroadcastsForFrontend();

		broadcastsNamesAndFolders = presets
			.ToDictionary(
				p => string.IsNullOrWhiteSpace(p.Caption) ? p.Name : p.Caption,
				p => p.Folder
			);

		xmlNamesForBroadcasts = new Dictionary<string, string[]>();
		var orderedPresets = presets.OrderBy(p => p.Id).ToList();

		foreach (var bindKv in binds.OrderBy(k => k.Key))
		{
			xmlNamesForBroadcasts[bindKv.Key] = orderedPresets
				.Select(p =>
					bindKv.Value.TextByPresetId.TryGetValue(p.Id, out string txt)
						? txt
						: "")
				.ToArray();
		}

        _logger.LogInformation(path + ": main xml has been parsed");
    }


	public void ApplySettings(ServerSettings settings)
	{
        defaultPreset = settings.DefaultPreset;
        fps = settings.Fps;
        pathToGrafics = settings.PicturesPath;
        pathToBroadcasts = settings.TemplatesPath;
        pathToInterfaceSettings = settings.PreferencesPath;
        pathToOldGrafics = settings.ArchivePath;
        turn_off_queue_png = settings.TurnOffPng;
        keep_alive_png = settings.KeepAlivePng;
        turn_off_queue_zip = settings.TurnOffZip;
        keep_alive_zip = settings.KeepAliveZip;
    }

	#endregion

	#region Функции для сохранения информации в файлы
	public int SaveXmlSample(int x, int y, double duration, string png, string path)
	{
		try
		{
			string xml = File.ReadAllText(path);
			XDocument doc = XDocument.Parse(xml);

			var animationElement = doc.Descendants("animation").FirstOrDefault();
			if (animationElement == null)
			{
				_logger.LogError("Animation element not found in the XML.");
				return 1;
			}


			int loop = (int)Math.Round(duration * 25);
			animationElement.SetAttributeValue("loop", loop);

			bool isZip = png.EndsWith("zip", StringComparison.OrdinalIgnoreCase);
			string keepAliveValue = isZip ? keep_alive_zip : keep_alive_png;
			string turnOffQueueValue = isZip ? turn_off_queue_zip : turn_off_queue_png;
			animationElement.SetAttributeValue("keep_alive", keepAliveValue);
			animationElement.SetAttributeValue("turn_off_queue", turnOffQueueValue);

			string folderPath = animationElement.Attribute("folder")?.Value;
			if (folderPath != null)
			{
				int lastSlashIndex = folderPath.LastIndexOf('\\');
				if (lastSlashIndex != -1)
				{
					string newFolderPath = folderPath.Substring(0, lastSlashIndex + 1) + png;
					animationElement.SetAttributeValue("folder", newFolderPath);
				}
			}
			var offsetElement = animationElement.Element("offset");
			if (offsetElement != null)
			{
				offsetElement.SetAttributeValue("x", x);
				offsetElement.SetAttributeValue("y", y);
			}


			WriteTextAtomically(path, doc.ToString());
			_logger.LogInformation(path + ": xml has been saved");
			return 0;
		}
		catch (Exception ex)
		{
			_logger.LogError("xml save goes wrong: ", ex);
			return 1;
		}
	}


	public void SaveXmlPropirities(string fileNameXml, string newName, int indexOfBroadcast, string path)
	{
        string xml = File.ReadAllText(path);
        XDocument doc = XDocument.Parse(xml);

        var templates = doc.Descendants("template");

        foreach (var template in templates)
        {
            string filePath = template.Attribute("file")?.Value;
            if (filePath != null && filePath.Contains(fileNameXml))
            {
                var parameter = template.Descendants("parameters")
                                        .FirstOrDefault(p => int.Parse(p.Attribute("id_preset")?.Value ?? "0") == indexOfBroadcast);

                if (parameter != null)
                {
                    parameter.SetAttributeValue("text", newName);
                    break;
                }
            }
        }
        WriteTextAtomically(path, doc.ToString());
        _logger.LogInformation(path + ": newName has been set");
    }
	#endregion
}
