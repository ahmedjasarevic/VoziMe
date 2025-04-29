namespace VoziMe.Models;

public class Ride
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int DriverId { get; set; }
    public double SourceLatitude { get; set; }
    public double SourceLongitude { get; set; }
    public string SourceAddress { get; set; }
    public double DestinationLatitude { get; set; }
    public double DestinationLongitude { get; set; }
    public string DestinationAddress { get; set; }
    public decimal Price { get; set; }
    public RideStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public enum RideStatus
{
    Requested,
    Accepted,
    InProgress,
    Completed,
    Cancelled
}
