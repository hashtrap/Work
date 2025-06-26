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
        private readonly string _apikey;

        public Location_finder(ILogger<Location_finder> log, IConfiguration config, IHttpClientFactory client,
                                IWebHostEnvironment env)
        {
            _logger = log;
            _configuration = config;
            _httpClientFactory = client;
            _env = env;
            _apikey = _configuration["Geoapify:ApiKey"];
        }


        [HttpGet]

        public async Task<IActionResult> Get_Map([FromQuery] double lat, [FromQuery] double lon, [FromQuery]int id, [FromQuery] bool force =false)
        {


            string new_folder =await make_default();

            string filepath = Path.Combine(new_folder, $"{id}.png");
            if (id<=0) 
            {
                string default_path = Path.Combine(new_folder,"default.png");
                _logger.LogInformation("Returning default value");
                return PhysicalFile(default_path,"image/png");
            }

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

                    

                    var httpClient = _httpClientFactory.CreateClient();
                    string UrlHelper = $"https://maps.geoapify.com/v1/staticmap?style=osm-liberty&width=1200&height=800&center=lonlat:{lon.ToString()},{lat.ToString()}&marker=lonlat%3A{lon.ToString()}%2C{lat.ToString()}%3Btype%3Aawesome%3Bcolor%3A%23bc0919%3Bsize%3Ax-large%3Bicon%3Ahome&zoom=17&pitch=45&apiKey={_apikey}";

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

        [HttpGet("{id}")]

        public async Task<IActionResult> return_id(int id,[FromQuery] double lat, [FromQuery] double lon) 
        {
          
            string new_folder = await make_default();
            string filepath = Path.Combine(new_folder, $"{id}.png");

            if (System.IO.File.Exists(filepath)) 
            {
                _logger.LogInformation($"File path already exists: {id}.png");
                return PhysicalFile(filepath, "image/png");
            }
            else 
            {
                _logger.LogInformation($"File path does not exist: {id}.png, generating new path.");
                return await Get_Map(lat, lon, id, false);
            }
        }

        private async Task<string> make_default() 
        {
            string folder = "API_Data";
            string root_dir = _env.ContentRootPath;

            string new_folder = Path.Combine(root_dir, folder);

            if (!Directory.Exists(new_folder))
            {
                Directory.CreateDirectory(new_folder);
                _logger.LogInformation($"Directory created: {new_folder}");

                string default_path = Path.Combine(new_folder, "default.png");

                var httpClient = _httpClientFactory.CreateClient();
                string UrlHelper =$"https://maps.geoapify.com/v1/staticmap?style=osm-liberty&width=1200&height=800&center=lonlat:-73.935242,40.730610&marker=lonlat%3A-73.935242%2C40.730610%3Btype%3Aawesome%3Bcolor%3A%23bc0919%3Bsize%3Ax-large%3Bicon%3Ahome&zoom=17&pitch=45&apiKey={_apikey}";

                var image = await httpClient.GetStreamAsync(UrlHelper);
                using (var memory_stream = new MemoryStream())
                {
                    await image.CopyToAsync(memory_stream);

                    memory_stream.Position = 0;

                    using (var file_stream = new FileStream(default_path, FileMode.Create))
                    {
                        await memory_stream.CopyToAsync(file_stream);
                    }
                }
                return new_folder;
            }

            else
            {
                _logger.LogInformation("Folder already exists");
                return new_folder;
            }
        }

    }
}
