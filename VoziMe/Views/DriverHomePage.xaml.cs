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

    private readonly int _userId;
    private int _driverId;
    private Driver _driver;

    public DriverHomePage(int userId)
    {
        InitializeComponent();
        _driverService = Application.Current.Handler.MauiContext.Services.GetService<DriverService>();
        _locationService = Application.Current.Handler.MauiContext.Services.GetService<LocationService>();
        _userId = userId;

        InitializeMap();
        _ = LoadDriverDataAndAvailability();
    }

    private async Task LoadDriverDataAndAvailability()
    {
        try
        {
            _driver = await _driverService.GetDriverByUserIdAsync(_userId);

            if (_driver != null)
            {
                _driverId = _driver.Id;
                _isAvailable = _driver.IsAvailable;

                AvailabilityToggleButton.Text = _isAvailable ? "Prekini potragu" : "Traži vožnju";
                AvailabilityToggleButton.BackgroundColor = _isAvailable ? Colors.Red : Colors.Green;
            }
            else
            {
                _isAvailable = false;
                AvailabilityToggleButton.Text = "Traži vožnju";
                AvailabilityToggleButton.BackgroundColor = Colors.Green;
                await DisplayAlert("Greška", "Nije pronađen vozač za ovog korisnika.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Greška", $"Nije moguće učitati podatke vozača: {ex.Message}", "OK");
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
