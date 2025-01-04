using System.Text.Json.Serialization;

namespace puzzle_tube_sorter.model {
    public class Bead {
        [JsonPropertyName("C")]
        public ConsoleColor Color { get; set; }

        public Bead(ConsoleColor Color) {
            this.Color = Color;
        }

        public override bool Equals(object? obj) {
            return obj is Bead bead && Color == bead.Color;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Color);
        }

        public override string? ToString() {
            return Color.ToString();
        }

    }
}
