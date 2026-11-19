public static class TextFilter
{
    public static string RemoveTags(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Replace("<", string.Empty).Replace(">", string.Empty);
    }
}
