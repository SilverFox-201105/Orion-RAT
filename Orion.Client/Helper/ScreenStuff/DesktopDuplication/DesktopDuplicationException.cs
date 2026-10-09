using System;

namespace Orion.Client.Helper.ScreenStuff.DesktopDuplication
{
    /// <summary>
    /// Exception thrown when an error occurs during desktop duplication operations.
    /// </summary>
    public class DesktopDuplicationException : Exception
    {
        public DesktopDuplicationException(string message) : base(message)
        {
        }

        public DesktopDuplicationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
