using Microsoft.AspNetCore.Mvc;

namespace PQSoft.ReqNRoll.UnitTests;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });
        
        builder.Services.AddControllers()
            .AddXmlSerializerFormatters();  // Add XML support
        
        var app = builder.Build();
        app.UseRouting();
        app.MapControllers();
        
        app.Run();
    }
}

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpPost]
    public IActionResult CreateUser([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrEmpty(request.Name))
            return BadRequest(new { error = "Name is required" });

        return Created($"/api/users/USER123", new { id = "USER123", name = request.Name });
    }

    [HttpGet("{id}")]
    public IActionResult GetUser(string id)
    {
        if (id == "NOTFOUND")
            return NotFound(new { error = "User not found" });

        return Ok(new { id, name = "Test User", active = true, created = "2024-01-01T10:00:00Z" });
    }
}

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    [HttpPost]
    public IActionResult CreateOrder([FromBody] CreateOrderRequest request)
    {
        return Created($"/api/orders/ORDER456", new 
        { 
            orderId = "ORDER456", 
            userId = request.UserId, 
            status = "pending",
            total = 99.99
        });
    }

    [HttpGet("{id}")]
    public IActionResult GetOrder(string id)
    {
        return Ok(new { orderId = id, status = "completed", items = new[] { "Widget", "Gadget" } });
    }
}

[ApiController]
[Route("api/[controller]")]
public class TextController : ControllerBase
{
    [HttpPost]
    [Consumes("text/plain")]
    [Produces("text/plain")]
    public IActionResult PostText()
    {
        using var reader = new StreamReader(Request.Body);
        var body = reader.ReadToEndAsync().Result;
        return Ok($"Echo: {body}");
    }
}

[ApiController]
[Route("api/[controller]")]
public class XmlController : ControllerBase
{
    [HttpPost]
    public IActionResult PostXml()
    {
        using var reader = new StreamReader(Request.Body);
        var body = reader.ReadToEndAsync().Result;
        
        // Extract name from simple XML (just for testing)
        var userId = "XML" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        var xmlResponse = $"<response><id>{userId}</id></response>";
        
        return Content(xmlResponse, "application/xml");
    }
}

[ApiController]
[Route("api/[controller]")]
public class CsvController : ControllerBase
{
    [HttpPost]
    [Consumes("text/csv")]
    [Produces("text/csv")]
    public IActionResult PostCsv()
    {
        using var reader = new StreamReader(Request.Body);
        var body = reader.ReadToEndAsync().Result;
        
        var userId = "CSV" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        var lines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length > 1)
        {
            var dataLine = lines[1].Trim();
            var csvResponse = $"id,name,age\n{userId},{dataLine}";
            return Content(csvResponse, "text/csv");
        }
        
        return BadRequest("Invalid CSV");
    }
}

public record CreateUserRequest(string Name);
public record CreateOrderRequest(string UserId);
