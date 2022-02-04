namespace SuggestionAppUI;

public static class RegisterServices
{
    // Extension method for extending the dependency injection services
    public static void ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        builder.Services.AddMemoryCache();

        // Add Dependency Injection container for DataAccess classes
        builder.Services.AddSingleton<IDbConnection, DbConnection>(); // creates a single instance for everyone
        builder.Services.AddSingleton<ICategoryData,MongoCategoryData>();
        builder.Services.AddSingleton<IStatusData, MongoStatusData>();
        builder.Services.AddSingleton<IUserData, MongoUserData>();
        builder.Services.AddSingleton<ISuggestionData, MongoSuggestionData>();
    }
}
