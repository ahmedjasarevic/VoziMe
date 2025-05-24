using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using VoziMe.Models;
using VoziMe.Services;
using System.Text.Json;
using Supabase;
using Supabase.Realtime;
using Supabase.Realtime.PostgresChanges;
using Microsoft.Maui.Controls;
using System.Text.Json;


using Supabase.Realtime.PostgresChanges;
using static Supabase.Realtime.PostgresChanges.PostgresChangesOptions;
using Supabase.Realtime.Exceptions;
using Supabase.Realtime.Interfaces;


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
    private Supabase.Client _supabaseClient;
    private Location _pickupLocation;
    private Driver _driver;
    private bool _rideHandled = false;
    private readonly int _userId;



  
  

    public DriverHomePage(int userId)
    {
        InitializeComponent();
        _driverService = Application.Current.Handler.MauiContext.Services.GetService<DriverService>();
        _locationService = Application.Current.Handler.MauiContext.Services.GetService<LocationService>();
        _userId = userId;
        


        // Initialize the map first
        InitializeMap();
        _ = LoadDriverDataAndAvailability();
    }

    private async Task LoadDriverDataAndAvailability()
    {
        try
        {
            _driver = await _driverService.GetDriverByUserIdAsync(_userId);

            if (_driver != null)
            {
                _driverId = _driver.Id;
                _isAvailable = _driver.IsAvailable;
                // Initialize Supabase client asynchronously
                Task.Run(async () => await InitializeSupabaseClientAsync());

            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Greska", $"Nije moguce ucitati status vozaca: {ex.Message}", "OK");

        }
        }
    

    private async Task InitializeSupabaseClientAsync()
    {
     
        try
        {
            if (_supabaseClient == null)
            {
                // Kreiraj Supabase klijent
                _supabaseClient = new Supabase.Client(
                    "https://vfqrsstbgqfwukfgslyo.supabase.co",
                    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InZmcXJzc3RiZ3Fmd3VrZmdzbHlvIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDYwMjM2NTIsImV4cCI6MjA2MTU5OTY1Mn0.JdvWKRstpHivjmG7DpkwoxvyIxON_yey7P_mfY8KVgg",
                    new Supabase.SupabaseOptions
                    {
                        AutoConnectRealtime = true
                    });

                // Inicijalizuj Supabase klijent
                await _supabaseClient.InitializeAsync();

                _supabaseClient.Realtime.AddDebugHandler((sender, message, ex) =>
                {
                    try
                    {
                        // Loguj celu poruku da vidiš šta dolazi
                        Console.WriteLine($"Realtime Debug: {message}");

                        // Pronađi poziciju prvog '{' karaktera
                        int jsonStartIndex = message.IndexOf('{');
                        if (jsonStartIndex != -1)
                        {
                            // Izdvojimo samo deo koji je validan JSON
                            string jsonMessage = message.Substring(jsonStartIndex);

                            // Pokušaj da parsiraš JSON
                            using (var jsonDoc = JsonDocument.Parse(jsonMessage))
                            {
                                var payload = jsonDoc.RootElement.GetProperty("payload");
                                var data = payload.GetProperty("data");
                                var record = data.GetProperty("record");

                                // Ekstraktuj relevantne podatke
                                var driverId = record.GetProperty("driverid").GetInt32();
                                var customerId = record.GetProperty("customerid").GetInt32();
                                var sourceLatitude = record.GetProperty("sourcelatitude").GetDouble();
                                var sourceLongitude = record.GetProperty("sourcelongitude").GetDouble();
                                var destinationLatitude = record.GetProperty("destinationlatitude").GetDouble();
                                var destinationLongitude = record.GetProperty("destinationlongitude").GetDouble();
                                var sourceAddress = record.GetProperty("sourceaddress").GetString();
                                var destinationAddress = record.GetProperty("destinationaddress").GetString();
                                _driverId = driverId;
                                _driver = new Driver();
                                _driver.Id = driverId;


                                Console.WriteLine($"Primljeni driverid: {driverId}");

                                // Ako je vožnja namijenjena trenutnom vozaču
                                if (driverId == _driverId && !_rideHandled)
                                {
                                    // Ažuriranje vožnje
                                    _pickupLocation = new Location(sourceLatitude, sourceLongitude);
                                    var destinationLocation = new Location(destinationLatitude, destinationLongitude);
                                    _rideHandled = true;


                                    // Pokretanje RideTrackingPage sa odgovarajućim podacima
                                    MainThread.BeginInvokeOnMainThread(async () =>
                                    {
                                        await DisplayAlert("Odabrani ste za vožnju", $"Nova vožnja je zakazana od {sourceAddress} do {destinationAddress}.", "OK");
                                        await Navigation.PushAsync(new DriverTrackingPage(
    new Location(_driver.Latitude, _driver.Longitude),
    _pickupLocation,
    destinationLocation,
    "Korisnik"
));
                                    });
                                }
                            }
                        }
                        else
                        {

                            // Ako ne možeš da nađeš '{', onda nije validan JSON
                            Console.WriteLine("Poruka nije validan JSON!");
                        }
                    }
                    catch (Exception parseEx)
                    {
                        Console.WriteLine($"Greška pri parsiranju JSON-a: {parseEx.Message}");
                    }
                });

                // Pretplati se na kanal
                var realtimeChannel = _supabaseClient.Realtime.Channel("realtime", "public", "rides");
                await realtimeChannel.Subscribe();
                Console.WriteLine("Uspešno pretplaćen na rides kanal!");

                Console.WriteLine("Supabase klijent inicijalizovan!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Greška pri inicijalizaciji: {ex.Message}");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDriverAvailability();

        // Only subscribe if client is initialized
        if (_supabaseClient?.Realtime != null)
        {
            await SubscribeToRides();
        }

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
            // Provjeri da li je klijent spreman
            if (_supabaseClient?.Realtime == null)
            {
                Console.WriteLine("Realtime nije dostupan!");
                return;
            }

            // Kreiraj kanal za "rides" tabelu
            _realtimeChannel = _supabaseClient.Realtime.Channel("realtime", "public", "rides");

            // Registruj Postgres Changes
            _realtimeChannel.Register(new PostgresChangesOptions("public", "rides"));

            // Dodaj handler za sve promjene
            _realtimeChannel.AddPostgresChangeHandler(ListenType.All, (sender, change) =>
            {
                try
                {
                    // Ekstraktuj JSON podatke
                    var payloadJson = JsonSerializer.Serialize(change.Payload.Data.Record);
                    var jsonDoc = JsonDocument.Parse(payloadJson);

                    // Provjeri da li postoji driverid
                    if (jsonDoc.RootElement.TryGetProperty("driverid", out var driverIdJson))
                    {
                        int driverId = driverIdJson.GetInt32();

                        Console.WriteLine($"Primljeni driverid: {driverId}");
                        Console.WriteLine($"Trenutni driverId: {_driverId}");

                        // Ako je vožnja namijenjena trenutnom vozaču
                        if (driverId == _driverId)
                        {
                            MainThread.BeginInvokeOnMainThread(async () =>
                            {
                                await DisplayAlert("Odabrani ste za vožnju", "Nova vožnja je zakazana za vas.", "OK");
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Greška pri parsiranju JSON-a: {ex.Message}");
                }
            });

            // Pretplata sa error handlingom
            await _realtimeChannel.Subscribe();
            Console.WriteLine("Uspešno pretplaćen na rides kanal!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Greška pri pretplati: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _realtimeChannel?.Unsubscribe();
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
            var driver = await _driverService.GetDriverByUserIdAsync(_userId);
         
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
                await DisplayAlert("Greška", "Nije pronađen vozač za ovog korisnika.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Greška", $"Nije moguće učitati podatke vozača: {ex.Message}", "OK");
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
