namespace Orion.Server.Utilities
{
    public static class ServerVersion
    {
        public const string AppName = "Orion";

        public const string Current = "6.0.0";

        public static string Display => $"v{Current}";

        public static string WindowTitle => $"{AppName} - {Display}";
    }
}
