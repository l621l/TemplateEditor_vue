using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

public partial class FileService
{
    private readonly object _graphicsMaintenanceLock = new object();

    public void MaintainBroadcastGraphics(int broadcastIndex)
    {
        lock (_graphicsMaintenanceLock)
        {
            var ordered = presets.OrderBy(p => p.Id).ToList();
            if (broadcastIndex < 0 || broadcastIndex >= ordered.Count)
                throw new ArgumentOutOfRangeException(nameof(broadcastIndex), "wrong broad");

            var preset = ordered[broadcastIndex];
            var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var templatesRoot = Path.GetFullPath(pathToBroadcasts);
            var templateFolder = Path.GetFullPath(Path.Combine(templatesRoot, preset.Folder));
            if (string.IsNullOrWhiteSpace(preset.Folder) ||
                !templateFolder.StartsWith(Path.TrimEndingDirectorySeparator(templatesRoot) + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Некорректная папка XML выбранного пресета.");
            foreach (var template in Directory.GetFiles(templateFolder, "*.xml", SearchOption.TopDirectoryOnly))
            {
                XDocument document;
                try { document = XDocument.Load(template); }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Не удалось прочитать XML: {template}. Архивация отменена.", ex);
                }

                foreach (var attribute in document.Descendants().Attributes("folder"))
                {
                    if (!IsBroadcastGraphic(attribute.Value)) continue;
                    var graphic = ServerSettings.RequireAbsolutePath(attribute.Value, $"{template}: folder");
                    if (!File.Exists(graphic))
                        throw new FileNotFoundException($"Не найден файл из XML {template}: {graphic}. Архивация отменена.", graphic);
                    referenced.Add(graphic);
                    folders.Add(Path.GetDirectoryName(graphic)!);
                }
            }

            if (referenced.Count == 0) return;
            if (folders.Count != 1)
                throw new InvalidOperationException("В XML эфира указано несколько папок графики. Архивация отменена.");
            if (string.IsNullOrWhiteSpace(pathToOldGrafics))
                throw new InvalidOperationException("Не задан path_to_old_pictures в settings.xml.");

            var graphicsFolder = folders.Single();
            var archiveFolder = Path.GetFullPath(pathToOldGrafics);
            if (string.Equals(Path.TrimEndingDirectorySeparator(graphicsFolder),
                Path.TrimEndingDirectorySeparator(archiveFolder), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Папка old_png совпадает с папкой графики эфира.");

            var now = DateTime.UtcNow;
            foreach (var graphic in referenced)
                File.SetLastWriteTimeUtc(graphic, now);

            var cutoff = now.AddDays(-90);
            var broadcastName = ArchiveNamePart(string.IsNullOrWhiteSpace(preset.Caption) ? preset.Name : preset.Caption);
            var archived = 0;
            foreach (var graphic in Directory.GetFiles(graphicsFolder))
            {
                if (!IsBroadcastGraphic(graphic) || referenced.Contains(graphic)) continue;
                var lastUsed = File.GetLastWriteTimeUtc(graphic);
                if (lastUsed >= cutoff) continue;

                Directory.CreateDirectory(archiveFolder);
                var stem = $"{Path.GetFileNameWithoutExtension(graphic)}_{broadcastName}_{lastUsed.ToLocalTime():yyyy-MM-dd_HH-mm-ss}";
                var extension = Path.GetExtension(graphic);
                var destination = Path.Combine(archiveFolder, stem + extension);
                var suffix = 1;
                while (File.Exists(destination) || Directory.Exists(destination))
                    destination = Path.Combine(archiveFolder, $"{stem}_{suffix++}{extension}");
                File.Move(graphic, destination);
                archived++;
                _logger.LogInformation("Archived broadcast graphic {Source} to {Destination}", graphic, destination);
            }
            _logger.LogInformation("Broadcast {Broadcast}: refreshed {Refreshed} graphics, archived {Archived}",
                preset.Caption, referenced.Count, archived);
        }
    }

    private static bool IsBroadcastGraphic(string path) =>
        string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Path.GetExtension(path), ".zip", StringComparison.OrdinalIgnoreCase);

    private static string ArchiveNamePart(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var result = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim().TrimEnd('.');
        return string.IsNullOrWhiteSpace(result) ? "broadcast" : result;
    }
}
