
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using VoziMe.Models;
using VoziMe.Services;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VoziMe.Views;

public partial class DriverSelectionPage : ContentPage
{
    private readonly DriverService _driverService;
    private readonly UserService _userService;
    private readonly LocationService _locationService;
    private double _currentLatitude;
    private double _currentLongitude;

    private readonly string _googleApiKey = "AIzaSyCBd-dkJ39xZnNFXLUIfRpwdVkFtfURhEY"; // <-- OVDE STAVI SVOJ KEY

    public DriverSelectionPage()
    {
        InitializeComponent();

        _driverService = Application.Current.Handler.MauiContext.Services.GetService<DriverService>();
        _userService = Application.Current.Handler.MauiContext.Services.GetService<UserService>();
        _locationService = Application.Current.Handler.MauiContext.Services.GetService<LocationService>();

        InitializeMapAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var location = await Geolocation.GetLastKnownLocationAsync();

        if (location != null)
        {
            var placemarks = await Geocoding.GetPlacemarksAsync(location);
            var placemark = placemarks?.FirstOrDefault();

            if (placemark != null)
            {
                string address = $"{placemark.Thoroughfare} {placemark.SubThoroughfare}, { placemark.Locality} ";
                SourceEntry.Text = address;
            }
            else
            {
                SourceEntry.Text = "Trenutna lokacija";
            }
        }
        else
        {
            SourceEntry.Text = "Trenutna lokacija";
        }
    }

    private async void InitializeMapAsync()
    {
        try
        {
            var location = await

_locationService.GetCurrentLocationAsync();
            _currentLatitude = location.Latitude;
            _currentLongitude = location.Longitude;

            LocationMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                new Location(_currentLatitude, _currentLongitude),
                Distance.FromKilometers(1)));

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
            LocationMap.Pins.Clear();

            var drivers = await _driverService.GetAllAvailableDriversAsync(); // koristi sve dostupne

            foreach (var driver in drivers)
            {
                if (driver.Latitude == 0 || driver.Longitude == 0)
                    continue;

                var pin = new Pin
                {
                    Label = driver.Name,
                    Address = await GetAddressFromCoordinatesAsync(driver.Latitude, driver.Longitude),
                    Location = new Location(driver.Latitude, driver.Longitude),
                    Type = PinType.Place
                };

                LocationMap.Pins.Add(pin);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Greška", $"Neuspješno učitavanje vozača: {ex.Message}", "OK");
        }
    }
    private async Task<string> GetAddressFromCoordinatesAsync(double latitude, double longitude)
    {
        try
        {
            var placemarks = await Geocoding.GetPlacemarksAsync(latitude, longitude);
            var placemark = placemarks?.FirstOrDefault();

            if (placemark != null)
            {
                return $"{placemark.Thoroughfare ?? ""} {placemark.SubThoroughfare ?? ""}, {placemark.Locality ?? placemark.CountryName}";
            }

            return "Nepoznata lokacija";
        }
        catch
        {
            return "Adresa nije dostupna";
        }
    }

    private async void SourceEntry_Completed(object sender, EventArgs e)

    {
        await GeocodeAndCenter(SourceEntry.Text);
    }

    private async Task GeocodeAndCenter(string address)
    {
        if (!string.IsNullOrWhiteSpace(address))
        {
            var locations = await Geocoding.GetLocationsAsync(address);
            var location = locations?.FirstOrDefault();
            if (location != null)
            {
                _currentLatitude = location.Latitude;
                _currentLongitude = location.Longitude;


                LocationMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                    new Location(_currentLatitude, _currentLongitude),
                    Distance.FromKilometers(1)));

                await LoadDriversAsync();
            }
            else
            {
                await DisplayAlert("Lokacija", "Lokacija nije pronađena.", "OK");
            }
        }
    }

    private async Task HandleDriverSelection(Driver selectedDriver)

    {
        var destination = DestinationEntry.Text;
        if (string.IsNullOrWhiteSpace(destination))
        {
            await DisplayAlert("Greška", "Unesite destinaciju.", "OK");
            return;
        }

        var locations = await Geocoding.GetLocationsAsync(destination);
        var loc = locations?.FirstOrDefault();
        if (loc == null)
        {
            await DisplayAlert("Greška", "Destinacija nije validna.", "OK");
            return;
        }

        bool confirm = await DisplayAlert("Potvrda", $"Naručiti vožnju sa vozačem {selectedDriver.Name} prema: {destination}?", "Da", "Ne");
        if (!confirm) return;

        var success = await _driverService.BookRideAsync(
            _userService.CurrentUser.Id,
            selectedDriver.Id,
            _currentLatitude,
            _currentLongitude,
            SourceEntry.Text ?? "Trenutna lokacija",
            loc.Latitude,
            loc.Longitude,
            destination);

        if (success)
        {
            await DisplayAlert("Uspjeh", "Vožnja naručena.", "OK");
            await Navigation.PushAsync(new RideTrackingPage(selectedDriver, new Location(_currentLatitude, _currentLongitude), _userService.CurrentUser.Id));
        }
        else
        {
            await DisplayAlert("Greška", "Naručivanje nije uspjelo.", "OK");
        }
    }

    private async void DriverCard_Tapped(object sender, EventArgs e)
    {
        if (sender is Frame frame && frame.BindingContext is Driver driver)
            await HandleDriverSelection(driver);

    }

    private async void RefreshDrivers_Clicked(object sender, EventArgs e)
    {
        await LoadDriversAsync();
    }

    private void ShowDriversButton_Clicked(object sender, EventArgs e)
    {
        BottomSheet.IsVisible = true;
        MapArea.HeightRequest = 450;
        MapArea.VerticalOptions = LayoutOptions.Start;
        ShowDriversButton.IsVisible = false;
    }

    // --------------------------- AUTOCOMPLETE 


    private async void SourceEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        await FetchPredictionsAsync(e.NewTextValue, isSource: true);
    }

    private async void DestinationEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        await FetchPredictionsAsync(e.NewTextValue, isSource: false);
    }

    private async Task

FetchPredictionsAsync(string input, bool isSource)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            if (isSource) SourceSuggestions.IsVisible = false;
            else DestinationSuggestions.IsVisible = false;
            return;
        }

        try
        {
            var url = $"https://maps.googleapis.com/maps/api/place/autocomplete/json?input={Uri.EscapeDataString(input)}&types=geocode&key={_googleApiKey}";
            var client = new HttpClient();
            var response = await

client.GetStringAsync(url);

            var result = JsonSerializer.Deserialize<GooglePlacesResponse>(response);
            if (isSource)
            {
                SourceSuggestions.ItemsSource = result?.Predictions;
                SourceSuggestions.IsVisible = result?.Predictions?.Any() == true;
            }
            else
            {
                DestinationSuggestions.ItemsSource = result?.Predictions;
                DestinationSuggestions.IsVisible = result?.Predictions?.Any() == true;
            }
        }

        catch (Exception ex)
        {
            Console.WriteLine($"Autocomplete error: {ex.Message}");
        }
    }

    private void SourceSuggestions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Prediction selected)
        {
            SourceEntry.Text = selected.Description;
            SourceSuggestions.IsVisible = false;
        }
    }

    private void

DestinationSuggestions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Prediction selected)
        {
            DestinationEntry.Text = selected.Description;
            DestinationSuggestions.IsVisible = false;
        }
    }

    // -------------------------- JSON MODEL -------------------------------
    public class GooglePlacesResponse
    {
        [JsonPropertyName("predictions")]
        public List<Prediction> Predictions { get; set; }

    }

    public class Prediction
    {
        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}
