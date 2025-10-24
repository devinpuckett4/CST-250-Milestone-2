using System;
using Minesweeper.BLL;
using Minesweeper.Models;

namespace Minesweeper.ConsoleApp
{
    internal class Program
    {
        // fixed width per cell so columns line up straight
        private const int CELL_W = 3;

        static void Main(string[] args)
        {
            // make a board and the service that runs the rules
            var board = new BoardModel(size: 10) { DifficultyPercentage = 0.12f };
            IBoardOperations svc = new BoardService();

            // set up bombs and neighbor counts
            svc.SetupBombs(board);
            svc.CountBombsNearby(board);

            // show the answer key first for the screenshot then pause
            Console.WriteLine("Answer Key cheat view:");
            PrintAnswers(board);
            Console.WriteLine();
            Console.Write("Press Enter to start gameplay...");
            Console.ReadLine();
            Console.Clear();

            // game flags
            bool victory = false;
            bool death = false;

            // main loop
            while (!victory && !death)
            {
                // show the masked board each turn
                PrintBoard(board);
                Console.WriteLine($"Rewards available: {board.RewardsRemaining}");
                Console.WriteLine("Enter: row col action   V = Visit, F = Flag, U = Use Reward");
                Console.Write("> ");

                // read one line
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("No input read. Example: 3 4 V");
                    continue;
                }

                // split on spaces tabs or commas
                var parts = input.Trim().Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                {
                    Console.WriteLine("Please enter three parts: row col action. Example: 3 4 V");
                    continue;
                }

                // parse row and col
                if (!int.TryParse(parts[0], out int r) || !int.TryParse(parts[1], out int c))
                {
                    Console.WriteLine("Row and col must be whole numbers like 0 1 V");
                    continue;
                }

                // accept short or long action words
                string actionRaw = parts[2].Trim().ToUpperInvariant();
                string action = actionRaw switch
                {
                    "V" or "VISIT" => "V",
                    "F" or "FLAG" => "F",
                    "U" or "USE" or "PEEK" => "U",
                    _ => ""
                };
                if (action == "")
                {
                    Console.WriteLine("Action must be V F or U");
                    continue;
                }

                // quick bounds check so we can give a message
                if (r < 0 || r >= board.Size || c < 0 || c >= board.Size)
                {
                    Console.WriteLine("That move is out of bounds");
                    continue;
                }

                // run the move
                var svcImpl = (BoardService)svc;

                if (action == "V")
                {
                    // remember if this cell had the reward before visit
                    bool hadReward = board.Cells[r, c].HasSpecialReward;

                    bool hitBomb = svcImpl.VisitCell(board, r, c);
                    if (hitBomb)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("You visited a bomb. Game over");
                        Console.ResetColor();
                    }
                    else
                    {
                        if (hadReward)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("You found the reward. You can use U once to peek a cell");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.WriteLine("Cell revealed");
                        }
                    }
                }
                else if (action == "F")
                {
                    if (board.Cells[r, c].IsVisited)
                    {
                        Console.WriteLine("You cannot flag a cell that is already visited");
                    }
                    else
                    {
                        svcImpl.ToggleFlag(board, r, c);
                        Console.WriteLine(board.Cells[r, c].IsFlagged ? "Flag placed" : "Flag removed");
                    }
                }
                else if (action == "U")
                {
                    // use one reward to peek at a cell
                    var msg = svcImpl.UseRewardPeek(board, r, c);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(msg);
                    Console.ResetColor();
                }

                // check state after the move
                var state = svcImpl.DetermineGameState(board);
                if (state == GameState.Won)
                {
                    victory = true;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\nYou win");
                    Console.ResetColor();
                    PrintAnswers(board); // final reveal
                }
                else if (state == GameState.Lost)
                {
                    death = true;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nYou lose");
                    Console.ResetColor();
                    PrintAnswers(board); // final reveal
                }
                else
                {
                    Console.WriteLine(); // spacer
                }
            }

            Console.WriteLine("\nDone. Press any key to exit.");
            Console.ReadKey();
        }

        // center short strings in a fixed width
        private static string Center(string s, int width)
        {
            if (string.IsNullOrEmpty(s)) s = " ";
            if (s.Length >= width) return s.Substring(0, width);
            int left = (width - s.Length) / 2;
            int right = width - s.Length - left;
            return new string(' ', left) + s + new string(' ', right);
        }

        // header with column numbers and a top border
        private static void PrintHeaderAndBorder(int n)
        {
            // left margin + the same left '|' used by rows, so headers align over cells
            Console.Write("    |");
            for (int c = 0; c < n; c++)
            {
                Console.Write(Center(c.ToString(), CELL_W));
                Console.Write("|");
            }
            Console.WriteLine();

            // top border that matches the row borders
            Console.Write("    +");
            for (int c = 0; c < n; c++) Console.Write(new string('-', CELL_W) + "+");
            Console.WriteLine();
        }

        // gameplay view. hidden shows question mark. flags show F
        private static void PrintBoard(BoardModel board)
        {
            int n = board.Size;

            PrintHeaderAndBorder(n);

            for (int r = 0; r < n; r++)
            {
                Console.Write($"{r,2}  |"); // row label and left border

                for (int c = 0; c < n; c++)
                {
                    var cell = board.Cells[r, c];
                    string content;

                    if (!cell.IsVisited)
                        content = cell.IsFlagged ? "F" : "?";
                    else if (cell.IsBomb)
                        content = "B";
                    else if (cell.NumberOfBombNeighbors > 0)
                        content = cell.NumberOfBombNeighbors.ToString();
                    else
                        content = ".";

                    if (content == "B") Console.ForegroundColor = ConsoleColor.Red;
                    else if (content == "F") Console.ForegroundColor = ConsoleColor.Cyan;
                    else if (int.TryParse(content, out _)) Console.ForegroundColor = ConsoleColor.Yellow;
                    else Console.ForegroundColor = ConsoleColor.Gray;

                    Console.Write(Center(content, CELL_W));
                    Console.ResetColor();
                    Console.Write("|");
                }

                Console.WriteLine();

                Console.Write("    +");
                for (int c = 0; c < n; c++) Console.Write(new string('-', CELL_W) + "+");
                Console.WriteLine();
            }
        }

        // full reveal answer key. ignores visited and flags
        private static void PrintAnswers(BoardModel board)
        {
            int n = board.Size;

            PrintHeaderAndBorder(n);

            for (int r = 0; r < n; r++)
            {
                Console.Write($"{r,2}  |");

                for (int c = 0; c < n; c++)
                {
                    var cell = board.Cells[r, c];
                    string content = cell.IsBomb
                        ? "B"
                        : (cell.NumberOfBombNeighbors > 0 ? cell.NumberOfBombNeighbors.ToString() : ".");

                    if (content == "B") Console.ForegroundColor = ConsoleColor.Red;
                    else if (int.TryParse(content, out _)) Console.ForegroundColor = ConsoleColor.Yellow;
                    else Console.ForegroundColor = ConsoleColor.Gray;

                    Console.Write(Center(content, CELL_W));
                    Console.ResetColor();
                    Console.Write("|");
                }

                Console.WriteLine();

                Console.Write("    +");
                for (int c = 0; c < n; c++) Console.Write(new string('-', CELL_W) + "+");
                Console.WriteLine();
            }
        }
    }
}