using Newtonsoft.Json.Linq;
using System;

namespace Formula81.XrmToolBox.Libraries.Json.Extensions
{
    public static class JTokenEx
    {
        public static bool TryValue<T>(this JToken jToken, out T value)
        {
            try
            {
                value = jToken.Value<T>();
                return true;
            }
            catch (Exception)
            {
                value = default;
                return false;
            }
        }
    }
}
