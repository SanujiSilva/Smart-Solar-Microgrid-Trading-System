namespace SmartSolarMicrogrid.Api.Helpers;

// Only deliberately safe business messages belong in this exception type.
public sealed class ApiException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
