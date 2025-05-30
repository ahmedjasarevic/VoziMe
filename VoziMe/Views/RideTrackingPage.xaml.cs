using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using VoziMe.Models;
using VoziMe.Services;
using System.Text.Json;
using System.Globalization;



namespace VoziMe.Views;

public partial class RideTrackingPage : ContentPage
{
    private readonly Driver _driver;
    private readonly Location _pickupLocation;
    private readonly Location _destinationLocation;  // Dodana destinacija
    private readonly LocationService _locationService;
    private readonly DriverService _driverService;

    private Pin _driverPin;
    private bool _isDriverArrived;
    private int _selectedRating = 0;
    private int _currentUserId; // Dodano polje za korisnički ID
    private Polyline _routeLine; // Dodano za prikazivanje rute

    private string _googleApiKey = "AIzaSyCBd-dkJ39xZnNFXLUIfRpwdVkFtfURhEY"; // Unesi tvoj API ključ ovde

    public RideTrackingPage(Driver driver, Location pickupLocation, Location destinationLocation, int currentUserId)
    {
        InitializeComponent();

        _driver = driver;
        _pickupLocation = pickupLocation;
        _destinationLocation = destinationLocation;
        _currentUserId = currentUserId;

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

        TrackingMap.Pins.Clear();
        TrackingMap.MapElements.Clear();

        var userPin = new Pin
        {
            Label = "Vaša lokacija",
            Location = _pickupLocation,
            Type = PinType.Place
        };
        TrackingMap.Pins.Add(userPin);

        var destPin = new Pin
        {
            Label = "Odredište",
            Location = _destinationLocation,
            Type = PinType.Place
        };
        TrackingMap.Pins.Add(destPin);

        var driverLoc = new Location(_driver.Latitude, _driver.Longitude);
        _driverPin = new Pin
        {
            Label = string.IsNullOrEmpty(_driver.Name) ? "Vozač" : _driver.Name,
            Location = driverLoc,
            Type = PinType.SavedPin
        };
        TrackingMap.Pins.Add(_driverPin);

        var route1 = await GetRoutePointsAsync(driverLoc, _pickupLocation);
        var polyline1 = new Polyline
        {
            StrokeColor = Colors.DarkGreen,
            StrokeWidth = 10
        };
        foreach (var point in route1)
            polyline1.Geopath.Add(point);
        TrackingMap.MapElements.Add(polyline1);

        var route2 = await GetRoutePointsAsync(_pickupLocation, _destinationLocation);
        var polyline2 = new Polyline
        {
            StrokeColor = Colors.Red,
            StrokeWidth = 5
        };
        foreach (var point in route2)
            polyline2.Geopath.Add(point);
        TrackingMap.MapElements.Add(polyline2);

        var fullRoute = route1.Concat(route2).ToList();
        var centerAll = new Location(
            (fullRoute.Min(p => p.Latitude) + fullRoute.Max(p => p.Latitude)) / 2,
            (fullRoute.Min(p => p.Longitude) + fullRoute.Max(p => p.Longitude)) / 2
        );
        var radius = Location.CalculateDistance(
            new Location(fullRoute.Min(p => p.Latitude), fullRoute.Min(p => p.Longitude)),
            new Location(fullRoute.Max(p => p.Latitude), fullRoute.Max(p => p.Longitude)),
            DistanceUnits.Kilometers
        ) / 2;

        TrackingMap.MoveToRegion(MapSpan.FromCenterAndRadius(centerAll, Distance.FromKilometers(radius + 0.5)));

        EtaLabel.Text = "Ruta spremna!";

        StartDriverLocationUpdates();
    }

