using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Location_Finder.Models;


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
        private static readonly string  default_path;
        private static readonly string filepath;

        public Location_finder(ILogger<Location_finder> log, IConfiguration config, IHttpClientFactory client,
                               IWebHostEnvironment env)
        {
            _logger = log;
            _configuration = config;
            _httpClientFactory = client;
            _env = env;
            _apikey = _configuration["Geoapify:API_KEY"]; //Request API key from local .json file

        }


        [HttpGet]
        
        public async Task<IActionResult> Get_Map([FromQuery] double? lat, [FromQuery] double? lon,
                                                 [FromQuery] int? id =null, [FromQuery] bool force =false,
                                                 [FromQuery]int width = 1200,[FromQuery] int height = 800,
                                                 [FromQuery]string style = "osm-liberty", [FromQuery]int pitch=45,
                                                 [FromQuery]double zoom=17)
        {


            string new_folder =make_default();//ensures folder exists and returns path to it

            string filepath = Path.Combine(new_folder, $"{id}.jpeg");//Creates path for the new image
            string default_path = Path.Combine(new_folder, "default.jpeg");//Creates default path

            if (!Value_checker(id,lon,lat,zoom,pitch,width,height,style)) //call function to check if parameters are valid
            {

                _logger.LogInformation("Invalid parameters provided, returning default image.");
                return PhysicalFile(default_path, "image/jpeg");
                
            }
            
            if (System.IO.File.Exists(filepath) && !force)//Retrives image from folder and sends to client
            {
                _logger.LogInformation($"File already exists: {id}.jpeg");                

                return PhysicalFile(filepath, "image/jpeg");//return file from folder path
            }
           
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


                    _logger.LogInformation($"New File created, returning  file:{filepath}");                    
                    return PhysicalFile(filepath, "image/jpeg");

                }
                catch (HttpRequestException ex)//Exception in case of a bad request to API
                {
                    _logger.LogError(ex, "An error occurred while fetching the map image.");
                    return PhysicalFile(default_path, "image/jpeg");
                }

            

        }

        [HttpGet("{id}")]

        public async Task<IActionResult> return_id(int id,[FromQuery] double? lat, [FromQuery] double? lon)//Method with mandatory id
        {                        

            string new_folder = make_default();
            string filepath = Path.Combine(new_folder, $"{id}.jpeg");

            if (System.IO.File.Exists(filepath)) 
            {
                _logger.LogInformation($"File path already exists: {id}.jpeg");
                return PhysicalFile(filepath, "image/jpeg");
            }
            else 
            {
                if ((lat!=null && lon!=null) && (lat!=0 && lon!=0)) // checks if we have provided measurments
                {
                    return await Get_Map(lat,lon,id);
                }
                return await no_data(id);
            }
        }

        private  string make_default() // Creates Directory and puts a default image 
        {
            string folder = "API_Data";
            string root_dir = _env.ContentRootPath;

            string new_folder = Path.Combine(root_dir, folder);

            if (!Directory.Exists(new_folder))// A check in case directory doesn't exist
            {
                Directory.CreateDirectory(new_folder);
                _logger.LogInformation($"Directory created: {new_folder}");

                return new_folder; //returns folder path
            }

            else
            {
                _logger.LogInformation("Folder already exists");
                return new_folder;
            }
        }

        private bool Value_checker(int? id, double? lon,double? lat,double zoom,int pitch,int width,
                                   int height,string style) 
        {
            HashSet<string> styles = new HashSet<string> //created a set for faster item access
            {
                "osm-carto","osm-bright","osm-bright-grey",
                "osm-bright-smooth","klokantech-basic","osm-liberty",
                "maptiler-3d","toner","toner-grey",
                "positron","positron-blue","positron-red",
                "dark-matter","dark-matter-brown","dark-matter-dark-purple",
                "dark-matter-dark-grey","dark-matter-dark-purple-roads","dark-matter-dark-yellow-roads"
            };

            if (id <= 0|| !id.HasValue)// id check
            {
                
                return false;
            }

            if (!((lat <= 90 && lat >= -90) && (lon <= 180 && lon >= -180)) || !lat.HasValue||!lon.HasValue) // langtitude and longitude check
            {
                
                return false;
            }

            if (zoom<1 || zoom>20) // zoom check
            {
                return false;
            }
            

            if (pitch<0 ||pitch>60) // pitch check
            {
                return false;
            }

            if (!styles.Contains(style)) // style check
            {
                return false;
            }

            if (width<0||width>4096||height<0||height>4096) //image size check
            {
                return false;
            }

            return true;

        }

        private async Task<IActionResult> no_data(int id) // method to retrive api data when id foesn't exist and coordinates aren't given
        {
            string new_folder = make_default();
            string default_path = Path.Combine(new_folder, "default.jpeg");
            try
            {
                var client = _httpClientFactory.CreateClient();
                string url = $"https://pre.grekodom.com/miniapi/getrealtycoordinates/{id}";

                string response = await client.GetStringAsync(url);

                var out_response = JsonConvert.DeserializeObject<ApiResponse>(response);// Deserialize the JSON response

                if (out_response == null || out_response.Result == null)// check in case of no data from API
                {
                    _logger.LogInformation($"No data found for ID: {id}");
                    return PhysicalFile(default_path, "image/jpeg");
                }   


                var coordinates = JsonConvert.DeserializeObject<Coordinate>(out_response.Result);// Deserialize the coordinates from the API response to simple object


                
                _logger.LogInformation($"X is:{coordinates.MapX}"); //check for coordinates
                _logger.LogInformation($"Y is:{coordinates.MapY}");

                double? X = string.IsNullOrEmpty(coordinates.MapX) ? null : Double.Parse(coordinates.MapX); //proper casting of coordinates
                double? Y = string.IsNullOrEmpty(coordinates.MapY) ? null : Double.Parse(coordinates.MapY); //proper casting of coordinates


                return await Get_Map(Y, X, id);// call function to get the icon and add the image to the folder
            }

            catch (HttpRequestException ex)
            {
                _logger.LogError($"API request failed: {ex.Message}");
                return BadRequest();
            }
            catch (JsonException ex)
            {
                _logger.LogError($"JSON parsing error: {ex.Message}");
                return BadRequest();
            }

            catch (SystemException e) 
            {
                _logger.LogError($"JSON deserialization fail:{e.Message}");
                return BadRequest();
            }
            
        }
        
    }
}
