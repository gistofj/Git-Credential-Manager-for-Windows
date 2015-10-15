using System;

namespace Microsoft.Alm.Git
{
    [Flags]
    public enum GitPathishType
    {
        None = 0,

        Comment = 1 << 0,
        Empty = 1 << 1,
        Exclusive = 1 << 2,
        Inclusive = 1 << 3,

        AnyOrAll = Comment | Empty | Exclusive | Inclusive,
        Patterns = Exclusive | Inclusive,
    }
}
