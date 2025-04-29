using VoziMe.Models;
using VoziMe.Services;


namespace VoziMe.Views;

public partial class LoginPage : ContentPage
{
    private readonly UserType _userType;
    private readonly UserService _userService;

    public LoginPage(UserType userType)
    {
        InitializeComponent();
        _userType = userType;
        _userService = Application.Current.Handler.MauiContext.Services.GetService<UserService>();
    }

    private async void OnLoginButtonClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UsernameEntry.Text) || string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            await DisplayAlert("Greška", "Molimo unesite korisnièko ime i lozinku", "OK");
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
                    // For driver, you would navigate to a driver dashboard
                    await DisplayAlert("Uspjeh", "Vozaè uspješno prijavljen. Vozaèki dashboard nije implementiran u ovoj verziji.", "OK");
                }
            }
            else
            {
                await DisplayAlert("Greška", "Pogrešno korisnièko ime ili lozinka", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Greška", $"Došlo je do greške: {ex.Message}", "OK");
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