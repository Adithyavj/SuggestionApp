namespace SuggestionAppUI;

public static class RegisterServices
{
    // Extension method for extending the dependency injection services
    public static void ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        builder.Services.AddMemoryCache();
    }
}
