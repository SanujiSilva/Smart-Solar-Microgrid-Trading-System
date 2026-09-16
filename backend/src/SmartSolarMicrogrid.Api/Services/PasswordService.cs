using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Services;

public sealed class PasswordService
{
    private readonly PasswordHasher<User> hasher = new(Options.Create(new PasswordHasherOptions { IterationCount = 210_000 }));
    private readonly User dummy = new() { FullName = "", Email = "", Phone = "" };
    private readonly string dummyHash;

    public PasswordService() => dummyHash = hasher.HashPassword(dummy, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

    public string Hash(User user, string password) => hasher.HashPassword(user, password);

    public PasswordVerificationResult Verify(User? user, string password)
    {
        PasswordVerificationResult result;
        try { result = hasher.VerifyHashedPassword(user ?? dummy, user?.PasswordHash ?? dummyHash, password); }
        catch (FormatException) { return PasswordVerificationResult.Failed; }
        return user is null ? PasswordVerificationResult.Failed : result;
    }
}
