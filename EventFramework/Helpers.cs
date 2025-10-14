
namespace EventFramework
{
    public static class EventFrameworkHelpers{
        public static CustomDictionary GetCustomDictionary(this IEventProxy proxy){
            if(proxy is CustomDictionary cd){
                return cd;
            }
            return null;
        }
    }
}