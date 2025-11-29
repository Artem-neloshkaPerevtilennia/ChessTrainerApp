using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ChessTrainerApp.Models;
using CommunityToolkit.Mvvm.Input;

namespace ChessTrainerApp.ViewModels
{
    public partial class ChessBoardViewModel : ObservableObject
    {
        // кольори клітинок
        [Obsolete]
        private readonly Color _lightSquareColor = Color.FromHex("#EEEED2");
        [Obsolete]
        private readonly Color _darkSquareColor = Color.FromHex("#769656");
        [Obsolete]
        private readonly Color _selectedSquareColor = Color.FromHex("#F6F669");

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

        // запис партії, що відображатиметься під дошкою
        [ObservableProperty]
        private string pgnText = "";
        
        // ходи у партії
        public List<string> MoveHistory { get; set; } = new List<string>();

        // клітина позаду пішака, що пішок на 2 клітини
        public SquareModel EnPassantTarget { get; set; }

        [ObservableProperty]
        private PieceColor currentTurn = PieceColor.White;

        private int _aiDepth = 2;

        [ObservableProperty]
        private PieceColor botColor;

        // Історія станів дошки
        private List<GameState> _history = new List<GameState>();
        private int _currentMoveIndex = -1; // -1 означає, що гра ще не почалась

        // Команди для кнопок
        public RelayCommand PreviousMoveCommand { get; }
        public RelayCommand NextMoveCommand { get; }

        // --- У КОНСТРУКТОРІ ---
        public ChessBoardViewModel()
        {
            Squares = new ObservableCollection<SquareModel>();
            
            // Ініціалізація команд
            PreviousMoveCommand = new RelayCommand(GoToPreviousMove);
            NextMoveCommand = new RelayCommand(GoToNextMove);

            InitializeBoard();
        }

