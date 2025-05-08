using Microsoft.AspNetCore.Mvc;
using Tutorial9.Model.DTOs;
using Tutorial9.Services;

namespace Tutorial9.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WarehouseController : ControllerBase
{
    private readonly IDbService _dbService;

    public WarehouseController(IDbService dbService)
    {
        _dbService = dbService;
    }

    [HttpPost]
    public async Task<IActionResult> AddProduct([FromBody] WarehouseRequest request)
    {
        try
        {
            var insertedId = await _dbService.AddProductToWarehouseAsync(request);
            return Created($"/api/warehouse/{insertedId}", new { Id = insertedId });
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }
        catch (InvalidOperationException e)
        {
            return Conflict(new { error = e.Message });

        }
        catch (Exception)
        {
            return StatusCode(500, "Internal server error");
        }
    }
}