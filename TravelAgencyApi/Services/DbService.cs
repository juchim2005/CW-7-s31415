
using TravelAgencyApi.Models;
using TravelAgencyApi.Models.DTOs;
using Microsoft.Data.SqlClient;
using TravelAgencyApi.Exceptions;

namespace TravelAgencyApi.Services;

public interface IDbService
{
    public Task<IEnumerable<TripGetDTO>> GetTripsAsync();
    public Task<List<TripGetDTO>> GetClientsTripsByIdAsync(int id);
    public Task<Client> CreateClientAsync(CreateClientDTO client);
    public Task RegisterClientToTripAsync(int clientId, int tripId);
    public Task DeleteClientFromTripAsync(int clientId, int tripId);
}

public class DbService(IConfiguration config) : IDbService
{
    
    private readonly string? _connectionString = config.GetConnectionString("Default");
    
    public async Task<IEnumerable<TripGetDTO>> GetTripsAsync()
    {
        var result = new List<TripGetDTO>();
        await using var connection = new SqlConnection(_connectionString);
        
        const string sql = "SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name AS CountryName" +
                           " FROM Trip t LEFT JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip" +
                           " LEFT JOIN Country c ON ct.IdCountry = c.IdCountry";
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new TripGetDTO
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                DateFrom = reader.GetDateTime(3),
                DateTo = reader.GetDateTime(4),
                MaxPeople = reader.GetInt32(5),
                Country = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }
        return result;
    }

    public async Task<List<TripGetDTO>> GetClientsTripsByIdAsync(int id)
    {
        var result = new List<TripGetDTO>();
        using var connection = new SqlConnection(_connectionString);
        
        const string sql = "SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name AS CountryName" +
                           " FROM Trip t INNER JOIN Client_Trip ct ON t.IdTrip = ct.IdTrip" +
                           " LEFT JOIN Country_Trip cct ON t.IdTrip = cct.IdTrip LEFT JOIN Country c ON cct.IdCountry = c.IdCountry" +
                           " WHERE ct.IdClient = @Id";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            throw new NotFoundException("Client not found");
        }

        while (await reader.ReadAsync())
        {
            result.Add(new TripGetDTO
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                DateFrom = reader.GetDateTime(3),
                DateTo = reader.GetDateTime(4),
                MaxPeople = reader.GetInt32(5),
                Country = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }

        if (result.Count == 0)
        {
            throw new NotFoundException("No trips found");
        }
        return result;
    }

    public async Task<Client> CreateClientAsync(CreateClientDTO client)
    {
        await using var connection = new SqlConnection(_connectionString);
        
        const string sql = "INSERT INTO Clients (FirstName, LastName, Email, Telephone, Pesel)"+
                           " VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel); " +
                           "SELECT scope_identity()";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@FirstName", client.FirstName);
        command.Parameters.AddWithValue("@LastName", client.LastName);
        command.Parameters.AddWithValue("@Email", client.Email);
        command.Parameters.AddWithValue("@Telephone", client.Telephone);
        command.Parameters.AddWithValue("@Pesel", client.Pesel);
        
        await connection.OpenAsync();
        var id = Convert.ToInt32(await command.ExecuteScalarAsync());

        return new Client
        {
            Id = id,
            FirstName = client.FirstName,
            LastName = client.LastName,
            Email = client.Email,
            Telephone = client.Telephone,
            Pesel = client.Pesel
        };
    }

    public async Task RegisterClientToTripAsync(int clientId, int tripId)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        

        await using (var checkClientCmd = new SqlCommand("SELECT COUNT(1) FROM Clients WHERE Id = @ClientId", connection))
        {
            checkClientCmd.Parameters.AddWithValue("@ClientId", clientId);

            var clientExists = Convert.ToInt32(await checkClientCmd.ExecuteScalarAsync());
            if (clientExists == 0)
                throw new NotFoundException("Client not found");
        }


        await using (var checkTripCmd = new SqlCommand("SELECT COUNT(1) FROM Trips WHERE Id = @TripId", connection))
        {
            checkTripCmd.Parameters.AddWithValue("@TripId", tripId);

            var tripExists = Convert.ToInt32(await checkTripCmd.ExecuteScalarAsync());
            if (tripExists == 0)
                throw new NotFoundException("Trip not found");
        }


        int maxParticipants;
        await using (var maxPeopleCmd = new SqlCommand("SELECT MaxPeople FROM Trip WHERE IdTrip = @TripId", connection))
        {
            maxPeopleCmd.Parameters.AddWithValue("@TripId", tripId);
            maxParticipants = Convert.ToInt32(await maxPeopleCmd.ExecuteScalarAsync());
        }


        int currentParticipants;
        await using (var currentPeopleCmd = new SqlCommand("SELECT COUNT(1) FROM Client_Trip WHERE IdTrip = @TripId", connection))
        {
            currentPeopleCmd.Parameters.AddWithValue("@TripId", tripId);
            currentParticipants = Convert.ToInt32(await currentPeopleCmd.ExecuteScalarAsync());
        }

        if (currentParticipants >= maxParticipants)
            throw new ClientLimitExceeded("Too many participants");
        
        var insertQuery = "INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt) VALUES (@ClientId, @TripId, @RegisteredAt)";
        var insertCmd = new SqlCommand(insertQuery, connection);
        insertCmd.Parameters.AddWithValue("@ClientId", clientId);
        insertCmd.Parameters.AddWithValue("@TripId", tripId);
        insertCmd.Parameters.AddWithValue("@RegisteredAt", DateTime.UtcNow);

        await insertCmd.ExecuteNonQueryAsync();

    }

    public async Task DeleteClientFromTripAsync(int clientId, int tripId)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        await using (var checkRegistrationCmd = new SqlCommand(
                         "SELECT COUNT(1) FROM Client_Trip WHERE IdClient = @ClientId AND IdTrip = @TripId", 
                         connection))
        {
            checkRegistrationCmd.Parameters.AddWithValue("@ClientId", clientId);
            checkRegistrationCmd.Parameters.AddWithValue("@TripId", tripId);

            var registrationExists = Convert.ToInt32(await checkRegistrationCmd.ExecuteScalarAsync());
            if (registrationExists == 0)
                throw new NotFoundException("Registration not found");
        }
        
        var deleteQuery = "DELETE FROM Client_Trip WHERE IdClient = @ClientId AND IdTrip = @TripId";
        var deleteCmd = new SqlCommand(deleteQuery, connection);
        deleteCmd.Parameters.AddWithValue("@ClientId", clientId);
        deleteCmd.Parameters.AddWithValue("@TripId", tripId);

        await deleteCmd.ExecuteNonQueryAsync();
    }
}