using System.Text.Json.Serialization;

namespace VoziMe.Models
{
    public class DirectionsResponse
    {
        [JsonPropertyName("routes")]
        public List<Route> Routes { get; set; }

        // Dodajemo BoundingBox ako je prisutan u odgovoru
        [JsonPropertyName("bounding_box")]
        public BoundingBox BoundingBox { get; set; }
    }

    public class Route
    {
        [JsonPropertyName("overview_polyline")]
        public OverviewPolyline OverviewPolyline { get; set; }
    }

    public class OverviewPolyline
    {
        [JsonPropertyName("points")]
        public string Points { get; set; }
    }

    // Klasa za BoundingBox koji sadrži informacije o koordinatama
    public class BoundingBox
    {
        [JsonPropertyName("northeast")]
        public LatLng Northeast { get; set; }  // Gornji desni kut

        [JsonPropertyName("southwest")]
        public LatLng Southwest { get; set; }  // Donji lijevi kut

        // Konstruktor za BoundingBox sa četiri argumenta
        public BoundingBox(double northeastLat, double northeastLng, double southwestLat, double southwestLng)
        {
            Northeast = new LatLng { Latitude = northeastLat, Longitude = northeastLng };
            Southwest = new LatLng { Latitude = southwestLat, Longitude = southwestLng };
        }
    }

    // Klasa za LatLng (Latitude & Longitude)
    public class LatLng
    {
        [JsonPropertyName("lat")]
        public double Latitude { get; set; }

        [JsonPropertyName("lng")]
        public double Longitude { get; set; }
    }
}