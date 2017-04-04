using Microsoft.CodeAnalysis;
using System;

namespace FUR10N.NullContracts
{
    public class ParseFailedException : Exception
    {
        public Location Location { get; }

        public ParseFailedException(Location location, string message, Exception exception = null)
            : base(message, exception)
        {
            this.Location = location;
        }
    }
}
