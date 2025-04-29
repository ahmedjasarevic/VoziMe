


using VoziMe.Models;
using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading.Tasks;
using static System.Environment;

namespace VoziMe;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService()
    {
        var databasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "vozi_me.db");
        _connectionString = $"Data Source={databasePath}";
    }

    public async Task<bool> InitializeDatabaseAsync()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            await CreateUsersTableAsync(connection);
            await CreateDriversTableAsync(connection);
            await CreateRidesTableAsync(connection);


            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database initialization error: {ex.Message}");
            return false;
        }
    }

    private async Task CreateUsersTableAsync(SqliteConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY 

AUTOINCREMENT,
                Name TEXT NOT NULL,
                Email TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL,
                UserType INTEGER NOT NULL,
                ProfileImage TEXT,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
        ";
        await command.ExecuteNonQueryAsync();
    }

    private async Task CreateDriversTableAsync(SqliteConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"

            CREATE TABLE IF NOT EXISTS Drivers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Car TEXT NOT NULL,
                Latitude REAL NOT NULL,
                Longitude REAL NOT NULL,
                Rating INTEGER NOT NULL DEFAULT 0,
                IsAvailable INTEGER NOT NULL DEFAULT 1,
                FOREIGN KEY (UserId) REFERENCES Users(Id)
            );
        ";
        await command.ExecuteNonQueryAsync();
    }

    private async Task

CreateRidesTableAsync(SqliteConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Rides (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CustomerId INTEGER NOT NULL,
                DriverId INTEGER NOT NULL,
                SourceLatitude REAL NOT NULL,
                SourceLongitude REAL NOT NULL,
                SourceAddress TEXT NOT NULL,
                DestinationLatitude REAL NOT NULL,
                DestinationLongitude REAL NOT NULL,
                DestinationAddress TEXT NOT NULL,

                Price REAL NOT NULL,
                Status INTEGER NOT NULL DEFAULT 0,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                CompletedAt DATETIME,
                FOREIGN KEY (CustomerId) REFERENCES Users(Id),
                FOREIGN KEY (DriverId) REFERENCES Drivers(Id)
            );
        ";
        await command.ExecuteNonQueryAsync();
    }

    public async Task SeedDatabaseAsync()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "SELECT COUNT(*) FROM Users";
            var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

            if (count == 0)
            {
                await AddSampleUsersAsync(connection);
                await AddSampleDriversAsync(connection);

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database seeding error: {ex.Message}");
        }
    }

    private async Task AddSampleUsersAsync(SqliteConnection connection)
    {
        var customerCommand = connection.CreateCommand();
        customerCommand.CommandText = @"
        INSERT INTO Users (Name, Email, PasswordHash, UserType, ProfileImage)
        VALUES ($Name, $Email, $PasswordHash, $UserType, $ProfileImage);";

        customerCommand.Parameters.AddWithValue("$Name", "Test User");
        customerCommand.Parameters.AddWithValue("$Email", "user@test.com");
        customerCommand.Parameters.AddWithValue("$PasswordHash", BCrypt.Net.BCrypt.HashPassword("password"));
        customerCommand.Parameters.AddWithValue("$UserType", (int)UserType.Customer);
        customerCommand.Parameters.AddWithValue("$ProfileImage", "profile_placeholder.png");

        await customerCommand.ExecuteNonQueryAsync();

        var driverNames = new[] { "Enis Halač", "Ahmed Jašarević", "Direk Aid" };
        var driverEmails = new[] { "enis@test.com", "ahmed@test.com", "direk@test.com" };

        for (int i = 0; i < driverNames.Length; i++)
        {
            var driverCommand = connection.CreateCommand();
            driverCommand.CommandText = @"
            INSERT INTO Users (Name, Email, PasswordHash, UserType, ProfileImage)
            VALUES ($Name, $Email, $PasswordHash, $UserType, $ProfileImage);";
            driverCommand.Parameters.AddWithValue("$Name", driverNames[i]);

            driverCommand.Parameters.AddWithValue("$Email", driverEmails[i]);
            driverCommand.Parameters.AddWithValue("$PasswordHash", BCrypt.Net.BCrypt.HashPassword("password"));
            driverCommand.Parameters.AddWithValue("$UserType", (int)UserType.Driver);
            driverCommand.Parameters.AddWithValue("$ProfileImage", $"driver{i + 1}.png");

            await driverCommand.ExecuteNonQueryAsync();
        }
    }

    private async Task AddSampleDriversAsync(SqliteConnection connection)
    {
        var getUsersCommand = connection.CreateCommand();
        getUsersCommand.CommandText = "SELECT Id, Name FROM Users WHERE UserType = $UserType";
        getUsersCommand.Parameters.AddWithValue("$UserType", (int)UserType.Driver);

        var driverUserIds = new List<(int id, string name)>();
        using (var reader = await getUsersCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                driverUserIds.Add((reader.GetInt32(0),

    reader.GetString(1)));
            }
        }

        var cars = new[] { "Fiat Marea", "Peugeot 307", "BMW 320" };
        var latitudes = new[] { 44.2037, 44.2050, 44.2020 };
        var longitudes = new[] { 17.9071, 17.9090, 17.9060 };

        for (int i = 0; i < driverUserIds.Count; i++)
        {
            var driverCommand = connection.CreateCommand();
            driverCommand.CommandText = @"
            INSERT INTO Drivers (UserId, Car, Latitude, Longitude, Rating, IsAvailable)
            VALUES ($UserId, $Car, $Latitude, $Longitude, $Rating, $IsAvailable);";


            driverCommand.Parameters.AddWithValue("$UserId", driverUserIds[i].id);
            driverCommand.Parameters.AddWithValue("$Car", cars[i % cars.Length]);
            driverCommand.Parameters.AddWithValue("$Latitude", latitudes[i % latitudes.Length]);
            driverCommand.Parameters.AddWithValue("$Longitude", longitudes[i % longitudes.Length]);
            driverCommand.Parameters.AddWithValue("$Rating", 5);
            driverCommand.Parameters.AddWithValue("$IsAvailable", 1);

            await driverCommand.ExecuteNonQueryAsync();

        }
    }

}
