using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Win32;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

public partial class FileService
{
    private readonly string _uploadsPath;
    private readonly ILogger<FileService> _logger;
	private readonly string _broadcastsPath;

    public FileService(ServerSettings settings, ILogger<FileService> logger)
    {
        _uploadsPath = settings.PicturesPath;
		_broadcastsPath = settings.TemplatesPath;
		_logger = logger;
		ApplySettings(settings);
		ParseMainXml(pathToInterfaceSettings);
        if (!Directory.Exists(_uploadsPath))
        {
            Directory.CreateDirectory(_uploadsPath);
        }
    }
	
	
    public double cropProg = 0;
    public string UploadsPath => _uploadsPath;
    public string BroadcastsPath => _broadcastsPath;

    public static bool IsTemporaryUpload(string path) =>
        Path.GetFileName(path).StartsWith(".templateeditor-", StringComparison.OrdinalIgnoreCase)
        && path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase);

    private static string TemporaryPathFor(string destination) =>
        Path.Combine(Path.GetDirectoryName(Path.GetFullPath(destination))!,
            $".templateeditor-{Guid.NewGuid():N}.tmp");

    private void DeleteTemporaryFile(string path)
    {
        try { File.Delete(path); }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not remove temporary file {Path}", path);
        }
    }

    private void WriteFileAtomically(string destination, Action<Stream> write)
    {
        var temporaryPath = TemporaryPathFor(destination);
        try
        {
            using (var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                write(output);
                output.Flush(flushToDisk: true);
            }
            if (File.Exists(destination))
                File.Replace(temporaryPath, destination, null);
            else
                File.Move(temporaryPath, destination);
        }
        finally { DeleteTemporaryFile(temporaryPath); }
    }

    private void WriteTextAtomically(string destination, string text) =>
        WriteFileAtomically(destination, output =>
        {
            using var writer = new StreamWriter(output, new System.Text.UTF8Encoding(false), 1024, leaveOpen: true);
            writer.Write(text);
        });

    private static string SanitizeFileName(string fileName)
    {
        fileName = fileName.Replace("&", "_");
        fileName = fileName.Replace("'", "_");
        fileName = fileName.Replace("\"", "_");
        fileName = fileName.Replace("<", "_");
        fileName = fileName.Replace(">", "_");
        return fileName;
    }

    private string EnsureTargetFolder(string targetFolder)
    {
        if (string.IsNullOrWhiteSpace(targetFolder))
            targetFolder = _uploadsPath;

        if (!Directory.Exists(targetFolder))
            Directory.CreateDirectory(targetFolder);

        return targetFolder;
    }

    private void findNotAlpha(byte[] data, ref int x1, ref int y1, ref int x2, ref int y2, int width, int height)
    {
        x1 = width;
        y1 = height;
        x2 = -1;
        y2 = -1;

        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < x1; ++x)
            {
                if (data[(y * width + x) * 4 + 3] != 0)
                {
                    x1 = x;
                    break;
                }
            }

            for (int x = width - 1; x > x2; --x)
            {
                if (data[(y * width + x) * 4 + 3] != 0)
                {
                    x2 = x;
                    break;
                }
            }

            if (x1 == 0 && x2 == width - 1)
                break;
        }

        for (int x = 0; x < width; ++x)
        {
            for (int y = 0; y < y1; ++y)
            {
                if (data[(y * width + x) * 4 + 3] != 0)
                {
                    y1 = y;
                    break;
                }
            }

            for (int y = height - 1; y > y2; --y)
            {
                if (data[(y * width + x) * 4 + 3] != 0)
                {
                    y2 = y;
                    break;
                }
            }

            if (y1 == 0 && y2 == height - 1)
                break;
        }
    }


    private Bitmap CropImageFast(Stream stream, ref int x1, ref int y1, out bool wasFullHd)
	{
		using Bitmap bi = new Bitmap(stream);
        wasFullHd = bi.Width == 1920 && bi.Height == 1080;
		BitmapData bdata;
		byte[] data;
		data = new byte[bi.Width * bi.Height * 4];
		bdata = bi.LockBits(new System.Drawing.Rectangle(0, 0, bi.Width, bi.Height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
		try { Marshal.Copy(bdata.Scan0, data, 0, data.Length); }
		finally { bi.UnlockBits(bdata); }
		int x2 = 0;
		int y2 = 0;
		x1 = bi.Width;
		y1 = bi.Height;
		findNotAlpha(data, ref x1, ref y1, ref x2, ref y2, bi.Width, bi.Height);
		System.Drawing.Rectangle rect = new System.Drawing.Rectangle(x1, y1, x2 - x1 + 1, y2 - y1 + 1);
		Bitmap cropped = bi.Clone(rect, bi.PixelFormat);
		return cropped;
	}

    private void findNotAlphaZip(string fullPath, ref int x1, ref int y1, ref int x2, ref int y2)
    {
        int minx = 1920;
        int maxx = 0;
        int miny = 1080;
        int maxy = 0;
		cropProg = 0;
        using (ZipArchive zip = System.IO.Compression.ZipFile.OpenRead(fullPath))
        {
            double numb = zip.Entries.Count;
			numb = 48 / numb;
            foreach (var file in zip.Entries)
            {
                using (StreamReader sr = new StreamReader(file.Open()))
                {
                    using (var memstream = new MemoryStream())
                    {
                        var buffer = new byte[512];
                        var bytesRead = default(int);
                        while ((bytesRead = sr.BaseStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            memstream.Write(buffer, 0, bytesRead);
                        }
                        memstream.Position = 0;
                        using var bi = new Bitmap(memstream);
                        BitmapData bdata;
                        byte[] data;
                        data = new byte[bi.Width * bi.Height * 4];
                        bdata = bi.LockBits(new System.Drawing.Rectangle(0, 0, bi.Width, bi.Height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        try { Marshal.Copy(bdata.Scan0, data, 0, data.Length); }
                        finally { bi.UnlockBits(bdata); }
                        x1 = bi.Width;
                        y1 = bi.Height;
                        x2 = 0;
                        y2 = 0;
                        findNotAlpha(data, ref x1, ref y1, ref x2, ref y2, x1, y1);
                        minx = (minx < x1) ? minx : x1;
                        miny = (miny < y1) ? miny : y1;
                        maxx = (maxx > x2) ? maxx : x2;
                        maxy = (maxy > y2) ? maxy : y2;

                    }
                }
				cropProg += numb;
            }
        }
        x1 = minx;
        y1 = miny;
        x2 = maxx;
        y2 = maxy;
    }

    private string CropAndSaveZipFast(string fullPath, string destinationPath, ref int x, ref int y)
    {
        _logger.LogInformation("Start crop");

        bool? isFullHd = null;
        int x1 = 0, x2 = 0;
        int y1 = 0, y2 = 0;

        _logger.LogInformation("Start find alpha");
        findNotAlphaZip(fullPath, ref x1, ref y1, ref x2, ref y2);
        _logger.LogInformation("End find alpha");

        var rect = new System.Drawing.Rectangle(
            x1, y1, x2 - x1 + 1, y2 - y1 + 1);

        var updatedEntries = new Dictionary<string, MemoryStream>();

        try
        {
            using (var zipStream = new FileStream(fullPath, FileMode.Open))
            using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                double step = 48.0 / zipArchive.Entries.Count;

                foreach (var entry in zipArchive.Entries)
                {
                    if (entry.FullName.EndsWith(
                        ".png", StringComparison.OrdinalIgnoreCase))
                    {
                        using var memstream = new MemoryStream();

                        using (var entryStream = entry.Open())
                        {
                            entryStream.CopyTo(memstream);
                        }

                        memstream.Position = 0;

                        using var bi = new Bitmap(memstream);

                        isFullHd ??=
                            bi.Width == 1920 && bi.Height == 1080;

                        using var cropped = bi.Clone(rect, bi.PixelFormat);

                        var croppedStream = new MemoryStream();
                        updatedEntries[entry.FullName] = croppedStream;

                        cropped.Save(croppedStream, ImageFormat.Png);
                        croppedStream.Position = 0;
                    }

                    cropProg += step;
                }
            }

            _logger.LogInformation("End crop");

            string newPath = isFullHd == true
                ? Path.Combine(
                    Path.GetDirectoryName(destinationPath)!,
                    $"{Path.GetFileNameWithoutExtension(destinationPath)}={x1}x{y1}{Path.GetExtension(destinationPath)}")
                : destinationPath;

            _logger.LogInformation("Start save");

            WriteFileAtomically(newPath, zipStream =>
            {
                using var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true);
                double step = 4.0 / updatedEntries.Count;

                foreach (var entry in updatedEntries)
                {
                    var zipEntry = zipArchive.CreateEntry(entry.Key);

                    using (var entryStream = zipEntry.Open())
                    {
                        entry.Value.CopyTo(entryStream);
                    }

                    cropProg += step;
                }
            });

            _logger.LogInformation("End save: " + newPath);

            File.SetCreationTime(newPath, DateTime.Now);

            x = x1;
            y = y1;

            return newPath;
        }
        finally
        {
            foreach (var stream in updatedEntries.Values)
            {
                stream.Dispose();
            }
        }
    }

    public async Task<string> SaveFileAsync(IFormFile file)
    {
        return await SaveFileAsync(file, _uploadsPath);
    }

    public async Task<string> SaveFileAsync(IFormFile file, string targetFolder)
    {
        _logger.LogInformation("Start save file");
        if (file.Length == 0)
        {
            throw new InvalidOperationException("File has no content.");
        }

        targetFolder = EnsureTargetFolder(targetFolder);
        string fileName = SanitizeFileName(file.FileName);
        var path = Path.Combine(targetFolder, fileName);
        string newPath;
        string extension = Path.GetExtension(path);
        int x1 = 0, y1 = 0;
        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            stream.Position = 0;
            using Bitmap croppedImage = CropImageFast(stream, ref x1, ref y1, out bool wasFullHd);
            newPath = wasFullHd
                ? Path.Combine(targetFolder,
                    $"{Path.GetFileNameWithoutExtension(path)}={x1}x{y1}{extension}")
                : path;
            _logger.LogInformation("TRY TO SAVE: " + newPath);
            WriteFileAtomically(newPath, output => croppedImage.Save(output, ImageFormat.Png));
        }
        _logger.LogInformation("End save file: " + newPath);
        return Path.GetFileName(newPath) + ", " + x1.ToString() + ", " + y1.ToString();
    }

    public async Task<string> SaveZipAsync(IFormFile file)
    {
        return await SaveZipAsync(file, _uploadsPath);
    }

    public async Task<string> SaveZipAsync(IFormFile file, string targetFolder)
    {
        _logger.LogInformation("SaveZipAsync");
        if (file.Length == 0)
        {
            throw new InvalidOperationException("File has no content.");
        }

        targetFolder = EnsureTargetFolder(targetFolder);
        string zipFileName = SanitizeFileName(file.FileName);
        var path = Path.Combine(targetFolder, zipFileName);
        var temporaryPath = TemporaryPathFor(path);
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }
            _logger.LogInformation("Downloaded: " + path);
            int x = 0;
            int y = 0;
            var savedPath = CropAndSaveZipFast(temporaryPath, path, ref x, ref y);
            return Path.GetFileName(savedPath) + ", " + x.ToString() + ", " + y.ToString();
        }
        finally { DeleteTemporaryFile(temporaryPath); }
    }

    public async Task<string> SaveFilesAsZipAsync(IFormFileCollection files)
    {
        return await SaveFilesAsZipAsync(files, _uploadsPath);
    }

    public async Task<string> SaveFilesAsZipAsync(IFormFileCollection files, string targetFolder)
    {
        _logger.LogInformation("SaveFilesAsZipAsync");
        if (files.Count == 0)
        {
            throw new InvalidOperationException("No files selected.");
        }

        targetFolder = EnsureTargetFolder(targetFolder);
        var zipFileName = SanitizeFileName(Path.GetFileNameWithoutExtension(files[0].FileName) + ".zip");
        var zipPath = Path.Combine(targetFolder, zipFileName);
        var temporaryPath = TemporaryPathFor(zipPath);
        try
        {
            using (var zipStream = new FileStream(temporaryPath, FileMode.CreateNew))
            using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                foreach (var file in files)
                {
                    if (file.Length == 0)
                    {
                        throw new InvalidOperationException("File has no content.");
                    }

                    var entry = zipArchive.CreateEntry(file.FileName);
                    using (var entryStream = entry.Open())
                    {
                        await file.CopyToAsync(entryStream);
                    }
                }
            }
            _logger.LogInformation("Downloaded and zipped: " + zipPath);
            int x = 0;
            int y = 0;
            var savedPath = CropAndSaveZipFast(temporaryPath, zipPath, ref x, ref y);
            return Path.GetFileName(savedPath) + ", " + x.ToString() + ", " + y.ToString();
        }
        finally
        {
            cropProg = 0;
            DeleteTemporaryFile(temporaryPath);
        }
    }

}

