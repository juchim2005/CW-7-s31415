using TravelAgencyApi.Exceptions;
using Microsoft.AspNetCore.Mvc;
using TravelAgencyApi.Models.DTOs;
using TravelAgencyApi.Services;

namespace TravelAgencyApi.Controllers;
[ApiController]
[Route("[controller]")]
public class ClientsController(IDbService dbService) : ControllerBase
{
    [HttpGet]
    [Route("{id}/trips")]
    public async Task<IActionResult> GetClientsTrips([FromRoute] int id)
    {
        try
        {
            return Ok(await dbService.GetClientsTripsByIdAsync(id));
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }


    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] CreateClientDTO body)
    {
        var client = await dbService.CreateClientAsync(body);
        
        return Created($"clients/{client.Id}", client);
    }

    [HttpPut]
    [Route("{id}/trips/{tripId}")]
    public async Task<IActionResult> RegisterClientToTrip([FromRoute] int id, [FromRoute] int tripId)
    {
        try
        {
            await dbService.RegisterClientToTripAsync(id, tripId);
            return NoContent();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (ClientLimitExceeded e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpDelete]
    [Route("{id}/trips/{tripId}")]
    public async Task<IActionResult> DeleteClientFromTrip([FromRoute] int id, [FromRoute] int tripId)
    {
        try
        {
            await dbService.DeleteClientFromTripAsync(id, tripId);
            return NoContent();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}