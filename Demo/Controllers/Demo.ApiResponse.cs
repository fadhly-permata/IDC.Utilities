using System.ComponentModel.DataAnnotations;
using System.Data;
using IDC.Utilities.Models;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private static readonly List<Product> _products = new()
    {
        new Product(1, "Laptop", 1000.00m),
        new Product(2, "Smartphone", 500.00m),
    };

    // ================== NORMAL ENDPOINTS ==================
    [HttpGet]
    public List<Product> GetAll() => _products;

    [HttpGet("{id:int}")]
    public Product GetById(int id) =>
        _products.FirstOrDefault(p => p.Id == id)
        ?? throw new KeyNotFoundException($"Product with ID {id} not found");

    [HttpPost]
    public object Create([FromBody] CreateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required");

        if (_products.Any(p => p.Name == request.Name))
            throw new ArgumentException($"Product with name {request.Name} already exists");

        _products.Add(new Product(_products.Max(p => p.Id) + 1, request.Name, request.Price));
        return request;
    }

    [HttpDelete("{id:int}")]
    public void Delete(int id)
    {
        _products.Remove(
            _products.FirstOrDefault(p => p.Id == id)
                ?? throw new KeyNotFoundException($"Product with ID {id} not found")
        );
    }

    // ================== ERROR DEMO ENDPOINTS ==================

    // 400 Bad Request
    [HttpGet("error/bad-request")]
    public void BadRequestError()
    {
        throw new ArgumentException("This is a bad request example");
    }

    // 401 Unauthorized
    [HttpGet("error/unauthorized")]
    public IActionResult UnauthorizedError()
    {
        return Unauthorized(ApiResponse.Failure("Authentication required"));
    }

    // 403 Forbidden
    [HttpGet("error/forbidden")]
    public IActionResult ForbiddenError()
    {
        return StatusCode(403, ApiResponse.Failure("Insufficient permissions"));
    }

    // 404 Not Found
    [HttpGet("error/not-found")]
    public IActionResult NotFoundError()
    {
        return NotFound(ApiResponse.Failure("The requested resource was not found"));
    }

    // 409 Conflict
    [HttpGet("error/conflict")]
    public IActionResult ConflictError()
    {
        return Conflict(ApiResponse.Failure("Resource conflict detected"));
    }

    // 422 Unprocessable Entity
    [HttpGet("error/validation")]
    public IActionResult ValidationError()
    {
        var errors = new List<ApiErrorDetails>
        {
            new("Name", "Name is required"),
            new("Price", "Price must be greater than 0"),
        };
        return UnprocessableEntity(ApiResponse.Failure("Validation failed", errors));
    }

    // 429 Too Many Requests
    [HttpGet("error/rate-limit")]
    public IActionResult RateLimitError()
    {
        Response.Headers["Retry-After"] = "60";
        return StatusCode(429, ApiResponse.Failure("Too many requests, please try again later"));
    }

    // 500 Internal Server Error
    [HttpGet("error/server")]
    public IActionResult ServerError()
    {
        throw new ApplicationException("An unexpected error occurred on the server");
    }

    // 503 Service Unavailable
    [HttpGet("error/maintenance")]
    public IActionResult MaintenanceError()
    {
        Response.Headers["Retry-After"] = "3600";
        return StatusCode(503, ApiResponse.Failure("Service is currently under maintenance"));
    }

    // Nested Exception Example
    [HttpGet("error/nested")]
    public IActionResult NestedError()
    {
        try
        {
            Level1();
        }
        catch (Exception ex)
        {
            throw new ApplicationException("Outer exception", ex);
        }

        return Ok();
    }

    private void Level1()
    {
        try
        {
            Level2();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Level 1 error", ex);
        }
    }

    private void Level2()
    {
        try
        {
            Level3();
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Level 2 error", ex);
        }
    }

    private void Level3()
    {
        try
        {
            Level4();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Level 3 error", ex);
        }
    }

    private void Level4()
    {
        try
        {
            Level5();
        }
        catch (Exception ex)
        {
            throw new Exception("Level 4 error", ex);
        }
    }

    private void Level5()
    {
        throw new DivideByZeroException("Level 5 - Cannot divide by zero");
    }
}

// Model classes
public record Product(int Id, string Name, decimal Price);

public class CreateProductRequest
{
    [Required(ErrorMessage = "Product name is required.", AllowEmptyStrings = false)]
    public required string Name { get; set; }

    [Range(
        minimum: 1,
        maximum: 100_000,
        ErrorMessage = "Price must be greater than 0 and less than 100,000"
    )]
    public decimal Price { get; set; }
}
