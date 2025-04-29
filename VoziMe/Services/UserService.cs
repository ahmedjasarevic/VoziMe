
using Microsoft.Data.Sqlite;
using VoziMe.Models;
using System.IO; // Dodato za Path.Combine

namespace VoziMe.Services;

public class UserService
{
    private readonly string _connectionString;

    public UserService()
    {
        var databasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "vozi_me.db");
        _connectionString = $"Data Source={databasePath}";

    }

    private User _currentUser;

    public User CurrentUser => _currentUser;

    public async Task<bool> LoginAsync(string email, string password, UserType userType)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = new SqliteCommand(
                "SELECT * FROM Users WHERE Email = @Email AND UserType = @UserType",

                connection);
            command.Parameters.AddWithValue("@Email", email);
            command.Parameters.AddWithValue("@UserType", (int)userType);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var storedHash = reader.GetString(reader.GetOrdinal("PasswordHash"));

                if (BCrypt.Net.BCrypt.Verify(password, storedHash))
                {
                    _currentUser = new User

                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Email = reader.GetString(reader.GetOrdinal("Email")),
                        UserType = (UserType)reader.GetInt32(reader.GetOrdinal("UserType")),
                        ProfileImage = reader.IsDBNull(reader.GetOrdinal("ProfileImage"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("ProfileImage"))
                    };

                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RegisterAsync(string name, string email, string password, UserType userType)
    {
        try
        {
            using var connection = new

SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // Check if user already exists
            var checkCommand = new SqliteCommand(
                "SELECT COUNT(*) FROM Users WHERE Email = @Email",
                connection);
            checkCommand.Parameters.AddWithValue("@Email", email);

            var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
            if (count > 0)
            {
                throw new Exception("Korisnik sa ovom email adresom već postoji");
            }

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            // Insert new user
            var insertCommand = new SqliteCommand(@"
                INSERT INTO Users (Name, Email, PasswordHash, UserType, ProfileImage)
                VALUES (@Name, @Email, @PasswordHash, @UserType, @ProfileImage);
                SELECT last_insert_rowid();",
                connection);

            insertCommand.Parameters.AddWithValue("@Name", name);
            insertCommand.Parameters.AddWithValue

("@Email", email);
            insertCommand.Parameters.AddWithValue("@PasswordHash", passwordHash);
            insertCommand.Parameters.AddWithValue("@UserType", (int)userType);
            insertCommand.Parameters.AddWithValue("@ProfileImage", "profile_placeholder.png");

            var userId = Convert.ToInt32(await insertCommand.ExecuteScalarAsync());

            // If registering as a driver, add driver details
            if (userType == UserType.Driver)
            {
                var driverCommand = new SqliteCommand(@"

                    INSERT INTO Drivers (UserId, Car, Latitude, Longitude, Rating, IsAvailable)
                    VALUES (@UserId, @Car, @Latitude, @Longitude, @Rating, @IsAvailable);",
                    connection);

                driverCommand.Parameters.AddWithValue("@UserId", userId);
                driverCommand.Parameters.AddWithValue("@Car", "Nije specificirano");
                driverCommand.Parameters.AddWithValue("@Latitude", 44.2037); // Default location
                driverCommand.Parameters.AddWithValue("@Longitude", 17.9071);


                driverCommand.Parameters.AddWithValue("@Rating", 0);
                driverCommand.Parameters.AddWithValue("@IsAvailable", 1);

                await driverCommand.ExecuteNonQueryAsync();
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Registration error: {ex.Message}");
            throw;
        }
    }

    public void Logout()

    {
        _currentUser = null;
    }
}
