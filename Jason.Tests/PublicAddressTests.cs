using System.Net;
using Jason.API.Security;

namespace Jason.Tests;

// Pruebas de la regla que impide que el feed descargue direcciones internas.
public class PublicAddressTests
{
    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("93.184.216.34")]
    [InlineData("172.32.0.1")]
    [InlineData("2606:4700:4700::1111")]
    public void IsPublic_DireccionesDeInternet_SonPublicas(string ip) =>
        Assert.True(PublicAddress.IsPublic(IPAddress.Parse(ip)));

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.5")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.1.10")]
    [InlineData("169.254.169.254")]
    [InlineData("100.64.0.1")]
    [InlineData("0.0.0.0")]
    [InlineData("::1")]
    [InlineData("fe80::1")]
    [InlineData("fd00::1")]
    [InlineData("::ffff:192.168.0.1")]
    public void IsPublic_DireccionesInternas_NoSonPublicas(string ip) =>
        Assert.False(PublicAddress.IsPublic(IPAddress.Parse(ip)));

    [Theory]
    [InlineData("http://localhost:5000/feed")]
    [InlineData("http://127.0.0.1/admin")]
    [InlineData("http://192.168.0.1/")]
    [InlineData("http://[::1]/")]
    [InlineData("http://impresora.local/")]
    public async Task Ensure_UrlInterna_EsRechazada(string url) =>
        await Assert.ThrowsAsync<InvalidOperationException>(() => PublicAddress.EnsureAsync(new Uri(url)));

    [Fact]
    public async Task Ensure_IpPublica_Pasa() =>
        await PublicAddress.EnsureAsync(new Uri("https://8.8.8.8/dns-query"));
}
