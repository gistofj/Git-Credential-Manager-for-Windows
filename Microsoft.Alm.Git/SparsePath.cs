using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Alm.Git
{
    public sealed class SparsePath : IComparable<SparsePath>, IComparable<string>, IEquatable<SparsePath>, IEquatable<string>
    {
        public static readonly IReadOnlyCollection<char> SpecialCharacters = new char[] { '#', '!' };

        private SparsePath(string value)
        {
            this.Value = value;
        }

        public string Value
        {
            get { return _value; }
            set { _value = CleanPath(value); }
        }
        private string _value;

        public static string CleanPath(string value)
        {
            if (value == null)
                return null;

            if (value.Length == 0)
                return String.Empty;

            StringBuilder buffer = new StringBuilder(value);

            // remove any preceeding white space
            while (Char.IsWhiteSpace(buffer[0]))
            {
                buffer.Remove(0, 1);
            }

            // remove any trailing white space
            while (Char.IsWhiteSpace(buffer[buffer.Length - 1]))
            {
                buffer.Remove(buffer.Length - 1, 1);
            }

            for (int i = 0; i < buffer.Length; i++)
            {
                if (SpecialCharacters.Contains(buffer[i]))
                {
                    buffer.Insert(i, '\\');
                    i += 1;
                }
                else if (Char.IsUpper(buffer[i]))
                {
                    buffer[i] = Char.ToLowerInvariant(buffer[i]);
                }
            }

            // get the result then escape it to make Git happy
            string result = buffer.ToString();
            result = Uri.EscapeUriString(result);

            return result;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public int CompareTo(SparsePath other)
        {
            return String.Compare(_value, other._value, StringComparison.OrdinalIgnoreCase);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public int CompareTo(string other)
        {
            return String.Compare(_value, other, StringComparison.OrdinalIgnoreCase);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool Equals(SparsePath other)
        {
            return this == other;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool Equals(string other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            return this == obj as SparsePath;
        }

        public override int GetHashCode()
        {
            return (_value == null)
                ? 0
                : _value.GetHashCode();
        }

        public override String ToString()
        {
            return _value ?? String.Empty;
        }

        public static bool operator ==(SparsePath path1, SparsePath path2)
        {
            if (ReferenceEquals(path1, path2))
                return true;
            if (ReferenceEquals(path1, null) || ReferenceEquals(null, path2))
                return false;

            return String.Equals(path1._value, path2._value, StringComparison.OrdinalIgnoreCase);
        }

        public static bool operator !=(SparsePath path1, SparsePath path2)
        {
            return !(path1 == path2);
        }

        public static bool operator ==(SparsePath path1, string path2)
        {
            if (ReferenceEquals(path1, path2))
                return true;
            if (ReferenceEquals(path1, null) || ReferenceEquals(path2, null))
                return false;

            return String.Equals(path1._value, path2, StringComparison.OrdinalIgnoreCase);
        }

        public static bool operator !=(SparsePath path1, string path2)
        {
            return !(path1 == path2);
        }

        public static bool operator ==(string path1, SparsePath path2)
        {
            if (ReferenceEquals(path1, path2))
                return true;
            if (ReferenceEquals(path1, null) || ReferenceEquals(null, path2))
                return false;

            return String.Equals(path1, path2._value, StringComparison.OrdinalIgnoreCase);
        }

        public static bool operator !=(string path1, SparsePath path2)
        {
            return !(path1 == path2);
        }

        public static bool operator <(SparsePath path1, SparsePath path2)
        {
            if (ReferenceEquals(path1, path2))
                return false;
            if (ReferenceEquals(path1, null) || ReferenceEquals(null, path2))
                return false;

            return path1.CompareTo(path2) < 0;
        }

        public static bool operator >(SparsePath path1, SparsePath path2)
        {
            if (ReferenceEquals(path1, path2))
                return false;
            if (ReferenceEquals(path1, null) || ReferenceEquals(null, path2))
                return false;

            return path1.CompareTo(path2) > 0;
        }

        public static bool operator <(SparsePath path1, string path2)
        {
            if (ReferenceEquals(path1, path2))
                return false;
            if (ReferenceEquals(path1, null) || ReferenceEquals(null, path2))
                return false;

            return path1.CompareTo(path2) < 0;
        }

        public static bool operator >(SparsePath path1, string path2)
        {
            if (ReferenceEquals(path1, path2))
                return false;
            if (ReferenceEquals(path1, null) || ReferenceEquals(null, path2))
                return false;

            return path1.CompareTo(path2) > 0;
        }

        public static bool operator <(string path1, SparsePath path2)
        {
            if (ReferenceEquals(path1, path2))
                return false;
            if (ReferenceEquals(path1, null) || ReferenceEquals(null, path2))
                return false;

            return path1.CompareTo(path2) < 0;
        }

        public static bool operator >(string path1, SparsePath path2)
        {
            if (ReferenceEquals(path1, path2))
                return false;
            if (ReferenceEquals(path1, null) || ReferenceEquals(null, path2))
                return false;

            return path1.CompareTo(path2) > 0;
        }

        public static bool operator <=(SparsePath path1, SparsePath path2)
        {
            if (ReferenceEquals(path1, path2))
                return false;
            if (ReferenceEquals(path1, null) || ReferenceEquals(null, path2))
                return false;

            return path1.CompareTo(path2) <= 0;
        }

        public static bool operator >=(SparsePath path1, SparsePath path2)
        {
            if (ReferenceEquals(path1, path2))
                return false;
            if (ReferenceEquals(path1, null) || ReferenceEquals(null, path2))
                return false;

            return path1.CompareTo(path2) >= 0;
        }

        public static bool operator <=(SparsePath path1, string path2)
        {
            if (ReferenceEquals(path1, path2))
                return false;
            if (ReferenceEquals(path1, null) || ReferenceEquals(null, path2))
                return false;

            return path1.CompareTo(path2) <= 0;
        }

        public static bool operator >=(SparsePath path1, string path2)
        {
            if (ReferenceEquals(path1, path2))
                return false;
            if (ReferenceEquals(path1, null) || ReferenceEquals(null, path2))
                return false;

            return path1.CompareTo(path2) >= 0;
        }

        public static bool operator <=(string path1, SparsePath path2)
        {
            if (ReferenceEquals(path1, path2))
                return false;
            if (ReferenceEquals(path1, null) || ReferenceEquals(null, path2))
                return false;

            return path1.CompareTo(path2) <= 0;
        }

        public static bool operator >=(string path1, SparsePath path2)
        {
            if (ReferenceEquals(path1, path2))
                return false;
            if (ReferenceEquals(path1, null) || ReferenceEquals(null, path2))
                return false;

            return path1.CompareTo(path2) >= 0;
        }

        public static implicit operator string (SparsePath path)
        {
            if (ReferenceEquals(path, null))
                return null;

            return path._value;
        }

        public static implicit operator SparsePath(string path)
        {
            if (ReferenceEquals(path, null))
                return null;

            return new SparsePath(path);
        }
    }
}
