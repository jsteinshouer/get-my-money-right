using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Api.Features.Identity.Identity;

namespace Api.Tests.Fixtures;

public static class TestClientExtensions
{
    /// <summary>Matches the server's ConfigureHttpJsonOptions so enums round-trip as strings in tests.</summary>
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task LoginAsync(this HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/identity/login", new Login.Command(email, password));
        response.EnsureSuccessStatusCode();
    }
}
