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

        // Це "примарна" клітинка позаду пішака, який стрибнув на 2 поля
        public SquareModel EnPassantTarget { get; set; }

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
        // У ChessBoardViewModel.cs

        // ❗ Зміни void на async Task
        [RelayCommand]
        private async Task HandleSquareClick(SquareModel clickedSquare)
        {
            // 1. Вибір фігури (Сценарій, коли ще нічого не вибрано)
            if (SelectedSquare == null)
            {
                if (clickedSquare.Piece.Type != PieceType.None && 
                    clickedSquare.Piece.Color == CurrentTurn) 
                {
                    SelectedSquare = clickedSquare;
                    SelectedSquare.SquareColor = Color.FromHex("#F6F669"); // Жовтий виділення
                }
                return; // Виходимо, чекаємо наступного кліку
            }

            // 2. Спроба ходу (Фігура вже вибрана)
            
            // Скидаємо колір виділення
            bool isEven = (SelectedSquare.Row + SelectedSquare.Column) % 2 == 0;
            SelectedSquare.SquareColor = isEven ? Color.FromHex("#EEEED2") : Color.FromHex("#769656");

            // Якщо клікнули на свою ж фігуру -> просто перемикаємо вибір
            if (clickedSquare.Piece.Color == CurrentTurn)
            {
                SelectedSquare = clickedSquare;
                SelectedSquare.SquareColor = Color.FromHex("#F6F669");
                return;
            }

            // --- ГОЛОВНА ЛОГІКА ХОДУ ---
            
            // Передаємо EnPassantTarget у валідатор!
            if (ChessRules.IsMoveValid(SelectedSquare, clickedSquare, Squares, EnPassantTarget))
            {
                // 1. Робимо хід (фізично переміщуємо, обробляємо En Passant)
                MakeMove(SelectedSquare, clickedSquare);

                // 2. ПЕРЕВІРКА НА ПЕРЕТВОРЕННЯ (Promotion)
                // Якщо пішак дійшов до краю (0 для білих, 7 для чорних або навпаки, залежно від орієнтації)
                if (clickedSquare.Piece.Type == PieceType.Pawn && (clickedSquare.Row == 0 || clickedSquare.Row == 7))
                {
                    // ❗ Чекаємо, поки користувач вибере фігуру
                    await PromotePawn(clickedSquare);
                }

                // 3. Тільки ТЕПЕР передаємо хід
                SwitchTurn();
            }

            SelectedSquare = null;
        }
        // У ChessBoardViewModel.cs

        private void MakeMove(SquareModel from, SquareModel to)
        {
            // --- 1. РОКИРОВКА (Castling) ---
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

            // --- 2. EN PASSANT (ВЗЯТТЯ) ---
            // Якщо пішак ходить по діагоналі на порожню клітинку -> це En Passant
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

            // --- 3. ФІЗИЧНИЙ ХІД ---
            to.Piece = from.Piece;
            from.Piece = new PieceModel { Type = PieceType.None, Color = PieceColor.None };
            to.Piece.HasMoved = true;

            // --- 4. ВСТАНОВЛЕННЯ НОВОЇ ЦІЛІ EN PASSANT ---
            // Якщо пішак стрибнув на 2 клітинки -> запам'ятовуємо клітинку за ним
            if (to.Piece.Type == PieceType.Pawn && Math.Abs(to.Row - from.Row) == 2)
            {
                int middleRow = (from.Row + to.Row) / 2;
                EnPassantTarget = Squares[middleRow * 8 + from.Column];
            }
            else
            {
                // Будь-який інший хід скидає можливість En Passant
                EnPassantTarget = null;
            }
            
            // ❗ SwitchTurn тут НЕ ВИКЛИКАЄМО (як ти і зробив), бо ще може бути Promotion
        }

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
