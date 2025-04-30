using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using VoziMe.Models;
using VoziMe.Services;

namespace VoziMe.Views;

public partial class RideTrackingPage : ContentPage
{
    private readonly Driver _driver;
    private readonly Location _pickupLocation;
    private readonly LocationService _locationService;
    private readonly DriverService _driverService;

    private Pin _driverPin;
    private bool _isDriverArrived;

    public RideTrackingPage(Driver driver, Location pickupLocation)
    {
        InitializeComponent();

        _driver = driver;
        _pickupLocation = pickupLocation;

        _locationService = Application.Current.Handler.MauiContext.Services.GetService<LocationService>();
        _driverService = Application.Current.Handler.MauiContext.Services.GetService<DriverService>();

        _isDriverArrived = false;

        InitializeTracking();
    }

    private async void InitializeTracking()
    {
        DriverNameLabel.Text = $"Vozač: {_driver.Name}";
        CarInfoLabel.Text = $"Auto: {_driver.Car}";
        EtaLabel.Text = "ETA: Izračunava se...";

        var center = new Location(_pickupLocation.Latitude, _pickupLocation.Longitude);
        TrackingMap.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromKilometers(2)));

        var userPin = new Pin
        {
            Label = "Vaša lokacija",
            Location = _pickupLocation,
            Type = PinType.Place
        };
        TrackingMap.Pins.Add(userPin);

        var driverLoc = new Location(_driver.Latitude, _driver.Longitude);
        _driverPin = new Pin
        {
            Label = _driver.Name,
            Location = driverLoc,
            Type = PinType.SavedPin
        };
        TrackingMap.Pins.Add(_driverPin);

        StartDriverLocationUpdates();
    }

    private void StartDriverLocationUpdates()
    {
        Device.StartTimer(TimeSpan.FromSeconds(5), () =>
        {
            _ = UpdateDriverLocationAsync();
            return true;
        });
    }

    private async Task UpdateDriverLocationAsync()
    {
        if (_isDriverArrived) return;

        try
        {
            var updatedLocation = _driverPin.Location;
            var distance = Location.CalculateDistance(updatedLocation, _pickupLocation, DistanceUnits.Kilometers);

            if (distance > 0.05)
            {
                var step = distance * 0.1;
                var newLat = updatedLocation.Latitude + ((_pickupLocation.Latitude - updatedLocation.Latitude) * 0.05);
                var newLong = updatedLocation.Longitude + ((_pickupLocation.Longitude - updatedLocation.Longitude) * 0.05);
                updatedLocation = new Location(newLat, newLong);

                _driverPin.Location = updatedLocation;
                EtaLabel.Text = $"Udaljenost: {distance:F2} km";
                TrackingMap.MoveToRegion(MapSpan.FromCenterAndRadius(updatedLocation, Distance.FromKilometers(2)));
            }
            else
            {
                _driverPin.Location = _pickupLocation;
                _isDriverArrived = true;
                EtaLabel.Text = "Vozač je stigao!";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Greška pri ažuriranju lokacije vozača: {ex.Message}");
        }
    }

    private async void FinishRideButton_Clicked(object sender, EventArgs e)
    {
        bool success = await _driverService.FinishRideAsync(_driver.Id);
        if (success)
        {
            await DisplayAlert("Vožnja završena", "Vozač je sada ponovo dostupan.", "OK");
            await Navigation.PushAsync(new DriverSelectionPage());

        }
        else
        {
            await DisplayAlert("Greška", "Došlo je do greške prilikom završavanja vožnje.", "OK");
        }
    }
}
