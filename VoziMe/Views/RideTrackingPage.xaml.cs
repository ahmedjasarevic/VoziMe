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

    private Pin _driverPin;
    private bool _isDriverArrived;

    public RideTrackingPage(Driver driver, Location pickupLocation)
    {
        InitializeComponent();

        _driver = driver;
        _pickupLocation = pickupLocation;

        _locationService = Application.Current.Handler.MauiContext.Services.GetService<LocationService>();

        _isDriverArrived = false;  // Postavi da vozač još nije stigao

        InitializeTracking();
    }

    private async void InitializeTracking()
    {
        DriverNameLabel.Text = $"Vozač: {_driver.Name}";
        CarInfoLabel.Text = $"Auto: {_driver.Car}";
        EtaLabel.Text = "ETA: Izračunava se...";

        // Postavi mapu
        var center = new Location(_pickupLocation.Latitude, _pickupLocation.Longitude);
        TrackingMap.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromKilometers(2)));

        // Dodaj početnu lokaciju korisnika
        var userPin = new Pin
        {
            Label = "Vaša lokacija",
            Location = _pickupLocation,
            Type = PinType.Place
        };
        TrackingMap.Pins.Add(userPin);

        // Dodaj vozača
        var driverLoc = new Location(_driver.Latitude, _driver.Longitude);
        _driverPin = new Pin
        {
            Label = _driver.Name,
            Location = driverLoc,
            Type = PinType.SavedPin
        };
        TrackingMap.Pins.Add(_driverPin);

        // Start polling (simulacija kretanja/pozicije)
        StartDriverLocationUpdates();
    }

    private void StartDriverLocationUpdates()
    {
        Device.StartTimer(TimeSpan.FromSeconds(5), () =>
        {
            _ = UpdateDriverLocationAsync(); // fire and forget
            return true; // nastavi timer
        });
    }

    private async Task UpdateDriverLocationAsync()
    {
        if (_isDriverArrived) return; // Ne ažuriraj ako je vozač stigao

        try
        {
            // Simulacija vozačevog kretanja prema korisniku
            var updatedLocation = _driverPin.Location;

            // Računanje udaljenosti između vozača i korisnika
            var distance = Location.CalculateDistance(updatedLocation, _pickupLocation, DistanceUnits.Kilometers);

            if (distance > 0.05) // Ako je udaljenost još veća od 50m, vozač se pomiče
            {
                // Pomiči vozača prema korisniku (simulacija kretanja)
                var step = distance * 0.1; // Pomiče se 10% udaljenosti
                var newLat = updatedLocation.Latitude + ((_pickupLocation.Latitude - updatedLocation.Latitude) * 0.05);
                var newLong = updatedLocation.Longitude + ((_pickupLocation.Longitude - updatedLocation.Longitude) * 0.05);
                updatedLocation = new Location(newLat, newLong);

                _driverPin.Location = updatedLocation;

                // Ažuriraj ETA
                EtaLabel.Text = $"Udaljenost: {distance:F2} km";

                // Centriraj mapu prema vozaču
                TrackingMap.MoveToRegion(MapSpan.FromCenterAndRadius(updatedLocation, Distance.FromKilometers(2)));
            }
            else
            {
                // Vozač je stigao, postavi ga na korisnikovoj lokaciji
                _driverPin.Location = _pickupLocation;
                _isDriverArrived = true; // Postavi da je vozač stigao
                EtaLabel.Text = "Vozač je stigao!";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Greška pri ažuriranju lokacije vozača: {ex.Message}");
        }
    }
}
