using VoziMe.Models;
using VoziMe.Services;
using Microsoft.Maui.Controls;
using System;
using System.IO;
using System.Threading.Tasks;
using Supabase;
using FileAccess = System.IO.FileAccess;

namespace VoziMe.Views
{
    public partial class UserProfilePage : ContentPage
    {
        private readonly UserService _userService;

        public UserProfilePage()
        {
            InitializeComponent();
            _userService = Application.Current.Handler.MauiContext.Services.GetService<UserService>();
            LoadUserProfile();
        }

        private string GetProfileImageUrl(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "profile_placeholder.png";

            return $"{UserService.SupabaseUrl}/storage/v1/object/public/{UserService.BucketName}/{UserService.FolderPath}/{fileName}";
        }

        private async void LoadUserProfile()
        {
            try
            {
                var currentUser = _userService.CurrentUser;

                if (currentUser != null)
                {
                    lblName.Text = currentUser.Name;
                    lblEmail.Text = currentUser.Email;
                    profileImage.Source = ImageSource.FromUri(new Uri(GetProfileImageUrl(currentUser.ProfileImage)));
                }
                else
                {
                    await DisplayAlert("Greška", "Niste prijavljeni.", "U redu");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Greška", $"Neuspjelo učitavanje profila: {ex.Message}", "U redu");
            }
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

                    // Prikaz nove slike
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
                await _userService.LogoutAsync();
                Application.Current.MainPage = new NavigationPage(new WelcomePage());
            }
            catch (Exception ex)
            {
                await DisplayAlert("Greška", $"Neuspjela odjava: {ex.Message}", "U redu");
            }
        }
    }
}
