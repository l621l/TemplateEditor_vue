using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyVueBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly FileService _fileService;
        private readonly ILogger<ImageController> _logger;
        private readonly ILogger _mainActsLogger;

        public ImageController(FileService fileService, ILogger<ImageController> logger, ILoggerFactory loggerFactory)
        {
            _fileService = fileService;
            _logger = logger;
            _mainActsLogger = loggerFactory.CreateLogger("MainActs");
        }

        private bool TryGetBroadAndXmlFromForm(out int broad, out int xml, out IActionResult errorResult)
        {
            broad = -1;
            xml = -1;
            errorResult = null;

            if (!int.TryParse(Request.Form["broad"], out broad))
            {
                errorResult = BadRequest("wrong broad");
                return false;
            }

            if (!int.TryParse(Request.Form["xml"], out xml))
            {
                errorResult = BadRequest("wrong xml");
                return false;
            }

            return true;
        }

        private string GetXmlPathByIndexes(int broad, int xml)
        {
            var allXmlFolders = _fileService.xmlNamesForBroadcasts.Keys.ToList();
            var orderedPresets = _fileService.presets.OrderBy(p => p.Id).ToList();

            if (broad < 0 || broad >= orderedPresets.Count)
                throw new ArgumentOutOfRangeException(nameof(broad), "wrong broad");

            if (xml < 0 || xml >= allXmlFolders.Count)
                throw new ArgumentOutOfRangeException(nameof(xml), "wrong xml");

            var preset = orderedPresets[broad];
            string bind = allXmlFolders[xml];
            var launch = _fileService.GetLaunchInfoByBroadcastAndBind(preset.Id, bind);

            if (string.IsNullOrWhiteSpace(launch.File))
                throw new FileNotFoundException("xml path is empty");

            return launch.File;
        }

        private string GetImagesFolderByIndexes(int broad, int xml)
        {
            string xmlPath = GetXmlPathByIndexes(broad, xml);
            return _fileService.GetImagesFolderByXmlPath(xmlPath);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage()
        {
            _mainActsLogger.LogInformation("Begin upload Image");
            var files = Request.Form.Files;
            if (files == null || files.Count == 0)
            {
                _mainActsLogger.LogInformation("No files selected.");
                return BadRequest("No files selected.");
            }

            if (!TryGetBroadAndXmlFromForm(out int broad, out int xml, out IActionResult errorResult))
                return errorResult;

            string imagesFolder;
            try
            {
                imagesFolder = GetImagesFolderByIndexes(broad, xml);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve images folder for broad={broad}, xml={xml}", broad, xml);
                return BadRequest(ex.Message);
            }

            if (files.Count >= 2)
            {
                var zipPath = await _fileService.SaveFilesAsZipAsync(files, imagesFolder);
                string[] subs = zipPath.Split(", ");
                var responseData = new
                {
                    fileName = subs[0],
                    x = subs[1],
                    y = subs[2]
                };
                _mainActsLogger.LogInformation("Uploaded zip from files " + responseData.fileName + " " + responseData.x + " " + responseData.y);
                return Ok(responseData);
            }
            else
            {
                var path = await _fileService.SaveFileAsync(files[0], imagesFolder);
                string[] subs = path.Split(", ");
                var responseData = new
                {
                    fileName = subs[0],
                    x = subs[1],
                    y = subs[2]
                };
                _mainActsLogger.LogInformation("File uploaded successfully. " + responseData.fileName + " " + responseData.x + " " + responseData.y);
                return Ok(responseData);
            }
        }

        [HttpPost("uploadzip")]
        public async Task<IActionResult> UploadZip()
        {
            _mainActsLogger.LogInformation("Start of uploading zip");
            var file = Request.Form.Files.FirstOrDefault();
            if (file == null || file.Length == 0)
            {
                _mainActsLogger.LogInformation("File is not selected or has no content.");
                return BadRequest("File is not selected or has no content.");
            }

            if (!TryGetBroadAndXmlFromForm(out int broad, out int xml, out IActionResult errorResult))
                return errorResult;

            string imagesFolder;
            try
            {
                imagesFolder = GetImagesFolderByIndexes(broad, xml);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve images folder for broad={broad}, xml={xml}", broad, xml);
                return BadRequest(ex.Message);
            }

            var path = await _fileService.SaveZipAsync(file, imagesFolder);
            string[] subs = path.Split(", ");
            var responseData = new
            {
                fileName = subs[0],
                x = subs[1],
                y = subs[2]
            };
            _mainActsLogger.LogInformation("zip uploaded: " + responseData.fileName + " " + responseData.x + " " + responseData.y);

            return Ok(responseData);
        }


        [HttpGet("savexml")]
        public IActionResult SaveXml([FromQuery] int broad, int xml, string img, int x, int y, double loop)
        {
            var allXmlFolders = _fileService.xmlNamesForBroadcasts.Keys.ToList();
            var orderedPresets = _fileService.presets.OrderBy(p => p.Id).ToList();
            var preset = orderedPresets[broad];
            string bind = allXmlFolders[xml];
            var launch = _fileService.GetLaunchInfoByBroadcastAndBind(preset.Id, bind);
            string path = launch.File;
            _mainActsLogger.LogInformation("gave for save: " + path);
            _mainActsLogger.LogInformation(img, x, y, loop);
            int subs = _fileService.SaveXmlSample(x, y, loop, img, path);
            var responseData = new
            {
                res = subs
            };
            _mainActsLogger.LogInformation("parsed: " + responseData);
            return Ok(responseData);
        }


        [HttpGet("getprog")]
        public IActionResult GetProgress()
        {
            var progress = _fileService.cropProg;
            return Ok(progress);
        }


        [HttpGet("list")]
        public IActionResult GetImages([FromQuery] int broad, [FromQuery] int xml)
        {
            string imagesFolder;
            try
            {
                imagesFolder = GetImagesFolderByIndexes(broad, xml);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve images folder for list broad={broad}, xml={xml}", broad, xml);
                return BadRequest(ex.Message);
            }

            if (!Directory.Exists(imagesFolder))
            {
                _mainActsLogger.LogInformation("Images folder does not exist: " + imagesFolder);
                return Ok(new List<string>());
            }

            var files = Directory.GetFiles(imagesFolder)
                .Where(filePath => !FileService.IsTemporaryUpload(filePath))
                .Select(filePath => new
                {
                    Name = Path.GetFileName(filePath),
                    Date = new FileInfo(filePath).CreationTime
                })
                .OrderByDescending(file => file.Date);
            _mainActsLogger.LogInformation("Images from: " + imagesFolder);
            foreach (var file in files)
                _mainActsLogger.LogInformation(file.Name + " " + file.Date);
            return Ok(files.Select(file => file.Name).ToList());
        }

        [HttpGet("getbroad")]
        public IActionResult GetBroadcasts()
        {
            var broadcasts = _fileService.broadcastsNamesAndFolders.Keys.ToList();
            _mainActsLogger.LogInformation("gave list of broadcasts: " + string.Join(", ", broadcasts));
            return Ok(broadcasts);
        }

        [HttpGet("getxml")]
        public IActionResult GetXmls([FromQuery] int broad)
        {
            try
            {
                _fileService.MaintainBroadcastGraphics(broad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to prepare graphics for broadcast {Broadcast}", broad);
            }

            var allXmlFolders = _fileService.xmlNamesForBroadcasts.Keys.ToList();
            var namesOfXml = new List<string>();

            foreach (string name in allXmlFolders)
            {
                namesOfXml.Add(_fileService.xmlNamesForBroadcasts[name][broad]);
            }

            var responseData = new
            {
                xml = namesOfXml,
                undef = new int[0]
            };
            return Ok(responseData);
        }

        [HttpGet("getXmlByBind")]
        public IActionResult getXmlByBind([FromQuery] int broad, string bind)
        {
            _mainActsLogger.LogInformation("AAAA");
            LaunchInfo xmlByBind = _fileService.GetLaunchInfoByBroadcastAndBind(broad, bind);
            string[] subs = _fileService.Parse(xmlByBind.File);
            string dur = (double.Parse(subs[2]) / 25).ToString().Replace(',', '.');
            var result = new string[4];
            result[0] = subs[0];
            result[1] = subs[1];
            result[2] = dur;
            result[3] = subs[3];
            var responseData = new
            {
                pathToXml = xmlByBind.File,
                text = xmlByBind.Text,
                result = result
            };
            _mainActsLogger.LogInformation(xmlByBind.File);
            return Ok(responseData);
        }


        [HttpGet("SaveXmlByPath")]
        public IActionResult SaveXmlByPath([FromQuery] string path, string img, int x, int y, double loop)
        {
            var allXmlFolders = _fileService.xmlNamesForBroadcasts.Keys.ToList();
            var allBroads = _fileService.broadcastsNamesAndFolders.Keys.ToList();
            _mainActsLogger.LogInformation("gave for save: " + path);
            _mainActsLogger.LogInformation(img, x, y, loop);
            int subs = _fileService.SaveXmlSample(x, y, loop, img, path);
            var responseData = new
            {
                res = subs
            };
            _mainActsLogger.LogInformation("parsed: " + responseData);
            return Ok(responseData);
        }



        [HttpGet("xmlchoose")]
        public IActionResult ChooseXml([FromQuery] int broad, int xml)
        {
            var allXmlFolders = _fileService.xmlNamesForBroadcasts.Keys.ToList();
            var orderedPresets = _fileService.presets.OrderBy(p => p.Id).ToList();

            if (broad < 0 || broad >= orderedPresets.Count)
                return BadRequest("wrong broad");

            if (xml < 0 || xml >= allXmlFolders.Count)
                return BadRequest("wrong xml");

            var preset = orderedPresets[broad];
            string bind = allXmlFolders[xml];
            var launch = _fileService.GetLaunchInfoByBroadcastAndBind(preset.Id, bind);
            string[] subs = _fileService.Parse(launch.File);
            _mainActsLogger.LogInformation(subs[0]);
            if (subs[0] == null)
            {
                return BadRequest("Failed to parse XML file.");
            }
            string dur = (double.Parse(subs[2]) / 25).ToString().Replace(',', '.');
            var responseData = new
            {
                x = subs[0],
                y = subs[1],
                loop = dur,
                fileName = subs[3]
            };
            _mainActsLogger.LogInformation("xmlchoose: " + launch.File);
            return Ok(responseData);
        }


        [HttpGet("getbroadcastsfull")]
        public IActionResult GetBroadcastsFull()
        {
            return Ok(_fileService.broadcastsForFrontend);
        }

        [HttpGet("{fileName}")]
        public IActionResult GetImage(string fileName, [FromQuery] int broad, [FromQuery] int xml)
        {
            string imagesFolder;
            try
            {
                imagesFolder = GetImagesFolderByIndexes(broad, xml);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve images folder for file broad={broad}, xml={xml}", broad, xml);
                return BadRequest(ex.Message);
            }

            var path = Path.Combine(imagesFolder, fileName);
            if (!System.IO.File.Exists(path))
            {
                return NotFound();
            }

            var fileBytes = System.IO.File.ReadAllBytes(path);
            _mainActsLogger.LogInformation("gave image: " + path);
            return File(fileBytes, "image/jpeg");
        }
    }
}
