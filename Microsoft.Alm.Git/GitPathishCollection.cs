using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Microsoft.Alm.Git
{
    public sealed class GitPathishCollection : IEnumerable<GitPathish>
    {
        public GitPathishCollection(IEnumerable<GitPathish> collection, GitPathishType options)
        {
            Debug.Assert(Enum.IsDefined(typeof(GitPathishType), options), "The `options` parameter is undefined.");

            AcceptedTypes = options | GitPathishType.Patterns;

            if (collection == null)
            {
                _values = new List<GitPathish>();
            }
            else
            {
                _values = new List<GitPathish>(collection);
            }
        }

        public GitPathishCollection(IEnumerable<GitPathish> collection)
            : this(collection, GitPathishType.AnyOrAll)
        { }

        public GitPathishCollection()
            : this(null, GitPathishType.AnyOrAll)
        { }

        public readonly GitPathishType AcceptedTypes;

        public int Count
        {
            get { lock (@lock) return _values.Count; }
        }
        public bool IsEmpty
        {
            get { lock (@lock) return _values.Count == 0; }
        }
        public bool IsReadOnly
        {
            get { lock (@lock) return _isReadonly; }
            internal set { lock (@lock) _isReadonly = value; }
        }

        private readonly object @lock = new object();
        private readonly List<GitPathish> _values;

        private bool _isReadonly;

        public bool Add(GitPathish item)
        {
            Trace.WriteLine("GitPathishCollection::Add");

            bool result = false;

            lock (@lock)
            {
                if ((result = InternalAdd(item, GitPathishType.AnyOrAll)))
                {
                    InternalCheck();
                }
            }

            return result;
        }

        public bool Add(string pathish)
        {
            var item = (GitPathish)pathish;
            return Add(item);
        }

        public bool AddExclusive(GitPathish item)
        {
            Trace.WriteLine("GitPathishCollection::AddExclusive");

            bool result = false;

            lock (@lock)
            {
                if ((result = InternalAdd(item, GitPathishType.Inclusive)))
                {
                    InternalCheck();
                }
            }

            return result;
        }

        public bool AddExclusive(string pathish)
        {
            GitPathish item = (GitPathish)pathish;

            return AddExclusive(item);
        }

        public bool AddExclusive(IEnumerable<GitPathish> collection, out IEnumerable<GitPathish> added)
        {
            Trace.WriteLine("GitPathishCollection::AddExclusive");

            if (collection == null)
            {
                added = new GitPathish[0];
                return false;
            }

            int expected = collection.Count();

            if (expected == 0)
            {
                added = new GitPathish[0];
                return true;
            }

            var list = new List<GitPathish>(expected);

            lock (@lock)
            {
                foreach (var item in collection)
                {
                    if (InternalAdd(item, GitPathishType.Inclusive))
                    {
                        list.Add(item);
                    }

                    if (list.Count > 0)
                    {
                        InternalCheck();
                    }
                }
            }

            added = list;
            return list.Count == expected;
        }

        public bool AddInclusive(GitPathish item)
        {
            Trace.WriteLine("GitPathishCollection::AddInclusive");

            bool result = false;

            lock (@lock)
            {
                if ((result = InternalAdd(item, GitPathishType.Exclusive)))
                {
                    InternalCheck();
                }
            }

            return result;
        }

        public bool AddInclusive(string pathish)
        {
            GitPathish item = (GitPathish)pathish;

            return AddInclusive(item);
        }

        public bool AddInclusive(IEnumerable<GitPathish> collection, out IEnumerable<GitPathish> added)
        {
            Trace.WriteLine("GitPathishCollection::AddInclusive");

            if (collection == null)
            {
                added = new GitPathish[0];
                return false;
            }

            int expected = collection.Count();

            if (expected == 0)
            {
                added = new GitPathish[0];
                return true;
            }

            var list = new List<GitPathish>(expected);

            lock (@lock)
            {
                foreach (var item in collection)
                {
                    if (InternalAdd(item, GitPathishType.Exclusive))
                    {
                        list.Add(item);
                    }

                    if (list.Count > 0)
                    {
                        InternalCheck();
                    }
                }
            }

            added = list;
            return list.Count == expected;
        }

        public bool Clear()
        {
            lock (@lock)
            {
                return InternalClear();
            }
        }

        internal void InternalCheck()
        {
            Debug.Assert(Monitor.IsEntered(@lock), "The current thread does not hold the expected lock.");

            if (_isReadonly || IsEmpty)
                return;

            List<GitPathish> values = new List<GitPathish>(_values.Count);

            for (int i = 0; i < _values.Count; i++)
            {
                if (_values[i].IsExclusive)
                {
                    for (int j = 0; j < _values.Count; j++)
                    {
                        if (i == j)
                            continue;

                        
                    }
                }
            }
        }

        internal bool InternalAdd(GitPathish item, GitPathishType allowedTypes)
        {
            Debug.Assert(Monitor.IsEntered(@lock), "The current thread does not hold the expected lock.");

            if (_isReadonly
                || ReferenceEquals(item, null)
                || (item.IsComment && (AcceptedTypes & GitPathishType.Comment) != GitPathishType.Comment)
                || (item.IsEmpty && (AcceptedTypes & GitPathishType.Empty) != GitPathishType.Empty)
                || (item.IsExclusive && (allowedTypes & GitPathishType.Exclusive) != GitPathishType.Exclusive)
                || (item.IsInclusive && (allowedTypes & GitPathishType.Inclusive) != GitPathishType.Inclusive))
                return false;

            _values.Add(item);
            return true;
        }

        internal bool InternalClear()
        {
            Debug.Assert(Monitor.IsEntered(@lock), "The current thread does not hold the expected lock.");

            if (_isReadonly)
                return false;

            _values.Clear();
            return true;
        }

        #region IEnumerable<T> implementation
        IEnumerator<GitPathish> IEnumerable<GitPathish>.GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (this as IEnumerable<GitPathish>).GetEnumerator();
        }
        #endregion
    }
}
