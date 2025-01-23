using System;
using CommunityToolkit.Mvvm.Input;

namespace BlackHole.ViewModels
{
    public class GameField : ViewModelBase
    {
        private string colour;
        private int x;
        private int y;

        public GameField(string piece, int x, int y)
        {
            this.x = x;
            this.y = y;
            colour = "White";
        }

        public String Colour
        {
            get { return colour; }
            set
            {
                if (colour != value)
                {
                    colour = value;
                    OnPropertyChanged();
                }
            }
        }


        /// <summary>
        /// Koordináta lekérdezése.
        /// </summary>
        public Tuple<int, int> XY
        {
            get { return new(X, Y); }
        }

        /// <summary>
        /// Lépés parancs lekérdezése, vagy beállítása.
        /// </summary>
        public RelayCommand<Tuple<int, int>>? TileClickedCommand { get; set; }
        public int X { get => x; set => x = value; }
        public int Y { get => y; set => y = value; }
    }
}
