using Lab88;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static bool gameRunning; // Флаг, указывающий на состояние игры (выполняется или нет)
    static int score; // Текущий счет игрока
    static int playerPosition; // Положение игрока по горизонтали
    static List<Enemy> enemies; // Список врагов
    static List<Bullet> bullets; // Список пуль
    static List<Bullet> enemyBullets; // Список пуль врагов
    static Random random; // Генератор случайных чисел
    static object lockObject; // Объект блокировки для потокобезопасного доступа к коллекциям
    static CancellationTokenSource cancellationTokenSource; // Источник отмены для отмены задач
    static string leaderboardFilePath; // Путь к файлу с таблицей лидеров
    static Task playerInputTask; // Задача для обработки пользовательского ввода
    static Task moveEnemiesTask; // Задача для перемещения врагов
    static Task moveBulletsTask; // Задача для перемещения пуль

    //В методе Main происходит инициализация переменных, установка начальных значений и вход в главный цикл игры.
    //Пользователю отображается главное меню, и в зависимости от выбора пользователя выполняются соответствующие действия.
    static void Main(string[] args)
    {
        // Инициализация переменных и объектов
        gameRunning = true;
        score = 0;
        playerPosition = Console.WindowWidth / 2;
        enemies = new List<Enemy>();
        bullets = new List<Bullet>();
        enemyBullets = new List<Bullet>();
        random = new Random();
        lockObject = new object();
        cancellationTokenSource = new CancellationTokenSource();
        leaderboardFilePath = "leaderboard.txt";

        Console.CursorVisible = false;

        while (gameRunning)
        {
            Console.Clear();
            ShowMainMenu();

            bool validChoice = false;
            while (!validChoice)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                switch (keyInfo.Key)
                {
                    case ConsoleKey.D1:
                        validChoice = true;
                        StartGame();
                        break;
                    case ConsoleKey.D2:
                        ShowLeaderboard();
                        break;
                    case ConsoleKey.D3:
                        validChoice = true;
                        ExitGame();
                        break;
                    default:
                        ShowMainMenu();
                        break;
                }
            }
        }
    }

    //Метод ShowMainMenu отображает главное меню в консоли.
    static void ShowMainMenu()
    {
        Console.Clear();
        Console.WriteLine("Главное меню");
        Console.WriteLine("1. Начать игру");
        Console.WriteLine("2. Посмотреть таблицу лидеров");
        Console.WriteLine("3. Выход");
    }


    //Метод StartGame запрашивает у пользователя имя игрока и вызывает метод PlayGame для начала игры.
    static void StartGame()
    {
        Console.Clear();
        Console.WriteLine("Введите имя игрока: ");
        string playerName = Console.ReadLine();
        PlayGame(playerName);
    }
    //Метод PlayGame запускает основной игровой цикл, в котором происходит отображение текущего счета, отрисовка игровых объектов,
    //обработка пользовательского ввода и перемещение врагов и пуль.После смерти игрока игра возвращается в меню.
    static void PlayGame(string playerName)
    {
        // Отмена предыдущих задач, если они запущены
        cancellationTokenSource.Cancel();
        cancellationTokenSource = new CancellationTokenSource();

        // Запуск новых потоков для обработки пользовательского ввода, перемещения врагов и пуль
        playerInputTask = Task.Run(HandlePlayerInput, cancellationTokenSource.Token);
        moveEnemiesTask = Task.Run(MoveEnemies, cancellationTokenSource.Token);
        moveBulletsTask = Task.Run(MoveBullets, cancellationTokenSource.Token);

        while (gameRunning)
        {
            Console.Clear();
            Console.WriteLine("Счет: " + score);
            Console.WriteLine("------------");

            RenderPlayer();
            RenderEnemies();
            RenderBullets();

            Thread.Sleep(50);
        }

        cancellationTokenSource.Cancel(); // Отмена задач для завершения потоков

        Task.WaitAll(playerInputTask, moveEnemiesTask, moveBulletsTask); // Ожидание завершения потоков

        SaveScore(playerName, score);

        Console.Clear();
        Console.WriteLine("Неповезло((((");
        Console.WriteLine("Счет: " + score);
        Console.ReadLine();

        // Возвращение в меню после смерти игрока
        gameRunning = false;
        score = 0;
        playerPosition = Console.WindowWidth / 2;
        enemies.Clear();
        bullets.Clear();
        enemyBullets.Clear();

        Console.Clear();
        Console.WriteLine("Нажмите любую клавишу для возврата в меню.");
        Console.ReadKey(true);

        gameRunning = true; // Возвращение в игру
        ShowMainMenu();
    }

    //Метод ShowLeaderboard отображает таблицу лидеров
    static void ShowLeaderboard()
    {
        Console.Clear();
        Console.WriteLine("Таблица лидеров");

        if (File.Exists(leaderboardFilePath))
        {
            var leaderboardLines = File.ReadAllLines(leaderboardFilePath);
            foreach (var line in leaderboardLines)
            {
                var parts = line.Split(':');
                if (parts.Length == 2)
                {
                    var playerName = parts[0];
                    var playerScore = parts[1];
                    Console.WriteLine($"{playerName}: {playerScore}");
                }
            }
        }

        Console.WriteLine("Нажмите любую клавишу для возврата в меню.");
        Console.ReadKey(true);
    }


    //Метод SaveScore сохраняет результат игры
    static void SaveScore(string playerName, int points)
    {
        string scoreLine = $"{playerName}:{points}";
        File.AppendAllText(leaderboardFilePath, scoreLine + Environment.NewLine);
    }

    static void ExitGame()
    {
        gameRunning = false;
        cancellationTokenSource.Cancel();

    }


    //Метод HandlePlayerInput обрабатывает пользовательский ввод, перемещая игрока влево или вправо при нажатии соответствующих клавиш,
    //а также создает пули при нажатии пробела.При нажатии клавиши Esc вызывается метод ExitGame для выхода из игры.
    static void HandlePlayerInput()
    {
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.LeftArrow)
                {
                    playerPosition = Math.Max(playerPosition - 1, 0);
                }
                else if (key == ConsoleKey.RightArrow)
                {
                    playerPosition = Math.Min(playerPosition + 1, Console.WindowWidth - 1);
                }
                else if (key == ConsoleKey.Spacebar)
                {
                    lock (lockObject)
                    {
                        bullets.Add(new Bullet(playerPosition, Console.WindowHeight - 2));
                    }
                }
            }
        }
    }

    static void MoveEnemies()
    {
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            lock (lockObject)
            {
                // Двигаем противников
                for (int i = 0; i < enemies.Count; i++)
                {
                    enemies[i].Y += 1;

                    // Проверка на столкновение с игроком
                    if (enemies[i].X == playerPosition && enemies[i].Y == Console.WindowHeight - 1)
                    {
                        gameRunning = false;
                        return;
                    }

                    // Добавление стрельбы врагов
                    if (random.Next(0, 10) < 3)
                    {
                        enemyBullets.Add(new Bullet(enemies[i].X, enemies[i].Y + 1));
                    }
                }

                // Удаляем противников, вышедших за пределы экрана
                enemies.RemoveAll(e => e.Y >= Console.WindowHeight);

                // Добавляем новых противников
                if (random.Next(0, 10) < 3)
                {
                    int x = random.Next(0, Console.WindowWidth);
                    int y = 0;
                    enemies.Add(new Enemy(x, y));
                }
            }

            Thread.Sleep(100);
        }
    }

    static void MoveBullets()
    {
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            lock (lockObject)
            {
                // Двигаем выстрелы игрока
                for (int i = 0; i < bullets.Count; i++)
                {
                    bullets[i].Y -= 1;

                    // Проверка на столкновение с врагами
                    for (int j = 0; j < enemies.Count; j++)
                    {
                        if (bullets[i].X == enemies[j].X && bullets[i].Y == enemies[j].Y)
                        {
                            score += 10; // Увеличиваем счет при попадании
                            bullets.RemoveAt(i);
                            i--;
                            enemies.RemoveAt(j);
                            break;
                        }
                    }

                    // Удаляем старые выстрелы
                    if (i >= 0 && i < bullets.Count && bullets[i].Y <= 0)
                    {
                        bullets.RemoveAt(i);
                        i--;
                    }
                }

                // Двигаем выстрелы врагов
                for (int i = 0; i < enemyBullets.Count; i++)
                {
                    enemyBullets[i].Y += 1;

                    // Проверка на столкновение с игроком
                    if (enemyBullets[i].X == playerPosition && enemyBullets[i].Y == Console.WindowHeight - 1)
                    {
                        gameRunning = false;
                        return;
                    }

                    // Удаляем старые выстрелы
                    if (i >= 0 && i < enemyBullets.Count && enemyBullets[i].Y >= Console.WindowHeight - 1)
                    {
                        enemyBullets.RemoveAt(i);
                        i--;
                    }
                }
            }

            Thread.Sleep(50);
        }
    }



    static void RenderPlayer()
    {
        Console.SetCursorPosition(playerPosition, Console.WindowHeight - 1);
        Console.Write("_");
    }

    static void RenderEnemies()
    {
        lock (lockObject)
        {
            foreach (var enemy in enemies)
            {
                Console.SetCursorPosition(enemy.X, enemy.Y);
                Console.Write("0");
            }
        }
    }

    // Метод RenderBullets отображает пули на консоли.
    static void RenderBullets()
    {
        lock (lockObject)
        {
            foreach (var bullet in bullets)
            {
                Console.SetCursorPosition(bullet.X, bullet.Y);
                Console.Write("|");
            }

            foreach (var bullet in enemyBullets)
            {
                Console.SetCursorPosition(bullet.X, bullet.Y);
                Console.Write("|");
            }
        }
    }
}