using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using VoziMe.Models;
using VoziMe.Services;

namespace VoziMe.Views;

public partial class DriverSelectionPage : ContentPage
{
    private readonly DriverService _driverService;
    private readonly UserService _userService;
    private readonly LocationService _locationService;
    private double _currentLatitude;
    private double _currentLongitude;

    public DriverSelectionPage()
    {
        InitializeComponent();

        _driverService = Application.Current.Handler.MauiContext.Services.GetService<DriverService>();
        _userService = Application.Current.Handler.MauiContext.Services.GetService<UserService>();
        _locationService = Application.Current.Handler.MauiContext.Services.GetService<LocationService>();

        InitializeMapAsync();
        LoadDriversAsync();
    }

    private async void InitializeMapAsync()
    {
        try
        {
            // Get current location
            var location = await _locationService.GetCurrentLocationAsync();
            Console.WriteLine($"Lokacija dohvaćena: {location.Latitude}, {location.Longitude}");

            _currentLatitude = location.Latitude;
            _currentLongitude = location.Longitude;

            // Set map position
            LocationMap.MoveToRegion(
                MapSpan.FromCenterAndRadius(
                    new Location(_currentLatitude, _currentLongitude),
                    Distance.FromKilometers(1)));

            // Load nearby drivers
            await LoadDriversAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Greška", $"Nije moguæe uèitati mapu: {ex.Message}", "OK");
        }
    }

    private async Task LoadDriversAsync()
    {
        try
        {
            var drivers = await _driverService.GetNearbyDriversAsync(_currentLatitude, _currentLongitude);
            DriversCollection.ItemsSource = drivers;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Greška", $"Nije moguæe uèitati vozaèe: {ex.Message}", "OK");
        }
    }

    private async void SourceEntry_Completed(object sender, EventArgs e)
    {
        var address = SourceEntry.Text;
        if (!string.IsNullOrWhiteSpace(address))
        {
            try
            {
                var locations = await Geocoding.GetLocationsAsync(address);
                var location = locations?.FirstOrDefault();
                if (location != null)
                {
                    _currentLatitude = location.Latitude;
                    _currentLongitude = location.Longitude;

                    LocationMap.MoveToRegion(
                        MapSpan.FromCenterAndRadius(
                            new Location(_currentLatitude, _currentLongitude),
                            Distance.FromKilometers(1)));

                    await LoadDriversAsync();
                }
                else
                {
                    await DisplayAlert("Lokacija", "Lokacija nije pronađena.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Greška", $"Greška pri pretrazi lokacije: {ex.Message}", "OK");
            }
        }
    }

    private async void OnDriverSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Driver selectedDriver)
        {
            bool confirm = await DisplayAlert(
                "Potvrda",
                $"Da li želite naruèiti vožnju sa vozaèem {selectedDriver.Name}?",
                "Da", "Ne");

            if (confirm)
            {
                try
                {
                    // Get destination address
                    string destinationAddress = "Radakovo"; // Default or from UI

                    // Book the ride
                    bool success = await _driverService.BookRideAsync(
                        _userService.CurrentUser.Id,
                        selectedDriver.Id,
                        _currentLatitude,
                        _currentLongitude,
                        SourceEntry.Text ?? "Fast Food King 2",
                        selectedDriver.Latitude, // For demo, we'll use driver's location as destination
                        selectedDriver.Longitude,
                        destinationAddress);

                    if (success)
                    {
                        await DisplayAlert("Uspjeh", "Vožnja je naruèena. Vozaè æe uskoro stiæi.", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Greška", "Nije moguæe naruèiti vožnju. Pokušajte ponovo.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Greška", $"Došlo je do greške: {ex.Message}", "OK");
                }
            }

            // Clear selection
            DriversCollection.SelectedItem = null;
        }
    }
}