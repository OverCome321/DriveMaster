namespace DriveMasterApp.Utils
{
    public static class CommandsFormatting
    {
        public static string GetCommandWithFormatting(string command)
        {
            return $"/{command}\r";
        }
    }
}
