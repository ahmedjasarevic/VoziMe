using Microsoft.Maui.Devices.Sensors;

namespace VoziMe.Services;

public class LocationService
{
    public async Task<(double Latitude, double Longitude)> GetCurrentLocationAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    Console.WriteLine("Dozvola za lokaciju nije odobrena.");
                    return (44.2037, 17.9071); // default fallback
                }
            }

            var location = await Geolocation.GetLocationAsync(new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.Medium,
                Timeout = TimeSpan.FromSeconds(30)
            });

            if (location != null)
            {
                return (location.Latitude, location.Longitude);
            }

            // fallback
            return (44.2037, 17.9071);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Greška kod dohvaćanja lokacije: {ex.Message}");
            return (44.2037, 17.9071);
        }
    }

    public async Task<Location> GetDriverLocationAsync(string driverId)
    {
        // TODO: Real API poziv
        await Task.Delay(500); // simulacija

        // For now, return slightly moved position for simulation
        return new Location(
            Random.Shared.NextDouble() * 0.01 + 44.2, // simulacija
            Random.Shared.NextDouble() * 0.01 + 17.9);
    }

    public async Task<string> GetAddressFromCoordinatesAsync(double latitude, double longitude)
    {
        try
        {
            var placemarks = await Geocoding.GetPlacemarksAsync(latitude, longitude);
            var placemark = placemarks?.FirstOrDefault();

            if (placemark != null)
            {
                return $"{placemark.Thoroughfare} {placemark.SubThoroughfare}, {placemark.Locality}";
            }

            return "Nepoznata lokacija";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Geocoding error: {ex.Message}");
            return "Nepoznata lokacija";
        }
    }
}