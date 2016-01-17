using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicConcept.Library
{
    public static class SortingHelper
    {
        public static readonly char SymbolChar = '#';
        public static readonly string LatinAlphabet = "abcdefghijklmnopqrstuvwxyz";


        public static readonly Dictionary<string, string> Alphabets = new Dictionary<string, string>
        {
            {"Latin", LatinAlphabet}, 
            {"Cyrillic", "абвгдеёжзийклмнопрстуфхцчшщъыьэюя"},
            {"Greek", "αβγδεζηθικλμνξοπρσςτυφχψω"}
        };

        public static char GetAlphabetGroup(string name, string alphabet)
        {
            if (!alphabet.Contains(SymbolChar))
                throw new ArgumentException("alphabet must contain the SymbolChar.");

            char c = name.ToLower().FirstOrDefault();
            if (alphabet.Contains(c))
                return c;
            else
                return SymbolChar;
        }
    }
}
