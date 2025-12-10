using System.Net;

namespace GoalSettingApp.Helpers
{
    /// <summary>
    /// Utility class for HTML encoding to prevent XSS attacks in email templates
    /// </summary>
    public static class HtmlEncoder
    {
        /// <summary>
        /// HTML-encodes a string to prevent script injection
        /// </summary>
        public static string Encode(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return WebUtility.HtmlEncode(text);
        }

        /// <summary>
        /// HTML-encodes all user-provided fields in a dictionary
        /// </summary>
        public static Dictionary<string, string> EncodeAll(Dictionary<string, string> values)
        {
            return values.ToDictionary(
                kvp => kvp.Key,
                kvp => Encode(kvp.Value)
            );
        }
    }
}
