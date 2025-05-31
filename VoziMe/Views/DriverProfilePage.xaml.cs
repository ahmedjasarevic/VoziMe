using VoziMe.Models;
using VoziMe.Services;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Supabase;
using Supabase.Storage;
using FileAccess = System.IO.FileAccess;


namespace VoziMe.Views
{
    public partial class DriverProfilePage : ContentPage
    {
        private readonly UserService _userService;
        private Driver _driver;

        public DriverProfilePage()
        {
            InitializeComponent();
            _userService = Application.Current.Handler.MauiContext.Services.GetService<UserService>();
            LoadDriverProfile();
        }

        string GetProfileImageUrl(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "profile_placeholder.png";

            return $"{UserService.SupabaseUrl}/storage/v1/object/public/{UserService.BucketName}/{UserService.FolderPath}/{fileName}";
        }

        private async void LoadDriverProfile()
        {
            try
            {
                var currentUser = _userService.CurrentUser;
                Console.WriteLine($"CURRENT USER: {currentUser?.Name}, {currentUser?.Email}, {currentUser?.Id}");


                if (currentUser != null)
                {
                    lblName.Text = currentUser.Name;
                    lblEmail.Text = currentUser.Email;
                    profileImage.Source = ImageSource.FromUri(new Uri(GetProfileImageUrl(currentUser.ProfileImage)));


                    _driver = await _userService.GetDriverByUserIdAsync(currentUser.Id);
                    if (_driver != null)
                    {
                        entryCar.Text = _driver.Car;
                        lblRating.Text = $"Ocjena: {_driver.Rating}/5";
                    }
                }
                else
                {
                    await DisplayAlert("Greška", "Niste prijavljeni.", "U redu");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Greška", $"Neuspjelo uèitavanje profila: {ex.Message}", "U redu");
            }
        }

        private async void btnSaveCar_Clicked(object sender, EventArgs e)
        {
            if (_driver == null)
            {
                await DisplayAlert("Greška", "Vozaè nije pronaðen.", "U redu");
                return;
            }

            string newCar = entryCar.Text?.Trim();
            if (string.IsNullOrEmpty(newCar))
            {
                await DisplayAlert("Greška", "Unesite naziv automobila.", "U redu");
                return;
            }

            await _userService.UpdateDriverCarAsync(_driver.UserId, newCar);
            await DisplayAlert("Uspjeh", "Podaci o vozilu su ažurirani.", "U redu");
        }

        private async void btnChangeImage_Clicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Odaberite profilnu sliku",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    using var stream = await result.OpenReadAsync();
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    byte[] fileBytes = memoryStream.ToArray();

                    var supabaseClient = new Supabase.Client(UserService.SupabaseUrl, UserService.SupabaseKey);
                    await supabaseClient.InitializeAsync();

                    string fileName = $"{_userService.CurrentUser.Id}_{result.FileName}";
                    string fullPath = $"{UserService.FolderPath}/{fileName}";

                    var bucket = supabaseClient.Storage.From(UserService.BucketName);
                    var uploadResponse = await bucket.Upload(fileBytes, fullPath, new Supabase.Storage.FileOptions
                    {
                        Upsert = true,
                        ContentType = result.ContentType
                    });

                    // Ažuriraj korisnika u bazi
                    await _userService.UpdateProfileImageAsync(_userService.CurrentUser.Id, fileName);

                    // Prikaz slike
                    profileImage.Source = ImageSource.FromUri(new Uri(GetProfileImageUrl(fileName)));

                    await DisplayAlert("Uspjeh", "Profilna slika je ažurirana.", "U redu");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Greška", $"Neuspjelo mijenjanje slike: {ex.Message}", "U redu");
            }
        }



        private async void btnLogout_Clicked(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Odjava", "Da li ste sigurni da se želite odjaviti?", "Da", "Ne");
            if (!confirm)
                return;

            try
            {
                // Očisti podatke o korisniku
                await _userService.LogoutAsync();

                // Vrati korisnika na login stranicu

                Application.Current.MainPage = new NavigationPage(new WelcomePage());

            }
            catch (Exception ex)
            {
                await DisplayAlert("Greška", $"Neuspjela odjava: {ex.Message}", "U redu");
            }
        }


    }

}