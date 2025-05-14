using Npgsql;
using VoziMe.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoziMe.Views;


namespace VoziMe.Services
{
    public class DriverService
    {


        private readonly string _connectionString;

        public DriverService(NpgsqlConnection connection)  // Pass it in the constructor
        {
            _connectionString = connection.ConnectionString;
        }
        public async Task<List<Ride>> GetAvailableRidesAsync(int driverId)
        {
            var rides = new List<Ride>();

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
            SELECT r.Id, r.CustomerId, r.SourceLatitude, r.SourceLongitude, r.SourceAddress,
                   r.DestinationLatitude, r.DestinationLongitude, r.DestinationAddress, r.Price, r.Status
            FROM Rides r
            WHERE r.Status = 0 AND r.DriverId IS NULL;
        ";

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    rides.Add(new Ride
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
                        SourceLatitude = reader.GetDouble(reader.GetOrdinal("SourceLatitude")),
                        SourceLongitude = reader.GetDouble(reader.GetOrdinal("SourceLongitude")),
                        SourceAddress = reader.GetString(reader.GetOrdinal("SourceAddress")),
                        DestinationLatitude = reader.GetDouble(reader.GetOrdinal("DestinationLatitude")),
                        DestinationLongitude = reader.GetDouble(reader.GetOrdinal("DestinationLongitude")),
                        DestinationAddress = reader.GetString(reader.GetOrdinal("DestinationAddress")),
                        Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                        Status = (RideStatus)reader.GetInt32(reader.GetOrdinal("Status"))
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAvailableRidesAsync error: {ex.Message}");
            }

            return rides;
        }


        public async Task<List<Driver>> GetNearbyDriversAsync(double latitude, double longitude, double radiusKm = 5.0)
        {
            var drivers = new List<Driver>();

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT d.Id, d.UserId, u.Name, u.ProfileImage, d.Car, d.Latitude, d.Longitude, d.Rating, d.isavailable
                    FROM Drivers d
                    JOIN Users u ON d.UserId = u.Id
                    WHERE d.isavailable = true;
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
                            IsAvailable = reader.GetBoolean(reader.GetOrdinal("isavailable"))
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
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = await connection.BeginTransactionAsync();

                // Provjeri da li vozač postoji i da li je dostupan
                var checkDriverCommand = connection.CreateCommand();
                checkDriverCommand.CommandText = @"
            SELECT isavailable FROM Drivers WHERE Id = @DriverId;
        ";
                checkDriverCommand.Parameters.AddWithValue("@DriverId", NpgsqlTypes.NpgsqlDbType.Integer, driverId);

                var isAvailable = (bool?)await checkDriverCommand.ExecuteScalarAsync();

                if (isAvailable == null || isAvailable == false)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                // Izračunaj distancu i cijenu
                var distance = CalculateDistance(sourceLatitude, sourceLongitude, destLatitude, destLongitude);
                var price = decimal.Parse(CalculatePrice(distance).Replace("KM", "").Trim());

                // Kreiraj vožnju
                var command = connection.CreateCommand();
                command.CommandText = @"
            INSERT INTO Rides (CustomerId, DriverId, SourceLatitude, SourceLongitude, SourceAddress,
                               DestinationLatitude, DestinationLongitude, DestinationAddress, Price, Status)
            VALUES (@CustomerId, @DriverId, @SourceLat, @SourceLong, @SourceAddr,
                    @DestLat, @DestLong, @DestAddr, @Price, @Status);

            UPDATE Drivers SET isavailable = false WHERE Id = @DriverId;
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

                // **POŠALJI OBAVIJEST VOZAČU**
                MessagingCenter.Send(this, "DriverSelected", driverId);

                await transaction.CommitAsync();
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
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
                UPDATE Rides SET Status = @Status, CompletedAt = CURRENT_TIMESTAMP WHERE Id = @RideId;

                UPDATE Drivers SET isavailable = true WHERE Id = @DriverId;
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

        public async Task<bool> FinishRideAsync(int driverId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 1. Osvježi drivera kao dostupnog
                var command = connection.CreateCommand();
                command.CommandText = @"
                UPDATE Drivers
                SET isavailable = true
                WHERE Id = @DriverId;";
                command.Parameters.AddWithValue("@DriverId", driverId);

                await command.ExecuteNonQueryAsync();

                // 2. (Opcionalno) Osvježi zadnju vožnju kao završenu
                var updateRideCommand = connection.CreateCommand();
                updateRideCommand.CommandText = @"
                UPDATE Rides
                SET Status = 1,
                    CompletedAt = CURRENT_TIMESTAMP
                WHERE DriverId = @DriverId AND Status = 0;";
                updateRideCommand.Parameters.AddWithValue("@DriverId", driverId);

                await updateRideCommand.ExecuteNonQueryAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška prilikom završavanja vožnje: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> RateDriverAsync(int driverId, int customerId, int rating)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 1. Ubaci novu ocjenu
                var insertCommand = connection.CreateCommand();
                insertCommand.CommandText = @"
            INSERT INTO driverratings (driverid, customerid, rating, ratedat)
            VALUES (@DriverId, @CustomerId, @Rating, CURRENT_TIMESTAMP);
        ";
                insertCommand.Parameters.AddWithValue("@DriverId", driverId);
                insertCommand.Parameters.AddWithValue("@CustomerId", customerId);
                insertCommand.Parameters.AddWithValue("@Rating", rating);
                await insertCommand.ExecuteNonQueryAsync();

                // 2. Izračunaj prosjek ocjena
                var avgCommand = connection.CreateCommand();
                avgCommand.CommandText = @"
            SELECT AVG(rating) FROM driverratings WHERE driverid = @DriverId;
        ";
                avgCommand.Parameters.AddWithValue("@DriverId", driverId);
                var avgResult = await avgCommand.ExecuteScalarAsync();

                // 3. Zaokruži prosjek i ažuriraj glavnu tabelu Drivers
                var averageRating = Convert.ToInt32(Math.Round(Convert.ToDouble(avgResult)));

                var updateCommand = connection.CreateCommand();
                updateCommand.CommandText = @"
            UPDATE Drivers SET Rating = @AvgRating WHERE Id = @DriverId;
        ";
                updateCommand.Parameters.AddWithValue("@AvgRating", averageRating);
                updateCommand.Parameters.AddWithValue("@DriverId", driverId);
                await updateCommand.ExecuteNonQueryAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri ocjenjivanju vozača: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> UpdateDriverAvailabilityAsync(int driverId, double latitude, double longitude, bool isAvailable)
        {
            if (latitude == 0 || longitude == 0)
            {
                return false;
            }
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
            UPDATE Drivers
            SET Latitude = @Latitude,
                Longitude = @Longitude,
                isavailable = @isavailable
            WHERE Id = @DriverId;
        ";

                command.Parameters.AddWithValue("@Latitude", latitude);
                command.Parameters.AddWithValue("@Longitude", longitude);
                command.Parameters.AddWithValue("@isavailable", isAvailable);  // Ovde šaljemo bool
                command.Parameters.AddWithValue("@DriverId", driverId);

                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateDriverAvailability error: {ex.Message}");
                return false;
            }
        }
        public async Task<Driver> GetDriverByUserIdAsync(int userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Drivers WHERE UserId = @UserId";
            command.Parameters.AddWithValue("@UserId", userId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Driver
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    Car = reader.GetString(reader.GetOrdinal("Car")),
                    Latitude = reader.GetDouble(reader.GetOrdinal("Latitude")),
                    Longitude = reader.GetDouble(reader.GetOrdinal("Longitude")),
                    Rating = reader.GetInt32(reader.GetOrdinal("Rating")),
                    IsAvailable = reader.GetBoolean(reader.GetOrdinal("isavailable"))
                };
            }

            return null;
        }

       

public async Task<bool> NotifyDriverWhenSelected(int driverId)
    {
        try
        {
            var driver = await GetDriverByUserIdAsync(driverId);
            if (driver == null)
            {
                return false;
            }

            // Šalje poruku vozaču
            MessagingCenter.Send(this, "DriverSelected", driverId);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error notifying driver: {ex.Message}");
            return false;
        }
    }

