using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Location_Finder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Images : ControllerBase
    {
        // GET: api/<Images>
        [HttpGet("image_file")]
        public IActionResult Get_Image_File([FromQuery] string file_name)
        {
            string folder = "API_Data";
            string root_dir = Directory.GetCurrentDirectory();
            string new_folder = Path.Combine(root_dir, folder);
            string filepath = Path.Combine(new_folder, $"{file_name ?? "default"}.png");
            if (System.IO.File.Exists(filepath))
            {

                return PhysicalFile(filepath, "image/png");
            }
            else
            {
                return NotFound($"File not found in {folder}.");
            }

        }
    }
}
