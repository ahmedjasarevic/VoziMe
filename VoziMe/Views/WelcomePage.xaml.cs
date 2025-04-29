using VoziMe.Models;


namespace VoziMe.Views;

public partial class WelcomePage : ContentPage
{
    public WelcomePage()
    {
        InitializeComponent();
    }

    private async void OnUserButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LoginPage(UserType.Customer));
    }

    private async void OnDriverButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LoginPage(UserType.Driver));
    }
}