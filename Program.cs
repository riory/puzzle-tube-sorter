using puzzle_tube_sorter.model;
using puzzle_tube_sorter.model.solvers;
using puzzle_tube_sorter.ui;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using static puzzle_tube_sorter.Program;

namespace puzzle_tube_sorter {

    internal class Program {

        public enum GameMode {
            Solver = 1,
            Manual = 2
        }

        static GameMode gameMode = GameMode.Solver;

        static int stepCount = 0;
        static int maxStep = 1000;
        static int spindleCount = 6;
        static double solveRating = -1;
        static int rowCount = 9;
        static int speed = 100;
        static int? seed = null;
        static bool nogui = false;
        static string stuckReason = string.Empty;

        // current bead collection
        static BeadSet beadSet = null;

        static Timer? _timer = null;

        static ISolver? solver;


        static void DrawField() {
            var xSpan = 10;
            int xOffset = (Console.WindowWidth - (xSpan * (spindleCount + 1))) / 2;
            var yOffset = 10;
            var spW = 2;


            // calculate solve rating
            // 1 means all columns are sorted
            // 5 means all columns are unsorted
            solveRating = beadSet.Spindles.Values.Where(sb => sb.Count > 0).Average(spindleBeads => {
                return spindleBeads.Distinct().Count();
            });


            // clear
            ConsolePrimitives.DrawRectangle(ConsoleColor.White, ConsoleColor.Black,
                0,
                Console.WindowWidth - 1,
                0,
                Console.WindowHeight - 1, ' ');

            Console.SetCursorPosition(0, 0);
            var solverName = solver == null ? "" : "_" + solver.Name;
            Console.WriteLine($"Mode: {gameMode}{solverName}\tStep: {stepCount}\tMaxStep: {maxStep}\tSeed: {seed}");
            // top bar
            ConsolePrimitives.DrawRectangle(ConsoleColor.White, ConsoleColor.Black,
                0,
                Console.WindowWidth - 1,
                2,
                3, '─');
            // bottom bar
            ConsolePrimitives.DrawRectangle(ConsoleColor.White, ConsoleColor.Black,
                0,
                Console.WindowWidth - 1,
                Console.WindowHeight - 3,
                Console.WindowHeight - 2, '─');


            for (var sp = 0; sp < spindleCount; sp++) {
                // draw spindles
                var spX = xOffset + (sp + 1) * xSpan;
                var y2 = yOffset + rowCount + 2;
                ConsolePrimitives.DrawRectangle(ConsoleColor.Gray, ConsoleColor.DarkGray, spX, spX + spW, yOffset, y2, '║');
                Console.SetCursorPosition(spX, y2 + 1);
                Console.Write(sp);
            }
            var spb = 0;
            // draw beads
            foreach (var bs in beadSet.Spindles.Values) {
                spb++;
                var rb = 0;
                foreach (Bead bead in bs) {
                    rb++;
                    var bX = xOffset + spb * xSpan;
                    var bY = rb + yOffset + (rowCount - bs.Count);
                    ConsolePrimitives.DrawRectangle(bead.Color, ConsoleColor.Black, bX - 1, bX + spW + 1, bY, bY + 1, '#');
                }
            }
        }

        public enum State {
            Solving = 0,
            Solved = 1,
            Stuck = 2
        }

        static State Solve() {
            if (solver == null) {
                stuckReason = "no solver was defined";
                return State.Stuck;
            }
            if (stepCount >= maxStep) {
                stuckReason = "maxStep exceeded";
                return State.Stuck;
            }
            // check if solved - all spindles are either empty or only contain same color beads, last is empty
            if (beadSet.isSetSolved()) {
                return State.Solved;
            }
            // not solved, execute solver strategy
            var solverStep = solver.GetStep(beadSet);
            stepCount++;
            if (!solverStep.stuck) {
                if (beadSet.MoveBead(solverStep.s1, solverStep.s2, solverStep.strategy)) {
                    if (nogui == false) {
                        ConsolePrimitives.DrawFullString(Console.WindowHeight - 1,
                            $"Solving ({solverStep.strategy}): {solverStep.s1} -> {solverStep.s2}");
                    }
                }
            } else {
                stuckReason = "solver decision";
                return State.Stuck;
            }

            return State.Solving;
        }

