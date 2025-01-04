namespace puzzle_tube_sorter.model.solvers {
    /// <summary>
    /// Bogo solver - move beads randomly, prays for miracle
    /// </summary>
    public class BogoSolver : ISolver {
        private Random _r;
        public BogoSolver() {
            _r = new Random();
        }

        public string Name => "BogoSolver";

        Step ISolver.GetStep(BeadSet bset) {
            var s1 = _r.Next(0, bset.Spindles.Count);
            var s2 = _r.Next(0, bset.Spindles.Count);
            return new Step(s1, s2, false, "random");
        }
    }
}
