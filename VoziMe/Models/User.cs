using System;

namespace VoziMe.Models
{
    // Enum za različite vrste korisnika
    public enum UserType
    {
        Customer = 1, // Korisnik
        Driver = 2    // Vozač
    }

    // Klasa korisnika
    public class User
    {
        public int Id { get; set; }                // ID korisnika
        public string Name { get; set; }            // Ime korisnika
        public string Email { get; set; }           // Email korisnika
        public string PasswordHash { get; set; }    // Hashovana lozinka
        public UserType UserType { get; set; }      // Tip korisnika (Customer/Driver)
        public string ProfileImage { get; set; }    // Putanja do slike profila
        public DateTime CreatedAt { get; set; }     // Datum kada je korisnik kreiran
    }
}