    private async Task<List<Location>> GetRoutePointsAsync(Location origin, Location destination)
    {
        var httpClient = new HttpClient();
        var originStr = $"{origin.Latitude.ToString(CultureInfo.InvariantCulture)},{origin.Longitude.ToString(CultureInfo.InvariantCulture)}";
        var destinationStr = $"{destination.Latitude.ToString(CultureInfo.InvariantCulture)},{destination.Longitude.ToString(CultureInfo.InvariantCulture)}";

        var url = $"https://maps.googleapis.com/maps/api/directions/json?origin={originStr}&destination={destinationStr}&mode=driving&key={_googleApiKey}";
        var response = await httpClient.GetStringAsync(url);

        var directions = JsonSerializer.Deserialize<DirectionsResponse>(response);

        if (directions?.Routes?.Count > 0)
            return DecodePolyline(directions.Routes.First().OverviewPolyline.Points);

        return new List<Location>();
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
            await DisplayAlert("Vožnja završena", "Vozač je sada ponovo dostupan. Ocijenite vožnju!", "OK");

            ShowRatingStars(); // Prikaz zvjezdica
        }
        else
        {
            await DisplayAlert("Greška", "Došlo je do greške prilikom završavanja vožnje.", "OK");
        }
    }

    private void ShowRatingStars()
    {
        RatingView.IsVisible = true;
        StarContainer.Children.Clear();

        for (int i = 1; i <= 5; i++)
        {
            var star = new Image
            {
                Source = "star_empty1.png",
                HeightRequest = 30,
                WidthRequest = 30,
                Margin = new Thickness(5)
            };

            int rating = i;
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (s, e) =>
            {
                _selectedRating = rating;
                UpdateStarImages(rating);
                await SubmitRatingAsync();
            };

            star.GestureRecognizers.Add(tapGesture);
            StarContainer.Children.Add(star);
        }
    }

    private void UpdateStarImages(int rating)
    {
        for (int i = 0; i < StarContainer.Children.Count; i++)
        {
            if (StarContainer.Children[i] is Image starImage)
            {
                starImage.Source = i < rating ? "star_filled.png" : "star_empty1.png";
            }
        }
    }

    private async Task SubmitRatingAsync()
    {
        int customerId = _currentUserId;

        bool result = await _driverService.RateDriverAsync(_driver.Id, customerId, _selectedRating);
        if (result)
        {
            await DisplayAlert("Hvala!", "Vaša ocjena je zabilježena.", "OK");
            await Navigation.PushAsync(new DriverSelectionPage());
        }
        else
        {
            await DisplayAlert("Greška", "Nije moguće snimiti ocjenu.", "OK");
        }
    }

    private async Task DrawRouteAsync(Location origin, Location destination)
    {
        try
        {
            Console.WriteLine($"Origin: {origin.Latitude},{origin.Longitude}");
            Console.WriteLine($"Destination: {destination.Latitude},{destination.Longitude}");

            var httpClient = new HttpClient();
            var url = $"https://maps.googleapis.com/maps/api/directions/json?origin={origin.Latitude},{origin.Longitude}&destination={destination.Latitude},{destination.Longitude}&mode=driving&key={_googleApiKey}";
            var response = await httpClient.GetStringAsync(url);
            Console.WriteLine($"API Response: {response}");

            var directions = JsonSerializer.Deserialize<DirectionsResponse>(response);

            if (directions?.Routes?.Count > 0)
            {
                var points = DecodePolyline(directions.Routes.First().OverviewPolyline.Points);

                _routeLine = new Polyline
                {
                    StrokeColor = Colors.Green,
                    StrokeWidth = 10
                };

                foreach (var point in points)
                {
                    _routeLine.Geopath.Add(point);
                }

                // Očisti prethodne elemente pre dodavanja nove rute
                TrackingMap.MapElements.Clear();
                TrackingMap.MapElements.Add(_routeLine);

                // Proračunavanje centra i radijusa
                var minLat = points.Min(p => p.Latitude);
                var maxLat = points.Max(p => p.Latitude);
                var minLon = points.Min(p => p.Longitude);
                var maxLon = points.Max(p => p.Longitude);

                var center = new Location((minLat + maxLat) / 2, (minLon + maxLon) / 2);
                var radius = Location.CalculateDistance(
                    new Location(minLat, minLon),
                    new Location(maxLat, maxLon),
                    DistanceUnits.Kilometers
                ) / 2;

                // Postavljanje mape da prati rutu sa centrom i radijusom
                TrackingMap.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromKilometers(radius)));
            }
            else
            {
                Console.WriteLine("No routes found in the response.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Greška kod prikaza rute: {ex.Message}");
        }
    }

    private List<Location> DecodePolyline(string encodedPoints)
    {
        var poly = new List<Location>();
        int index = 0, lat = 0, lng = 0;

        while (index < encodedPoints.Length)
        {
            int b, shift = 0, result = 0;
            do { b = encodedPoints[index++] - 63; result |= (b & 0x1f) << shift; shift += 5; } while (b >= 0x20);
            lat += (result & 1) != 0 ? ~(result >> 1) : (result >> 1);

            shift = 0; result = 0;
            do { b = encodedPoints[index++] - 63; result |= (b & 0x1f) << shift; shift += 5; } while (b >= 0x20);
            lng += (result & 1) != 0 ? ~(result >> 1) : (result >> 1);

            poly.Add(new Location(lat / 1E5, lng / 1E5));
        }

        return poly;
    }
}