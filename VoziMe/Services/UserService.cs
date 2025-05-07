using VoziMe.Models;
using Npgsql;
using System;
using System.IO; // For Path.Combine

namespace VoziMe.Services
{
    public class UserService
    {
        private readonly string _connectionString;

        public UserService()
        {
            // PostgreSQL connection string setup
            _connectionString = "User Id=postgres.vfqrsstbgqfwukfgslyo;Password=SanidMuhic123;Server=aws-0-eu-central-1.pooler.supabase.com;Port=5432;Database=postgres";

        }

        private User _currentUser;

        public User CurrentUser => _currentUser;

        public async Task<bool> LoginAsync(string email, string password, UserType userType)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var command = new NpgsqlCommand(
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
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check if a user with the same email exists
                var checkCommand = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM Users WHERE Email = @Email",
                    connection);
                checkCommand.Parameters.AddWithValue("@Email", email);

                var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                if (count > 0)
                {
                    // If user exists, return false
                    throw new Exception("A user with this email already exists");
                }

                // Proceed with registration if no user with this email exists
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                var insertCommand = new NpgsqlCommand(@"
                INSERT INTO Users (Name, Email, PasswordHash, UserType, ProfileImage)
                VALUES (@Name, @Email, @PasswordHash, @UserType, @ProfileImage)
                RETURNING Id;",
                connection);

                insertCommand.Parameters.AddWithValue("@Name", name);
                insertCommand.Parameters.AddWithValue("@Email", email);
                insertCommand.Parameters.AddWithValue("@PasswordHash", passwordHash);
                insertCommand.Parameters.AddWithValue("@UserType", (int)userType);
                insertCommand.Parameters.AddWithValue("@ProfileImage", "profile_placeholder.png");

                var userId = (int)await insertCommand.ExecuteScalarAsync();

                // If user is a driver, add driver details
                if (userType == UserType.Driver)
                {
                    var driverCommand = new NpgsqlCommand(@"
                    INSERT INTO Drivers (UserId, Car, Latitude, Longitude, Rating, IsAvailable)
                    VALUES (@UserId, @Car, @Latitude, @Longitude, @Rating, @IsAvailable);",
                    connection);

                    driverCommand.Parameters.AddWithValue("@UserId", userId);
                    driverCommand.Parameters.AddWithValue("@Car", "Not specified");
                    driverCommand.Parameters.AddWithValue("@Latitude", 44.2037); // Default location
                    driverCommand.Parameters.AddWithValue("@Longitude", 17.9071);
                    driverCommand.Parameters.AddWithValue("@Rating", 0);
                    driverCommand.Parameters.AddWithValue("@IsAvailable", true);

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
        public async Task<User> GetUserByEmailAsync(string email)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Users WHERE Email = @Email";
            command.Parameters.AddWithValue("@Email", email);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new User
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                    UserType = (UserType)reader.GetInt32(reader.GetOrdinal("UserType")),
                    ProfileImage = reader.IsDBNull(reader.GetOrdinal("ProfileImage")) ? null : reader.GetString(reader.GetOrdinal("ProfileImage"))
                };
            }

            return null;
        }

        public void Logout()
        {
            _currentUser = null;
        }
    }
}
