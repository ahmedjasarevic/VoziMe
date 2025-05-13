using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using VoziMe.Models;
using VoziMe.Services;
using System.Text.Json;
using Supabase;
using Supabase.Realtime;
using Supabase.Realtime.Models;
using Supabase.Postgrest;

using Client = Supabase.Client;
using Supabase.Realtime.PostgresChanges;


namespace VoziMe.Views;

public partial class DriverHomePage : ContentPage
{


    private readonly DriverService _driverService;
    private readonly LocationService _locationService;
    private bool _isAvailable = false;
    private int _driverId;
    private Pin _driverPin;
    private Polyline _routeLine;
    private RealtimeChannel _realtimeChannel;

    // Initialize Supabase client
    private readonly Supabase.Client _supabaseClient;

    public DriverHomePage(int driverId)
    {
        InitializeComponent();
        _driverService = Application.Current.Handler.MauiContext.Services.GetService<DriverService>();
        _locationService = Application.Current.Handler.MauiContext.Services.GetService<LocationService>();
        _driverId = driverId;

        // Initialize Supabase
        _supabaseClient = new Supabase.Client("YOUR_SUPABASE_URL", "YOUR_SUPABASE_ANON_KEY");

        InitializeMap();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDriverAvailability();

        // Subscribe to Supabase real-time notifications for new rides
        SubscribeToRides();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Unsubscribe from Supabase real-time notifications when leaving the page
        _realtimeChannel?.Unsubscribe();
    }
    private async void SubscribeToRides()
    {
        try
        {
            // Kreiraj kanal za tabelu "rides" u public šemi
            _realtimeChannel = _supabaseClient.Realtime.Channel("public:rides");

            // Dodaj listener za promene u bazi
            _realtimeChannel.AddPostgrestChangesListener("INSERT", (message) =>
            {
                var payload = message.Payload;
                Console.WriteLine("Nova vožnja je dodana: " + payload);
                NotifyDriverOnDashboard();
            });

            // Aktiviraj pretplatu na događaje
            await _realtimeChannel.Subscribe();

            Console.WriteLine("Pretplata na 'rides' je aktivna.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Greška kod pretplate na 'rides': {ex.Message}");
        }
    }


    private void HandleNewRide(object payload)
    {
        var ride = payload as Dictionary<string, object>;
        if (ride != null)
        {
            // Handle new ride here, for example, trigger notification
            NotifyDriverOnDashboard();
        }
    }

    public async Task ShowRideNotification()
    {
        await DisplayAlert("Vožnja zakazana", "Nova vožnja je zakazana za vas.", "OK");
    }

   

    private async Task LoadDriverAvailability()
    {
        try
        {
            var driver = await _driverService.GetDriverByUserIdAsync(_driverId);
            if (driver != null)
            {
                _isAvailable = driver.IsAvailable;
                AvailabilityToggleButton.Text = _isAvailable ? "Prekini potragu" : "Traži vožnju";
                AvailabilityToggleButton.BackgroundColor = _isAvailable ? Colors.Red : Colors.Green;
            }
            else
            {
                _isAvailable = false;
                AvailabilityToggleButton.Text = "Traži vožnju";
                AvailabilityToggleButton.BackgroundColor = Colors.Green;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Greška", $"Nije moguće učitati status vozača: {ex.Message}", "OK");
        }
    }

    public async Task NotifyDriverOnDashboard()
    {
        await ShowRideNotification();
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

    private async void TrackRide_Clicked(object sender, EventArgs e)
    {
        var ride = await _driverService.GetActiveRideAsync(_driverId);
        if (ride != null)
        {
            var pickupLocation = new Location(ride.SourceLatitude, ride.SourceLongitude);
            var destinationLocation = new Location(ride.DestinationLatitude, ride.DestinationLongitude);
            await DrawRouteAsync(pickupLocation, destinationLocation);
        }
        else
        {
            await DisplayAlert("Greška", "Nema aktivne vožnje.", "OK");
        }
    }

    private async Task DrawRouteAsync(Location origin, Location destination)
    {
        try
        {
            var httpClient = new HttpClient();
            var url = $"https://maps.googleapis.com/maps/api/directions/json?origin={origin.Latitude},{origin.Longitude}&destination={destination.Latitude},{destination.Longitude}&mode=driving&key=YOUR_GOOGLE_API_KEY";
            var response = await httpClient.GetStringAsync(url);
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

                LocationMap.MapElements.Clear();
                LocationMap.MapElements.Add(_routeLine);

                var minLat = points.Min(p => p.Latitude);
                var maxLat = points.Max(p => p.Latitude);
                var minLon = points.Min(p => p.Longitude);
                var maxLon = points.Max(p => p.Longitude);

                var center = new Location((minLat + maxLat) / 2, (minLon + maxLon) / 2);
                var radius = Location.CalculateDistance(new Location(minLat, minLon), new Location(maxLat, maxLon), DistanceUnits.Kilometers) / 2;

                LocationMap.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromKilometers(radius)));
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
            do
            {
                b = encodedPoints[index++] - 63;
                result |= (b & 0x1f) << shift;
                shift += 5;
            } while (b >= 0x20);
            lat += (result & 1) != 0 ? ~(result >> 1) : (result >> 1);

            shift = 0;
            result = 0;
            do
            {
                b = encodedPoints[index++] - 63;
                result |= (b & 0x1f) << shift;
                shift += 5;
            } while (b >= 0x20);
            lng += (result & 1) != 0 ? ~(result >> 1) : (result >> 1);

            poly.Add(new Location(lat / 1E5, lng / 1E5));
        }

        return poly;
    }
}
