using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using VoziMe.Models;
using VoziMe.Services;

namespace VoziMe.Views;

public partial class DriverHomePage : ContentPage
{
    private readonly DriverService _driverService;
    private readonly LocationService _locationService;
    private bool _isAvailable = false;
    private int _driverId; // ovo je `Drivers.Id`, ne `Users.Id`

    public DriverHomePage(int driverId)
    {
        InitializeComponent();
        _driverService = Application.Current.Handler.MauiContext.Services.GetService<DriverService>();
        _locationService = Application.Current.Handler.MauiContext.Services.GetService<LocationService>();
        _driverId = driverId;

        InitializeMap();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Inicijalizuj status dostupnosti vozača
        await LoadDriverAvailability();
    }

    private async Task LoadDriverAvailability()
    {
        try
        {
            // Dohvati trenutnu dostupnost vozača iz baze
            var driver = await _driverService.GetDriverByUserIdAsync(_driverId);

            if (driver != null)
            {
                _isAvailable = driver.IsAvailable;

                // Ažuriraj UI prema dostupnosti vozača
                AvailabilityToggleButton.Text = _isAvailable ? "Prekini potragu" : "Traži vožnju";
                AvailabilityToggleButton.BackgroundColor = _isAvailable ? Colors.Red : Colors.Green;
            }
            else
            {
                // Ako vozač nije pronađen, setuj dostupnost na false
                _isAvailable = false;
                AvailabilityToggleButton.Text = "Traži vožnju";
                AvailabilityToggleButton.BackgroundColor = Colors.Green;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Greška", $"Nije moguće učitati status vozača: {ex.Message}", "OK");
        }
    }

    private async void InitializeMap()
    {
        try
        {
            var location = await _locationService.GetCurrentLocationAsync();
            if (location != default)
            {
                LocationMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                    new Location(location.Latitude, location.Longitude),
                    Distance.FromKilometers(1)));
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Greška", $"Nije moguće prikazati mapu: {ex.Message}", "OK");
        }
    }

    private async void ToggleAvailability_Clicked(object sender, EventArgs e)
    {
        var location = await _locationService.GetCurrentLocationAsync();
        if (location == default)
        {
            await DisplayAlert("Lokacija", "Nije moguće dobiti trenutnu lokaciju.", "OK");
            return;
        }

        // Obrni status tek sad
        _isAvailable = !_isAvailable;

        bool success = await _driverService.UpdateDriverAvailabilityAsync(
            _driverId,
            location.Latitude,
            location.Longitude,
            _isAvailable
        );

        if (!success)
        {
            await DisplayAlert("Greška", "Greška pri ažuriranju statusa dostupnosti.", "OK");
            return;
        }

        AvailabilityToggleButton.Text = _isAvailable ? "Prekini potragu" : "Traži vožnju";
        AvailabilityToggleButton.BackgroundColor = _isAvailable ? Colors.Red : Colors.Green;
    }
}
