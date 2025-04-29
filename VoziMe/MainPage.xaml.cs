using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using VoziMe;

namespace VoziMe;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        InitializeDatabase();
    }

    private async void InitializeDatabase()
    {
        var databaseService = new DatabaseService();
        await databaseService.InitializeDatabaseAsync();
    }
}