using System;
using System.Linq;

namespace Linq2MongoDB.LINQPadDriver
{
    public static class StringSanitizer
    {
        public static string SanitizeCollectionName(string name)
        {
            var newName = RemoveAllUnallowedCharacters(name);
            if (newName.Length == 0)
            {
                return null;
            }

            if (char.IsLetter(newName[0]))
            {
                return new string(newName.Skip(1).Prepend(char.ToUpperInvariant(newName[0])).ToArray());
            }

            return new string(newName.Prepend('_').ToArray());
        }

        private static char[] RemoveAllUnallowedCharacters(string name)
            => Array.FindAll(name.ToCharArray(), c => char.IsLetterOrDigit(c) || c == '_');
    }
}