        // створення дошки
        private void InitializeBoard()
        {
            Squares.Clear();
            // кольори клітинок
            Color lightSquare = _lightSquareColor;
            Color darkSquare = _darkSquareColor;

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
                        Piece = ChessBoardViewModel.GetInitialPiece(r, c)
                    };
                    Squares.Add(square);
                }
            }

            // Очищаємо історію при новій грі
            _history.Clear();
            _currentMoveIndex = -1;
            PgnText = "";
            MoveHistory.Clear();
            
            // Зберігаємо початковий стан (хід 0)
            SaveState();
        }

        // отримання фігури на клітинці на початку гри
        private static PieceModel GetInitialPiece(int r, int c)
        {
            // Визначаємо колір
            PieceColor color = PieceColor.None;
            if (r == 0 || r == 1) color = PieceColor.Black;
            else if (r == 6 || r == 7) color = PieceColor.White;

            if (color == PieceColor.None) 
                return new PieceModel { Type = PieceType.None, Color = PieceColor.None };

            // Визначаємо тип
            PieceType type = PieceType.None;
            
            if (r == 1 || r == 6) type = PieceType.Pawn;
            else
            {
                // Ряд фігур (0 або 7)
                type = c switch
                {
                    0 or 7 => PieceType.Rook,
                    1 or 6 => PieceType.Knight,
                    2 or 5 => PieceType.Bishop,
                    3 => PieceType.Queen,
                    4 => PieceType.King,
                    _ => PieceType.None
                };
            }

            return new PieceModel { Type = type, Color = color };
        }

        // натискання на клітинку
        [RelayCommand]
        private async Task HandleSquareClick(SquareModel clickedSquare)
        {
            if (GameStatus != GameStatus.InProgress) return;

            // Якщо зараз хід Бота - ігноруємо кліки людини
            if (CurrentTurn == BotColor) return;

            // сценарій 1: вибір фігури
            if (SelectedSquare == null)
            {
                if (clickedSquare.Piece.Type != PieceType.None &&
                    clickedSquare.Piece.Color == CurrentTurn)
                {
                    SelectedSquare = clickedSquare;
                    SelectedSquare.SquareColor = _selectedSquareColor; // виділення клітини фігури, яку обрано
                }
                return;
            }

            // сценарій 2: спроба ходу

            // скидаємо колір виділення
            ResetSquareColor(SelectedSquare);

            // якщо клікнули на свою ж фігуру, то просто перемикаємо вибір
            if (clickedSquare.Piece.Color == CurrentTurn)
            {
                SelectedSquare = clickedSquare;
                SelectedSquare.SquareColor = _selectedSquareColor;
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
            // запис ходу
            RecordMove(from, to);

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
        private async Task SwitchTurn()
        {
            // Змінюємо гравця
            CurrentTurn = CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;

            SaveState();

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

                // збереження гри
                if (isCheck)
                {
                    GameStatus = GameStatus.Checkmate;
                    GameOverMessage = $"МАТ! Перемогли {enemyColor}";
                    
                    // --- ЗБЕРЕЖЕННЯ ---
                    SaveGameResult(enemyColor.ToString()); 
                }
                else
                {
                    GameStatus = GameStatus.Stalemate;
                    GameOverMessage = "ПАТ! Нічия.";
                    
                    // --- ЗБЕРЕЖЕННЯ ---
                    SaveGameResult("Draw");
                }
            }

            // перевірка чи зараз хід бота (поки що просто чорних)
            if (!IsGameOver && CurrentTurn == BotColor)
                await BotTurn();
        }

        private void ResetSquareColor(SquareModel square)
        {
            // Повертаємо оригінальний колір (зелений або кремовий)
            // Тобі треба буде винести логіку визначення кольору в окремий метод або зберігати оригінальний колір
            bool isEven = (square.Row + square.Column) % 2 == 0;
            square.SquareColor = isEven ? _lightSquareColor : _darkSquareColor;
        }
    
        // хід бота
        private async Task BotTurn()
        {
            if (GameStatus != GameStatus.InProgress) return;

            // 1. Пауза для ефекту "думки" (тут UI не блокується)
            await Task.Delay(500);

            // 2. Створюємо копію дошки ТУТ (у головному потоці)
            var boardClone = GetBoardClone();
            
            // Клонуємо ціль En Passant, якщо вона є
            SquareModel enPassantClone = null;
            if (EnPassantTarget != null)
            {
                enPassantClone = boardClone.FirstOrDefault(s => s.Row == EnPassantTarget.Row && s.Column == EnPassantTarget.Column);
            }

            // 3. Запускаємо важкі розрахунки на копії (у фоновому потоці)
            var bestMoveClone = await Task.Run(() => 
            {
                // Передаємо КЛОН. Зміни тут не вплинуть на UI.
                return ChessAI.GetBestMove(boardClone, CurrentTurn, _aiDepth, enPassantClone);
            });

            // 4. Застосовуємо результат на РЕАЛЬНІЙ дошці
            if (bestMoveClone.HasValue)
            {
                // Знаходимо реальні клітинки, що відповідають клонованим
                var realFrom = Squares.First(s => s.Row == bestMoveClone.Value.From.Row && s.Column == bestMoveClone.Value.From.Column);
                var realTo = Squares.First(s => s.Row == bestMoveClone.Value.To.Row && s.Column == bestMoveClone.Value.To.Column);

                MakeMove(realFrom, realTo);
                
                // Авто-перетворення пішака для бота
                if (realTo.Piece.Type == PieceType.Pawn && (realTo.Row == 0 || realTo.Row == 7))
                {
                    realTo.Piece = new PieceModel { Type = PieceType.Queen, Color = CurrentTurn, HasMoved = true };
                }

                SwitchTurn();
            }
            else
            {
                // Пат або Мат (обробить SwitchTurn)
                SwitchTurn(); 
            }
        }
        
        // клонування дошки для розрахунків
        private List<SquareModel> GetBoardClone()
        {
            var clone = new List<SquareModel>();
            
            foreach (var square in Squares)
            {
                clone.Add(new SquareModel
                {
                    Row = square.Row,
                    Column = square.Column,
                    SquareColor = square.SquareColor, // Це не важливо для ШІ, але хай буде
                    Piece = new PieceModel 
                    { 
                        Type = square.Piece.Type, 
                        Color = square.Piece.Color, 
                        HasMoved = square.Piece.HasMoved 
                    }
                });
            }
            return clone;
        }

        // метод запису ходу
        private void RecordMove(SquareModel from, SquareModel to)
        {
            // нотація фігури (пішака прийнято не записувати)
            string pieceNotation = "";
            if (from.Piece.Type != PieceType.Pawn)
            {
                pieceNotation = from.Piece.Type switch
                {
                    PieceType.Knight => "N",
                    PieceType.Bishop => "B",
                    PieceType.Rook => "R",
                    PieceType.Queen => "Q",
                    PieceType.King => "K",
                    _ => ""
                };
            }

            string captureNotation = "";
            if (to.Piece.Type != PieceType.None || (from.Piece.Type == PieceType.Pawn && from.Column != to.Column))
            {
                if (from.Piece.Type == PieceType.Pawn)
                    captureNotation = $"{(char)('a' + from.Column)}x";
                else
                    captureNotation = "x";
            }

            string destination = $"{(char)('a' + to.Column)}{8 - to.Row}";
            string pgnMove = ""; // 

            // Рокировка (спеціальний випадок)
            if (from.Piece.Type == PieceType.King && Math.Abs(to.Column - from.Column) == 2)
            {
                pgnMove = (to.Column > from.Column) ? "O-O" : "O-O-O";
            }
            else
            {
                pgnMove = $"{pieceNotation}{captureNotation}{destination}";
            }
            
            // Якщо зараз ходять білі - додаємо номер ходу
            if (CurrentTurn == PieceColor.White)
            {
                int moveNumber = (MoveHistory.Count / 2) + 1;
                PgnText += $"{moveNumber}. {pgnMove} ";
            }
            // Якщо зараз ходять чорні - просто додаємо хід
            else
            {
                PgnText += $"{pgnMove} ";
            }

            // Додаємо в список (для внутрішньої логіки)
            MoveHistory.Add(pgnMove);
        }
    
        // збереження гри
        private async void SaveGameResult(string winner)
        {
            string pgnString = PgnText; 

            var record = new GameRecord
            {
                DatePlayed = DateTime.Now,
                WhitePlayer = "User",
                BlackPlayer = "Bot",
                Winner = winner,
                PGN = pgnString
            };

            await Services.DatabaseService.AddGameAsync(record);
        }
    
        public void SetupGame(PieceColor playerColor, int depth)
        {
            _aiDepth = depth;
            
            // Якщо гравець Білий -> Бот Чорний. І навпаки.
            BotColor = playerColor == PieceColor.White ? PieceColor.Black : PieceColor.White;

            InitializeBoard();
            
            // Якщо Бот грає за Білих -> він ходить першим
            if (BotColor == PieceColor.White)
            {
                Task.Run(async () => 
                {
                    await Task.Delay(1000);
                    await BotTurn();
                });
            }
        }
    
        public class GameState
        {
            public PieceModel[] BoardSnapshot { get; set; } // Масив фігур
            public PieceColor Turn { get; set; }            // Чий хід
            public SquareModel EnPassantTarget { get; set; } // Ціль для взяття
            public string PgnText { get; set; }             // Текст історії
            public List<string> MoveHistory { get; set; }   // Список ходів
        }
    
        // --- МЕТОДИ НАВІГАЦІЇ ---
        [RelayCommand]
        private void GoToPreviousMove()
        {
            if (_currentMoveIndex > 0)
            {
                _currentMoveIndex--;
                LoadState(_history[_currentMoveIndex]);
            }
        }
        [RelayCommand]
        private void GoToNextMove()
        {
            if (_currentMoveIndex < _history.Count - 1)
            {
                _currentMoveIndex++;
                LoadState(_history[_currentMoveIndex]);
            }
        }

        // --- МЕТОДИ ЗБЕРЕЖЕННЯ/ЗАВАНТАЖЕННЯ ---

        // Викликати цей метод після КОЖНОГО ходу (і на початку гри)
        private void SaveState()
        {
            // 1. Якщо ми повернулися назад і зробили новий хід - видаляємо "майбутнє"
            if (_currentMoveIndex < _history.Count - 1)
            {
                _history.RemoveRange(_currentMoveIndex + 1, _history.Count - (_currentMoveIndex + 1));
            }

            // 2. Створюємо глибоку копію фігур (щоб вони не змінювались за посиланням)
            var piecesCopy = new PieceModel[64];
            for (int i = 0; i < 64; i++)
            {
                var original = Squares[i].Piece;
                piecesCopy[i] = new PieceModel 
                { 
                    Type = original.Type, 
                    Color = original.Color, 
                    HasMoved = original.HasMoved 
                };
            }

            // 3. Зберігаємо стан
            var state = new GameState
            {
                BoardSnapshot = piecesCopy,
                Turn = CurrentTurn,
                EnPassantTarget = EnPassantTarget,
                PgnText = PgnText,
                MoveHistory = new List<string>(MoveHistory) // Копіюємо список
            };

            _history.Add(state);
            _currentMoveIndex++;
        }

        private void LoadState(GameState state)
        {
            // Відновлюємо фігури на дошці
            for (int i = 0; i < 64; i++)
            {
                // Оновлюємо властивості існуючих об'єктів SquareModel
                // Це важливо, щоб UI відреагував (Binding)
                Squares[i].Piece = state.BoardSnapshot[i];
            }

            // Відновлюємо змінні
            CurrentTurn = state.Turn;
            EnPassantTarget = state.EnPassantTarget;
            PgnText = state.PgnText;
            MoveHistory = new List<string>(state.MoveHistory);
            
            // Скидаємо виділення
            if (SelectedSquare != null)
            {
                ResetSquareColor(SelectedSquare);
                SelectedSquare = null;
            }
        }
    }
}
