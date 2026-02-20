using System.Diagnostics;
using System.DirectoryServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WpfApp1;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const int Columns                   = 50;
    private const int Rows                      = 30;
    private const int SquareSize                = 30;
    private const int MarginOffset              = 0;

    private Rectangle[][] Grid                  = null!;
    private bool[][] GridBoolCurrent            = null!;
    private bool[][] GridBoolNext               = null!;

    private readonly TimeSpan DrawInterwalMS    = TimeSpan.FromMilliseconds(200L);

    private readonly SolidColorBrush Alive      = Brushes.LightBlue;
    private readonly SolidColorBrush Dead       = Brushes.DarkBlue;
    private readonly Random Random              = new Random(1997);

    private bool _isRunning;

    private readonly DispatcherTimer _loopClock;

    public MainWindow()
    {
        InitializeComponent();
        InitGrid();
        _isRunning = false;
        _loopClock = new DispatcherTimer
        {
            Interval = DrawInterwalMS
        };
        _loopClock.Tick += OnTick;
    }

    private void InitGrid() 
    {
        Grid = new Rectangle[Rows][];
        GridBoolCurrent = new bool[Rows][];
        GridBoolNext    = new bool[Rows][];
        for (int i = 0; i < Rows; i++)
        {
            Rectangle[] row = new Rectangle[Columns];
            bool[] rowBool = new bool[Columns];
            for (int j = 0; j < Columns; j++)
            {
                double x, y;
                x = 1D * (i * SquareSize);
                y = 1D * (j * SquareSize);
                int randomValue = Random.Next(0, 101);
                bool state = false; // randomValue < 1000;

                Rectangle rect = new Rectangle
                {
                    Width = SquareSize,
                    Height = SquareSize,
                    ClipToBounds = false,
                    Stroke = Brushes.Black,
                    Fill = state ? Alive : Dead,
                };
                row[j] = rect;
                rowBool[j] = state;
            }
            Grid[i] = row;
            GridBoolCurrent[i] = rowBool;
            GridBoolNext[i] = new bool[Columns];            
        }
    }

    private void OnInitClick(object sender, RoutedEventArgs e)
    {
        CanvasComponent.Children.Clear();
        int i = 0, j;
        foreach (Rectangle[] row in Grid)
        {
            j = 0;
            foreach (Rectangle rec in row)
            {
                CanvasComponent.Children.Add(rec);
                Canvas.SetLeft(rec, j * SquareSize + MarginOffset);
                Canvas.SetTop(rec, i * SquareSize + MarginOffset);
                //await Task.Delay(50);
                j++;
            }
            i++;
        }
    }

    private void RedrawGrid()
    {
        for(int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Columns; c++)
            {
                Grid[r][c].Fill = GridBoolCurrent[r][c] ? Alive : Dead;
            }
        }
        Debug.WriteLine("Redrawing");
    }

    private void OnStartClick(object sender, RoutedEventArgs e)
    {
        if (_loopClock.IsEnabled)
        {
            _isRunning = false;
            _loopClock.Stop();
        }
        else
        {
            _isRunning = true;
            _loopClock.Start();
        }
    }

    private void OnCanvasClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        Point p = e.GetPosition(CanvasComponent);
        double x = p.X - MarginOffset, y = p.Y - MarginOffset;
        Debug.Assert(x >= 0 && y >= 0);
        int row = Convert.ToInt32(Math.Floor(y / SquareSize));
        int col = Convert.ToInt32(Math.Floor(x / SquareSize));
        SwapStateOfCell(row, col);
    }

    private void SwapStateOfCell(int row, int col) 
    {
        if (row >= Rows)    return;
        if (col >= Columns) return;
        if (row < 0)        return;
        if (col < 0)        return;

        Grid[row][col].Fill = (GridBoolCurrent[row][col]) ? Dead : Alive;
        GridBoolCurrent[row][col] = !GridBoolCurrent[row][col];
    }

    private void OnTick(object? sender, EventArgs e)
    {
        ExecuteStep();
    }

    private void ExecuteStep() 
    {
        CalculateNextGeneration();
        SwapGenerations();
        RedrawGrid();
    }

    private void SwapGenerations()
    {
        for (int r = 0; r < Rows; r++)
        {
            Array.Copy(GridBoolNext[r], GridBoolCurrent[r], Columns);
            Array.Clear(GridBoolNext[r], 0, Columns);
        }
    }

    private void CalculateNextGeneration()
    {
        for (int r = 0; r < Rows; r++)
        {
            int summary;
            for (int c = 0; c < Columns; c++)
            {
                summary = GetNumberOfNeighboursAlive(r, c);                
                SetNewState(r, c, summary);
            }
        }
    }

    private int GetNumberOfNeighboursAlive(int row, int col)
    {
        int count = 0;
        // above row
        if (row - 1 >= 0)
        {
            if (col - 1 >= 0) count += (GridBoolCurrent[row - 1][col - 1]) ? 1 : 0;
            if (0 <= col) count += (GridBoolCurrent[row - 1][col]) ? 1 : 0;
            if (col + 1 < Columns) count += (GridBoolCurrent[row - 1][col + 1]) ? 1 : 0;

        }
        // current row
        if (0 <= row && row < Rows)
        {
            if (col - 1 >= 0) count += (GridBoolCurrent[row][col - 1]) ? 1 : 0;
            if (col + 1 < Columns) count += (GridBoolCurrent[row][col + 1]) ? 1 : 0;
        }
        // belove row
        if (row + 1 < Rows)
        {
            if (col - 1 >= 0) count += (GridBoolCurrent[row + 1][col - 1]) ? 1 : 0;
            if (0 <= col) count += (GridBoolCurrent[row + 1][col]) ? 1 : 0;
            if (col + 1 < Columns) count += (GridBoolCurrent[row + 1][col + 1]) ? 1 : 0;

        }
        return count;
    }

    private void SetNewState(int row, int col, int neighboursAlive)
    {
        bool alive = GridBoolCurrent[row][col];

        if (alive)
        {
            GridBoolNext[row][col] = neighboursAlive == 2 || neighboursAlive == 3;
        }
        else 
        {
            GridBoolNext[row][col] = neighboursAlive == 3; 
        }
    }
}