using System.Collections.Generic;

namespace puzzle_tube_sorter.model.solvers {
    /// <summary>
    /// RandomStrategySolver - uses strategy to solve
    /// 1. Search for most solved spindle (max same color beads from end)
    /// 2. Unload to free column 
    /// 2. For each spindle sequentially try to solve it
    /// </summary>
    public class RandomStrategySolver : ISolver {

        private enum Strategy {
            RandomUnload = 1,
            RandomLoad = 2,
            RandomPurge = 3,
            FinalMove = 4
        }

        private Strategy _strat;
        private BeadSet.SpindleInfo? _randomSpindle;
        private int _solveCount;
        private Random _r;

        public RandomStrategySolver() {
            this._strat = Strategy.RandomUnload;
            this._randomSpindle = null;
            this._solveCount = 0;
            _r = new Random();
        }

        public string Name => "RandomStrategySolver";

        Step ISolver.GetStep(BeadSet bset) {
            var unloadTarget = -1;
            while (true) {
                this._solveCount = bset.GetSpindleSolveCount();
                if (this._solveCount == bset.Spindles.Count - 1 && bset.Spindles.Last().Value.Count != 0) {
                    _strat = Strategy.FinalMove;
                }
                // Random unload attempts to shift all beads that as different from last color
                // target to unload is picked at random (excluding current selected spindle
                if (_strat == Strategy.RandomUnload) {
                    if (_randomSpindle == null) {
                        _randomSpindle = GetRandomUnsolvedSpindle(bset, []);
                    }
                    unloadTarget = GetUnloadTarget(bset, [_randomSpindle.index]);
                    if (unloadTarget != -1 && bset.Spindles[_randomSpindle.index].Count() > _randomSpindle.solvedDepth) {
                        return new Step(_randomSpindle.index, unloadTarget, false, _strat.ToString() + "_" + _randomSpindle.index);
                    }

                    //check if current spindle is solved
                    if (bset.IsSpindleSolved(_randomSpindle.index)) {
                        _randomSpindle = GetRandomUnsolvedSpindle(bset, [_randomSpindle.index]);
                    } else {
                        //nothing left to unload, attempt to load
                        _strat = Strategy.RandomLoad;
                    }
                }
                if (_strat == Strategy.RandomLoad) {
                    if (_randomSpindle != null && bset.IsSpindleSolved(_randomSpindle.index)) {
                        _randomSpindle = GetRandomUnsolvedSpindle(bset, [_randomSpindle.index]);
                        _strat = Strategy.RandomUnload;
                        continue;
                    }
                    // prevent looping in case unsolved and fully loaded
                    if (_randomSpindle == null || bset.Spindles[_randomSpindle.index].Count == bset.rowCount) {
                        _randomSpindle = GetRandomUnsolvedSpindle(bset, []);
                    }
                    // find matching color spindle/bead
                    var donor = GetDonor(bset, _randomSpindle);
                    if (donor.solvedDepth == 0) {
                        // top bead can be unloaded
                        return new Step(donor.index, _randomSpindle.index, false, _strat.ToString() + "_" + _randomSpindle.index);
                    } else {
                        // top bead can not be unloaded, dump to available spot
                        unloadTarget = GetUnloadTarget(bset, [_randomSpindle.index, donor.index]);
                        if (unloadTarget != -1) {
                            return new Step(donor.index, unloadTarget, false, _strat.ToString() + "_" + _randomSpindle.index);
                        }

                        // HACK - anti-locking behaviour
                        if (this._solveCount > (bset.Spindles.Count / 2) - 1) {
                            _strat = Strategy.RandomPurge;
                            _randomSpindle = GetRandomUnsolvedSpindle(bset, []);
                        } else {
                            // nowhere to unload, try solve another
                            _randomSpindle = null;
                            _strat = Strategy.RandomUnload;
                        }
                        
                    }
                }
                // purge random spindle completely
                if (_strat == Strategy.RandomPurge) {
                    unloadTarget = GetUnloadTarget(bset, [_randomSpindle.index]);
                    if (unloadTarget != -1 && bset.Spindles[_randomSpindle.index].Count() > 0) {
                        return new Step(_randomSpindle.index, unloadTarget, false, _strat.ToString() + "_" + _randomSpindle.index);
                    }
                    _randomSpindle = null;
                    _strat = Strategy.RandomLoad;
                }
                // free up last spindle if not already free
                if (_strat == Strategy.FinalMove) {
                    var lastIndex = bset.Spindles.Keys.Last();
                    unloadTarget = GetUnloadTarget(bset, [lastIndex]);
                    if (unloadTarget != -1) {
                        return new Step(lastIndex, unloadTarget, false, _strat.ToString() + "_" + lastIndex);
                    }
                }

            }

        }


