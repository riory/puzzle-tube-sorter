using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace puzzle_tube_sorter.model {
    public class BeadSet {

        public class SpindleInfo {
            public int index { get; set; }
            public int solvedDepth { get; set; }
            public ConsoleColor color { get; set; }

            public SpindleInfo(int index, int solvedDepth, ConsoleColor color) {
                this.index = index;
                this.solvedDepth = solvedDepth;
                this.color = color;
            }
        }

        private bool? _solvedCache;
        
        public int spindleCount { get; set; }
        public int rowCount { get; set; }


        private Dictionary<int, Stack<Bead>> _spindles;

        public Dictionary<int, Stack<Bead>> Spindles {
            get { return _spindles; }
            set {
                _solvedCache = null;
                _spindles = value;
            }
        }

        public BeadSet() {
            this.spindleCount = 0;
            this.rowCount = 0;
            this._spindles = new Dictionary<int, Stack<Bead>>();
        }

        public BeadSet(int spindleCount, int rowCount, Dictionary<int, Stack<Bead>> Spindles) {
            this.spindleCount = spindleCount;
            this.rowCount = rowCount;
            this._spindles = Spindles;
        }

        /// <summary>
        /// isSetSolved - Cached solved state
        /// </summary>
        /// <returns>true if solved</returns>
        public bool isSetSolved() {
            if (_solvedCache != null) {
                return _solvedCache.Value;
            }
            var lastSpindle = Spindles.Last();
            var allFull = Spindles.Where(kvp => kvp.Key != lastSpindle.Key).Select(v => v.Value).All(spindleBeads => {
                // all rowCount beads present, single color
                return spindleBeads.Count() == rowCount && spindleBeads.Distinct().Count() == 1;
            });
            var lastEmpty = lastSpindle.Value.Count == 0;
            _solvedCache = allFull && lastEmpty;
            return _solvedCache.Value;
        }

        /// <summary>
        /// IsSpindleSolved
        /// </summary>
        /// <param name="spindleIndex"></param>
        /// <returns>true if specified spindle is solved - all colors are the same</returns>
        public bool IsSpindleSolved(int spindleIndex) {
            var val = this.Spindles[spindleIndex];
            if (val.Count == this.rowCount) {
                var lastColor = val.Last().Color;
                var solvedCount = val.Reverse().TakeWhile(b => b.Color == lastColor).Count();
                return solvedCount == this.rowCount;
            }
            return false;
        }

        /// <summary>
        /// GetMostSolvedSpindle
        /// </summary>
        /// <param name="restrictedIndex">Spindles that should be ignored</param>
        /// <returns>Last spindle that is mostly solved (but not entirely)</returns>
        public SpindleInfo GetMostSolvedSpindle(int[] restrictedIndex) {
            var maxSolvedSpindleIndex = -1;
            var maxSolvedCount = -1;
            ConsoleColor color = ConsoleColor.Black;
            foreach (var kvp in Spindles) {
                if (restrictedIndex.Contains(kvp.Key)) {
                    continue;
                }
                if (kvp.Value.Count() > 0) {
                    var lastColor = kvp.Value.Last().Color;
                    var solvedCount = kvp.Value.Reverse().TakeWhile(b => b.Color == lastColor).Count();
                    // don't pick already solved spindles
                    if (solvedCount > maxSolvedCount && solvedCount < this.rowCount) {
                        maxSolvedCount = solvedCount;
                        maxSolvedSpindleIndex = kvp.Key;
                        color = lastColor;
                    }
                }
            }
            return new SpindleInfo(maxSolvedSpindleIndex, maxSolvedCount, color);
        }

        public int GetSpindleSolveCount() {
            int count = 0;
            foreach (var kvp in Spindles) {
                if (IsSpindleSolved(kvp.Key)) { count++; }
            }
            return count;
        }



        public bool MoveBead(int sp1, int sp2, string strategy) {
            if (
                sp1 == sp2 || // not same
                sp1 < 0 || sp2 < 0 || // not negative
                sp1 > spindleCount - 1 || sp2 > spindleCount - 1 || // not out of limits
                Spindles[sp1].Count == 0 || // source is empty
                Spindles[sp2].Count == rowCount // target is full
            ) {
                return false; // invalid move, do nothing
            }
            _solvedCache = null;
            var bead = Spindles[sp1].Pop();
            Spindles[sp2].Push(bead);
            return true;
        }
        
        public static string Serialize(BeadSet bs) {
            var options = new JsonSerializerOptions() {
                IncludeFields = true,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            };
            return JsonSerializer.Serialize(bs, options);
        }


        /// <summary>
        /// Generates rangom bead set
        /// </summary>
        /// <param name="spindleCount">number of spindles</param>
        /// <param name="rowCount">number of beads on spindle</param>
        /// <returns></returns>
        public static BeadSet GenerateRandom(int spindleCount, int rowCount, int? seed ) {
            var beadColors = new[] { ConsoleColor.Red, ConsoleColor.White, ConsoleColor.Blue, ConsoleColor.Green, ConsoleColor.Yellow };
            var availableColors = new List<ConsoleColor>();
            availableColors.AddRange(beadColors);
            var beadCounts = new Dictionary<ConsoleColor, int>();

            var rnd = new Random(seed.HasValue ? seed.Value : (int)DateTime.Now.Ticks);
            //initiate counts 
            for (var bc = 0; bc < beadColors.Length; bc++) {
                beadCounts.Add(beadColors[bc], 0);
            }
            var beads = new Dictionary<int, Stack<Bead>>();
            // last spindle should be empty
            for (var sp = 0; sp < spindleCount - 1; sp++) {
                beads.Add(sp, new Stack<Bead>());
                for (var r = 0; r < rowCount; r++) {
                    var rColor = availableColors[rnd.Next(availableColors.Count)];
                    // next bead will exaust color pool, need to remove
                    if (beadCounts[rColor] == rowCount - 1) {
                        availableColors.Remove(rColor);
                    }
                    beadCounts[rColor]++;
                    beads[sp].Push(new Bead(rColor));
                }
            }
            // last spindle is empty
            beads.Add(spindleCount - 1, new Stack<Bead>());
            return new BeadSet(spindleCount, rowCount, beads);
        }
    }
}
