using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ChessTrainerApp.Models;
using ChessTrainerApp.Services;
using CommunityToolkit.Mvvm.Input;

namespace ChessTrainerApp.ViewModels
{
    public partial class ChessBoardViewModel : ObservableObject
    {
        [Obsolete]
        private readonly Color _lightSquareColor = Color.FromHex("#EEEED2");
        [Obsolete]
        private readonly Color _darkSquareColor = Color.FromHex("#769656");
        [Obsolete]
        private readonly Color _selectedSquareColor = Color.FromHex("#F6F669");
        [Obsolete]
        private readonly Color _checkColor = Color.FromHex("#FF6B6B");
        [Obsolete]
        private readonly Color _lastMoveColor = Color.FromHex("#CED26A"); 

        private SquareModel _kingInCheckSquare;

        public ObservableCollection<SquareModel> Squares { get; }

        [ObservableProperty]
        private SquareModel selectedSquare;

        [ObservableProperty]
        private GameStatus gameStatus = GameStatus.InProgress;

        [ObservableProperty]
        private string gameOverMessage = "";

        [ObservableProperty]
        private bool isGameOver = false;

        public ObservableCollection<PgnMoveModel> PgnList { get; } = new();
        
        public List<string> MoveHistory { get; set; } = new List<string>();

        public SquareModel EnPassantTarget { get; set; }

        [ObservableProperty]
        private PieceColor currentTurn = PieceColor.White;

        private int _aiDepth = 3;

        [ObservableProperty]
        private PieceColor botColor;

        private List<GameState> _history = new List<GameState>();
        private int _currentMoveIndex = -1;

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

        private SquareModel _lastFromSquare;
        private SquareModel _lastToSquare;

        private int _halfMoveClock = 0;

        [Obsolete]
        public ChessBoardViewModel()
        {
            Squares = new ObservableCollection<SquareModel>();
            
            PreviousMoveCommand = new RelayCommand(GoToPreviousMove);
            NextMoveCommand = new RelayCommand(GoToNextMove);

            InitializeBoard();
        }

        [Obsolete]
        private void InitializeBoard()
        {
            Squares.Clear();
            Color lightSquare = _lightSquareColor;
            Color darkSquare = _darkSquareColor;

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

            _history.Clear();
            _currentMoveIndex = -1;
            RefreshPgnVisuals();
            MoveHistory.Clear();
            
            SaveState();
            UpdateMaterialBalance();
        }

