using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ChessTrainerApp.Models;
using ChessTrainerApp.Services;
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

        // Червоний колір для шаху (не надто яскравий, ближче до "тривожного")
        private readonly Color _checkColor = Color.FromHex("#FF6B6B"); 

        // Зберігаємо клітинку короля, який зараз під шахом
        private SquareModel _kingInCheckSquare;

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

        private int _aiDepth = 3;

        [ObservableProperty]
        private PieceColor botColor;

        // Історія станів дошки
        private List<GameState> _history = new List<GameState>();
        private int _currentMoveIndex = -1; // -1 означає, що гра ще не почалась

        // Команди для кнопок
        public RelayCommand PreviousMoveCommand { get; }
        public RelayCommand NextMoveCommand { get; }

        [ObservableProperty]
        private string topPlayerName = "Ботяра";

        [ObservableProperty]
        private string bottomPlayerName = "Ви";

        [ObservableProperty]
        private string topMaterialAdvantage = "";

        [ObservableProperty]
        private string bottomMaterialAdvantage = "";

        [ObservableProperty]
        private double boardRotation = 0;

        [ObservableProperty]
        private GameMode currentGameMode;

        [ObservableProperty]
        private bool canUndoMove;

        [ObservableProperty]
        private bool isTrainingMode;

        [ObservableProperty]
        private bool isBlindfoldMode;

        // Колір для останнього ходу (схожий на selection, але трохи темніший/інший)
        private readonly Color _lastMoveColor = Color.FromHex("#CED26A"); 

        // Зберігаємо посилання на клітинки останнього ходу
        private SquareModel _lastFromSquare;
        private SquareModel _lastToSquare;

        // Лічильник півходів без взяття/ходу пішака
        private int _halfMoveClock = 0;

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
                        Piece = ChessBoardViewModel.GetInitialPiece(r, c),
                        TextOpacity = IsBlindfoldMode ? 0 : 1
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
            UpdateMaterialBalance();
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
            if (CurrentTurn == BotColor) return;
            if (_currentMoveIndex < _history.Count - 1) return;

            // СЦЕНАРІЙ 1: Вибір фігури
            // Якщо нічого не обрано АБО клікнули на свою іншу фігуру
            if (SelectedSquare == null || (clickedSquare.Piece.Color == CurrentTurn && clickedSquare != SelectedSquare))
            {
                if (clickedSquare.Piece.Type != PieceType.None && clickedSquare.Piece.Color == CurrentTurn)
                {
                    // Знімаємо старе виділення
                    if (SelectedSquare != null) 
                    {
                        var oldSelection = SelectedSquare;
                        SelectedSquare = null;
                        UpdateSquareColor(oldSelection);
                    }
                    
                    ClearPossibleMoves();

                    // Виділяємо нову
                    SelectedSquare = clickedSquare;
                    UpdateSquareColor(SelectedSquare);

                    var moves = ChessRules.GetValidMovesForPiece(SelectedSquare, Squares, EnPassantTarget);
                    foreach (var move in moves)
                    {
                        // Якщо на клітинці хтось є -> це ВЗЯТТЯ (Кільце)
                        // (Або якщо це En Passant - це теж взяття, хоча клітинка пуста, 
                        //  але En Passant Target зазвичай обробляється окремо. 
                        //  Для простоти: якщо клітинка пуста і це не EnPassant, то крапка)
                        Console.WriteLine($"[DEBUG] Підсвічую хід на: {move.PositionName}");
                        if (move.Piece.Type != PieceType.None || move == EnPassantTarget)
                        {
                            move.IsCaptureHint = true;
                        }
                        else
                        {
                            move.IsRegularMoveHint = true;
                        }
                    }
                }
                return;
            }

            // СЦЕНАРІЙ 2: Спроба ходу
            
            // 1. Запам'ятовуємо, хто ходить
            var originSquare = SelectedSquare;

            // 2. Скасування вибору (якщо клікнули на ту саму фігуру)
            if (originSquare == clickedSquare)
            {
                SelectedSquare = null;
                UpdateSquareColor(originSquare);
                ClearPossibleMoves();
                return;
            }

            // 3. Валідація і Хід
            if (ChessRules.IsMoveValid(originSquare, clickedSquare, Squares, EnPassantTarget))
            {
                // ❗ КЛЮЧОВИЙ МОМЕНТ ❗
                // Спочатку прибираємо "Вибір" (Selection), щоб він не перебивав колір "Останнього ходу"
                SelectedSquare = null;
                
                // Оновлюємо колір клітинки, з якої йдемо (вона перестане бути Selected)
                UpdateSquareColor(originSquare); 
                ClearPossibleMoves();

                // Тепер робимо хід (він пофарбує клітинки у LastMoveColor)
                MakeMove(originSquare, clickedSquare);

                if (clickedSquare.Piece.Type == PieceType.Pawn && (clickedSquare.Row == 0 || clickedSquare.Row == 7))
                {
                    await PromotePawn(clickedSquare);
                }

                await SwitchTurn();
            }
            // Якщо хід невалідний - нічого не робимо (можна лишити виділення або скинути)
        }

        // виконання ходу
        private void MakeMove(SquareModel from, SquareModel to)
        {
            // 1. Зберігаємо посилання на старі підсвічені клітинки
            var prevFrom = _lastFromSquare;
            var prevTo = _lastToSquare;

            bool isPawnMove = from.Piece.Type == PieceType.Pawn;
            bool isCapture = to.Piece.Type != PieceType.None;

            // Взяття на проході - це теж взяття
            if (from.Piece.Type == PieceType.Pawn && from.Column != to.Column && to.Piece.Type == PieceType.None)
            {
                isCapture = true;
            }

            if (isPawnMove || isCapture)
            {
                _halfMoveClock = 0; // Скидаємо лічильник (подія незворотна)
            }
            else
            {
                _halfMoveClock++; // Нарощуємо лічильник
            }

            // 2. Оновлюємо глобальні змінні на НОВИЙ хід
            _lastFromSquare = from;
            _lastToSquare = to;

            // 3. "Миємо" старі клітинки
            // Оскільки _lastFromSquare вже змінився, UpdateSquareColor поверне їм звичайний колір
            if (prevFrom != null) UpdateSquareColor(prevFrom);
            if (prevTo != null) UpdateSquareColor(prevTo);

            // --- ЛОГІКА ХОДУ ---
            RecordMove(from, to);

            // Рокировка
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
                
                // Оновлюємо колір тури, щоб прибрати артефакти
                UpdateSquareColor(rookOldSq);
                UpdateSquareColor(rookNewSq);
            }

            // En Passant
            if (from.Piece.Type == PieceType.Pawn && from.Column != to.Column && to.Piece.Type == PieceType.None)
            {
                int enemyPawnRow = from.Row;
                int enemyPawnCol = to.Column;
                var enemyPawnSq = Squares[enemyPawnRow * 8 + enemyPawnCol];
                enemyPawnSq.Piece = new PieceModel { Type = PieceType.None, Color = PieceColor.None };
            }

            // Фізичний хід
            to.Piece = from.Piece;
            from.Piece = new PieceModel { Type = PieceType.None, Color = PieceColor.None };
            to.Piece.HasMoved = true;

            // En Passant Target
            if (to.Piece.Type == PieceType.Pawn && Math.Abs(to.Row - from.Row) == 2)
            {
                int middleRow = (from.Row + to.Row) / 2;
                EnPassantTarget = Squares[middleRow * 8 + from.Column];
            }
            else
            {
                EnPassantTarget = null;
            }

            // 4. Фарбуємо НОВИЙ хід (Тепер SelectedSquare == null, тому спрацює умова LastMove)
            UpdateSquareColor(from);
            UpdateSquareColor(to);
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
            // --- А. Очищаємо попередній шах (перед зміною ходу) ---
            if (_kingInCheckSquare != null)
            {
                var temp = _kingInCheckSquare;
                _kingInCheckSquare = null;
                UpdateSquareColor(temp); // Поверне звичайний колір або колір останнього ходу
            }

            // Змінюємо гравця
            CurrentTurn = CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
            SaveState();

            bool hasMoves = ChessRules.HasAnyLegalMove(CurrentTurn, Squares, EnPassantTarget);

            if (_halfMoveClock >= 100)
            {
                GameStatus = GameStatus.Draw;
                GameOverMessage = "½ - ½\nНічия (Правило 50 ходів)";
                IsGameOver = true;
                SaveGameResult("Draw");
                return; // Виходимо
            }

            // 2. ПЕРЕВІРКА: ТРИРАЗОВЕ ПОВТОРЕННЯ
            string currentHash = GetPositionSignature();
            
            // Рахуємо, скільки разів цей хеш зустрічається в історії
            int repetitionCount = _history.Count(state => state.PositionHash == currentHash);

            // Додаємо +1, бо поточний стан ми могли ще не встигнути додати в історію 
            // (залежить від того, де ти викликаєш SaveState - до чи після перевірки).
            // Якщо SaveState викликається на початку SwitchTurn (як ми робили), то він вже в історії.
            
            if (repetitionCount >= 3)
            {
                GameStatus = GameStatus.Draw;
                GameOverMessage = "½ - ½\nНічия (Триразове повторення)";
                IsGameOver = true;
                SaveGameResult("Draw");
                return;
            }

            // --- Б. Перевірка на Шах ---
            var kingSquare = Squares.FirstOrDefault(s => s.Piece.Type == PieceType.King && s.Piece.Color == CurrentTurn);
            var enemyColor = CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
            
            bool isCheck = ChessRules.IsSquareUnderAttack(kingSquare, enemyColor, Squares);

            if (isCheck)
            {
                // 🚨 ШАХ! Фарбуємо короля в червоний
                _kingInCheckSquare = kingSquare;
                UpdateSquareColor(_kingInCheckSquare);
            }

            if (!hasMoves)
            {
                if (isCheck)
                {
                    GameStatus = GameStatus.Checkmate;
                    GameOverMessage = $"МАТ! Перемогли {enemyColor}";
                    SaveGameResult(enemyColor.ToString()); 
                }
                else
                {
                    GameStatus = GameStatus.Stalemate;
                    GameOverMessage = "ПАТ! Нічия.";
                    SaveGameResult("Draw");
                }
                IsGameOver = true;
            }

            if (!IsGameOver && CurrentTurn == BotColor)
                await BotTurn();
        }

        private void ResetSquareColor(SquareModel square)
        {
            bool isEven = (square.Row + square.Column) % 2 == 0;
            // Використовуй свої змінні кольорів
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
    
        public void SetupGame(PieceColor playerColor, int depth, GameMode mode, bool isBlindfold = false)
        {
            _aiDepth = depth;
            CurrentGameMode = mode;
            IsTrainingMode = (mode == GameMode.Training);
            BotColor = playerColor == PieceColor.White ? PieceColor.Black : PieceColor.White;
            
            // Встановлюємо режим наосліп
            IsBlindfoldMode = isBlindfold; 

            // Поворот дошки
            BoardRotation = playerColor == PieceColor.Black ? 180 : 0;

            InitializeBoard();
            
            if (BotColor == PieceColor.White)
            {
                Task.Run(async () => 
                {
                    await Task.Delay(1000);
                    await BotTurn();
                });
            }
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

            // 2. Створюємо глибоку копію фігур
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
                
                // 👇 ДЛЯ ПРАВИЛ НІЧИЄЇ
                HalfMoveClock = _halfMoveClock,
                PositionHash = GetPositionSignature(), 

                // 👇 ДЛЯ PGN (Текстова версія)
                PgnText = PgnText,
                MoveHistory = new List<string>(MoveHistory),

                // 👇 ДЛЯ ПІДСВІТКИ (Жовтий колір)
                LastFromIndex = _lastFromSquare != null ? (_lastFromSquare.Row * 8 + _lastFromSquare.Column) : -1,
                LastToIndex = _lastToSquare != null ? (_lastToSquare.Row * 8 + _lastToSquare.Column) : -1
            };

            _history.Add(state);
            _currentMoveIndex++;
        }

        private void LoadState(GameState state)
        {
            // 1. ВІДНОВЛЕННЯ ФІГУР
            for (int i = 0; i < 64; i++)
            {
                Squares[i].Piece = state.BoardSnapshot[i];
            }

            // 2. ВІДНОВЛЕННЯ ЗМІННИХ
            CurrentTurn = state.Turn;
            EnPassantTarget = state.EnPassantTarget;
            _halfMoveClock = state.HalfMoveClock;

            // 3. 🧹 ОЧИЩЕННЯ СТАРОЇ ПІДСВІТКИ (Критично важливо!)
            // Ми скидаємо колір тим клітинкам, які були жовтими СЕКУНДУ ТОМУ
            if (_lastFromSquare != null) ResetSquareColor(_lastFromSquare);
            if (_lastToSquare != null) ResetSquareColor(_lastToSquare);
            if (_kingInCheckSquare != null) ResetSquareColor(_kingInCheckSquare);

            // 4. ВІДНОВЛЕННЯ ПІДСВІТКИ З ІСТОРІЇ
            if (state.LastFromIndex != -1 && state.LastToIndex != -1)
            {
                // Знаходимо клітинки за збереженими індексами
                _lastFromSquare = Squares[state.LastFromIndex];
                _lastToSquare = Squares[state.LastToIndex];

                // Фарбуємо їх у жовтий
                UpdateSquareColor(_lastFromSquare);
                UpdateSquareColor(_lastToSquare);
            }
            else
            {
                // Це початок гри, підсвітки немає
                _lastFromSquare = null;
                _lastToSquare = null;
            }

            // Відновлення червоного короля (якщо в цьому стані був шах)
            var kingSquare = Squares.FirstOrDefault(s => s.Piece.Type == PieceType.King && s.Piece.Color == CurrentTurn);
            var enemyColor = CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
            if (kingSquare != null && ChessRules.IsSquareUnderAttack(kingSquare, enemyColor, Squares))
            {
                _kingInCheckSquare = kingSquare;
                UpdateSquareColor(_kingInCheckSquare);
            }
            else
            {
                _kingInCheckSquare = null;
            }

            // 5. Скидання виділення курсора
            if (SelectedSquare != null)
            {
                ResetSquareColor(SelectedSquare);
                SelectedSquare = null;
                ClearPossibleMoves();
            }
            
            // Оновлення матеріалу
            UpdateMaterialBalance();
        }
        
        // вимикаємо всі зелені кружечки
        private void ClearPossibleMoves()
        {
            foreach (var square in Squares)
            {
                square.IsRegularMoveHint = false;
                square.IsCaptureHint = false;
            }
        }
    
        // У ChessBoardViewModel.cs
        private void UpdateMaterialBalance()
        {
            int whiteScore = 0;
            int blackScore = 0;

            foreach (var square in Squares)
            {
                if (square.Piece.Type == PieceType.None || square.Piece.Type == PieceType.King) continue;

                int value = square.Piece.Type switch
                {
                    PieceType.Pawn => 1,
                    PieceType.Knight => 3,
                    PieceType.Bishop => 3,
                    PieceType.Rook => 5,
                    PieceType.Queen => 9,
                    _ => 0
                };

                if (square.Piece.Color == PieceColor.White)
                    whiteScore += value;
                else
                    blackScore += value;
            }

            int diff = whiteScore - blackScore;

            // Скидаємо значення
            TopMaterialAdvantage = "";
            BottomMaterialAdvantage = "";

            if (diff == 0) return; // Матеріал рівний

            // Логіка: Хто є хто?
            // BotColor - це колір БОТА (Верхній гравець)
            // PlayerColor - це колір ЛЮДИНИ (Нижній гравець)
            
            // Якщо БІЛІ мають перевагу (diff > 0)
            if (diff > 0)
            {
                if (BotColor == PieceColor.White) 
                    TopMaterialAdvantage = $"+{diff}"; // Бот білий, він веде
                else 
                    BottomMaterialAdvantage = $"+{diff}"; // Ти білий, ти ведеш
            }
            // Якщо ЧОРНІ мають перевагу (diff < 0)
            else 
            {
                int absDiff = Math.Abs(diff);
                if (BotColor == PieceColor.Black) 
                    TopMaterialAdvantage = $"+{absDiff}"; // Бот чорний, він веде
                else 
                    BottomMaterialAdvantage = $"+{absDiff}"; // Ти чорний, ти ведеш
            }
        }

        // здача партії
        [RelayCommand]
        private void ResignGame()
        {
            // Якщо гра вже завершена або це режим перегляду - нічого не робимо
            if (GameStatus != GameStatus.InProgress) return;

            // Гравець здається -> Переміг Бот
            var winnerColor = BotColor; 

            // Встановлюємо статус
            GameStatus = GameStatus.Checkmate; // Технічно це поразка
            GameOverMessage = "🏳️ Ви здалися!";
            IsGameOver = true;

            // Зберігаємо результат у базу
            SaveGameResult(winnerColor.ToString());
        }
    
        [RelayCommand]
        private void UndoLastMove()
        {
            if (CurrentGameMode != GameMode.Training) return;
            if (CurrentTurn == BotColor) return; 

            int statesToRemove = (BotColor != PieceColor.None) ? 2 : 1;

            if (_history.Count <= statesToRemove) return;

            // 1. ВІЗУАЛЬНА ЧИСТКА
            if (_lastFromSquare != null) ResetSquareColor(_lastFromSquare);
            if (_lastToSquare != null) ResetSquareColor(_lastToSquare);
            if (SelectedSquare != null) 
            {
                ResetSquareColor(SelectedSquare);
                SelectedSquare = null;
            }
            ClearPossibleMoves();

            // 2. ВИДАЛЕННЯ З ІСТОРІЇ
            for (int i = 0; i < statesToRemove; i++)
            {
                // Видаляємо стан гри
                _history.RemoveAt(_history.Count - 1);
                
                // 👇 ВИПРАВЛЕННЯ ТУТ: Працюємо з MoveHistory замість PgnMoves
                if (MoveHistory.Count > 0)
                {
                    MoveHistory.RemoveAt(MoveHistory.Count - 1);
                }
            }

            // Оновлюємо текст на екрані
            RebuildPgnText(); 

            // 3. ЗАВАНТАЖЕННЯ СТАРОГО СТАНУ
            _currentMoveIndex = _history.Count - 1;
            LoadState(_history[_currentMoveIndex]);

            IsGameOver = false;
            GameStatus = GameStatus.InProgress;
        }

        private void RebuildPgnText()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < MoveHistory.Count; i++)
            {
                if (i % 2 == 0)
                    sb.Append($"{(i / 2) + 1}. {MoveHistory[i]} ");
                else
                    sb.Append($"{MoveHistory[i]} ");
            }
            PgnText = sb.ToString();
        }

        private void UpdateSquareColor(SquareModel square)
        {
            // 1. ПРІОРИТЕТ: ШАХ (Найважливіше!)
            if (square == _kingInCheckSquare)
            {
                square.SquareColor = _checkColor;
            }
            // 2. Пріоритет: Вибрана фігура
            else if (SelectedSquare == square)
            {
                square.SquareColor = _selectedSquareColor;
            }
            // 3. Пріоритет: Останній хід
            else if (square == _lastFromSquare || square == _lastToSquare)
            {
                square.SquareColor = _lastMoveColor;
            }
            // 4. Стандарт
            else
            {
                bool isEven = (square.Row + square.Column) % 2 == 0;
                square.SquareColor = isEven ? _lightSquareColor : _darkSquareColor;
            }
        }

        [RelayCommand]
        private async Task GoToSetup()
        {
            // Оскільки PlayPage викликав PushAsync(new ChessBoardPage()),
            // то PlayPage зараз "під нами" в стеку.
            // Ми просто закриваємо поточну сторінку, і користувач опиняється в меню налаштувань.
            
            await Shell.Current.Navigation.PopAsync();
            
            // Альтернатива (якщо ти хочеш примусово відкрити нову PlayPage):
            // await Shell.Current.Navigation.PushAsync(new PlayPage());
            // Але PopAsync - це правильний і "чистий" спосіб для кнопки "Назад/Меню".
        }
    
        private string GetPositionSignature()
        {
            var sb = new System.Text.StringBuilder();

            // 1. Розташування фігур
            foreach (var square in Squares)
            {
                if (square.Piece.Type == PieceType.None) 
                    sb.Append("1"); // Пусто
                else 
                    sb.Append(square.Piece.Symbol); // Наприклад "WP" (White Pawn)
            }

            // 2. Чий хід
            sb.Append($"|{CurrentTurn}");

            // 3. Можливості рокіровки (Важливо! Якщо король ходив - це інша позиція)
            // Просто перевіримо, чи рухалися Королі та Тури
            var whiteKing = Squares.FirstOrDefault(s => s.Piece.Type == PieceType.King && s.Piece.Color == PieceColor.White);
            var blackKing = Squares.FirstOrDefault(s => s.Piece.Type == PieceType.King && s.Piece.Color == PieceColor.Black);
            
            sb.Append($"|WK:{whiteKing?.Piece.HasMoved ?? true}");
            sb.Append($"|BK:{blackKing?.Piece.HasMoved ?? true}");
            
            // (Можна додати тури, але для курсової короля зазвичай достатньо)

            // 4. En Passant (це теж впливає на унікальність)
            if (EnPassantTarget != null)
                sb.Append($"|EP:{EnPassantTarget.PositionName}");

            return sb.ToString();
        }
    }
}
