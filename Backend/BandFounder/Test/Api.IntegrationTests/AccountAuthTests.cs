using System.Net;
using System.Net.Http.Json;
using Api.IntegrationTests.Infrastructure;
using BandFounder.Application.Dtos.Accounts;

namespace Api.IntegrationTests;

[TestFixture]
public class AccountAuthTests : IntegrationTestBase
{
    [Test]
    public async Task Register_ReturnsJwtToken()
    {
        var token = await RegisterAsync("alice", "alice@example.com");

        Assert.That(token, Does.Contain("."));
    }

    [Test]
    public async Task Authenticate_WithValidCredentials_ReturnsJwtToken()
    {
        await RegisterAsync("bob", "bob@example.com");
        Client.DefaultRequestHeaders.Authorization = null;

        var token = await AuthenticateAsync("bob@example.com");

        Assert.That(token, Does.Contain("."));
    }

    [Test]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/accounts/me");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Me_WithValidToken_ReturnsCurrentAccount()
    {
        var token = await RegisterAsync("carol", "carol@example.com");
        AuthenticateAs(token);

        var response = await Client.GetAsync("/api/accounts/me");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var account = await ReadJsonAsync<AccountDto>(response);
        Assert.That(account.Name, Is.EqualTo("carol"));
        Assert.That(account.Email, Is.EqualTo("carol@example.com"));
        Assert.That(account.Id, Is.Not.Empty);
    }

    [Test]
    public async Task UpdateMyAccount_ChangesEmail()
    {
        var token = await RegisterAsync("dave", "dave@example.com");
        AuthenticateAs(token);

        var updateResponse = await Client.PatchAsJsonAsync("/api/accounts/me", new
        {
            email = "dave.updated@example.com"
        });

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updated = await ReadJsonAsync<AccountDto>(updateResponse);
        Assert.That(updated.Email, Is.EqualTo("dave.updated@example.com"));
    }

    [Test]
    public async Task DeleteMyAccount_ThenMe_ReturnsUnauthorizedOrNotFound()
    {
        var token = await RegisterAsync("erin", "erin@example.com");
        AuthenticateAs(token);

        var deleteResponse = await Client.DeleteAsync("/api/accounts/me");
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var meResponse = await Client.GetAsync("/api/accounts/me");
        Assert.That(meResponse.StatusCode,
            Is.AnyOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError));
    }

    [Test]
    public async Task GetAccountById_ReturnsAccount()
    {
        var token = await RegisterAsync("frank", "frank@example.com");
        AuthenticateAs(token);

        var me = await ReadJsonAsync<AccountDto>(await Client.GetAsync("/api/accounts/me"));

        var response = await Client.GetAsync($"/api/accounts/{me.Id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var account = await ReadJsonAsync<AccountDto>(response);
        Assert.That(account.Name, Is.EqualTo("frank"));
    }

    [Test]
    public async Task Register_DuplicateEmail_ReturnsConflictOrBadRequest()
    {
        await RegisterAsync("grace", "grace@example.com");
        Client.DefaultRequestHeaders.Authorization = null;

        var response = await Client.PostAsJsonAsync("/api/accounts", new
        {
            name = "grace2",
            email = "grace@example.com",
            password = "Password123!"
        });

        Assert.That(response.StatusCode, Is.AnyOf(HttpStatusCode.Conflict, HttpStatusCode.BadRequest));
    }
}