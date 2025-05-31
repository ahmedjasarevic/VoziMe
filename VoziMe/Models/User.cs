using System;

namespace VoziMe.Models
{
    public enum UserType
    {
        Customer = 1, // Korisnik
        Driver = 2    // Vozač
    }

    // Klasa korisnika
    public class User
    {
        public int Id { get; set; }                
        public string Name { get; set; }           
        public string Email { get; set; }          
        public string PasswordHash { get; set; }    
        public UserType UserType { get; set; }     
        public string ProfileImage { get; set; }    
        public DateTime CreatedAt { get; set; }    
    }
}
