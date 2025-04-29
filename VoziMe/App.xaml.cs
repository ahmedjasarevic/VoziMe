using VoziMe.Services;
using VoziMe.Views;

namespace VoziMe;

public partial class App : Application
{
    private readonly DatabaseService _databaseService;

    public App(DatabaseService databaseService)
    {
        InitializeComponent();
        _databaseService = databaseService;

        // Initialize database
        InitializeDatabase();

        // Start with splash screen, using DI to get DatabaseService for SplashPage
        MainPage = new NavigationPage(new SplashPage(_databaseService))
        {
            BarBackgroundColor = Colors.Black,
            BarTextColor = Colors.White
        };
    }

    private async void InitializeDatabase()
    {
        try
        {
            await _databaseService.InitializeDatabaseAsync();
        }
        catch (Exception ex)
        {
            // Log error or show message
            Console.WriteLine($"Database initialization error: {ex.Message}");
        }
    }
}
