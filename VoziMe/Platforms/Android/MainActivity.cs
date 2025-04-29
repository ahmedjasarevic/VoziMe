using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace VoziMe
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        const int RequestLocationId = 0;
        readonly string[] LocationPermissions =
        {
            Android.Manifest.Permission.AccessCoarseLocation,
            Android.Manifest.Permission.AccessFineLocation
        };

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Poziv za traženje dozvola
            RequestPermissionsIfNecessary();
        }

        // Funkcija za proveru i traženje dozvola
        void RequestPermissionsIfNecessary()
        {
            if ((int)Build.VERSION.SdkInt >= 23)
            {
                if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.AccessFineLocation) != Permission.Granted ||
                    ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.AccessCoarseLocation) != Permission.Granted)
                {
                    ActivityCompat.RequestPermissions(this, LocationPermissions, RequestLocationId);
                }
            }
        }

        // Ovde bi mogao da obradiš slučaj kada korisnik odbije dozvolu
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == RequestLocationId)
            {
                if (grantResults[0] == Permission.Granted)
                {
                    // Dozvola je data, možeš da koristiš lokaciju
                    Console.WriteLine("Lokacija dozvoljena!");
                }
                else
                {
                    // Dozvola nije data, obavesti korisnika ili obradi grešku
                    Console.WriteLine("Lokacija nije dozvoljena.");
                }
            }
        }
    }
}
