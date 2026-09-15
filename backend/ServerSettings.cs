using System.Xml.Linq;

public sealed record ServerSettings(
    string ListenUrl, string FrontendOrigin, string PicturesPath, string TemplatesPath,
    string PreferencesPath, string ArchivePath, int DefaultPreset, int Fps,
    string TurnOffPng, string KeepAlivePng, string TurnOffZip, string KeepAliveZip)
{
    public static ServerSettings Load(string file)
    {
        if (!File.Exists(file)) throw new FileNotFoundException($"Не найден settings.xml рядом с приложением: {file}");
        XElement root;
        try { root = XDocument.Load(file).Element("settings") ?? throw new FormatException("Нет элемента <settings>."); }
        catch (Exception ex) { throw new FormatException($"Не удалось прочитать настройки {file}: {ex.Message}", ex); }
        string Required(string key) => !string.IsNullOrWhiteSpace(root.Element(key)?.Value)
            ? root.Element(key)!.Value.Trim() : throw new FormatException($"В {file} не задан <{key}>.");
        string Absolute(string key) => RequireAbsolutePath(Required(key), $"{file}: <{key}>");
        string Url(string key) {
            var value = Required(key);
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https") || string.IsNullOrEmpty(uri.Host) ||
                uri.AbsolutePath != "/" || uri.Query.Length != 0 || uri.Fragment.Length != 0 || uri.UserInfo.Length != 0)
                throw new FormatException($"<{key}> должен содержать HTTP/HTTPS адрес без пути: {value}");
            return uri.GetLeftPart(UriPartial.Authority);
        }
        int Number(string key, int minimum) => int.TryParse(Required(key), out var value) && value >= minimum
            ? value : throw new FormatException($"Некорректное значение <{key}> в {file}.");
        string Boolean(string key) => bool.TryParse(Required(key), out var value)
            ? value.ToString().ToLowerInvariant() : throw new FormatException($"<{key}> должен быть true или false.");
        var settings = new ServerSettings(Url("listen_url"), Url("frontend_origin"),
            Absolute("path_to_pictures"), Absolute("path_to_templates"), Absolute("path_to_prefs"),
            Absolute("path_to_old_pictures"), Number("default_preset", 0), Number("fps", 1),
            Boolean("turn_off_queue_png"), Boolean("keep_alive_png"), Boolean("turn_off_queue_zip"), Boolean("keep_alive_zip"));
        if (!File.Exists(settings.PreferencesPath)) throw new FileNotFoundException($"Не найден preferences.xml: {settings.PreferencesPath}");
        if (!Directory.Exists(settings.TemplatesPath)) throw new DirectoryNotFoundException($"Не найдена папка шаблонов: {settings.TemplatesPath}");
        return settings;
    }

    public static string RequireAbsolutePath(string value, string source)
    {
        if (string.IsNullOrWhiteSpace(value) || !Path.IsPathFullyQualified(value))
            throw new FormatException($"{source}: нужен абсолютный путь, получено '{value}'.");
        return Path.GetFullPath(value);
    }
}
