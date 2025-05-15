using VoziMe.Models;
using VoziMe.Views;

namespace VoziMe
{
    public partial class AppShell : Shell
    {
        public AppShell(User loggedInUser)
        {
            Shell.SetNavBarIsVisible(this, false); // Sakrij Shell navbar

            var tabBar = new TabBar();

            ShellContent homeContent;
            ShellContent profileContent;

            if (loggedInUser.UserType == UserType.Driver)
            {
                // Proslijedi UserId kao driverId kroz konstruktor
                homeContent = new ShellContent
                {
                    Title = "Početna",
                    Icon = "home.png",
                    ContentTemplate = new DataTemplate(() =>
                    {
                        return new DriverHomePage(loggedInUser.Id); // Proslijedi ID odmah
                    })
                };

                profileContent = new ShellContent
                {
                    Title = "Profil",
                    Icon = "profile.png",
                    ContentTemplate = new DataTemplate(() =>
                    {
                        return new DriverProfilePage(); // Ako i ova stranica koristi ID, dodaj ga isto ovako
                    })
                };
            }
            else // Putnik
            {
                homeContent = new ShellContent
                {
                    Title = "Početna",
                    Icon = "home.png",
                    ContentTemplate = new DataTemplate(() =>
                    {
                        return new DriverSelectionPage(); // Ako koristi korisnički ID
                    })
                };

                profileContent = new ShellContent
                {
                    Title = "Profil",
                    Icon = "profile.png",
                    ContentTemplate = new DataTemplate(() =>
                    {
                        return new UserProfilePage(); // Ako koristi ID
                    })
                };
            }

            tabBar.Items.Add(homeContent);
            tabBar.Items.Add(profileContent);

            Items.Add(tabBar);
        }
    }
}
