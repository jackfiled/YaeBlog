using Microsoft.AspNetCore.Mvc;

namespace YaeBlog.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    [HttpGet("{*filename}")]
    public IActionResult Images(string filename)
    {
        // 这里疑似有点太愚蠢了
        string contentType = "image/png";

        if (filename.EndsWith("jpg") || filename.EndsWith("jpeg"))
        {
            contentType = "image/jpeg";
        }

        if (filename.EndsWith("svg"))
        {
            contentType = "image/svg+xml";
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
