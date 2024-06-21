using Microsoft.AspNetCore.Mvc;

namespace YaeBlog.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    [HttpGet("{*filename}")]
    public IActionResult Images(string filename)
    {
        string contentType = "image/png";
        if (filename.EndsWith("jpg") || filename.EndsWith("jpeg"))
        {
            contentType = "image/jpeg";
        }

        FileInfo imageFile = new(filename);

        if (!imageFile.Exists)
        {
            return NotFound();
        }

        Stream imageStream = imageFile.OpenRead();
        return File(imageStream, contentType);
    }
}
