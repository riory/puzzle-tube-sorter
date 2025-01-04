using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace puzzle_tube_sorter.ui {
    public static class ConsolePrimitives {
        private static ConsoleColor _fg, _bg;
        private static void storeColors() {
            _fg = Console.ForegroundColor;
            _bg = Console.BackgroundColor;
        }
        private static void restoreColors() {
            Console.ForegroundColor = _fg;
            Console.BackgroundColor = _bg;
        }


        /// <summary>
        /// DrawRectangle - 
        /// </summary>
        /// <param name="foreGround"></param>
        /// <param name="backGround"></param>
        /// <param name="x1"></param>
        /// <param name="x2"></param>
        /// <param name="y1"></param>
        /// <param name="y2"></param>
        /// <param name="filler"></param>
        public static void DrawRectangle(ConsoleColor foreGround, ConsoleColor backGround, int x1, int x2, int y1, int y2, char filler = '#') {
            if (x1 >= x2 || y1 >= y2) {
                return;
            }
            storeColors();
            Console.ForegroundColor = foreGround;
            Console.BackgroundColor = backGround;

            var str = new string(filler, x2 - x1);
            for (int y = y1; y < y2; y++) {
                Console.SetCursorPosition(x1, y);
                Console.Write(str);
            }
            restoreColors();
        }

        public static void DrawFullString(int y, string text, ConsoleColor foreGround = ConsoleColor.White) {
            storeColors();
            Console.ForegroundColor = foreGround;
            Console.SetCursorPosition(0, y);
            var pad = Console.WindowWidth - text.Length - 1;
            Console.Write(text + new string(' ', pad));
            restoreColors();
        }
    }
}
