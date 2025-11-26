using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ChessTrainerApp.Models;
using CommunityToolkit.Mvvm.Input;

namespace ChessTrainerApp.ViewModels
{
    public partial class ChessBoardViewModel : ObservableObject
    {
        // Колекція для відображення дошки (9x9)
        public ObservableCollection<SquareModel> Squares { get; }

        [ObservableProperty]
        private SquareModel selectedSquare;

        [ObservableProperty]
        private GameStatus gameStatus = GameStatus.InProgress;

        [ObservableProperty]
        private string gameOverMessage = "";

        [ObservableProperty]
        private bool isGameOver = false;

        public ChessBoardViewModel()
        {
            Squares = new ObservableCollection<SquareModel>();
            InitializeBoard();
        }

        // створення дошки
        private void InitializeBoard()
        {
            Squares.Clear();
            // кольори клітинок
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
                        SquareColor = (r + c) % 2 == 0 ? lightSquare : darkSquare,
                        Piece = GetInitialPiece(r, c)
                    };
                    Squares.Add(square);
                }
            }
        }

        // клітина позаду пішака, що пішок на 2 клітини
        public SquareModel EnPassantTarget { get; set; }

        // отримання фігури на клітинці на початку гри
        private PieceModel GetInitialPiece(int r, int c)
        {
            PieceColor color = (r == 0 || r == 1) ? PieceColor.Black :
                            (r == 6 || r == 7) ? PieceColor.White : PieceColor.None;

            // якщо це 3-6 ряд, то фігури там нема
            if (color == PieceColor.None)
                return new PieceModel { Type = PieceType.None, Color = PieceColor.None };

            PieceType type = PieceType.None;
            if (r == 1 || r == 6) // пішаки
                type = PieceType.Pawn;
            else if (r == 0 || r == 7) // легкі та важкі фігури з королем
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

            // встановлення відповідної іконки
            return new PieceModel { Type = type, Color = color };
        }

        [ObservableProperty]
        private PieceColor currentTurn = PieceColor.White;

        // натискання на клітинку
        [RelayCommand]
        private async Task HandleSquareClick(SquareModel clickedSquare)
        {
            if (GameStatus != GameStatus.InProgress) return;

            // сценарій 1: вибір фігури
            if (SelectedSquare == null)
            {
                if (clickedSquare.Piece.Type != PieceType.None &&
                    clickedSquare.Piece.Color == CurrentTurn)
                {
                    SelectedSquare = clickedSquare;
                    SelectedSquare.SquareColor = Color.FromHex("#F6F669"); // виділення клітини фігури, яку обрано
                }
                return;
            }

            // сценарій 2: спроба ходу

            // скидаємо колір виділення
            bool isEven = (SelectedSquare.Row + SelectedSquare.Column) % 2 == 0;
            SelectedSquare.SquareColor = isEven ? Color.FromHex("#EEEED2") : Color.FromHex("#769656");

            // якщо клікнули на свою ж фігуру, то просто перемикаємо вибір
            if (clickedSquare.Piece.Color == CurrentTurn)
            {
                SelectedSquare = clickedSquare;
                SelectedSquare.SquareColor = Color.FromHex("#F6F669");
                return;
            }

            // перевірка валідності ходу
            if (ChessRules.IsMoveValid(SelectedSquare, clickedSquare, Squares, EnPassantTarget))
            {
                // відображення ходу
                MakeMove(SelectedSquare, clickedSquare);

                // перевірка на перетворення пішака на іншу фігуру
                if (clickedSquare.Piece.Type == PieceType.Pawn && (clickedSquare.Row == 0 || clickedSquare.Row == 7))
                {
                    // надаємо користувачу обрати фігуру
                    await PromotePawn(clickedSquare);
                }

                // передача ходу опоненту
                SwitchTurn();
            }

            SelectedSquare = null;
        }

        // виконання ходу
        private void MakeMove(SquareModel from, SquareModel to)
        {
            // рокировка
            if (from.Piece.Type == PieceType.King && Math.Abs(to.Column - from.Column) == 2)
            {
                int direction = to.Column - from.Column > 0 ? 1 : -1;
                int rookOldCol = direction == 1 ? 7 : 0;
                int rookNewCol = direction == 1 ? 5 : 3;

                var rookOldSq = Squares[from.Row * 8 + rookOldCol];
                var rookNewSq = Squares[from.Row * 8 + rookNewCol];

                rookNewSq.Piece = rookOldSq.Piece;
                rookOldSq.Piece = new PieceModel { Type = PieceType.None, Color = PieceColor.None };
                rookNewSq.Piece.HasMoved = true;
            }

            // взяття на проході
            if (from.Piece.Type == PieceType.Pawn &&
                from.Column != to.Column &&
                to.Piece.Type == PieceType.None)
            {
                // Ворог стоїть на тому ж рядку, звідки ми прийшли, але в колонці, куди ми йдемо
                int enemyPawnRow = from.Row;
                int enemyPawnCol = to.Column;

                var enemyPawnSq = Squares[enemyPawnRow * 8 + enemyPawnCol];

                // З'їдаємо ворога
                enemyPawnSq.Piece = new PieceModel { Type = PieceType.None, Color = PieceColor.None };
            }

            // фізичний хід
            to.Piece = from.Piece;
            from.Piece = new PieceModel { Type = PieceType.None, Color = PieceColor.None };
            to.Piece.HasMoved = true;

            // якщо пішак стрибнув на 2 клітинки, то запам'ятовуємо клітинку за ним
            if (to.Piece.Type == PieceType.Pawn && Math.Abs(to.Row - from.Row) == 2)
            {
                int middleRow = (from.Row + to.Row) / 2;
                EnPassantTarget = Squares[middleRow * 8 + from.Column];
            }
            else
            {
                // будь-який інший хід скидає можливість взяття на проході
                EnPassantTarget = null;
            }

            // SwitchTurn викликається у HandleSquareClick()
        }

        // перетворення пішака
        private async Task PromotePawn(SquareModel square)
        {
            // Показуємо меню і чекаємо відповіді
            string result = await Shell.Current.DisplayActionSheet(
                "Оберіть фігуру:", null, null,
                "Ферзь", "Тура", "Слон", "Кінь");

            // Якщо користувач скасував (клацнув поза меню), за замовчуванням Ферзь
            if (result == null) result = "Ферзь";

            PieceType newType = result switch
            {
                "Ферзь" => PieceType.Queen,
                "Тура" => PieceType.Rook,
                "Слон" => PieceType.Bishop,
                "Кінь" => PieceType.Knight,
                _ => PieceType.Queen
            };

            // Оновлюємо фігуру на дошці
            square.Piece = new PieceModel
            {
                Type = newType,
                Color = square.Piece.Color,
                HasMoved = true
            };
        }

        // передача ходу
        private void SwitchTurn()
        {
            // Змінюємо гравця
            CurrentTurn = CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;

            // чи є у нового гравця ходи?
            bool hasMoves = ChessRules.HasAnyLegalMove(CurrentTurn, Squares, EnPassantTarget);

            if (!hasMoves)
            {
                // ходів немає, перевірка чи це шах
                var kingSquare = Squares.FirstOrDefault(s => s.Piece.Type == PieceType.King && s.Piece.Color == CurrentTurn);
                var enemyColor = CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
                
                bool isCheck = ChessRules.IsSquareUnderAttack(kingSquare, enemyColor, Squares);

                if (isCheck)
                {
                    GameStatus = GameStatus.Checkmate;
                    GameOverMessage = $"МАТ! Перемогли {enemyColor}";
                }
                else
                {
                    GameStatus = GameStatus.Stalemate;
                    GameOverMessage = "ПАТ! Нічия.";
                }

                IsGameOver = true;
                // Тут можна викликати метод збереження гри в майбутньому
            }
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
