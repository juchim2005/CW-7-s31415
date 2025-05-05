using TravelAgencyApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace TravelAgencyApi.Controllers;

[ApiController]
[Route("[controller]")]
public class TripsController(IDbService dbService) : ControllerBase
{

    [HttpGet]

    public async Task<IActionResult> GetTrips()
    {
        return Ok(await dbService.GetTripsAsync());
    }
    
}