namespace TravelAgencyApi.Exceptions;

public class ClientLimitExceeded(string message) : Exception(message);
