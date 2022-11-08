using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace BindingTask1.Controllers;

public class Response
{
    public string[] Result { get; init; }
}

[ApiController]
[Route("binding")]
public class BindingHeader : ControllerBase
{
    /// <summary>
    /// Считывает массив из заголовков 
    /// </summary>
    /// <param name="headerList"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetHeaderAsync([FromHeader(Name = "Hello")] string[] headerList)
    {
        return Ok(new Response
        {
            Result = headerList
        });
    }
}