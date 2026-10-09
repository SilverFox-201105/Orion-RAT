using System.IO;

namespace Orion.Common.Extensions
{
    public static class DriveTypeExtensions
    {
        /// <summary>
        /// Converts the value of the <see cref="DriveType"/> instance to its friendly string representation.
        /// </summary>
        /// <param name="type">The <see cref="DriveType"/>.</param>
        /// <returns>The friendly string representation of the value of this <see cref="DriveType"/> instance.</returns>
        public static string ToFriendlyString(this DriveType type)
        {
            switch (type)
            {
                case DriveType.Fixed:
                    return "Local Disk";
                case DriveType.Network:
                    return "网络驱动器";
                case DriveType.Removable:
                    return "可移动驱动器";
                default:
                    return type.ToString();
            }
        }
    }
}
