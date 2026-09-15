using System.Collections.Generic;
using System.IO;
using System.Windows;
using System;
using System.Linq;


public sealed class PresetItem
{
	public int Id { get; set; }
	public string Name { get; set; } = "";
	public string Folder { get; set; } = "";
	public string Channel { get; set; } = "";
	public string Caption { get; set; } = "";
}

public sealed class BindItem
{
	public string Bind { get; set; } = "";
	public string FileMask { get; set; } = "";
	public Dictionary<int, string> TextByPresetId { get; set; } = new Dictionary<int, string>();
}

public sealed class LaunchInfo
{
	public string File { get; set; } = "";
	public string Text { get; set; } = "";
}

public sealed class BroadcastFrontendInfo
{
	public int Id { get; set; }
	public string Name { get; set; } = "";
	public string Folder { get; set; } = "";
	public string Channel { get; set; } = "";
	public string Caption { get; set; } = "";
	public Dictionary<string, string> FilesByBind { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	public Dictionary<string, string> TextByBind { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public partial class FileService                   // Класс с инфой из общего xml и файла с настройками
{
	public string pathToBroadcasts;
	public string pathToGrafics;
	public string pathToOldGrafics;
	public string pathToInterfaceSettings;
	public string turn_off_queue_png;
	public string turn_off_queue_zip;
	public string keep_alive_png;
	public string keep_alive_zip;
	public int fps;
	public int defaultPreset;

	public Dictionary<string, string> broadcastsNamesAndFolders;
	public Dictionary<string, string[]> xmlNamesForBroadcasts;

	public List<PresetItem> presets = new List<PresetItem>();
	public Dictionary<string, BindItem> binds = new Dictionary<string, BindItem>(StringComparer.OrdinalIgnoreCase);
	public Dictionary<int, BroadcastFrontendInfo> broadcastsForFrontend = new Dictionary<int, BroadcastFrontendInfo>();

}
