using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System;
using Model.Model;
using System.Linq;
using System.Reactive;
using ReactiveUI;

namespace BlackHole.ViewModels;

public class GameViewModel : ViewModelBase
{
    private GameModel model;

    public RelayCommand NewGameCommand5x5 { get; private set; }
    public RelayCommand NewGameCommand7x7 { get; private set; }
    public RelayCommand NewGameCommand9x9 { get; private set; }
    public RelayCommand SaveGameCommand { get; private set; }
    public RelayCommand LoadGameCommand { get; private set; }

    public RelayCommand<string>? MoveCommand { get; private set; }

    public ObservableCollection<GameField> Fields { get; set; }

    //Status bar változók
    public string CurrentPlayer { get { return model.CurrPlayer; } }
    public string TableSize { get { return model.TableSize.ToString(); } }

    /// <summary>
    /// Új játék eseménye.
    /// </summary>
    public event EventHandler? NewGame5x5;
    public event EventHandler? NewGame7x7;
    public event EventHandler? NewGame9x9;
    public event EventHandler? SaveGame;
    public event EventHandler? LoadGame;

    public GameViewModel(GameModel model)
    {
        this.model = model;
        model.FieldChanged += new EventHandler<GameFieldEventArgs>(Model_FieldChanged);
        model.GameLoaded += new EventHandler<EventArgs>(Model_GameLoaded);

        NewGameCommand5x5 = new RelayCommand(OnNewGame5x5);
        NewGameCommand7x7 = new RelayCommand(OnNewGame7x7);
        NewGameCommand9x9 = new RelayCommand(OnNewGame9x9);

    //MoveCommand = new RelayCommand(ExecuteMove);
        MoveCommand = new RelayCommand<string>(dir =>
        {
            if (dir != null && dir is string)
                ExecuteMove(dir);
        });

        SaveGameCommand = new RelayCommand(OnSaveGame);
        LoadGameCommand = new RelayCommand(OnLoadGame);

        Fields = new ObservableCollection<GameField>();

        NewTable();
    }

    private void ExecuteMove(string dir)
    {
        model.PlayerClicked(-1, -1, dir);
    }

    private void Model_GameLoaded(object? sender, EventArgs e)
    {
        NewTable();
        OnPropertyChanged(nameof(TableSize));
        OnPropertyChanged(nameof(CurrentPlayer));
    }

    private void RefreshTable()
    {
        foreach (GameField f in Fields)
        {
            if (model.Table.GetValueIJ(f.X, f.Y) == "blue")
            {
                f.Colour = "Blue";
            }
            else if (model.Table.GetValueIJ(f.X, f.Y) == "red")
            {
                f.Colour = "Red";
            }
            else if (model.Table.GetValueIJ(f.X, f.Y) == "black")
            {
                f.Colour = "Black";
            }
            else
            {
                f.Colour = "White";
            }
        }

        OnPropertyChanged(nameof(CurrentPlayer));
    }

    private void NewTable()
    {
        Fields.Clear();
        for (int i = 0; i < model.TableSize; i++)
        {
            for (int j = 0; j < model.TableSize; j++)
            {
                GameField f = new GameField(string.Empty, i, j);
                f.TileClickedCommand = new RelayCommand<Tuple<int, int>>(position =>
                {
                    if (position != null)
                        ButtonCommand(position.Item1, position.Item2);
                });
                Fields.Add(f);

            }
        }

        RefreshTable();

    }

    private void Model_MoveHappened(object? sender, EventArgs e)
    {
        RefreshTable();
    }

    private void Model_FieldChanged(object? sender, GameFieldEventArgs e)
    {
        GameField field = Fields.Single(f => f.X == e.X && f.Y == e.Y);
        field.Colour = model.Table.GetValueIJ(field.X, field.Y) == String.Empty ? "White" : model.Table.GetValueIJ(field.X, field.Y);
        OnPropertyChanged(nameof(CurrentPlayer));
    }

    private void ButtonCommand(int x, int y)
    {
        model.PlayerClicked(x, y, "");
    }

    /// <summary>
    /// Új játék indításának eseménykiváltása.
    /// </summary>
    private void OnNewGame5x5()
    {
        NewGame5x5?.Invoke(this, EventArgs.Empty);
        OnPropertyChanged(nameof(TableSize));
        NewTable();
    }

    private void OnNewGame7x7()
    {
        NewGame7x7?.Invoke(this, EventArgs.Empty);
        OnPropertyChanged(nameof(TableSize));
        NewTable();
    }

    private void OnNewGame9x9()
    {
        NewGame9x9?.Invoke(this, EventArgs.Empty);
        OnPropertyChanged(nameof(TableSize));
        NewTable();
    }

    private void OnSaveGame()
    {
        SaveGame?.Invoke(this, EventArgs.Empty);
    }

    private void OnLoadGame()
    {
        LoadGame?.Invoke(this, EventArgs.Empty);
    }

}

