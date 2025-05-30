using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using VoziMe.Models;
using VoziMe.Services;
using System.Text.Json;
using System.Globalization;


namespace VoziMe.Views;

public partial class DriverTrackingPage : ContentPage
{
    private readonly Location _driverStart;
    private readonly Location _pickupLocation;
    private readonly Location _destination;
    private Polyline _routeLine;
    private string _googleApiKey = "AIzaSyCBd-dkJ39xZnNFXLUIfRpwdVkFtfURhEY";

    public DriverTrackingPage(Location driverStart, Location pickupLocation, Location destination, string customerName)
    {
        InitializeComponent();

        _driverStart = driverStart;

        _pickupLocation = pickupLocation;
        _destination = destination;
        Console.WriteLine($"Vozač: {_driverStart.Latitude}, {_driverStart.Longitude}");
        Console.WriteLine($"Korisnik: {_pickupLocation.Latitude}, {_pickupLocation.Longitude}");


        CustomerInfoLabel.Text = $"Putnik: {customerName}";
        DestinationLabel.Text = "Vozite ga do odredišta";
        EtaLabel.Text = "Ruta se učitava...";

        InitializeRoute();
       
    }

    private async void InitializeRoute()
    {
        try
        {
            DriverMap.Pins.Clear();
            DriverMap.MapElements.Clear();

            // 1. PINOVI
            DriverMap.Pins.Add(new Pin { Label = "Vozač", Location = _driverStart, Type = PinType.SavedPin });
            DriverMap.Pins.Add(new Pin { Label = "Korisnik", Location = _pickupLocation, Type = PinType.Place });
            DriverMap.Pins.Add(new Pin { Label = "Destinacija", Location = _destination, Type = PinType.Place });

            // 2. PRVA DIONICA (vozač ➝ korisnik)
            var route1 = await GetRoutePointsAsync(_driverStart, _pickupLocation);
            var polyline1 = new Polyline
            {
                StrokeColor = Colors.DarkGreen,
                StrokeWidth = 10
            };
            foreach (var point in route1)
                polyline1.Geopath.Add(point);
            DriverMap.MapElements.Add(polyline1);

            // 3. DRUGA DIONICA (korisnik ➝ destinacija)
            var route2 = await GetRoutePointsAsync(_pickupLocation, _destination);
            var polyline2 = new Polyline
            {
                StrokeColor = Colors.Red,
                StrokeWidth = 5
            };
            foreach (var point in route2)
                polyline2.Geopath.Add(point);
            DriverMap.MapElements.Add(polyline2);

            // 4. Centriraj mapu prema sredini svih tačaka
            var fullRoute = route1.Concat(route2).ToList();
            if (route1.Count == 0)
            {
                EtaLabel.Text = "Nema rute od vozača do korisnika.";
                return;
            }

            if (route2.Count == 0)
            {
                EtaLabel.Text = "Nema rute od korisnika do destinacije.";
                return;
            }

            var center = new Location(
                (fullRoute.Min(p => p.Latitude) + fullRoute.Max(p => p.Latitude)) / 2,
                (fullRoute.Min(p => p.Longitude) + fullRoute.Max(p => p.Longitude)) / 2
            );
            var radius = Location.CalculateDistance(
                new Location(fullRoute.Min(p => p.Latitude), fullRoute.Min(p => p.Longitude)),
                new Location(fullRoute.Max(p => p.Latitude), fullRoute.Max(p => p.Longitude)),
                DistanceUnits.Kilometers
            ) / 2;

            DriverMap.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromKilometers(radius + 0.5)));
            EtaLabel.Text = "Ruta spremna!";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Greška u ruti: {ex.Message}");
            EtaLabel.Text = "Greška u učitavanju rute.";
        }
    }

   

private async Task<List<Location>> GetRoutePointsAsync(Location origin, Location destination)
{
    var httpClient = new HttpClient();
    var originStr = $"{origin.Latitude.ToString(CultureInfo.InvariantCulture)},{origin.Longitude.ToString(CultureInfo.InvariantCulture)}";
    var destinationStr = $"{destination.Latitude.ToString(CultureInfo.InvariantCulture)},{destination.Longitude.ToString(CultureInfo.InvariantCulture)}";

    var url = $"https://maps.googleapis.com/maps/api/directions/json?origin={originStr}&destination={destinationStr}&mode=driving&key={_googleApiKey}";
    var response = await httpClient.GetStringAsync(url);

    Console.WriteLine("Google Directions API odgovor:");
    Console.WriteLine(response);

        var directions = JsonSerializer.Deserialize<DirectionsResponse>(response, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (directions?.Routes == null || directions.Routes.Count == 0 || directions.Routes.First().OverviewPolyline == null)
        {
            Console.WriteLine("Greška: Nepotpuni podaci iz API-ja.");
            return new List<Location>();
        }


        if (directions?.Routes?.Count > 0)
        return DecodePolyline(directions.Routes.First().OverviewPolyline.Points);

    return new List<Location>();
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
    private async void OnFinishRideClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Završi vožnju", "Jeste li sigurni da želite završiti vožnju?", "Da", "Ne");
        if (confirm)
        {
            // Pozovi neki servis da označi vožnju kao završenu, snimi podatke, itd.
            await DisplayAlert("Vožnja završena", "Vožnja je uspješno završena.", "OK");

            // Navigacija natrag ili na neku 'Ride Summary' stranicu
            await Navigation.PopToRootAsync();
        }
    }

}
