namespace Autabee.OpcScout
{
    public static class AutabeeDictionaryExtension
    {
        public static void Add<T, D>(this Dictionary<T, D> dict, KeyValuePair<T, D> keyValuePair)
        {
            dict.Add(keyValuePair.Key, keyValuePair.Value);
        }
    }
}
