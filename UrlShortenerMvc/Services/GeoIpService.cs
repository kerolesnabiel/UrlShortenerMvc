using System.Net;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Exceptions;

namespace UrlShortenerMvc.Services;

public interface IGeoIpService
{
    string? GetCountry(IPAddress? ipAddress);
}

internal sealed class GeoIpService : IGeoIpService, IDisposable
{
    private readonly DatabaseReader _reader;
    private readonly ILogger<GeoIpService> _logger;

    public GeoIpService(IWebHostEnvironment environment, ILogger<GeoIpService> logger)
    {
        var databasePath = Path.Combine(environment.ContentRootPath, "GeoIP", "GeoLite2-Country.mmdb");

        if (!File.Exists(databasePath))
            throw new FileNotFoundException("GeoLite2-Country.mmdb was not found.", databasePath);

        _reader = new DatabaseReader(databasePath);
        _logger = logger;
    }

    public string? GetCountry(IPAddress? ipAddress)
    {
        if (ipAddress == null)
            return null;

        if (IPAddress.IsLoopback(ipAddress) || IsPrivate(ipAddress))
            return null;

        try
        {
            var response = _reader.Country(ipAddress);
            return response.Country.IsoCode;
        }
        catch (AddressNotFoundException ex)
        {
            _logger.LogWarning(ex, "GeoIP lookup Not Found for {IpAddress}", ipAddress);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GeoIP lookup failed for {IpAddress}", ipAddress);
            return null;
        }
    }

    private static bool IsPrivate(IPAddress ipAddress)
    {
        if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = ipAddress.GetAddressBytes();

            return
                bytes[0] == 10 ||
                (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                (bytes[0] == 192 && bytes[1] == 168) ||
                (bytes[0] == 127);
        }

        if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            return ipAddress.IsIPv6LinkLocal || ipAddress.IsIPv6SiteLocal || ipAddress.IsIPv6UniqueLocal;

        return false;
    }

    public void Dispose()
    {
        _reader.Dispose();
    }
}
