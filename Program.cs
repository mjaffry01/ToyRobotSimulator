using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ToyRobotSimulator
{
    public enum Direction
    {
        North,
        East,
        South,
        West
    }

    public static class DirectionExtensions
    {
        private static readonly LinkedList<Direction> Directions = new LinkedList<Direction>(new[] { Direction.North, Direction.East, Direction.South, Direction.West });

        public static Direction RotateLeft(this Direction direction)
        {
            var current = Directions.Find(direction);
            return (current.Previous ?? Directions.Last).Value;
        }

        public static Direction RotateRight(this Direction direction)
        {
            var current = Directions.Find(direction);
            return (current.Next ?? Directions.First).Value;
        }
    }

    public class ToyRobot
    {
        private readonly int _maxX;
        private readonly int _maxY;

        public ToyRobot(int maxX = 5, int maxY = 5)
        {
            _maxX = maxX;
            _maxY = maxY;
        }

        public int X { get; private set; }
        public int Y { get; private set; }
        public Direction Facing { get; private set; }

        public void Place(int x, int y, Direction direction)
        {
            if (IsValid(x, y))
            {
                X = x;
                Y = y;
                Facing = direction;
            }
        }

        public void Move()
        {
            switch (Facing)
            {
                case Direction.North when IsValid(X, Y + 1):
                    Y++;
                    break;
                case Direction.East when IsValid(X + 1, Y):
                    X++;
                    break;
                case Direction.South when IsValid(X, Y - 1):
                    Y--;
                    break;
                case Direction.West when IsValid(X - 1, Y):
                    X--;
                    break;
            }
        }

        public void Left() => Facing = Facing.RotateLeft();

        public void Right() => Facing = Facing.RotateRight();

        public string Report() => $"{X},{Y},{Facing}";

        private bool IsValid(int x, int y) => x >= 0 && x < _maxX && y >= 0 && y < _maxY;
    }

    public class CommandParser
    {
        private static readonly Regex PlaceCommand = new Regex(@"PLACE (\d+),(\d+),(NORTH|EAST|SOUTH|WEST)");

        public (string Command, int? X, int? Y, Direction? Direction) Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentException("Command cannot be empty.");
            }

            var match = PlaceCommand.Match(input);
            if (match.Success)
            {
                var x = int.Parse(match.Groups[1].Value);
                var y = int.Parse(match.Groups[2].Value);
                var direction = Enum.Parse<Direction>(match.Groups[3].Value, true);
                return ("PLACE", x, y, direction);
            }

            return (input.ToUpper(), null, null, null);
        }
    }

    public class CommandExecutor
    {
        private readonly ToyRobot _toyRobot;
        private readonly CommandParser _parser = new CommandParser();

        public CommandExecutor(ToyRobot toyRobot)
        {
            _toyRobot = toyRobot;
        }

        public void Execute(string commandString)
        {
            var (command, x, y, direction) = _parser.Parse(commandString);

            switch (command)
            {
                case "MOVE":
                    _toyRobot.Move();
                    break;
                case "LEFT":
                    _toyRobot.Left();
                    break;
                case "RIGHT":
                    _toyRobot.Right();
                    break;
                case "REPORT":
                    Reporter.Display(_toyRobot.Report());
                    break;
                case "PLACE":
                    if (x.HasValue && y.HasValue && direction.HasValue)
                    {
                        _toyRobot.Place(x.Value, y.Value, direction.Value);
                    }
                    break;
                default:
                    throw new InvalidOperationException("Invalid command.");
            }
        }
    }

    public static class Reporter
    {
        public static void Display(string message)
        {
            Console.WriteLine(message);
        }
    }

    class Program
    {
        public static void Main()
        {
            Console.WriteLine("Please enter the file paths separated by a comma (,):");
            var input = Console.ReadLine();

            if (string.IsNullOrEmpty(input))
            {
                Console.WriteLine("No input provided.");
                return;
            }

            var inputFiles = input.Split(',')
                                  .Select(path => path.Trim())
                                  .Where(File.Exists)
                                  .ToList();

            if (!inputFiles.Any())
            {
                Console.WriteLine("No valid input files found.");
                return;
            }

            foreach (var path in inputFiles)
            {
                Console.WriteLine("Executing commands from: " + path);
                Console.WriteLine();

                try
                {
                    ExecuteCommandsFrom(path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                }

                Console.WriteLine();
                Console.WriteLine("-----------------------");
                Console.WriteLine();
            }

            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }

        private static void ExecuteCommandsFrom(string path)
        {
            var toyRobot = new ToyRobot();
            var executor = new CommandExecutor(toyRobot);

            using (var file = new StreamReader(path))
            {
                string command;
                while ((command = file.ReadLine()) != null)
                {
                    Console.WriteLine("Executing command: " + command);
                    try
                    {
                        executor.Execute(command);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Command execution failed: " + ex.Message);
                    }
                }
            }
        }
    }
}