        static void ParseArgs(string[] args) {
            if (args.Length > 0) {
                try {
                    foreach (string arg in args) {
                        if (arg.ToLowerInvariant() == "nogui") {
                            nogui = true;
                            continue;
                        }
                        var key = arg.Substring(0, arg.IndexOf('='));
                        var val = arg.Substring(arg.IndexOf('=') + 1);
                        if (key.ToLower() == "gamemode") {
                            gameMode = (GameMode)Enum.Parse(typeof(GameMode), val);
                        }
                        if (key.ToLower() == "speed") {
                            speed = Int32.Parse(val);
                        }
                        if (key.ToLower() == "seed") {
                            seed = Int32.Parse(val);
                        }
                        if (key.ToLower() == "maxstep") {
                            maxStep = Int32.Parse(val);
                            if (maxStep < 0) {
                                maxStep = Int32.MaxValue;
                            }
                        }
                        if (key.ToLower() == "solver") {
                            Type? t = Type.GetType("puzzle_tube_sorter.model.solvers." + val);
                            if (t != null) {
                                solver = Activator.CreateInstance(t) as ISolver;
                            }
                        }

                    }
                } catch (Exception) {
                }
            }
        }

        static void Main(string[] args) {

            ParseArgs(args);

            // load default solver if not specified
            if (gameMode == GameMode.Solver && solver == null) {
                solver = new RandomStrategySolver();
            }

            beadSet = BeadSet.GenerateRandom(spindleCount, rowCount, seed);


            DrawField();
            var input = string.Empty;

            switch (gameMode) {
                case GameMode.Solver:
                    // run solver with no gui updates
                    if (nogui) {
                        var state = State.Solving;
                        while (state == State.Solving) {
                            state = Solve();
                        }
                        DrawField();
                        DrawState(state);
                    } else {
                        _timer = new Timer(TimerCallback, null, 0, speed);
                    }

                    Console.ReadLine();
                    break;
                case GameMode.Manual:
                    var reMove = new Regex(@"^\d\d$");
                    Console.SetCursorPosition(0, Console.WindowHeight - 2);
                    while (input != "q") {
                        var pr = beadSet.isSetSolved() ? "SOLVED! Type 'q' to quit: " : "Input move (0-5,0-5) or 'q' to quit: ";
                        ConsolePrimitives.DrawFullString(Console.WindowHeight - 2, pr, 
                            beadSet.isSetSolved() ? ConsoleColor.Green : ConsoleColor.White);
                        Console.SetCursorPosition(pr.Length, Console.WindowHeight - 2);
                        input = Console.ReadLine();
                        if (input != null && reMove.IsMatch(input)) {
                            stepCount++;
                            int s1 = input[0] - '0';
                            int s2 = input[1] - '0';
                            if (beadSet.MoveBead(s1, s2, "")) {
                                DrawField();
                                ConsolePrimitives.DrawFullString(Console.WindowHeight - 1, $"Moving: {s1} -> {s2}");                                
                            }
                        }
                    }
                    break;
            }
            // restore defaults
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Clear();
        }

        private static void DrawState(State state) {
            if (solver != null) {
                switch (state) {
                    case State.Solving:
                        // drawing is done in move bead call
                        break;
                    case State.Solved:
                        ConsolePrimitives.DrawFullString(Console.WindowHeight - 1,
                            $"!! Solved using {solver.Name} in total of {stepCount} step(s) !!", ConsoleColor.Green);
                        break;
                    case State.Stuck:
                        ConsolePrimitives.DrawFullString(Console.WindowHeight - 1,
                            $"Stuck!! ({solver.Name}) reason: {stuckReason}", ConsoleColor.Red);
                        break;
                }
            }
        }

        private static bool _mutex = false;
        private static void TimerCallback(Object o) {
            if (_mutex || _timer == null) return;
            _mutex = true;
            if (beadSet.Spindles != null && beadSet.Spindles.Count > 0) {
                var state = Solve();
                DrawField();
                if (state != State.Solving) {
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                }
                DrawState(state);
            }
            _mutex = false;
        }
    }
}