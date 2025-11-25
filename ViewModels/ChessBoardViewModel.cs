using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ChessTrainerApp.Models;
using CommunityToolkit.Mvvm.Input;

namespace ChessTrainerApp.ViewModels
{
    // Використовуємо ObservableObject з CommunityToolkit.Mvvm
    public partial class ChessBoardViewModel : ObservableObject
    {
        // Колекція для відображення дошки (9x9)
        public ObservableCollection<SquareModel> Squares { get; } 
        
        [ObservableProperty] // Автоматична генерація OnPropertyChanged
        private SquareModel selectedSquare;

        public ChessBoardViewModel()
        {
            Squares = new ObservableCollection<SquareModel>();
            InitializeBoard();
        }

        private void InitializeBoard()
        {
            Squares.Clear();
            Color lightSquare = Color.FromHex("#EEEED2");
            Color darkSquare = Color.FromHex("#769656");

            // Ініціалізація 8x8 клітинок
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    var square = new SquareModel
                    {
                        Row = r,
                        Column = c,
                        // Змінена логіка кольорів
                        SquareColor = (r + c) % 2 == 0 ? lightSquare : darkSquare, 
                        Piece = GetInitialPiece(r, c)
                    };
                    Squares.Add(square);
                }
            }
        }

        // Частина класу ChessBoardViewModel
        private PieceModel GetInitialPiece(int r, int c)
        {
            PieceColor color = (r == 0 || r == 1) ? PieceColor.Black : 
                            (r == 6 || r == 7) ? PieceColor.White : PieceColor.None;

            if (color == PieceColor.None)
                return new PieceModel { Type = PieceType.None, Color = PieceColor.None };

            PieceType type = PieceType.None;
            if (r == 1 || r == 6) // Пішаки
                type = PieceType.Pawn;
            else if (r == 0 || r == 7) // Важкі фігури
            {
                switch (c)
                {
                    case 0:
                    case 7:
                        type = PieceType.Rook;
                        break;
                    case 1:
                    case 6:
                        type = PieceType.Knight;
                        break;
                    case 2:
                    case 5:
                        type = PieceType.Bishop;
                        break;
                    case 3:
                        type = PieceType.Queen;
                        break;
                    case 4:
                        type = PieceType.King;
                        break;
                }
            }
            
            // TODO: Встановити DisplayValue (іконки)
            return new PieceModel { Type = type, Color = color };
        }
        
        // ChessBoardViewModel.cs
        [ObservableProperty]
        private PieceColor currentTurn = PieceColor.White;

        // Онови метод HandleSquareClick
        [RelayCommand]
        private void HandleSquareClick(SquareModel clickedSquare)
        {
            // СЦЕНАРІЙ 1: Вибір фігури
            if (SelectedSquare == null)
            {
                // Перевіряємо, чи клікнули на фігуру ТА чи це фігура поточного гравця
                if (clickedSquare.Piece.Type != PieceType.None && 
                    clickedSquare.Piece.Color == CurrentTurn) 
                {
                    SelectedSquare = clickedSquare;
                    SelectedSquare.SquareColor = Color.FromHex("#F6F669"); // Жовтий
                    
                    // TODO: Тут ми будемо підсвічувати можливі ходи (Step 3)
                }
            }
            // СЦЕНАРІЙ 2: Спроба ходу
            else
            {
                ResetSquareColor(SelectedSquare); // Скидаємо колір

                // Якщо клікнули на свою ж фігуру - просто міняємо вибір
                if (clickedSquare.Piece.Color == CurrentTurn)
                {
                    SelectedSquare = clickedSquare;
                    SelectedSquare.SquareColor = Color.FromHex("#F6F669");
                    return;
                }

                // ВАЛІДАЦІЯ ХОДУ (Тут буде головна логіка)
                if (ChessRules.IsMoveValid(SelectedSquare, clickedSquare, Squares))
                {
                    MakeMove(SelectedSquare, clickedSquare);
                    SwitchTurn();
                }

                SelectedSquare = null;
            }
        }

        private void MakeMove(SquareModel from, SquareModel to)
        {
            to.Piece = from.Piece;
            from.Piece = new PieceModel { Type = PieceType.None, Color = PieceColor.None };
        }

        private void SwitchTurn()
        {
            CurrentTurn = CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
        }

        private void ResetSquareColor(SquareModel square)
        {
            // Повертаємо оригінальний колір (зелений або кремовий)
            // Тобі треба буде винести логіку визначення кольору в окремий метод або зберігати оригінальний колір
            bool isEven = (square.Row + square.Column) % 2 == 0;
            square.SquareColor = isEven ? Color.FromHex("#EEEED2") : Color.FromHex("#769656"); 
        }
    }
}
