using VoziMe.Services;
using VoziMe.Views;

namespace VoziMe.Views;

public partial class SplashPage : ContentPage
{
    private readonly DatabaseService _databaseService;

    // Inicijalizacija u konstruktoru
    public SplashPage(DatabaseService databaseService)
    {
        InitializeComponent();
        _databaseService = databaseService;  // Koristi injected DatabaseService
    }

    // Ova metoda se poziva kad stranica postane vidljiva
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Proveri da li je _databaseService inicijalizovan
            if (_databaseService != null)
            {
                // Seed database with sample data
                await _databaseService.SeedDatabaseAsync();

                // Simulate loading time
                await Task.Delay(2000);

                // Navigacija do welcome stranice
                await Navigation.PushAsync(new WelcomePage());

                // Ukloni ovu stranicu sa stoga stranica
                Navigation.RemovePage(this);
            }
            else
            {
                Console.WriteLine("DatabaseService is not initialized.");
            }
        }
        catch (Exception ex)
        {
            // Loguj greške, ako doðe do bilo kog izuzetka
            Console.WriteLine($"Error in SplashPage: {ex.Message}");
        }
    }
}
