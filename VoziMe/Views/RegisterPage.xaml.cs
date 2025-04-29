using VoziMe.Models;
using VoziMe.Services;


namespace VoziMe.Views;

public partial class RegisterPage : ContentPage
{
    private readonly UserType _userType;
    private readonly UserService _userService;

    public RegisterPage(UserType userType)
    {
        InitializeComponent();
        _userType = userType;
        _userService = Application.Current.Handler.MauiContext.Services.GetService<UserService>();
    }

    private async void OnRegisterButtonClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameEntry.Text) ||
            string.IsNullOrWhiteSpace(EmailEntry.Text) ||
            string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            await DisplayAlert("Gre�ka", "Molimo popunite sva polja", "OK");
            return;
        }

        if (!IsValidEmail(EmailEntry.Text))
        {
            await DisplayAlert("Gre�ka", "Unesite ispravnu email adresu", "OK");
            return;
        }

        try
        {
            bool isRegistered = await _userService.RegisterAsync(
                NameEntry.Text,
                EmailEntry.Text,
                PasswordEntry.Text,
                _userType);

            if (isRegistered)
            {
                await DisplayAlert("Uspjeh", "Registracija uspje�na. Mo�ete se prijaviti.", "OK");
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("Gre�ka", "Registracija nije uspjela. Poku�ajte ponovo.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Gre�ka", $"Do�lo je do gre�ke: {ex.Message}", "OK");
        }
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private async void OnGoogleSignupClicked(object sender, EventArgs e)
    {
        // Implement Google signup
        await DisplayAlert("Info", "Google registracija nije implementirana u ovoj verziji", "OK");
    }

    private async void OnFacebookSignupClicked(object sender, EventArgs e)
    {
        // Implement Facebook signup
        await DisplayAlert("Info", "Facebook registracija nije implementirana u ovoj verziji", "OK");
    }

    private async void OnLoginLabelTapped(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}