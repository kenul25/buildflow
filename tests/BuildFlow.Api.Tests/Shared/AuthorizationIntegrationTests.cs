using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Tests.Infrastructure;
namespace BuildFlow.Api.Tests.Shared;
[Collection("PostgreSQL"), Trait("Category", "Integration")]
public sealed class AuthorizationIntegrationTests(PostgreSqlFixture fixture)
{
    [Theory]
    [InlineData("/api/projects")]
    [InlineData("/api/inventory/materials")]
    [InlineData("/api/Suppliers")]
    [InlineData("/api/scheduling/workers")]
    public async Task AnonymousAccessIsRejected(string path)
    {
        using var client = fixture.Factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
    }
    [Fact]
    public async Task InvalidBearerTokenIsRejected()
    {
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }
    [Fact]
    public async Task ExpiredSignedTokenIsRejected()
    {
        using var client = await fixture.ClientAsync(clock: new PastClock());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }
    [Fact]
    public async Task InactiveUsersSignedTokenIsRejected()
    {
        using var client = await fixture.ClientAsync(active: false);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }
    [Fact]
    public async Task LoginRefreshAndLogoutInvalidateTheSession()
    {
        using var client = fixture.Factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = TestApiFactory.AdminEmail, Password = TestApiFactory.AdminPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = (await login.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest { RefreshToken = auth.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var replacement = (await refresh.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.NotEqual(auth.RefreshToken, replacement.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest { RefreshToken = auth.RefreshToken })).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", replacement.AccessToken);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest { RefreshToken = replacement.RefreshToken })).StatusCode);
    }
    [Fact]
    public async Task WrongPasswordIsRejected()
    {
        using var client = fixture.Factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = TestApiFactory.AdminEmail, Password = "Wrong-Password-123!" })).StatusCode);
    }
    private sealed class PastClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow.AddHours(-2);
    }
}
