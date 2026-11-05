using System.Security.Cryptography;
using Jason.API.Exceptions;
using Jason.DataAccess.Repositories;
using Jason.Models.Dtos;
using Jason.Models.Entities;
using Microsoft.AspNetCore.DataProtection;

namespace Jason.API.Services;

public interface ISecretService
{
    Task<IEnumerable<SecretDto>> GetAllAsync();
    Task<SecretDto> SaveAsync(SaveSecretDto dto);
    Task DeleteAsync(string key);

    // Para uso interno de la API (por ejemplo al llamar a una fuente que pide API key).
    // El valor en claro nunca sale por un endpoint.
    Task<string?> GetValueAsync(string key);
}

public class SecretService : ISecretService
{
    // Cambiar este texto haría ilegibles todos los secrets guardados
    private const string Purpose = "Jason.Secrets.v1";

    private readonly ISettingRepository _settings;
    private readonly ISourceRepository _sources;
    private readonly IDataProtector _protector;

    public SecretService(ISettingRepository settings, ISourceRepository sources, IDataProtectionProvider protectionProvider)
    {
        _settings = settings;
        _sources = sources;
        _protector = protectionProvider.CreateProtector(Purpose);
    }

    public async Task<IEnumerable<SecretDto>> GetAllAsync()
    {
        var settings = await _settings.ReadAsync();
        return settings.OrderBy(s => s.Key).Select(ToDto);
    }

    public async Task<SecretDto> SaveAsync(SaveSecretDto dto)
    {
        var key = dto.Key.Trim();

        if (dto.SourceId is int sourceId && await _sources.FindAsync(sourceId) is null)
            throw ApiException.NotFound("La fuente indicada no existe.");

        var encrypted = _protector.Protect(dto.Value);
        var existing = await _settings.FindByKeyAsync(key);

        if (existing is null)
        {
            existing = new Setting
            {
                Key = key,
                ValueEncrypted = encrypted,
                SourceId = dto.SourceId,
                UpdatedAt = DateTime.UtcNow
            };
            await _settings.CreateAsync(existing);
        }
        else
        {
            existing.ValueEncrypted = encrypted;
            existing.SourceId = dto.SourceId;
            existing.UpdatedAt = DateTime.UtcNow;
            await _settings.UpdateAsync(existing);
        }

        return ToDto(existing);
    }

    public async Task DeleteAsync(string key)
    {
        var existing = await _settings.FindByKeyAsync(key)
            ?? throw ApiException.NotFound("No existe un secret con ese nombre.");
        await _settings.DeleteAsync(existing);
    }

    public async Task<string?> GetValueAsync(string key)
    {
        var existing = await _settings.FindByKeyAsync(key);
        return existing is null ? null : _protector.Unprotect(existing.ValueEncrypted);
    }

    private SecretDto ToDto(Setting setting)
    {
        string masked;
        try
        {
            masked = SecretMask.Apply(_protector.Unprotect(setting.ValueEncrypted));
        }
        catch (CryptographicException)
        {
            // Pasa si la base se copió a otra computadora: las llaves de cifrado son de cada equipo
            masked = "No se puede leer en este equipo";
        }

        return new SecretDto
        {
            Key = setting.Key,
            SourceId = setting.SourceId,
            MaskedValue = masked,
            UpdatedAt = setting.UpdatedAt
        };
    }
}

public static class SecretMask
{
    // Muestra solo los últimos 4 caracteres, y solo si el valor es lo bastante largo para no regalar la clave
    public static string Apply(string value) =>
        value.Length >= 12 ? "••••••••" + value[^4..] : "••••••••";
}
