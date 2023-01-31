using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace Tetris
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.Timers.Timer timer = new System.Timers.Timer(1500);
        System.Random random = new System.Random();

        const int blockSize = 50;
        bool[,] isOccupied = new bool[8, 15];//第15行为底边
        Rectangle[,] rectangles = new Rectangle[8, 14];

        int score = 0;
        //int next = 0;

        Figure currentFigure, nextFigure;

        public delegate void GameTick();

        public MainWindow()
        {
            InitializeComponent();
            timer.Elapsed += Timer_Elapsed;
        }


        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.Invoke(new GameTick(Tick));
        }

        public void Tick()
        {
            //Trace.WriteLine("Tick");
        }

        public void StartGame()
        {
            timer.Start();
            timer.AutoReset = true;

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 14; j++)
                {
                    isOccupied[i, j] = false;
                    rectangles[i, j] = new Rectangle();
                    rectangles[i, j].Tag = new Point();
                }
                isOccupied[i, 14] = true;//底边始终为true
            }

            AddFigure(true);

            scoreBox.Content = score;
        }

        public void GameOver()
        {
            timer.Stop();
            timer.Close();
        }

        public void AddFigure(bool isFirst)
        {
            if (isFirst)
            {
                currentFigure = new Figure(0, blockSize, ref mainCanvas, ref isOccupied, ref rectangles);
                currentFigure.FallToBottom += ReplaceFigure;
                currentFigure.Show();
            }
            nextFigure = new Figure(0, blockSize, ref mainCanvas, ref isOccupied, ref rectangles);
            nextFigure.FallToBottom += ReplaceFigure;
        }

        private void RemoveLine(int targetLine)
        {
            Point temp;

            for (int i = 0; i < 8; i++)
            {
                mainCanvas.Children.Remove(rectangles[i, targetLine]);
            }

            for (int j = targetLine; j > 0; j--)
            {
                for (int i = 0; i < 8; i++)
                {
                    isOccupied[i, j] = isOccupied[i, j - 1];

                    rectangles[i, j] = rectangles[i, j - 1];
                    temp = (Point)rectangles[i, j].Tag;
                    Canvas.SetTop(rectangles[i, j], (temp.Y + 1) * blockSize);
                    temp.Y++;
                    rectangles[i, j].Tag = temp;

                }
            }

            for (int i = 0; i < 8; i++)
            {
                isOccupied[i, 0] = false;
            }
        }

        private void CheckLineFull()
        {
            int fullLineCount = 0;
            bool isFull;
            for (int j = 0; j < 14; j++)
            {
                isFull = true;
                for (int i = 0; i < 8; i++)//此处循环的行列表达方式不同
                {
                    if (!isOccupied[i, j])
                    {
                        isFull = false;
                        break;
                    }
                }
                if (isFull)
                {
                    fullLineCount++;
                    RemoveLine(j);
                    Trace.WriteLine(j, "line is full");
                }
            }
            Trace.WriteLine(fullLineCount, "lines are full");
            CalculateScore(fullLineCount);
        }

        private void CheckColumnFull()
        {

        }

        private void ReplaceFigure()
        {
            CheckLineFull();
            currentFigure = nextFigure;
            currentFigure.Show();
            AddFigure(false);
        }

        private void CalculateScore(int lineNum)
        {

        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StartGame();
        }

        private void mainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A)
            {
                Trace.WriteLine("left");
                currentFigure.Move(true);
            }
            if (e.Key == Key.D)
            {
                Trace.WriteLine("right");
                currentFigure.Move(false);
            }
            if (e.Key == Key.W)
            {
                Trace.WriteLine("rotate");
                currentFigure.ChangeDirection();
            }
            if (e.Key == Key.S)
            {
                Trace.WriteLine("fall");
                currentFigure.Fall();
            }
        }
    }

    public class Figure
    {
        int blockSize;
        int type;//type用于表示图形的类型
        Point position = new Point(0, 0);//总体定位器
        int direction = 0;
        Rectangle[] rectangles = new Rectangle[4];

        Canvas fatherCanvas;
        Rectangle[,] gameArea;
        bool[,] isOccupied;

        Point temp;//tag中转变量

        public delegate void LifeEnd();
        public event LifeEnd FallToBottom;

        public Figure(int type, int blockSize, ref Canvas fatherCanvas, ref bool[,] occupiedTable, ref Rectangle[,] gameArea)
        {
            this.type = type;
            this.fatherCanvas = fatherCanvas;
            this.isOccupied = occupiedTable;
            this.gameArea = gameArea;
            this.blockSize = blockSize;

            switch (type)
            {
                case 0:
                    DrawI();
                    break;
                case 1:
                    DrawL();
                    break;
                case 2:
                    DrawReverseL();
                    break;
                case 3:
                    DrawO();
                    break;
                case 4:
                    DrawZ();
                    break;
                case 5:
                    DrawReverseZ();
                    break;
            }
        }

        public void Show()
        {
            for (int i = 0; i < 4; i++)
            {
                fatherCanvas.Children.Add(rectangles[i]);
            }
        }

        public void ChangeDirection()
        {
            if (direction > 3)
            {
                direction = 0;
            }

            switch (type)
            {
                case 0:
                    //DrawI();
                    switch (direction)
                    {
                        case 0:
                            for (int i = 0; i < 4; i++)
                            {
                                if (IsLegal(position.X, position.Y + i - 1))
                                {
                                    return;
                                }
                            }

                            for (int i = 0; i < 4; i++)
                            {
                                Canvas.SetTop(rectangles[i], (position.Y + i - 1) * blockSize);
                                Canvas.SetLeft(rectangles[i], position.X * blockSize);
                                rectangles[i].Tag = new Point(position.X, position.Y + i - 1);
                            }
                            break;
                        case 1:
                            for (int i = 0; i < 4; i++)
                            {
                                if (IsLegal(position.X + i - 1, (int)position.Y))
                                {
                                    return;
                                }
                            }

                            for (int i = 0; i < 4; i++)
                            {

                                Canvas.SetTop(rectangles[i], position.Y * blockSize);
                                Canvas.SetLeft(rectangles[i], (position.X + i - 1) * blockSize);
                                rectangles[i].Tag = new Point(position.X + i - 1, position.Y);
                            }
                            break;
                        case 2:
                            for (int i = 0; i < 4; i++)
                            {
                                if (IsLegal(position.X, position.Y + i - 1))
                                {
                                    return;
                                }
                            }

                            for (int i = 0; i < 4; i++)
                            {
                                Canvas.SetTop(rectangles[i], (position.Y + i - 1) * blockSize);
                                Canvas.SetLeft(rectangles[i], position.X * blockSize);
                                rectangles[i].Tag = new Point(position.X, position.Y + i - 1);
                            }
                            break;
                        case 3:
                            for (int i = 0; i < 4; i++)
                            {
                                if (IsLegal(position.X + i - 1, (int)position.Y))
                                {
                                    return;
                                }
                            }

                            for (int i = 0; i < 4; i++)
                            {

                                Canvas.SetTop(rectangles[i], position.Y * blockSize);
                                Canvas.SetLeft(rectangles[i], (position.X + i - 1) * blockSize);
                                rectangles[i].Tag = new Point(position.X + i - 1, position.Y);
                            }
                            break;
                    }
                    break;
                case 1:
                    //DrawL();
                    break;
                case 2:
                    //DrawReverseL();
                    break;
                case 3:
                    //DrawO();
                    break;
                case 4:
                    //DrawZ();
                    break;
                case 5:
                    //DrawReverseZ();
                    break;
            }

            Trace.WriteLine(direction);
            direction++;
        }

        private bool IsLegal(double willCheckX, double willCheckY)
        {
            if (willCheckX < 0 || willCheckY < 0 || willCheckX > 7 || willCheckY > 14)
            {
                return true;
            }
            return isOccupied[(int)willCheckX, (int)willCheckY];
        }

        public void Fall()
        {
            for (int i = 0; i < 4; i++)
            {
                if (isOccupied[(int)((Point)rectangles[i].Tag).X, (int)((Point)rectangles[i].Tag).Y + 1])
                {
                    AdjustEnd();
                    return;
                }
            }
            for (int i = 0; i < 4; i++)
            {
                Canvas.SetTop(rectangles[i], (((Point)rectangles[i].Tag).Y + 1) * blockSize);
                temp = (Point)rectangles[i].Tag;
                temp.Y += 1;
                rectangles[i].Tag = temp;
            }
            position.Y++;
        }

        /// <summary>
        /// 左右移动，左true右false
        /// </summary>
        /// <param name="LeftOrRight">移动的方向：true为左，false为右</param>
        public void Move(bool LeftOrRight)
        {
            Trace.WriteLine("move");
            if (LeftOrRight)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (IsLegal(((Point)rectangles[i].Tag).X - 1, ((Point)rectangles[i].Tag).Y))
                    {
                        return;
                    }
                }

                for (int i = 0; i < 4; i++)
                {
                    Canvas.SetLeft(rectangles[i], (((Point)rectangles[i].Tag).X - 1) * blockSize);
                    temp = (Point)rectangles[i].Tag;
                    temp.X -= 1;
                    rectangles[i].Tag = temp;
                }

                position.X--;
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    if (IsLegal(((Point)rectangles[i].Tag).X + 1, ((Point)rectangles[i].Tag).Y))
                    {
                        return;
                    }
                }
                for (int i = 0; i < 4; i++)
                {
                    Canvas.SetLeft(rectangles[i], (((Point)rectangles[i].Tag).X + 1) * blockSize);
                    temp = (Point)rectangles[i].Tag;
                    temp.X += 1;
                    rectangles[i].Tag = temp;
                }

                position.X++;
            }

        }

        void AdjustEnd()
        {
            Trace.WriteLine("figure death");
            for (int i = 0; i < 4; i++)
            {
                isOccupied[(int)((Point)rectangles[i].Tag).X, (int)((Point)rectangles[i].Tag).Y] = true;
                gameArea[(int)((Point)rectangles[i].Tag).X, (int)((Point)rectangles[i].Tag).Y] = rectangles[i];
            }
            if (FallToBottom != null)
            {
                FallToBottom();
            }
        }

        void DrawI()
        {
            for (int i = 0; i < 4; i++)
            {
                rectangles[i] = new Rectangle();
                rectangles[i].Width = blockSize;
                rectangles[i].Height = blockSize;
                rectangles[i].Stroke = new SolidColorBrush(Colors.White);
                rectangles[i].Fill = new SolidColorBrush(Colors.Green);
                rectangles[i].Tag = new Point(2 + i, 0);
                Canvas.SetTop(rectangles[i], 0);
                Canvas.SetLeft(rectangles[i], (2 + i) * blockSize);
            }

            position.X = 3;
        }

        void DrawL()
        {

        }

        void DrawReverseL()
        {

        }

        void DrawO()
        {

        }

        void DrawZ()
        {

        }

        void DrawReverseZ()
        {

        }
    }
}
