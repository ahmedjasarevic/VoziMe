using VoziMe.Models;
using VoziMe.Services;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

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
                    profileImage.Source = currentUser.ProfileImage ?? "profile_placeholder.png";

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
                    // Samo ime fajla (možeš prilagoditi spremanje slike na server ili u lokalni folder)
                    string fileName = result.FileName;

                    profileImage.Source = ImageSource.FromFile(result.FullPath);

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