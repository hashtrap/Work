using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Diagnostics.Metrics;

namespace Location_Finder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Location_finder : ControllerBase
    {

        private IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly ILogger<Location_finder> _logger;

        public Location_finder(ILogger<Location_finder> log, IConfiguration config, IHttpClientFactory client,
                                IWebHostEnvironment env)
        {
            _logger = log;
            _configuration = config;
            _httpClientFactory = client;
            _env = env;
        }

        
        [HttpGet]
        
        public async Task<IActionResult> Get_Map([FromQuery] double latitude, [FromQuery] double longtitude, int id,[FromQuery] bool force=false)
        {
                id =id % 101;
                string folder = "API_Data";
                string root_dir = _env.ContentRootPath;

                string new_folder = Path.Combine(root_dir, folder);

                if (!Directory.Exists(new_folder))
                {
                    Directory.CreateDirectory(new_folder);
                    _logger.LogInformation($"Directory created: {new_folder}");
                }

                else
                {
                    _logger.LogInformation("Folder already exists");
                }

                string filepath = Path.Combine(new_folder, $"{id}.png");

                if (System.IO.File.Exists(filepath) && !force)
                {
                    _logger.LogInformation($"File already exists: {id}.png");
                _logger.LogInformation($"force status:{force.ToString()}");

                    return PhysicalFile(filepath, "image/png");
                }
                else
                {
                     try
                     {

                        string _apikey = _configuration["Geoapify:ApiKey"];

                        var httpClient = _httpClientFactory.CreateClient();
                        string UrlHelper = $"https://maps.geoapify.com/v1/staticmap?style=osm-liberty&width=1200&height=800&center=lonlat:{longtitude.ToString()},{latitude.ToString()}&marker=lonlat%3A{longtitude.ToString()}%2C{latitude.ToString()}%3Btype%3Aawesome%3Bcolor%3A%23bc0919%3Bsize%3Ax-large%3Bicon%3Ahome&zoom=17&pitch=45&apiKey={_apikey}";

                        var image = await httpClient.GetStreamAsync(UrlHelper);
                        using (var memory_stream = new MemoryStream())
                        {
                            await image.CopyToAsync(memory_stream);

                            memory_stream.Position = 0;

                            using (var file_stream = new FileStream(filepath, FileMode.Create))
                            {
                                await memory_stream.CopyToAsync(file_stream);
                            }
                        }

                    
                     _logger.LogInformation($"New File created, returning  file.{filepath}");
                    _logger.LogInformation($"force status:{force.ToString()}");
                    return PhysicalFile(filepath, "image/png");
                    
                     }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogError(ex, "An error occurred while fetching the map image.");
                        return BadRequest(new { Message = "Internal server error. Please try again later." });
                    }

                }        

        }
    }
}
