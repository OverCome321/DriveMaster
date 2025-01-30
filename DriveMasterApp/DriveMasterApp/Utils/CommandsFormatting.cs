namespace DriveMasterApp.Utils
{
    public static class CommandsFormatting
    {
        /// <summary>
        /// Статичный метод для преобразования строки в формат /команда\r
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static string GetCommandWithFormatting(string command)
        {
            return $"/{command}\r";
        }
    }
}
