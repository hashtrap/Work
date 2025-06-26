using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;
using System.Runtime.InteropServices.Marshalling;

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
        
        public async Task<IActionResult> Get_Map([FromQuery] double lat, [FromQuery] double lon,
                                                 [FromQuery][Required]int id, [FromQuery] bool force =false,
                                                 [FromQuery]int width = 1200,[FromQuery] int height = 800,
                                                 [FromQuery]string style = "osm-liberty", [FromQuery]int pitch=45,
                                                 [FromQuery]double zoom=17)
        {


            string new_folder =await make_default();

            string filepath = Path.Combine(new_folder, $"{id}.png");//Creates path for the new image
            string default_path = Path.Combine(new_folder, "default.png");//Creates default path

            if (id<=0) //if user puts 0 or smaller it return default image
            {
                
                _logger.LogInformation("Returning default value,id was incorrect");
                return PhysicalFile(default_path,"image/png");
            }

            if (!((lat<=90&&lat>=-90)&&(lon<=180&&lon>=-180))) //A check if longtitude and latitude are between the correct thresholds
            {
                _logger.LogInformation("Returning default value,longtitude or latitude was incorrect");
                return PhysicalFile(default_path, "image/png");
            }

            if (System.IO.File.Exists(filepath) && !force)//Retrives image from folder and sends to client
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
                    string UrlHelper = $"https://maps.geoapify.com/v1/staticmap?style={style}&width={width}&height={height}&center=lonlat:{lon.ToString()}," +
                            $"{lat.ToString()}&marker=lonlat%3A{lon.ToString()}%2C{lat.ToString()}%3Btype%3Aawesome%3Bcolor%3A%23bc0919%3B" +
                            $"size%3Ax-large%3Bicon%3Ahome&zoom={zoom}&pitch={pitch}&apiKey={_apikey}";

                    var image = await httpClient.GetStreamAsync(UrlHelper);
                    using (var memory_stream = new MemoryStream())// Both using methods are to do automatic disposing
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
                catch (HttpRequestException ex)//Exception in case of a ba request to API
                {
                    _logger.LogError(ex, "An error occurred while fetching the map image.");
                    return BadRequest(new { Message = "Internal server error. Please try again later." });
                }

            }

        }

        [HttpGet("{id}")]

        public async Task<IActionResult> return_id(int id,[FromQuery] double lat, [FromQuery] double lon)//Method with mandatory id
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

        private async Task<string> make_default() // Creates Directory and puts a default image 
        {
            string folder = "API_Data";
            string root_dir = _env.ContentRootPath;

            string new_folder = Path.Combine(root_dir, folder);

            if (!Directory.Exists(new_folder))// A check in case directory doesn't exist
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