    public async Task<Ride> GetActiveRideAsync(int driverId)
        {
            Ride activeRide = null;

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
            SELECT r.Id, r.CustomerId, r.SourceLatitude, r.SourceLongitude, r.SourceAddress,
                   r.DestinationLatitude, r.DestinationLongitude, r.DestinationAddress, r.Price, r.Status
            FROM Rides r
            WHERE r.DriverId = @DriverId AND r.Status = 1;  -- Status 1 = Active ride
        ";
                command.Parameters.AddWithValue("@DriverId", driverId);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    activeRide = new Ride
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
                        SourceLatitude = reader.GetDouble(reader.GetOrdinal("SourceLatitude")),
                        SourceLongitude = reader.GetDouble(reader.GetOrdinal("SourceLongitude")),
                        SourceAddress = reader.GetString(reader.GetOrdinal("SourceAddress")),
                        DestinationLatitude = reader.GetDouble(reader.GetOrdinal("DestinationLatitude")),
                        DestinationLongitude = reader.GetDouble(reader.GetOrdinal("DestinationLongitude")),
                        DestinationAddress = reader.GetString(reader.GetOrdinal("DestinationAddress")),
                        Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                        Status = (RideStatus)reader.GetInt32(reader.GetOrdinal("Status"))
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetActiveRideAsync error: {ex.Message}");
            }

            return activeRide;
        }


        public async Task<List<Driver>> GetAllAvailableDriversAsync()
        {
            var drivers = new List<Driver>();

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
SELECT d.Id, d.UserId, u.Name, u.ProfileImage, d.Car, d.Latitude, d.Longitude, d.Rating, d.isavailable
FROM Drivers d
JOIN Users u ON d.UserId = u.Id
WHERE d.isavailable = true;



        ";

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    drivers.Add(new Driver
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        ProfileImage = reader.IsDBNull(reader.GetOrdinal("ProfileImage")) ? null : reader.GetString(reader.GetOrdinal("ProfileImage")),
                        Car = reader.GetString(reader.GetOrdinal("Car")),
                        Latitude = reader.GetDouble(reader.GetOrdinal("Latitude")),
                        Longitude = reader.GetDouble(reader.GetOrdinal("Longitude")),
                        Rating = reader.GetInt32(reader.GetOrdinal("Rating")),
                        IsAvailable = reader.GetBoolean(reader.GetOrdinal("isavailable"))
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greška kod učitavanja vozača: " + ex.Message);
            }

            return drivers;
        }

    }
}

