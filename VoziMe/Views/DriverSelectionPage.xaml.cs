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
            await DisplayAlert("Greška", $"Nije moguće učitati mapu: {ex.Message}", "OK");
        }
    }
    private async Task LoadDriversAsync()
    {
        try
        {
            var drivers = await _driverService.GetNearbyDriversAsync(_currentLatitude, _currentLongitude);

            // Provjera broja vozača
            Console.WriteLine($"Broj vozača: {drivers.Count()}");

            // Ako nema vozača, obavijestiti korisnika
            if (drivers.Count() == 0)
            {
                await DisplayAlert("Nema vozača", "Trenutno nema vozača u vašoj blizini.", "OK");
            }

            // Dodavanje vozača u CollectionView
            DriversCollection.ItemsSource = drivers;

            // Dodavanje pinova za vozače na mapu
            foreach (var driver in drivers)
            {
                if (driver.Latitude != 0 && driver.Longitude != 0)
                {
                    var driverLocation = new Location(driver.Latitude, driver.Longitude);

                    // Provjerite da li već postoji pin za vozača
                    var existingPin = LocationMap.Pins.FirstOrDefault(pin => pin.Label == driver.Name);
                    if (existingPin == null)
                    {
                        var pin = new Pin
                        {
                            Label = driver.Name,
                            Address = "Nema adrese", // Dodajte odgovarajući opis
                            Type = PinType.Place,
                            Location = driverLocation
                        };

                        LocationMap.Pins.Add(pin);
                        Console.WriteLine($"Dodavanje pin-a za: {driver.Name}"); // Provjera dodavanja pinova
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Greška", $"Greška pri učitavanju vozača: {ex.Message}", "OK");
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
    private async Task HandleDriverSelection(Driver selectedDriver)
    {
        var destination = DestinationEntry.Text;

        if (string.IsNullOrWhiteSpace(destination))
        {
            await DisplayAlert("Greška", "Molimo unesite destinaciju prije nego što naručite vožnju.", "OK");
            return;
        }

        var destinationLocations = await Geocoding.GetLocationsAsync(destination);
        var destinationLocation = destinationLocations?.FirstOrDefault();

        if (destinationLocation == null)
        {
            await DisplayAlert("Greška", "Nismo mogli pronaći unesenu destinaciju.", "OK");
            return;
        }

        bool confirm = await DisplayAlert(
            "Potvrda",
            $"Naručiti vožnju sa vozačem {selectedDriver.Name} prema: {destination}?",
            "Da", "Ne");

        if (confirm)
        {
            try
            {
                bool success = await _driverService.BookRideAsync(
                    _userService.CurrentUser.Id,
                    selectedDriver.Id,
                    _currentLatitude,
                    _currentLongitude,
                    SourceEntry.Text ?? "Trenutna lokacija",
                    destinationLocation.Latitude,
                    destinationLocation.Longitude,
                    destination);

                if (success)
                {
                    await DisplayAlert("Uspjeh", "Vožnja je naručena. Vozač će uskoro stići.", "OK");
                    await Navigation.PushAsync(new RideTrackingPage(selectedDriver, new Location(_currentLatitude, _currentLongitude)));
                }
                else
                {
                    await DisplayAlert("Greška", "Nije moguće naručiti vožnju. Pokušajte ponovo.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Greška", $"Greška pri naručivanju: {ex.Message}", "OK");
            }
        }
    }

    private async void DriverCard_Tapped(object sender, EventArgs e)
    {
        if (sender is Frame frame && frame.BindingContext is Driver selectedDriver)
        {
            await HandleDriverSelection(selectedDriver);
        }
    }

}
