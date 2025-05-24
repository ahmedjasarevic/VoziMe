using VoziMe.Models;
using VoziMe.Services;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

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

        private async void LoadUserProfile()
        {
            try
            {
                var currentUser = _userService.CurrentUser;

                if (currentUser != null)
                {
                    lblName.Text = currentUser.Name;
                    lblEmail.Text = currentUser.Email;
                    profileImage.Source = currentUser.ProfileImage ?? "profile_placeholder.png";
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
                    string fileName = result.FileName;
                    profileImage.Source = ImageSource.FromFile(result.FullPath);

                    // Pretpostavka: UserService ima metodu za update slike korisnika
                    await _userService.UpdateProfileImageAsync(_userService.CurrentUser.Id, fileName);

                    await DisplayAlert("Uspjeh", "Profilna slika je ažurirana.", "U redu");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Greška", $"Neuspjelo mijenjanje slike: {ex.Message}", "U redu");
            }
        }
    }
}
