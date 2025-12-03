using Microsoft.Maui.Controls.Shapes;
using ChessTrainerApp.Models;
using ChessTrainerApp.ViewModels;

namespace ChessTrainerApp;

public partial class ChessBoardPage : ContentPage
{
    public static readonly BindableProperty DotSizeProperty =
        BindableProperty.Create(nameof(DotSize), typeof(double), typeof(ChessBoardPage), 10.0);
    public double DotSize
    {
        get => (double)GetValue(DotSizeProperty);
        set => SetValue(DotSizeProperty, value);
    }

    public static readonly BindableProperty RingSizeProperty =
        BindableProperty.Create(nameof(RingSize), typeof(double), typeof(ChessBoardPage), 40.0);
    public double RingSize
    {
        get => (double)GetValue(RingSizeProperty);
        set => SetValue(RingSizeProperty, value);
    }

    public static readonly BindableProperty PieceFontSizeProperty =
        BindableProperty.Create(nameof(PieceFontSize), typeof(double), typeof(ChessBoardPage), 30.0);
    public double PieceFontSize
    {
        get => (double)GetValue(PieceFontSizeProperty);
        set => SetValue(PieceFontSizeProperty, value);
    }

    public ChessBoardPage()
    {
        InitializeComponent();
        Loaded += OnPageLoaded;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        
        if (ChessBoardContainer == null) return;

        // ... (Твій код розрахунку size дошки) ...
        double safeHeight = height - 280; 
        double size = Math.Min(width, safeHeight);
        size -= 20; 
        if (size < 300) size = 300;

        ChessBoardContainer.WidthRequest = size;
        ChessBoardContainer.HeightRequest = size;

        // Розрахунок
        double cellSize = size / 8;
        
        PieceFontSize = cellSize * 0.75; 
        DotSize = cellSize * 0.30; // 30% від клітинки
        RingSize = cellSize * 0.90; // 90% від клітинки

        // 👇 ДОДАЙ ЦЕЙ ВИКЛИК 👇
        // Оновлюємо розміри існуючих кружечків
        UpdateShapesSizes();
    }

    private void OnPageLoaded(object sender, EventArgs e)
    {
        if (BindingContext is ChessBoardViewModel vm)
        {
            DrawBoard(vm.Squares);

            vm.Squares.CollectionChanged += (s, args) => DrawBoard(vm.Squares);
            
            vm.PropertyChanged += (s, args) => 
            {
                if (args.PropertyName == nameof(ChessBoardViewModel.BoardRotation))
                {
                    UpdateRotation(vm.BoardRotation);
                }
            };
            
            UpdateRotation(vm.BoardRotation);
        }
    }

    private void DrawBoard(IList<SquareModel> squares)
    {
        if (BoardGrid == null) return;
        BoardGrid.Children.Clear();

        double pixelDotSize = DotSize > 0 ? DotSize : 15;
        double pixelRingSize = RingSize > 0 ? RingSize : 35;

        foreach (var square in squares)
        {
            var cellView = new Grid
            {
                BindingContext = square,
                Padding = 0,
                Margin = 0,
            };
            
            cellView.SetBinding(Grid.BackgroundColorProperty, nameof(SquareModel.SquareColor));
            
            var tap = new TapGestureRecognizer();
            tap.Command = (BindingContext as ChessBoardViewModel)?.HandleSquareClickCommand;
            tap.CommandParameter = square;
            cellView.GestureRecognizers.Add(tap);

            var dot = new Ellipse
            {
                Fill = Color.FromRgba("rgba(0, 0, 0, 0.5)"), 
                
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                
                InputTransparent = true,

                WidthRequest = pixelDotSize,
                HeightRequest = pixelDotSize,
                
                IsVisible = square.IsRegularMoveHint
            };

            var ring = new Ellipse
            {
                Stroke = Color.FromRgba("rgba(0, 0, 0, 0.5)"),
                StrokeThickness = 5,
                Fill = Colors.Transparent,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                InputTransparent = true,
                WidthRequest = pixelRingSize,
                HeightRequest = pixelRingSize,
                
                IsVisible = square.IsCaptureHint
            };

            square.PropertyChanged += (s, e) => 
            {
                if (e.PropertyName == nameof(SquareModel.IsRegularMoveHint))
                {
                    MainThread.BeginInvokeOnMainThread(() => dot.IsVisible = square.IsRegularMoveHint);
                }
                if (e.PropertyName == nameof(SquareModel.IsCaptureHint))
                {
                    ring.IsVisible = square.IsCaptureHint;
                }
            };

            var label = new Label
            {
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                InputTransparent = true
            };
            
            label.SetBinding(Label.TextProperty, "Piece.DisplayValue");
            if (Resources.TryGetValue("PieceColorConverter", out var converter))
            {
                label.SetBinding(Label.TextColorProperty, new Binding("Piece.Color", converter: converter as IValueConverter));
            }
            label.SetBinding(VisualElement.OpacityProperty, nameof(SquareModel.TextOpacity));
            label.SetBinding(Label.FontSizeProperty, new Binding(nameof(PieceFontSize), source: this));

            cellView.Children.Add(dot);
            cellView.Children.Add(ring);
            cellView.Children.Add(label);

            BoardGrid.Add(cellView, square.Column, square.Row);
        }
        
        if (BindingContext is ChessBoardViewModel vm) UpdateRotation(vm.BoardRotation);
    }
    
    private void UpdateRotation(double rotationAngle)
    {
        if (BoardGrid == null) return;

        foreach (var child in BoardGrid.Children)
        {
            if (child is Grid cell)
            {
                foreach (var item in cell.Children)
                {
                    if (item is Label label)
                    {
                        label.Rotation = rotationAngle;
                    }
                }
            }
        }
    }

    private void UpdateShapesSizes()
    {
        if (BoardGrid == null) return;

        foreach (var child in BoardGrid.Children)
        {
            if (child is Grid cell)
            {
                foreach (var item in cell.Children)
                {
                    if (item is Ellipse shape)
                    {
                        if (shape.Fill is SolidColorBrush brush && brush.Color.Alpha > 0)
                        {
                            shape.WidthRequest = DotSize;
                            shape.HeightRequest = DotSize;
                        }
                        else
                        {
                            shape.WidthRequest = RingSize;
                            shape.HeightRequest = RingSize;
                        }
                    }
                }
            }
        }
    }
}