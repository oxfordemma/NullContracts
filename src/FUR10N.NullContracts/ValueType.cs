using System;

namespace FUR10N.NullContracts
{
    public enum ValueType
    {
        /// <summary>
        /// We don't know if it is null or not
        /// </summary>
        MaybeNull,
        /// <summary>
        /// We know that it cannot be null
        /// </summary>
        NotNull,
        /// <summary>
        /// We know that is is null
        /// </summary>
        Null,
        /// <summary>
        /// This is a throw-away value. It's cheaper to do this than make the enum nullable.
        /// </summary>
        OutOfRange
    }
}
