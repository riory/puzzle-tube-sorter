using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace puzzle_tube_sorter.model.solvers
{
    public class Step {
        public int s1 { get; set; }
        public int s2 { get; set; }
        public bool stuck { get; set; }
        public string strategy { get; set; }

        public Step(int s1, int s2, bool stuck, string strategy) {
            this.s1 = s1;
            this.s2 = s2;
            this.stuck = stuck;
            this.strategy = strategy;
        }
    }
    public interface ISolver
    {
        string Name { get; }
        /// <summary>
        /// Next step to execute
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        Step GetStep(BeadSet bset);
    }
}