        private static PieceModel GetInitialPiece(int r, int c)
        {
            PieceColor color = PieceColor.None;
            if (r == 0 || r == 1) color = PieceColor.Black;
            else if (r == 6 || r == 7) color = PieceColor.White;

            if (color == PieceColor.None) 
                return new PieceModel { Type = PieceType.None, Color = PieceColor.None };

            PieceType type = PieceType.None;
            
            if (r == 1 || r == 6) type = PieceType.Pawn;
            else
            {
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

        [RelayCommand]
        [Obsolete]
        private async Task HandleSquareClick(SquareModel clickedSquare)
        {
            if (GameStatus != GameStatus.InProgress) return;
            if (CurrentTurn == BotColor) return;
            if (_currentMoveIndex < _history.Count - 1) return;

            if (SelectedSquare == null || (clickedSquare.Piece!.Color == CurrentTurn && clickedSquare != SelectedSquare))
            {
                if (clickedSquare.Piece!.Type != PieceType.None && clickedSquare.Piece.Color == CurrentTurn)
                {
                    if (SelectedSquare != null) 
                    {
                        var oldSelection = SelectedSquare;
                        SelectedSquare = null!;
                        UpdateSquareColor(oldSelection);
                    }
                    
                    ClearPossibleMoves();

                    SelectedSquare = clickedSquare;
                    UpdateSquareColor(SelectedSquare);

                    var moves = ChessRules.GetValidMovesForPiece(SelectedSquare, Squares, EnPassantTarget);
                    foreach (var move in moves)
                    {
                        Console.WriteLine($"[DEBUG] Підсвічую хід на: {move.PositionName}");
                        if (move.Piece!.Type != PieceType.None || move == EnPassantTarget)
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
            
            var originSquare = SelectedSquare;

            if (originSquare == clickedSquare)
            {
                SelectedSquare = null!;
                UpdateSquareColor(originSquare);
                ClearPossibleMoves();
                return;
            }

            if (ChessRules.IsMoveValid(originSquare, clickedSquare, Squares, EnPassantTarget))
            {
                SelectedSquare = null!;
                
                UpdateSquareColor(originSquare); 
                ClearPossibleMoves();

                MakeMove(originSquare, clickedSquare);

                if (clickedSquare.Piece.Type == PieceType.Pawn && (clickedSquare.Row == 0 || clickedSquare.Row == 7))
                {
                    await PromotePawn(clickedSquare);
                }

                await SwitchTurn();
            }
        }

        [Obsolete]
        private void MakeMove(SquareModel from, SquareModel to)
        {
            var prevFrom = _lastFromSquare;
            var prevTo = _lastToSquare;

            bool isPawnMove = from.Piece!.Type == PieceType.Pawn;
            bool isCapture = to.Piece!.Type != PieceType.None;

            if (from.Piece.Type == PieceType.Pawn && from.Column != to.Column && to.Piece.Type == PieceType.None)
            {
                isCapture = true;
            }

            if (isPawnMove || isCapture)
            {
                _halfMoveClock = 0;
            }
            else
            {
                _halfMoveClock++;
            }

            _lastFromSquare = from;
            _lastToSquare = to;

            if (prevFrom != null) UpdateSquareColor(prevFrom);
            if (prevTo != null) UpdateSquareColor(prevTo);

            RecordMove(from, to);

            if (from.Piece.Type == PieceType.King && Math.Abs(to.Column - from.Column) == 2)
            {
                int direction = to.Column - from.Column > 0 ? 1 : -1;
                int rookOldCol = direction == 1 ? 7 : 0;
                int rookNewCol = direction == 1 ? 5 : 3;
                var rookOldSq = Squares[from.Row * 8 + rookOldCol];
                var rookNewSq = Squares[from.Row * 8 + rookNewCol];
                
                rookNewSq.Piece = rookOldSq.Piece;
                rookOldSq.Piece = new PieceModel { Type = PieceType.None, Color = PieceColor.None };
                rookNewSq.Piece!.HasMoved = true;
                
                UpdateSquareColor(rookOldSq);
                UpdateSquareColor(rookNewSq);
            }

            if (from.Piece.Type == PieceType.Pawn && from.Column != to.Column && to.Piece.Type == PieceType.None)
            {
                int enemyPawnRow = from.Row;
                int enemyPawnCol = to.Column;
                var enemyPawnSq = Squares[enemyPawnRow * 8 + enemyPawnCol];
                enemyPawnSq.Piece = new PieceModel { Type = PieceType.None, Color = PieceColor.None };
            }

            to.Piece = from.Piece;
            from.Piece = new PieceModel { Type = PieceType.None, Color = PieceColor.None };
            to.Piece.HasMoved = true;

            if (to.Piece.Type == PieceType.Pawn && Math.Abs(to.Row - from.Row) == 2)
            {
                int middleRow = (from.Row + to.Row) / 2;
                EnPassantTarget = Squares[middleRow * 8 + from.Column];
            }
            else
            {
                EnPassantTarget = null!;
            }

            UpdateSquareColor(from);
            UpdateSquareColor(to);
        }

        private async Task PromotePawn(SquareModel square)
        {
            string result = await Shell.Current.DisplayActionSheet(
                "Оберіть фігуру:", null, null,
                "Ферзь", "Тура", "Слон", "Кінь");

            if (result == null) result = "Ферзь";

            PieceType newType = result switch
            {
                "Ферзь" => PieceType.Queen,
                "Тура" => PieceType.Rook,
                "Слон" => PieceType.Bishop,
                "Кінь" => PieceType.Knight,
                _ => PieceType.Queen
            };

            square.Piece = new PieceModel
            {
                Type = newType,
                Color = square.Piece!.Color,
                HasMoved = true
            };
        }

        [Obsolete]
        private async Task SwitchTurn()
        {
            if (_kingInCheckSquare != null)
            {
                var temp = _kingInCheckSquare;
                _kingInCheckSquare = null!;
                UpdateSquareColor(temp);
            }

            CurrentTurn = CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
            SaveState();
            RefreshPgnVisuals();

            bool hasMoves = ChessRules.HasAnyLegalMove(CurrentTurn, Squares, EnPassantTarget);

            if (_halfMoveClock >= 100)
            {
                GameStatus = GameStatus.Draw;
                GameOverMessage = "½ - ½\nНічия (Правило 50 ходів)";
                IsGameOver = true;
                SaveGameResult("Draw");
                return;
            }

            string currentHash = GetPositionSignature();
            
            int repetitionCount = _history.Count(state => state.PositionHash == currentHash);

            if (repetitionCount >= 3)
            {
                GameStatus = GameStatus.Draw;
                GameOverMessage = "½ - ½\nНічия (Триразове повторення)";
                IsGameOver = true;
                SaveGameResult("Draw");
                return;
            }

            var kingSquare = Squares.FirstOrDefault(s => s.Piece!.Type == PieceType.King && s.Piece.Color == CurrentTurn);
            var enemyColor = CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
            
            bool isCheck = ChessRules.IsSquareUnderAttack(kingSquare, enemyColor, Squares);

            if (isCheck)
            {
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

        [Obsolete]
        private void ResetSquareColor(SquareModel square)
        {
            bool isEven = (square.Row + square.Column) % 2 == 0;
            square.SquareColor = isEven ? _lightSquareColor : _darkSquareColor;
        }

        [Obsolete]
        private async Task BotTurn()
        {
            if (GameStatus != GameStatus.InProgress) return;

            await Task.Delay(500);

            var boardClone = GetBoardClone();
            
            SquareModel enPassantClone = null;
            if (EnPassantTarget != null)
            {
                enPassantClone = boardClone.FirstOrDefault(s => s.Row == EnPassantTarget.Row && s.Column == EnPassantTarget.Column)!;
            }

            var bestMoveClone = await Task.Run(() => 
            {
                return ChessAI.GetBestMove(boardClone, CurrentTurn, _aiDepth, enPassantClone);
            });

            if (bestMoveClone.HasValue)
            {
                var realFrom = Squares.First(s => s.Row == bestMoveClone.Value.From.Row && s.Column == bestMoveClone.Value.From.Column);
                var realTo = Squares.First(s => s.Row == bestMoveClone.Value.To.Row && s.Column == bestMoveClone.Value.To.Column);

                MakeMove(realFrom, realTo);
                
                if (realTo.Piece!.Type == PieceType.Pawn && (realTo.Row == 0 || realTo.Row == 7))
                {
                    realTo.Piece = new PieceModel { Type = PieceType.Queen, Color = CurrentTurn, HasMoved = true };
                }

                _ = SwitchTurn();
            }
            else
            {
                _ = SwitchTurn(); 
            }
        }
        
        private List<SquareModel> GetBoardClone()
        {
            var clone = new List<SquareModel>();
            
            foreach (var square in Squares)
            {
                clone.Add(new SquareModel
                {
                    Row = square.Row,
                    Column = square.Column,
                    //SquareColor = square.SquareColor,
                    Piece = new PieceModel 
                    { 
                        Type = square.Piece!.Type, 
                        Color = square.Piece.Color, 
                        HasMoved = square.Piece.HasMoved 
                    }
                });
            }
            return clone;
        }

        private void RecordMove(SquareModel from, SquareModel to)
        {
            string pieceNotation = "";
            if (from.Piece!.Type != PieceType.Pawn)
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
            if (to.Piece!.Type != PieceType.None || (from.Piece.Type == PieceType.Pawn && from.Column != to.Column))
            {
                if (from.Piece.Type == PieceType.Pawn)
                    captureNotation = $"{(char)('a' + from.Column)}x";
                else
                    captureNotation = "x";
            }

            string destination = $"{(char)('a' + to.Column)}{8 - to.Row}";
            string pgnMove = "";

            if (from.Piece.Type == PieceType.King && Math.Abs(to.Column - from.Column) == 2)
            {
                pgnMove = (to.Column > from.Column) ? "O-O" : "O-O-O";
            }
            else
            {
                pgnMove = $"{pieceNotation}{captureNotation}{destination}";
            }
            
            if (_currentMoveIndex < MoveHistory.Count - 1)
            {
                MoveHistory.RemoveRange(_currentMoveIndex + 1, MoveHistory.Count - (_currentMoveIndex + 1));
            }

            MoveHistory.Add(pgnMove);
            
            RefreshPgnVisuals();
        }
    
        private async void SaveGameResult(string winner)
        {
            string pgnString = string.Join(" ", PgnList.Select(m => m.Text));; 

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

        [Obsolete]
        public void SetupGame(PieceColor playerColor, int depth, GameMode mode, bool isBlindfold = false)
        {
            _aiDepth = depth;
            CurrentGameMode = mode;
            IsTrainingMode = (mode == GameMode.Training);
            BotColor = playerColor == PieceColor.White ? PieceColor.Black : PieceColor.White;
            
            IsBlindfoldMode = isBlindfold; 

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
    
        [RelayCommand]
        [Obsolete]
        private void GoToPreviousMove()
        {
            if (_currentMoveIndex > 0)
            {
                _currentMoveIndex--;
                LoadState(_history[_currentMoveIndex]);
                RefreshPgnVisuals();
            }
        }
        
        [RelayCommand]
        [Obsolete]
        private void GoToNextMove()
        {
            if (_currentMoveIndex < _history.Count - 1)
            {
                _currentMoveIndex++;
                LoadState(_history[_currentMoveIndex]);
                RefreshPgnVisuals();
            }
        }

        private void SaveState()
        {
            if (_currentMoveIndex < _history.Count - 1)
            {
                _history.RemoveRange(_currentMoveIndex + 1, _history.Count - (_currentMoveIndex + 1));
            }

            var piecesCopy = new PieceModel[64];
            for (int i = 0; i < 64; i++)
            {
                var original = Squares[i].Piece;
                piecesCopy[i] = new PieceModel 
                { 
                    Type = original!.Type, 
                    Color = original.Color, 
                    HasMoved = original.HasMoved 
                };
            }

            var state = new GameState
            {
                BoardSnapshot = piecesCopy,
                Turn = CurrentTurn,
                EnPassantTarget = EnPassantTarget,
                
                HalfMoveClock = _halfMoveClock,
                PositionHash = GetPositionSignature(), 

                MoveHistory = new List<string>(MoveHistory),

                LastFromIndex = _lastFromSquare != null ? (_lastFromSquare.Row * 8 + _lastFromSquare.Column) : -1,
                LastToIndex = _lastToSquare != null ? (_lastToSquare.Row * 8 + _lastToSquare.Column) : -1
            };

            _history.Add(state);
            _currentMoveIndex++;
        }

        [Obsolete]
        private void LoadState(GameState state)
        {
            for (int i = 0; i < 64; i++)
            {
                Squares[i].Piece = state.BoardSnapshot![i];
            }

            CurrentTurn = state.Turn;
            EnPassantTarget = state.EnPassantTarget!;
            _halfMoveClock = state.HalfMoveClock;

            if (_lastFromSquare != null) ResetSquareColor(_lastFromSquare);
            if (_lastToSquare != null) ResetSquareColor(_lastToSquare);
            if (_kingInCheckSquare != null) ResetSquareColor(_kingInCheckSquare);

            if (state.LastFromIndex != -1 && state.LastToIndex != -1)
            {
                _lastFromSquare = Squares[state.LastFromIndex];
                _lastToSquare = Squares[state.LastToIndex];

                UpdateSquareColor(_lastFromSquare);
                UpdateSquareColor(_lastToSquare);
            }
            else
            {
                _lastFromSquare = null!;
                _lastToSquare = null!;
            }

            var kingSquare = Squares.FirstOrDefault(s => s.Piece!.Type == PieceType.King && s.Piece.Color == CurrentTurn);
            var enemyColor = CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
            if (kingSquare != null && ChessRules.IsSquareUnderAttack(kingSquare, enemyColor, Squares))
            {
                _kingInCheckSquare = kingSquare;
                UpdateSquareColor(_kingInCheckSquare);
            }
            else
            {
                _kingInCheckSquare = null!;
            }

            if (SelectedSquare != null)
            {
                ResetSquareColor(SelectedSquare);
                SelectedSquare = null!;
                ClearPossibleMoves();
            }
            
            UpdateMaterialBalance();
            RefreshPgnVisuals();
        }

        private void ClearPossibleMoves()
        {
            foreach (var square in Squares)
            {
                square.IsRegularMoveHint = false;
                square.IsCaptureHint = false;
            }
        }
    
        private void UpdateMaterialBalance()
        {
            int whiteScore = 0;
            int blackScore = 0;

            foreach (var square in Squares)
            {
                if (square.Piece!.Type == PieceType.None || square.Piece.Type == PieceType.King) continue;

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

            TopMaterialAdvantage = "";
            BottomMaterialAdvantage = "";

            if (diff == 0) return;

            if (diff > 0)
            {
                if (BotColor == PieceColor.White) 
                    TopMaterialAdvantage = $"+{diff}";
                else 
                    BottomMaterialAdvantage = $"+{diff}";
            }
            else 
            {
                int absDiff = Math.Abs(diff);
                if (BotColor == PieceColor.Black) 
                    TopMaterialAdvantage = $"+{absDiff}";
                else 
                    BottomMaterialAdvantage = $"+{absDiff}";
            }
        }

        [RelayCommand]
        private void ResignGame()
        {
            if (GameStatus != GameStatus.InProgress) return;

            var winnerColor = BotColor; 

            GameStatus = GameStatus.Checkmate;
            GameOverMessage = "🏳️ Ви здалися!";
            IsGameOver = true;

            SaveGameResult(winnerColor.ToString());
        }
    
        [RelayCommand]
        [Obsolete]
        private void UndoLastMove()
        {
            if (CurrentGameMode != GameMode.Training) return;
            if (CurrentTurn == BotColor) return; 

            int statesToRemove = (BotColor != PieceColor.None) ? 2 : 1;

            if (_history.Count < statesToRemove) return;

            if (_lastFromSquare != null) ResetSquareColor(_lastFromSquare);
            if (_lastToSquare != null) ResetSquareColor(_lastToSquare);
            if (SelectedSquare != null) 
            {
                ResetSquareColor(SelectedSquare);
                SelectedSquare = null!;
            }
            ClearPossibleMoves();

            for (int i = 0; i < statesToRemove; i++)
            {
                _history.RemoveAt(_history.Count - 1);
                
                if (MoveHistory.Count > 0)
                {
                    MoveHistory.RemoveAt(MoveHistory.Count - 1);
                }
            }

            _currentMoveIndex = _history.Count - 1;
            LoadState(_history[_currentMoveIndex]);

            IsGameOver = false;
            GameStatus = GameStatus.InProgress;

            RefreshPgnVisuals();
        }

        [Obsolete]
        private void UpdateSquareColor(SquareModel square)
        {
            if (square == _kingInCheckSquare)
            {
                square.SquareColor = _checkColor;
            }
            else if (SelectedSquare == square)
            {
                square.SquareColor = _selectedSquareColor;
            }
            else if (square == _lastFromSquare || square == _lastToSquare)
            {
                square.SquareColor = _lastMoveColor;
            }
            else
            {
                bool isEven = (square.Row + square.Column) % 2 == 0;
                square.SquareColor = isEven ? _lightSquareColor : _darkSquareColor;
            }
        }

        [RelayCommand]
        private async Task GoToSetup()
        {
            await Shell.Current.Navigation.PopAsync();
        }
    
        private string GetPositionSignature()
        {
            var sb = new System.Text.StringBuilder();

            foreach (var square in Squares)
            {
                if (square.Piece!.Type == PieceType.None) 
                    sb.Append("1");
                else 
                    sb.Append(square.Piece.Symbol);
            }

            sb.Append($"|{CurrentTurn}");

            var whiteKing = Squares.FirstOrDefault(s => s.Piece!.Type == PieceType.King && s.Piece.Color == PieceColor.White);
            var blackKing = Squares.FirstOrDefault(s => s.Piece!.Type == PieceType.King && s.Piece.Color == PieceColor.Black);
            
            sb.Append($"|WK:{whiteKing?.Piece!.HasMoved ?? true}");
            sb.Append($"|BK:{blackKing?.Piece!.HasMoved ?? true}");
            
            if (EnPassantTarget != null)
                sb.Append($"|EP:{EnPassantTarget.PositionName}");

            return sb.ToString();
        }
    
        private void RefreshPgnVisuals()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                PgnList.Clear();

                for (int i = 0; i < MoveHistory.Count; i++)
                {
                    string moveText = MoveHistory[i];
                    string displayText;

                    if (i % 2 == 0) displayText = $"{(i / 2) + 1}. {moveText}";
                    else displayText = moveText;

                    var item = new PgnMoveModel
                    {
                        Text = displayText,
                        IsSelected = (i == _currentMoveIndex - 1) 
                    };

                    PgnList.Add(item);
                }
            });
        }
    }
}
