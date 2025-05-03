using Npgsql;
using VoziMe.Models;
using VoziMe.Services;


namespace VoziMe.Views;

public partial class LoginPage : ContentPage
{
    private readonly DriverService _driverService;
    private readonly UserType _userType;
    private readonly UserService _userService;

    public LoginPage(UserType userType)
    {
        InitializeComponent();
        _userType = userType;
        _userService = Application.Current.Handler.MauiContext.Services.GetService<UserService>();
        _driverService = Application.Current.Handler.MauiContext.Services.GetService<DriverService>();
    }

    private async void OnLoginButtonClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UsernameEntry.Text) || string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            await DisplayAlert("Gre�ka", "Molimo unesite korisni�ko ime i lozinku", "OK");
            return;
        }

        try
        {
            bool isAuthenticated = await _userService.LoginAsync(UsernameEntry.Text, PasswordEntry.Text, _userType);

            if (isAuthenticated)
            {
                if (_userType == UserType.Customer)
                {
                    await Navigation.PushAsync(new DriverSelectionPage());
                }
                else
                {
                    var user = await _userService.GetUserByEmailAsync(UsernameEntry.Text);
                    var driver = await _driverService.GetDriverByUserIdAsync(user.Id);
                    int driverId = driver.Id;
                    // For driver, you would navigate to a driver dashboard
                    await Navigation.PushAsync(new DriverHomePage(driverId));
                }
            }
            else
            {
                await DisplayAlert("Gre�ka", "Pogre�no korisni�ko ime ili lozinka", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Gre�ka", $"Do�lo je do gre�ke: {ex.Message}", "OK");
        }
    }

    private async void OnGoogleLoginClicked(object sender, EventArgs e)
    {
        // Implement Google login
        await DisplayAlert("Info", "Google prijava nije implementirana u ovoj verziji", "OK");
    }

    private async void OnFacebookLoginClicked(object sender, EventArgs e)
    {
        // Implement Facebook login
        await DisplayAlert("Info", "Facebook prijava nije implementirana u ovoj verziji", "OK");
    }

    private async void OnRegisterLabelTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage(_userType));
    }
}