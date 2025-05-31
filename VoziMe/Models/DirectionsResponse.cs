using System.Text.Json.Serialization;

namespace VoziMe.Models
{
    public class DirectionsResponse
    {
        [JsonPropertyName("routes")]
        public List<Route> Routes { get; set; }

        
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

    
    public class BoundingBox
    {
        [JsonPropertyName("northeast")]
        public LatLng Northeast { get; set; }  

        [JsonPropertyName("southwest")]
        public LatLng Southwest { get; set; }  

        
        public BoundingBox(double northeastLat, double northeastLng, double southwestLat, double southwestLng)
        {
            Northeast = new LatLng { Latitude = northeastLat, Longitude = northeastLng };
            Southwest = new LatLng { Latitude = southwestLat, Longitude = southwestLng };
        }
    }

    
    public class LatLng
    {
        [JsonPropertyName("lat")]
        public double Latitude { get; set; }

        [JsonPropertyName("lng")]
        public double Longitude { get; set; }
    }
}