        /// <summary>
        /// GetDonor - searches for 
        /// </summary>
        /// <param name="bset"></param>
        /// <param name="mostSolvedSpindle"></param>
        /// <returns></returns>
        private BeadSet.SpindleInfo GetDonor(BeadSet bset, BeadSet.SpindleInfo mostSolvedSpindle) {
            var minDepth = Int32.MaxValue;
            var minDepthIndex = -1;
            foreach (var kvp in bset.Spindles) {
                if (kvp.Value.Count() > 0 && kvp.Key != mostSolvedSpindle.index) {
                    var matchDepth = kvp.Value.TakeWhile(b => b.Color != mostSolvedSpindle.color).Count();
                    // has bead in spindle
                    if (matchDepth < minDepth && matchDepth < bset.rowCount) {
                        minDepth = matchDepth;
                        minDepthIndex = kvp.Key;
                    }
                }
            }
            return new BeadSet.SpindleInfo(minDepthIndex, minDepth, mostSolvedSpindle.color);
        }

        private BeadSet.SpindleInfo LoadMostEmpty(Dictionary<int, Stack<Bead>> collection, int[] restrictedIndex) {
            var minBeadCount = Int32.MaxValue;
            var minBeadIndex = -1;

            ConsoleColor color = ConsoleColor.Black;
            foreach (var kvp in collection) {
                if (kvp.Value.Count() > 0) {
                    var beadCount = kvp.Value.Count();
                    // don't pick already solved spindles
                    if (beadCount < minBeadCount) {
                        minBeadCount = beadCount;
                        minBeadIndex = kvp.Key;
                    }
                }
            }
            return new BeadSet.SpindleInfo(minBeadIndex, minBeadCount, color);
        }

        /// <summary>
        /// GetRandomUnsolvedSpindle - returns unsolved spindle at random, excluding restricted items
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="restrictedIndex"></param>
        /// <returns></returns>
        private BeadSet.SpindleInfo GetRandomUnsolvedSpindle(BeadSet bset, int[] restrictedIndex) {
            var collection = bset.Spindles;
            while (true) {
                var rind = _r.Next(0, collection.Count());
                var kvp = collection[rind];
                // consider for solving only if spindle has beads
                if (kvp.Count() > 0) {
                    var lastColor = kvp.Last().Color;
                    var solvedCount = kvp.Reverse().TakeWhile(b => b.Color == lastColor).Count();
                    if (solvedCount < bset.rowCount) {
                        return new BeadSet.SpindleInfo(rind, solvedCount, lastColor);
                    }
                }
            }
        }      

        /// <summary>
        /// Returns unload target that can be used for unloading, -1 if no target exists
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="spindle"></param>
        /// <returns></returns>
        private int GetUnloadTarget(BeadSet bset, int[] restrictedIndex) {
            var targets = new List<int>();
            var collection = bset.Spindles;
            foreach (var kvp in collection) {
                if (!restrictedIndex.Contains(kvp.Key) && kvp.Value.Count() < bset.rowCount) {
                    targets.Add(kvp.Key);
                }
            }
            if (targets.Count > 0) {
                // pick target at random
                return targets[_r.Next(0, targets.Count())];
            }
            return -1;
        }
    }
}
