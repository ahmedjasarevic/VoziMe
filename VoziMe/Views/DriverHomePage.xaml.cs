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
            await DisplayAlert("Gre�ka", $"Nije mogu�e prikazati mapu: {ex.Message}", "OK");
        }
    }

    private async void ToggleAvailability_Clicked(object sender, EventArgs e)
    {
        _isAvailable = !_isAvailable;

        var location = await _locationService.GetCurrentLocationAsync();
        if (location == default)
        {
            await DisplayAlert("Lokacija", "Nije mogu�e dobiti trenutnu lokaciju.", "OK");
            return;
        }

        bool success = await _driverService.UpdateDriverAvailabilityAsync(
            _driverId,
            location.Latitude,
            location.Longitude,
            _isAvailable
        );

        if (!success)
        {
            await DisplayAlert("Gre�ka", "Gre�ka pri a�uriranju statusa dostupnosti.", "OK");
            return;
        }

        AvailabilityToggleButton.Text = _isAvailable ? "Prekini potragu" : "Tra�i vo�nju";
        AvailabilityToggleButton.BackgroundColor = _isAvailable ? Colors.Red : Colors.Green;
    }
}