using System.Net;
using System.Net.Sockets;

namespace Jason.API.Security;

// Las fuentes pueden llegar desde un archivo importado de otro grupo, así que el servidor
// no debe descargar direcciones internas (localhost, redes privadas, metadatos de la nube).
public static class PublicAddress
{
    // Lanza InvalidOperationException si el host de la URL no apunta a una dirección pública.
    public static async Task EnsureAsync(Uri url, CancellationToken cancellationToken = default)
    {
        string host = url.DnsSafeHost;

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase))
        {
            throw NotPublic(host);
        }

        IPAddress[] addresses;
        if (IPAddress.TryParse(host, out IPAddress? literal))
        {
            addresses = new[] { literal };
        }
        else
        {
            try
            {
                addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
            }
            catch (SocketException)
            {
                throw new InvalidOperationException("No se encontró la dirección " + host + ".");
            }
        }

        if (addresses.Length == 0 || addresses.Any(address => !IsPublic(address)))
            throw NotPublic(host);
    }

    public static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            return false;

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // fe80::/10 (enlace local), fec0::/10 (sitio local) y fc00::/7 (privadas)
            return !address.IsIPv6LinkLocal
                && !address.IsIPv6SiteLocal
                && (address.GetAddressBytes()[0] & 0xFE) != 0xFC;
        }

        byte[] bytes = address.GetAddressBytes();
        return !(bytes[0] == 0
            || bytes[0] == 10
            || (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127)
            || bytes[0] == 127
            || (bytes[0] == 169 && bytes[1] == 254)
            || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            || (bytes[0] == 192 && bytes[1] == 168)
            || bytes[0] >= 224);
    }

    private static InvalidOperationException NotPublic(string host) =>
        new("La fuente " + host + " no es una dirección pública, por eso no se descarga.");
}
