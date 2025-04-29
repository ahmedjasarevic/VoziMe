namespace VoziMe.Models;

public class Driver
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; }
    public string ProfileImage { get; set; }
    public string Car { get; set; }
    public string Distance { get; set; }
    public string Price { get; set; }
    public int Rating { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsAvailable { get; set; }
}