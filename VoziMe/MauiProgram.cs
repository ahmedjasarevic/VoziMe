using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Maps;
using VoziMe.Services;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace VoziMe;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        // Direktno uneseni konekcioni string
        var connectionString = "User Id=postgres.vfqrsstbgqfwukfgslyo;Password=SanidMuhic123;Server=aws-0-eu-central-1.pooler.supabase.com;Port=5432;Database=postgres";


        builder
            .UseMauiApp<App>()
            .UseMauiMaps()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Registruj servise
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<UserService>();
        builder.Services.AddSingleton<DriverService>();
        builder.Services.AddSingleton<LocationService>();

        // Registruj Supabase konekciju sa bazom kao singleton
        builder.Services.AddSingleton<NpgsqlConnection>(_ => new NpgsqlConnection(connectionString));

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
