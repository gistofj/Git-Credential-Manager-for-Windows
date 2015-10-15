using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Microsoft.Alm.Git
{
    public sealed class GitPathish : IComparable<GitPathish>, IComparable<string>, IEquatable<GitPathish>, IEquatable<string>
    {
        public const char CommentCharacter = '#';
        public const char EscapeCharacter = '\\';
        public const char ExclusionCharacter = '!';
        public const char PathSeparatorCharacter = '/';
        public const char WildCardCharacter = '*';

        public GitPathish(string value, GitPathishType type)
        {
            string pattern = CleanPattern(value, type);
            _buffer = new StringBuilder(pattern ?? String.Empty);
        }

        public char this[int index]
        {
            get
            {
                lock (@lock)
                {
                    Debug.Assert(index >= 0, "The `index` parameter is less than zero.");
                    Debug.Assert(_buffer != null, "The `_value` member is null.");
                    Debug.Assert(_buffer == null || index < _buffer.Length, "The `index` parameter is out of bounds.");

                    return (index < 0 || _buffer == null || index >= _buffer.Length)
                        ? '\0'
                        : _buffer[index];
                }
            }
        }

        public bool IsComment
        {
            get { return Type == GitPathishType.Comment; }
            set
            {
                lock (@lock)
                {
                    if (!IsEmpty)
                    {
                        if (value && !IsComment)
                        {
                            _buffer.Insert(0, CommentCharacter);
                            _type = GitPathishType.Comment;
                        }
                        else if (!value && IsComment)
                        {
                            _buffer.Remove(0, 1);
                            _type = GitPathishType.None;
                        }
                    }
                }
            }
        }

        public bool IsEmpty
        {
            get { return Type == GitPathishType.Empty; }
        }

        public bool IsExclusive
        {
            get { return Type == GitPathishType.Exclusive; }
            set
            {
                lock (@lock)
                {
                    if (!IsEmpty)
                    {
                        if (value && IsInclusive)
                        {
                            _buffer.Insert(0, ExclusionCharacter);
                            _type = GitPathishType.Comment;
                        }
                        else if (!value && IsExclusive)
                        {
                            _buffer.Remove(0, 1);
                            _type = GitPathishType.None;
                        }
                    }
                }
            }
        }

        public bool IsInclusive
        {
            get { return Type == GitPathishType.Inclusive; }
        }

        public string Pattern
        {
            get
            {
                lock (@lock)
                {
                    switch (Type & GitPathishType.Patterns)
                    {
                        case GitPathishType.Exclusive:
                            return _buffer.ToString(1, _buffer.Length - 1);

                        case GitPathishType.Inclusive:
                            return _buffer.ToString(0, _buffer.Length - 0);

                        default:
                        case GitPathishType.None:
                            return String.Empty;
                    }
                }
            }
        }

        public GitPathishType Type
        {
            get
            {
                lock (@lock)
                {
                    if (_type == GitPathishType.None)
                    {
                        if (_buffer.Length == 0)
                        {
                            _type = GitPathishType.Empty;
                        }
                        else if (_buffer[0] == CommentCharacter)
                        {
                            _type = GitPathishType.Comment;
                        }
                        else if (_buffer[0] == ExclusionCharacter)
                        {
                            _type = GitPathishType.Exclusive;
                        }
                        else
                        {
                            _type = GitPathishType.Inclusive;
                        }
                    }

                    return _type;
                }
            }
        }

        public string Value
        {
            get { lock (@lock) return _buffer.ToString(); }
            private set
            {
                lock (@lock)
                {
                    _buffer.Clear();
                    _buffer.Append(value);
                    _type = GitPathishType.None;
                }
            }
        }

        private readonly object @lock = new object();

        private GitPathishType _type;
        private readonly StringBuilder _buffer;

        public static string CleanPattern(string value, GitPathishType type)
        {
            Debug.Assert(Enum.IsDefined(typeof(GitPathishType), type), "The `type` parameter is not defined.");
            Debug.Assert(type != GitPathishType.None, "The `type` parameter as 'None' is undefined.");

            if (value == null)
                return null;

            if (value.Length == 0 || type == GitPathishType.Empty || type == GitPathishType.None)
                return String.Empty;

            // insure that it's a defined type of pattern
            if (!Enum.IsDefined(typeof(GitPathishType), type))
            {
                type = GitPathishType.AnyOrAll;
            }

            if (type == GitPathishType.Empty)
                return String.Empty;

            if (type == GitPathishType.Comment && value[0] != CommentCharacter)
                return String.Empty;

            /**
                The logic presented here is based on the documenation found at:
                    http://git-scm.com/docs/gitignore.
            **/

            StringBuilder buffer = new StringBuilder(value);

            // remove any preceeding white space.
            while (buffer.Length > 0 && Char.IsWhiteSpace(buffer[0]))
            {
                buffer.Remove(0, 1);
            }

            // remove any trailing white space.
            while (buffer.Length > 0 && Char.IsWhiteSpace(buffer[buffer.Length - 1]))
            {
                // the space is intentional, stop stripping spaces.
                if (buffer[buffer.Length - 2] != EscapeCharacter)
                    break;

                buffer.Remove(buffer.Length - 1, 1);
            }

            if (buffer.Length > 0)
            {
                // a line starting with # serves as a comment. Put a backslash ("\") in front of the first # for patterns that begin with a #.
                // a line starting with ! serves as an exclusion. Put a backslash ("\") in front of the first ! for patterns that begin with a !.
                switch (buffer[0])
                {
                    // if the first character is a comment character, but comments are not expected remove the character.
                    case CommentCharacter:
                        {
                            if ((type & GitPathishType.Comment) != GitPathishType.Comment)
                            {
                                buffer.Insert(0, EscapeCharacter);
                            }
                        }
                        break;

                    // if the first character is a exclusion character, but exclusion are not expected remove the character.
                    case ExclusionCharacter:
                        {
                            if ((type & GitPathishType.Exclusive) != GitPathishType.Exclusive)
                            {
                                buffer.Insert(0, EscapeCharacter);
                            }
                        }
                        break;
                }
            }

            // if the pattern ends with a slash, it is removed for the purpose of the following description, but it would only find a match with a directory.
            while (buffer.Length > 0 && buffer[buffer.Length - 1] == PathSeparatorCharacter)
            {
                buffer.Remove(buffer.Length - 1, 1);
            }

            if (buffer.Length == 0)
                return String.Empty;

            string result = buffer.ToString();
            return result;
        }

        public static int Compare(GitPathish path1, GitPathish path2, bool ignoreCase)
        {
            if (ReferenceEquals(path1, null))
                throw new ArgumentNullException("path1", "The `path1` cannot be null.");
            if (ReferenceEquals(null, path2))
                throw new ArgumentNullException("path2", "The `path2` cannot be null.");

            using (path1.Lock())
            using (path2.Lock())
            {
                return InternalCompare(path1, path2, ignoreCase);
            }
        }
        public static int Compare(GitPathish path1, GitPathish path2)
            => Compare(path1, path2, false);

        public static int Compare(GitPathish path1, string path2, bool ignoreCase)
        {
            if (ReferenceEquals(path1, null))
                throw new ArgumentNullException("path1", "The `path1` cannot be null.");
            if (ReferenceEquals(null, path2))
                throw new ArgumentNullException("path2", "The `path2` cannot be null.");

            using (path1.Lock())
            {
                return InternalCompare(path1, path2, ignoreCase);
            }
        }
        public static int Compare(GitPathish path1, string path2)
            => Compare(path1, path2, false);

        public static int Compare(string path1, GitPathish path2, bool ignoreCase)
        {
            if (ReferenceEquals(path1, null))
                throw new ArgumentNullException("path1", "The `path1` cannot be null.");
            if (ReferenceEquals(null, path2))
                throw new ArgumentNullException("path2", "The `path2` cannot be null.");

            using (path2.Lock())
            {
                return InternalCompare(path1, path2, ignoreCase);
            }
        }
        public static int Compare(string path1, GitPathish path2)
            => Compare(path1, path2, false);

        public int CompareTo(GitPathish other)
        {
            return Compare(this, other);
        }

        public int CompareTo(string other)
        {
            return Compare(this, other);
        }

        public bool Equals(GitPathish other)
        {
            return this == other;
        }

        public bool Equals(string other)
        {
            return Compare(this, other) == 0;
        }

        public override bool Equals(object obj)
        {
            return Compare(this, obj as GitPathish) == 0;
        }

        public override int GetHashCode()
        {
            const int Largest32BitPrime = 2147483647;

            lock (@lock)
            {
                unchecked
                {
                    int hash = 0;
                    int length = _buffer.Length;

                    for (int i = 0; i < length; i++)
                    {
                        hash = ((hash << 5) + Largest32BitPrime) ^ _buffer[i];
                    }

                    return hash;
                }
            }
        }

        public static bool IsSubsumed(GitPathish major, GitPathish minor)
        {
            const string Depth1Match = "*";
            const string DepthNMatch = "**";

            using (major.Lock())
            using (minor.Lock())
            {
                if (InternalCompare(major, minor, false) == 0)
                    return true;

                if ((major.Type & GitPathishType.Patterns) == GitPathishType.None
                    || (minor.Type & GitPathishType.Patterns) == GitPathishType.None)
                    return false;

                if (String.Equals(major.Pattern, Depth1Match, StringComparison.Ordinal))
                    return true;

                string[] majors;
                string[] minors;

                if (InternalSegment(major, out majors) && InternalSegment(minor, out minors))
                {
                    int a = 0;
                    int b = 0;

                    // keep walking until the entire length of at least one has been walked
                    while (b < minors.Length && a < majors.Length)
                    {
                        // while comprehensive and subordinate paths match, continue walking them
                        while (a < majors.Length && b < minors.Length
                            && String.Equals(majors[a], minors[b], StringComparison.Ordinal))
                        {
                            a += 1;
                            b += 1;
                        }

                        // if the subordinate or comprehensive is a wildcard, step both
                        if ((a < majors.Length && String.Equals(majors[a], Depth1Match, StringComparison.Ordinal))
                            || (b < minors.Length && String.Equals(minors[b], Depth1Match, StringComparison.Ordinal)))
                        {
                            a += 1;
                            b += 1;
                        }

                        // while the comprehensive is an infinite wildcard, walk the subordinate
                        while (b < minors.Length
                            && a < majors.Length
                            && String.Equals(majors[a], DepthNMatch, StringComparison.Ordinal))
                        {
                            b += 1;

                            // look ahead and if the comprehensive's next step match the subordinate, step the comprehensive
                            if (a + 1 < majors.Length && b < minors.Length
                                && String.Equals(majors[a + 1], minors[b], StringComparison.Ordinal))
                            {
                                a += 1;
                            }
                        }

                        // while the subordinate is an infinite wildcard, walk the comprehensive
                        while (a < majors.Length
                            && b < minors.Length
                            && String.Equals(minors[b], DepthNMatch, StringComparison.Ordinal))
                        {
                            a += 1;

                            // look ahead and if the subordinate's next step match the comprehensive, step the child
                            if (b + 1 < minors.Length && a < majors.Length
                                && String.Equals(minors[b + 1], majors[a], StringComparison.Ordinal))
                            {
                                b += 1;
                            }
                        }
                    }

                    // if we've walked the entire minor, this it is subsumed or major
                    return (b >= minors.Length);
                }
            }

            return false;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrEmpty(GitPathish path)
        {
            return (ReferenceEquals(path, null) || path.IsEmpty);
        }

        public override string ToString()
        {
            return Value;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal static int InternalCompare(GitPathish path1, GitPathish path2, bool ignoreCase)
        {
            Debug.Assert(Monitor.IsEntered(path1.@lock), "Expected lock on `path1` parameter not held.");
            Debug.Assert(Monitor.IsEntered(path2.@lock), "Expected lock on `path2` parameter not held.");

            if (ReferenceEquals(path1, path2))
                return 0;

            var a = path1._buffer;
            var b = path2._buffer;
            int len = Math.Min(a.Length, b.Length);

            int result = 0;
            for (int i = 0; i < len; i++)
            {
                char ca = ignoreCase
                    ? Char.ToLowerInvariant(a[i])
                    : a[i];
                char cb = ignoreCase
                    ? Char.ToLowerInvariant(b[i])
                    : b[i];

                if ((result = ca - cb) != 0)
                    return result;
            }

            return a.Length - b.Length;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal static int InternalCompare(GitPathish path1, string path2, bool ignoreCase)
        {
            Debug.Assert(Monitor.IsEntered(path1.@lock), "Expected lock on `path1` parameter not held.");

            if (ReferenceEquals(path1, path2))
                return 0;

            var a = path1._buffer;
            var b = path2;
            int len = Math.Min(a.Length, b.Length);

            int result = 0;
            for (int i = 0; i < len; i++)
            {
                char ca = ignoreCase
                    ? Char.ToLowerInvariant(a[i])
                    : a[i];
                char cb = ignoreCase
                    ? Char.ToLowerInvariant(b[i])
                    : b[i];

                if ((result = ca - cb) != 0)
                    return result;
            }

            return a.Length - b.Length;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal static int InternalCompare(string path1, GitPathish path2, bool ignoreCase)
        {
            Debug.Assert(Monitor.IsEntered(path2.@lock), "Expected lock on `path2` parameter not held.");

            if (ReferenceEquals(path1, path2))
                return 0;

            var a = path1;
            var b = path2._buffer;
            int len = Math.Min(a.Length, b.Length);

            int result = 0;
            for (int i = 0; i < len; i++)
            {
                char ca = ignoreCase
                    ? Char.ToLowerInvariant(a[i])
                    : a[i];
                char cb = ignoreCase
                    ? Char.ToLowerInvariant(b[i])
                    : b[i];

                if ((result = ca - cb) != 0)
                    return result;
            }

            return a.Length - b.Length;
        }

        internal static bool InternalSegment(GitPathish instance, out string[] segments)
        {
            if (ReferenceEquals(instance, null))
            {
                segments = new string[0];
                return false;
            }

            segments = instance.Pattern.Split(PathSeparatorCharacter);
            return true;
        }

        private Releaser Lock()
        {
            Monitor.Enter(@lock);

            return new Releaser(this.Unlock);
        }

        private void Unlock()
        {
            Monitor.Exit(@lock);
        }

        public static bool operator ==(GitPathish path1, GitPathish path2)
        {
            if (ReferenceEquals(path1, path2))
                return true;
            if (ReferenceEquals(path1, null) || ReferenceEquals(null, path2))
                return false;

            using (path1.Lock())
            using (path2.Lock())
            {
                return InternalCompare(path1, path2, false) == 0;
            }
        }

        public static bool operator !=(GitPathish path1, GitPathish path2)
        {
            return !(path1 == path2);
        }

        public static bool operator ==(GitPathish path1, string path2)
        {
            if (ReferenceEquals(path1, path2))
                return true;
            if (ReferenceEquals(path1, null) || ReferenceEquals(null, path2))
                return false;

            using (path1.Lock())
            {
                return InternalCompare(path1, path2, false) == 0;
            }
        }

        public static bool operator !=(GitPathish path1, string path2)
        {
            return !(path1 == path2);
        }

        public static bool operator ==(string path1, GitPathish path2)
        {
            if (ReferenceEquals(path1, path2))
                return true;
            if (ReferenceEquals(path1, null) || ReferenceEquals(null, path2))
                return false;

            using (path2.Lock())
            {
                return InternalCompare(path1, path2, false) == 0;
            }
        }

        public static bool operator !=(string path1, GitPathish path2)
        {
            return !(path1 == path2);
        }

        public static bool operator <(GitPathish path1, GitPathish path2)
        {
            if (ReferenceEquals(path1, path2) || ReferenceEquals(path1, null) || ReferenceEquals(null, path2))
                return false;

            using (path1.Lock())
            using (path2.Lock())
            {
                return InternalCompare(path1, path2, false) < 0;
            }
        }

        public static bool operator >(GitPathish path1, GitPathish path2)
        {
            if (ReferenceEquals(path1, path2) || ReferenceEquals(path1, null) || ReferenceEquals(null, path2))
                return false;

            using (path1.Lock())
            using (path2.Lock())
            {
                return InternalCompare(path1, path2, false) < 0;
            }
        }

        public static bool operator <(GitPathish path1, string path2)
        {
            if (ReferenceEquals(path1, path2) || ReferenceEquals(path1, null) || ReferenceEquals(null, path2))
                return false;

            using (path1.Lock())
            {
                return InternalCompare(path1, path2, false) < 0;
            }
        }

        public static bool operator >(GitPathish path1, string path2)
        {
            if (ReferenceEquals(path1, path2) || ReferenceEquals(path1, null) || ReferenceEquals(null, path2))
                return false;

            using (path1.Lock())
            {
                return InternalCompare(path1, path2, false) > 0;
            }
        }

        public static bool operator <(string path1, GitPathish path2)
        {
            if (ReferenceEquals(path1, path2) || ReferenceEquals(path1, null) || ReferenceEquals(null, path2))
                return false;

            using (path2.Lock())
            {
                return InternalCompare(path1, path2, false) < 0;
            }
        }

        public static bool operator >(string path1, GitPathish path2)
        {
            if (ReferenceEquals(path1, path2) || ReferenceEquals(path1, null) || ReferenceEquals(null, path2))
                return false;

            using (path2.Lock())
            {
                return InternalCompare(path1, path2, false) > 0;
            }
        }

        public static bool operator <=(GitPathish path1, GitPathish path2)
        {
            return !(path1 > path2);
        }

        public static bool operator >=(GitPathish path1, GitPathish path2)
        {
            return !(path1 < path2);
        }

        public static bool operator <=(GitPathish path1, string path2)
        {
            return !(path1 > path2);
        }

        public static bool operator >=(GitPathish path1, string path2)
        {
            return !(path1 < path2);
        }

        public static bool operator <=(string path1, GitPathish path2)
        {
            return !(path1 > path2);
        }

        public static bool operator >=(string path1, GitPathish path2)
        {
            return !(path1 < path2);
        }

        public static implicit operator string (GitPathish path)
        {
            if (ReferenceEquals(path, null))
                return null;

            return path.Value;
        }

        public static explicit operator GitPathish(string path)
        {
            return new GitPathish(path, GitPathishType.AnyOrAll);
        }

        private struct Releaser : IDisposable
        {
            public Releaser(ReleaseDelegate release)
            {
                Debug.Assert(release != null, "The `release` parameter is null.");

                _release = release;
            }

            private ReleaseDelegate _release;

            public void Dispose()
            {
                if (_release != null)
                {
                    _release();
                    _release = null;
                }
            }
        }

        private delegate void ReleaseDelegate();
    }
}
