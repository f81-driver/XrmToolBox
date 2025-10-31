namespace Formula81.XrmToolBox.Libraries.Core.Extensions
{
    public static class StringEx
    {
        public static string NullifyEmptyString(this string str)
        {
            return string.IsNullOrEmpty(str) ? null : str;
        }
    }
}
