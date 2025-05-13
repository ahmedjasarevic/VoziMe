using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using VoziMe.Models;
using VoziMe.Services;
using System.Text.Json;
using Supabase;
using Supabase.Realtime;
using Supabase.Realtime.Models;
using Supabase.Realtime.Socket;
using Supabase.Postgrest;
using Microsoft.Maui.Controls;


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

        // Inicijalizacija Supabase
        _supabaseClient = new Supabase.Client(
            "https://vfqrsstbgqfwukfgslyo.supabase.co",
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InZmcXJzc3RiZ3Fmd3VrZmdzbHlvIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc0NjAyMzY1MiwiZXhwIjoyMDYxNTk5NjUyfQ.DqGBnnje3__AhgluVi3MBwbQsTHztC0Ele2d4wLo66Y"
        );

        // Priključi se na real-time server
        _supabaseClient.Realtime.Connect();

        InitializeMap();
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDriverAvailability();

        // Subscribe to Supabase real-time notifications for new rides
        await SubscribeToRides();

        // Subscribe to MessagingCenter for local messages
        MessagingCenter.Subscribe<DriverService, int>(this, "DriverSelected", async (sender, driverId) =>
        {
            if (driverId == _driverId)
            {
                await ShowRideNotification();
            }
        });
    }
    private async Task SubscribeToRides()
    {
        try
        {
            Console.WriteLine("Pokušavam se pretplatiti na 'rides' kanal...");

            if (_supabaseClient.Realtime.Socket?.IsConnected != true)
            {
                Console.WriteLine("Socket nije povezan. Pokušavam se povezati...");
                await _supabaseClient.Realtime.ConnectAsync();
                await Task.Delay(2000); // Čekaj 2 sekunde da se socket poveže
            }

            if (_supabaseClient.Realtime.Socket?.IsConnected != true)
            {
                Console.WriteLine("Socket i dalje nije povezan. Provjeri svoje Supabase URL i API ključ.");
                return;
            }

            _realtimeChannel = _supabaseClient.Realtime.Channel("public:rides");

            // Dodaj handler za promjene
            _realtimeChannel.AddPostgresChangeHandler(PostgresChangesOptions.ListenType.All, (sender, change) =>
            {
                Console.WriteLine("Primljena nova promjena na kanalu 'rides'.");
                if (change.Payload?.Data?.Record != null)
                {
                    Console.WriteLine($"Nova vožnja: {JsonSerializer.Serialize(change.Payload.Data.Record)}");
                    MessagingCenter.Send(this, "NewRideNotification", "Nova vožnja je zakazana!");
                }
                else
                {
                    Console.WriteLine("Nema podataka u promjeni.");
                }
            });

            // Pokušaj pretplate
            var result = await _realtimeChannel.Subscribe();
            Console.WriteLine($"Pretplata na kanal 'rides' je aktivna: {result.State}");

            // Provjeri ponovo status socket-a
            if (_supabaseClient.Realtime.Socket?.IsConnected == true)
            {
                Console.WriteLine("Socket je sada povezan i pretplata je aktivna.");
            }
            else
            {
                Console.WriteLine("Socket nije povezan ni nakon pretplate.");
            }
            Console.WriteLine($"Kanal: {_realtimeChannel}");




        }
        catch (Exception ex)
        {
            Console.WriteLine($"Greška kod pretplate na 'rides': {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _realtimeChannel?.Unsubscribe();

        // Unsubscribe from MessagingCenter
        MessagingCenter.Unsubscribe<DriverService, int>(this, "DriverSelected");
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
        // Ovdje možeš dodati logiku za lokalno slanje poruke
        MessagingCenter.Send(this, "NewRideNotification", "Nova vožnja je zakazana!");
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
