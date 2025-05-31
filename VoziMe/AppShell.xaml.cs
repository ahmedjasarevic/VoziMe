using VoziMe.Models;
using VoziMe.Views;

namespace VoziMe
{
    public partial class AppShell : Shell
    {
        public AppShell(User loggedInUser)
        {
            Shell.SetNavBarIsVisible(this, false); 

            var tabBar = new TabBar();

            ShellContent homeContent;
            ShellContent profileContent;

            if (loggedInUser.UserType == UserType.Driver)
            {
                homeContent = new ShellContent
                {
                    Title = "Početna",
                    Icon = "home.png",
                    ContentTemplate = new DataTemplate(() =>
                    {
                        return new DriverHomePage(loggedInUser.Id);
                    })
                };

                profileContent = new ShellContent
                {
                    Title = "Profil",
                    Icon = "profile.png",
                    ContentTemplate = new DataTemplate(() =>
                    {
                        return new DriverProfilePage(); 
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
                        return new DriverSelectionPage(); 
                    })
                };

                profileContent = new ShellContent
                {
                    Title = "Profil",
                    Icon = "profile.png",
                    ContentTemplate = new DataTemplate(() =>
                    {
                        return new UserProfilePage();
                    })
                };
            }

            tabBar.Items.Add(homeContent);
            tabBar.Items.Add(profileContent);

            Items.Add(tabBar);
        }
    }
}
