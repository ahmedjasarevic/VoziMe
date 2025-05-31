using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using VoziMe;

namespace VoziMe;


public partial class MainPage : ContentPage
{
    private readonly DatabaseService _databaseService;

    public MainPage(DatabaseService databaseService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        InitializeDatabase();
    }

    private async void InitializeDatabase()
    {
        await _databaseService.InitializeDatabaseAsync();
    }
}
