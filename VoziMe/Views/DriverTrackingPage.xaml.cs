using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using VoziMe.Models;
using VoziMe.Services;
using System.Text.Json;

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

        CustomerInfoLabel.Text = $"Putnik: {customerName}";
        DestinationLabel.Text = "Vozite ga do odredišta";
        EtaLabel.Text = "Ruta se učitava...";

        InitializeRoute();
        try
        {
            var fullRoutePoints = new List<Location>();


            // Dodaj Pins za korisnika i destinaciju
            var userPin = new Pin
            {
                Label = "Korisnik",
                Location = _pickupLocation,
                Type = PinType.Place
            };
            DriverMap.Pins.Add(userPin);

            var destinationPin = new Pin
            {
                Label = "Destinacija",
                Location = _destination,
                Type = PinType.Place
            };
            DriverMap.Pins.Add(destinationPin);

            // Prikazivanje rute
            _routeLine = new Polyline
            {
                StrokeColor = Colors.Green,
                StrokeWidth = 10
            };

            foreach (var point in fullRoutePoints)
                _routeLine.Geopath.Add(point);

            DriverMap.MapElements.Clear();
            DriverMap.MapElements.Add(_routeLine);

            // Centriraj mapu prema ruti
            var center = new Location(
                (fullRoutePoints.Min(p => p.Latitude) + fullRoutePoints.Max(p => p.Latitude)) / 2,
                (fullRoutePoints.Min(p => p.Longitude) + fullRoutePoints.Max(p => p.Longitude)) / 2
            );
            var radius = Location.CalculateDistance(
                new Location(fullRoutePoints.Min(p => p.Latitude), fullRoutePoints.Min(p => p.Longitude)),
                new Location(fullRoutePoints.Max(p => p.Latitude), fullRoutePoints.Max(p => p.Longitude)),
                DistanceUnits.Kilometers) / 2;

            DriverMap.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromKilometers(radius)));
            EtaLabel.Text = "Ruta spremna!";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Greška u ruti: {ex.Message}");
            EtaLabel.Text = "Greška u učitavanju rute.";
        }
    }

    private async void InitializeRoute()
    {
        try
        {
            var fullRoutePoints = new List<Location>();

            // 1. Ruta: Vozač → Korisnik
            var driverToPickup = await GetRoutePointsAsync(_driverStart, _pickupLocation);
            fullRoutePoints.AddRange(driverToPickup);

            // 2. Ruta: Korisnik → Destinacija
            var pickupToDest = await GetRoutePointsAsync(_pickupLocation, _destination);
            fullRoutePoints.AddRange(pickupToDest);

            _routeLine = new Polyline
            {
                StrokeColor = Colors.Green,
                StrokeWidth = 10
            };

            foreach (var point in fullRoutePoints)
                _routeLine.Geopath.Add(point);

            DriverMap.MapElements.Clear();
            DriverMap.MapElements.Add(_routeLine);

            var center = new Location(
                (fullRoutePoints.Min(p => p.Latitude) + fullRoutePoints.Max(p => p.Latitude)) / 2,
                (fullRoutePoints.Min(p => p.Longitude) + fullRoutePoints.Max(p => p.Longitude)) / 2
            );
            var radius = Location.CalculateDistance(
                new Location(fullRoutePoints.Min(p => p.Latitude), fullRoutePoints.Min(p => p.Longitude)),
                new Location(fullRoutePoints.Max(p => p.Latitude), fullRoutePoints.Max(p => p.Longitude)),
                DistanceUnits.Kilometers) / 2;

            DriverMap.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromKilometers(radius)));
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
        var url = $"https://maps.googleapis.com/maps/api/directions/json?origin={origin.Latitude},{origin.Longitude}&destination={destination.Latitude},{destination.Longitude}&mode=driving&key={_googleApiKey}";
        var response = await httpClient.GetStringAsync(url);
        var directions = JsonSerializer.Deserialize<DirectionsResponse>(response);

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
}
