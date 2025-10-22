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
        
        // Suppress logging for cleaner test output. Alternative: builder.Logging.SetMinimumLevel(LogLevel.Error);
        builder.Logging.ClearProviders();
        
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

[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetStats()
    {
        return Ok(new
        {
            count = 42,
            price = 99.99,
            rating = 4.5,
            available = true,
            discount = 0.15
        });
    }
}

[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    [HttpPost]
    public IActionResult CreateProfile([FromBody] CreateProfileRequest request)
    {
        var profileId = "PROFILE" + new Random().Next(1000, 9999);
        return Created($"/api/profile/{profileId}", new
        {
            id = profileId,
            name = request.Name,
            age = request.Age,
            premium = request.Premium,
            status = "active"
        });
    }
}

[ApiController]
[Route("api/[controller]")]
public class LoginController : ControllerBase
{
    [HttpPost]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (request.Username == "testuser" && request.Password == "testpass")
        {
            var sessionToken = "sess_" + Guid.NewGuid().ToString("N").Substring(0, 16);
            Response.Headers["Set-Cookie"] = $"session={sessionToken}; Path=/; HttpOnly";
            return Ok(new { message = "Login successful" });
        }
        return Unauthorized();
    }
}

[ApiController]
[Route("api/[controller]")]
public class ProfileController2 : ControllerBase
{
    [HttpGet]
    [Route("/api/profile")]
    public IActionResult GetProfile()
    {
        var sessionCookie = Request.Headers.Cookie.FirstOrDefault();
        if (sessionCookie?.Contains("session=") == true)
        {
            var sessionToken = sessionCookie.Split("session=")[1].Split(';')[0];
            return Ok(new { username = "testuser", sessionId = sessionToken });
        }
        return Unauthorized();
    }
}

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    [HttpPost]
    public IActionResult Upload()
    {
        var uploadId = "UP_" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();
        var fileHash = "hash_" + Guid.NewGuid().ToString("N").Substring(0, 16);
        var fileId = "FILE_" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        
        Response.Headers["X-Upload-Id"] = uploadId;
        Response.Headers["X-File-Hash"] = fileHash;
        Response.Headers["Location"] = $"/api/files/{fileId}";
        
        // Store the hash in a static dictionary for consistency across requests
        FileHashStore.Store(uploadId, fileHash);
        
        return StatusCode(201, new { status = "uploaded" });
    }
}

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult GetFile(string id)
    {
        var uploadRef = Request.Headers["X-Upload-Reference"].FirstOrDefault();
        if (string.IsNullOrEmpty(uploadRef))
            return BadRequest("Upload reference required");
            
        // Get the stored hash for this upload
        var fileHash = FileHashStore.Get(uploadRef);
        Response.Headers["ETag"] = $"\"{fileHash}\"";
        
        return Ok(new 
        { 
            fileId = id, 
            uploadId = uploadRef,
            hash = fileHash
        });
    }
}

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost]
    public IActionResult Authenticate([FromBody] AuthRequest request)
    {
        if (request.ClientId == "test-client")
        {
            var accessToken = "tok_" + Guid.NewGuid().ToString("N").Substring(0, 20);
            var tokenType = "Bearer";
            var expiresIn = 3600;
            
            Response.Headers["Authorization"] = $"Bearer {accessToken}";
            Response.Headers["X-Token-Type"] = tokenType;
            Response.Headers["X-Expires-In"] = expiresIn.ToString();
            
            return Ok(new { tokenType });
        }
        return Unauthorized();
    }
}

[ApiController]
[Route("api/[controller]")]
public class ProtectedController : ControllerBase
{
    [HttpGet]
    public IActionResult GetProtected()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ") == true)
        {
            return Ok(new 
            { 
                message = "Access granted",
                tokenType = "Bearer",
                expiresIn = 3600
            });
        }
        return Unauthorized();
    }
}

public record CreateUserRequest(string Name);
public record CreateOrderRequest(string UserId);
public record CreateProfileRequest(string Name, int Age, bool Premium);
public record LoginRequest(string Username, string Password);
public record AuthRequest(string ClientId);

public static class FileHashStore
{
    private static readonly Dictionary<string, string> _store = new();
    
    public static void Store(string uploadId, string hash)
    {
        _store[uploadId] = hash;
    }
    
    public static string Get(string uploadId)
    {
        return _store.TryGetValue(uploadId, out var hash) ? hash : "unknown";
    }
}
