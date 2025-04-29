using Microsoft.Data.Sqlite;
using VoziMe.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace VoziMe.Services;

public class DriverService
{
    private readonly string _connectionString;

    public DriverService()
    {
        var databasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "vozi_me.db");
        _connectionString = $"Data Source={databasePath}";
    }

    public async Task<List<Driver>> GetNearbyDriversAsync(double latitude, double longitude, double radiusKm = 5.0)
    {
        var drivers = new List<Driver>();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT d.Id, d.UserId, u.Name, u.ProfileImage, d.Car, d.Latitude, d.Longitude, d.Rating, d.IsAvailable
                FROM Drivers d
                JOIN Users u ON d.UserId = u.Id
                WHERE d.IsAvailable = 1;
            ";

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var driverLatitude = reader.GetDouble(reader.GetOrdinal("Latitude"));
                var driverLongitude = reader.GetDouble(reader.GetOrdinal("Longitude"));

                var distance = CalculateDistance(latitude, longitude, driverLatitude, driverLongitude);

                if (distance <= radiusKm)
                {
                    var timeEstimate = Math.Max(2, (int)(distance * 2));

                    drivers.Add(new Driver
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        ProfileImage = reader.IsDBNull(reader.GetOrdinal("ProfileImage"))
                            ? "profile_placeholder.png"
                            : reader.GetString(reader.GetOrdinal("ProfileImage")),
                        Car = reader.GetString(reader.GetOrdinal("Car")),
                        Distance = $"{timeEstimate}min",
                        Price = CalculatePrice(distance),
                        Rating = reader.GetInt32(reader.GetOrdinal("Rating")),
                        Latitude = driverLatitude,
                        Longitude = driverLongitude,
                        IsAvailable = reader.GetInt32(reader.GetOrdinal("IsAvailable")) == 1
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetNearbyDrivers error: {ex.Message}");
        }

        return drivers;
    }

    public async Task<bool> BookRideAsync(int customerId, int driverId,
        double sourceLatitude, double sourceLongitude, string sourceAddress,
        double destLatitude, double destLongitude, string destAddress)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var distance = CalculateDistance(sourceLatitude, sourceLongitude, destLatitude, destLongitude);
            var price = decimal.Parse(CalculatePrice(distance).Replace("KM", ""));

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Rides (CustomerId, DriverId, SourceLatitude, SourceLongitude, SourceAddress,
                                   DestinationLatitude, DestinationLongitude, DestinationAddress, Price, Status)
                VALUES (@CustomerId, @DriverId, @SourceLat, @SourceLong, @SourceAddr,
                        @DestLat, @DestLong, @DestAddr, @Price, @Status);

                UPDATE Drivers SET IsAvailable = 0 WHERE Id = @DriverId;
            ";

            command.Parameters.AddWithValue("@CustomerId", customerId);
            command.Parameters.AddWithValue("@DriverId", driverId);
            command.Parameters.AddWithValue("@SourceLat", sourceLatitude);
            command.Parameters.AddWithValue("@SourceLong", sourceLongitude);
            command.Parameters.AddWithValue("@SourceAddr", sourceAddress);
            command.Parameters.AddWithValue("@DestLat", destLatitude);
            command.Parameters.AddWithValue("@DestLong", destLongitude);
            command.Parameters.AddWithValue("@DestAddr", destAddress);
            command.Parameters.AddWithValue("@Price", price);
            command.Parameters.AddWithValue("@Status", (int)RideStatus.Requested);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"BookRide error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CompleteRideAsync(int rideId, int driverId)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
            UPDATE Rides SET Status = @Status, CompletedAt = CURRENT_TIMESTAMP WHERE Id = @RideId;

            UPDATE Drivers SET IsAvailable = 1 WHERE Id = @DriverId;
        ";

            command.Parameters.AddWithValue("@Status", (int)RideStatus.Completed); // Završena vožnja
            command.Parameters.AddWithValue("@RideId", rideId);
            command.Parameters.AddWithValue("@DriverId", driverId);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CompleteRide error: {ex.Message}");
            return false;
        }
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }

    private string CalculatePrice(double distanceKm)
    {
        var basePrice = 2.0; // Base fare in KM
        var pricePerKm = 1.5; // Price per km in KM

        var totalPrice = basePrice + (distanceKm * pricePerKm);
        return $"{Math.Round(totalPrice)}KM";
    }
}
