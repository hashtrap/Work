using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

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


        public Location_finder(ILogger<Location_finder> log, IConfiguration config,IHttpClientFactory client,IWebHostEnvironment env)
        {
            _logger = log;
            _configuration = config;
            _httpClientFactory = client;
            _env = env;
        }

        // GET: api/<Location_finder>
        [HttpGet]
        public async Task<IActionResult> Get_Map([FromQuery] string? latitude, [FromQuery]string? longtitude)
        {


            string _apikey = _configuration["Geoapify:ApiKey"];

            var httpClient = _httpClientFactory.CreateClient();

            try 
            {
                string Url = $"https://maps.geoapify.com/v1/staticmap?style=osm-liberty&width=1200&height=800&center=lonlat:{longtitude},{latitude}&marker=lonlat%3A23.69912%2C37.92087%3Btype%3Aawesome%3Bcolor%3A%23bc0919%3Bsize%3Ax-large%3Bicon%3Ahome&zoom=17&pitch=45&apiKey={_apikey}";

                var image = await httpClient.GetStreamAsync(Url);

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

                string filepath = Path.Combine(new_folder, "map.png");
                using (var memory_stream = new MemoryStream())
                {
                    await image.CopyToAsync(memory_stream);

                    memory_stream.Position = 0;

                    using (var file_stream = new FileStream(filepath, FileMode.Create))
                    {
                        await memory_stream.CopyToAsync(file_stream);
                    }
                }

                return Ok($"latitude:{latitude} and longtitue:{longtitude}");
            }

            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the map image.");
                return StatusCode(500, "Internal server error. Please try again later.");
            }

        }

    }
}
