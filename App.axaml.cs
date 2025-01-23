using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using BlackHole.ViewModels;
using BlackHole.Views;
using Model.Model;
using Model.Persistence;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using System;
using System.IO;

namespace BlackHole;

public partial class App : Application
{
    #region Fields

    private GameModel _model = null!;
    private GameViewModel _viewModel = null!;

    #endregion

    #region Properites

    private TopLevel? TopLevel
    {
        get
        {
            return ApplicationLifetime switch
            {
                IClassicDesktopStyleApplicationLifetime desktop => TopLevel.GetTopLevel(desktop.MainWindow),
                ISingleViewApplicationLifetime singleViewPlatform => TopLevel.GetTopLevel(singleViewPlatform.MainView),
                _ => null
            };
        }
    }

    #endregion

    #region Application methods

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        // modell létrehozása
        _model = new GameModel(new GameFileDataAccess());
        _model.GameOver += new EventHandler<GameEventArgs>(Model_GameOver);
        _model.NewGame();

        // nézemodell létrehozása
        _viewModel = new GameViewModel(_model);
        _viewModel.NewGame5x5 += new EventHandler(ViewModel_NewGame5x5);
        _viewModel.NewGame7x7 += new EventHandler(ViewModel_NewGame7x7);
        _viewModel.NewGame9x9 += new EventHandler(ViewModel_NewGame9x9);
        _viewModel.LoadGame += new EventHandler(ViewModel_LoadGame);
        _viewModel.SaveGame += new EventHandler(ViewModel_SaveGame);

        // nézet létrehozása
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // asztali környezethez
            desktop.MainWindow = new MainWindow
            {
                DataContext = _viewModel
            };

            desktop.Startup += async (s, e) =>
            {
                _model.NewGame(); // indításkor új játékot kezdünk

                // betöltjük a felfüggesztett játékot, amennyiben van
                try
                {
                    await _model.LoadGameAsync(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SudokuSuspendedGame"));
                }
                catch { }
            };

            desktop.Exit += async (s, e) =>
            {
                // elmentjük a jelenleg folyó játékot
                try
                {
                    await _model.SaveGameAsync(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SudokuSuspendedGame"));
                    // mentés a felhasználó Documents könyvtárába, oda minden bizonnyal van jogunk írni
                }
                catch { }
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            // mobil környezethez
            singleViewPlatform.MainView = new MainView
            {
                DataContext = _viewModel
            };

            if (Application.Current?.TryGetFeature<IActivatableLifetime>() is { } activatableLifetime)
            {
                activatableLifetime.Activated += async (sender, args) =>
                {
                    if (args.Kind == ActivationKind.Background)
                    {
                        // betöltjük a felfüggesztett játékot, amennyiben van
                        try
                        {
                            await _model.LoadGameAsync(
                                Path.Combine(AppContext.BaseDirectory, "SuspendedGame"));
                        }
                        catch
                        {
                        }
                    }
                };
                activatableLifetime.Deactivated += async (sender, args) =>
                {
                    if (args.Kind == ActivationKind.Background)
                    {

                        // elmentjük a jelenleg folyó játékot
                        try
                        {
                            await _model.SaveGameAsync(
                                Path.Combine(AppContext.BaseDirectory, "SuspendedGame"));
                            // Androidon az AppContext.BaseDirectory az alkalmazás adat könyvtára, ahova
                            // akár külön jogosultság nélkül is lehetne írni
                        }
                        catch
                        {
                        }
                    }
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    #endregion

    #region ViewModel event handlers

    /// <summary>
    /// Új játék indításának eseménykezelője.
    /// </summary>
    private void ViewModel_NewGame5x5(object? sender, EventArgs e)
    {
        _model.NewGame(5);
    }

    /// <summary>
    /// Új játék indításának eseménykezelője.
    /// </summary>
    private void ViewModel_NewGame7x7(object? sender, EventArgs e)
    {
        _model.NewGame(7);
    }

    /// <summary>
    /// Új játék indításának eseménykezelője.
    /// </summary>
    private void ViewModel_NewGame9x9(object? sender, EventArgs e)
    {
        _model.NewGame(9);
    }

    /// <summary>
    /// Játék betöltésének eseménykezelője.
    /// </summary>
    private async void ViewModel_LoadGame(object? sender, System.EventArgs e)
    {
        if (TopLevel == null)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                    "Sudoku játék",
                    "A fájlkezelés nem támogatott!",
                    ButtonEnum.Ok, Icon.Error)
                .ShowAsync();
            return;
        }


        try
        {
            var files = await TopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Sudoku tábla betöltése",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Sudoku tábla")
                    {
                        Patterns = new[] { "*.stl" }
                    }
                }
            });

            if (files.Count > 0)
            {
                // játék betöltése
                using (var stream = await files[0].OpenReadAsync())
                {
                    await _model.LoadGameAsync(stream);
                }
            }
        }
        catch (GameDataException)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                    "Sudoku játék",
                    "A fájl betöltése sikertelen!",
                    ButtonEnum.Ok, Icon.Error)
                .ShowAsync();
        }

    }

    /// <summary>
    /// Játék mentésének eseménykezelője.
    /// </summary>
    private async void ViewModel_SaveGame(object? sender, EventArgs e)
    {
        if (TopLevel == null)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                    "Black Hole játék",
                    "A fájlkezelés nem támogatott!",
                    ButtonEnum.Ok, Icon.Error)
                .ShowAsync();
            return;
        }


        try
        {
            var file = await TopLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                Title = "Sudoku tábla mentése",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Sudoku tábla")
                    {
                        Patterns = new[] { "*.stl" }
                    }
                }
            });

            if (file != null)
            {
                // játék mentése
                using (var stream = await file.OpenWriteAsync())
                {
                    await _model.SaveGameAsync(stream);
                }
            }
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                    "Sudoku játék",
                    "A fájl mentése sikertelen!" + ex.Message,
                    ButtonEnum.Ok, Icon.Error)
                .ShowAsync();
        }

    }

    #endregion

    /// <summary>
    /// Játék végének eseménykezelője.
    /// </summary>
    private async void Model_GameOver(object? sender, GameEventArgs e)
    {
        await MessageBoxManager.GetMessageBoxStandard(
                "Black Hole játék",
                "Jték vége" + Environment.NewLine +
                "A győztes: " + e.Winner,
                ButtonEnum.Ok, Icon.Info)
            .ShowAsync();

    }
}