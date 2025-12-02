using Microsoft.Maui.Controls.Shapes; // 👈 ОСЬ ЦЕ БУЛО ПРОПУЩЕНО
using ChessTrainerApp.Models;
using ChessTrainerApp.ViewModels;
using System.ComponentModel; // Для PropertyChanged

namespace ChessTrainerApp;

public partial class ChessBoardPage : ContentPage
{
    // Властивості для розмірів (розраховуються автоматично)
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
        // Підписуємося на подію, коли сторінка повністю завантажена
        Loaded += OnPageLoaded;
    }

    // Логіка розрахунку розмірів (Адаптивність)
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        
        // Шукаємо Border з іменем ChessBoardContainer (він має бути в XAML)
        // Якщо ти його перейменував або видалив - тут буде null
        if (ChessBoardContainer == null) return;

        double safeHeight = height - 280; 
        double size = Math.Min(width, safeHeight);
        size -= 20; 
        if (size < 300) size = 300;

        ChessBoardContainer.WidthRequest = size;
        ChessBoardContainer.HeightRequest = size;

        double cellSize = size / 8;
        
        PieceFontSize = cellSize * 0.75; 
        DotSize = cellSize * 0.30; 
        RingSize = cellSize * 0.90;
    }

    private void OnPageLoaded(object sender, EventArgs e)
    {
        if (BindingContext is ChessBoardViewModel vm)
        {
            // Малюємо дошку вперше
            DrawBoard(vm.Squares);

            // Підписуємося на зміни у колекції (нова гра)
            vm.Squares.CollectionChanged += (s, args) => DrawBoard(vm.Squares);
            
            // Підписуємося на зміни властивостей (поворот дошки)
            vm.PropertyChanged += (s, args) => 
            {
                if (args.PropertyName == nameof(ChessBoardViewModel.BoardRotation))
                {
                    UpdateRotation(vm.BoardRotation);
                }
            };
            
            // Застосовуємо початковий поворот
            UpdateRotation(vm.BoardRotation);
        }
    }

    // Генерація дошки кодом (C#) замість XAML BindableLayout
    private void DrawBoard(IList<SquareModel> squares)
    {
        if (BoardGrid == null) return;
        BoardGrid.Children.Clear(); 

        // Беремо актуальний розмір крапки прямо зараз
        double currentDotSize = DotSize > 0 ? DotSize : 15; // Якщо 0, то хоча б 15
        double currentRingSize = RingSize > 0 ? RingSize : 35;

        foreach (var square in squares)
        {
            var cellView = new Grid { BindingContext = square };
            cellView.SetBinding(Grid.BackgroundColorProperty, nameof(SquareModel.SquareColor));
            
            var tap = new TapGestureRecognizer();
            tap.Command = (BindingContext as ChessBoardViewModel)?.HandleSquareClickCommand;
            tap.CommandParameter = square;
            cellView.GestureRecognizers.Add(tap);

            // --- КРАПКА ---
            var dot = new Ellipse
            {
                Fill = Color.FromRgba("#AA000000"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                InputTransparent = true,
                
                // ❗ ВСТАНОВЛЮЄМО РОЗМІР ПРЯМО ТУТ (БЕЗ BINDING)
                WidthRequest = currentDotSize,
                HeightRequest = currentDotSize
            };
            // Тільки видимість прив'язуємо
            dot.SetBinding(Ellipse.IsVisibleProperty, nameof(SquareModel.IsRegularMoveHint));

            // --- КІЛЬЦЕ ---
            var ring = new Ellipse
            {
                Stroke = Color.FromRgba("#AA000000"),
                StrokeThickness = 5,
                Fill = Colors.Transparent,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                InputTransparent = true,
                
                // ❗ РОЗМІР ПРЯМО ТУТ
                WidthRequest = currentRingSize,
                HeightRequest = currentRingSize
            };
            ring.SetBinding(Ellipse.IsVisibleProperty, nameof(SquareModel.IsCaptureHint));

            // --- ФІГУРА ---
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
                    // Крутимо ТІЛЬКИ текст (Label)
                    // Еліпси крутити не треба, вони круглі :)
                    if (item is Label label)
                    {
                        label.Rotation = rotationAngle;
                    }
                }
            }
        }
    }
}