using Humanizer;

namespace CattleInformationService.Shared.Infrastructure.Persistence.EFC.Configuration.Extensions;

// <summary>
///     Provides extension methods for string manipulation in Entity Framework Core configuration.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    ///     Converts a string to snake_case.
    /// </summary>
    /// <param name="text">The string to convert.</param>
    /// <returns>The string in snake_case.</returns>
    public static string ToSnakeCase(this string text)
    {
        return new string(Convert(text.GetEnumerator()).ToArray());

        static IEnumerable<char> Convert(CharEnumerator e)
        {
            if (!e.MoveNext()) yield break;
            yield return char.ToLower(e.Current);
            while (e.MoveNext())
                if (char.IsUpper(e.Current))
                {
                    yield return '_';
                    yield return char.ToLower(e.Current);
                }
                else
                {
                    yield return e.Current;
                }
        }
    }
    
    /// <summary>
    ///     Converts a string to its plural form.
    /// </summary>
    /// <param name="text">The string to pluralize.</param>
    /// <returns>The pluralized string.</returns>
    public static string ToPlural(this string text)
    {
        return text.Pluralize(false);
    }
}