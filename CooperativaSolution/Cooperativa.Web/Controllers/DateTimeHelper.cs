namespace Cooperativa.Web.Helpers;

public static class DateTimeHelper
{
    /// <summary>
    /// Garante que o DateTime seja UTC. Se for Local, converte. Se for Unspecified, define como UTC.
    /// Se o valor for 'default', retorna o valor de fallback (também garantido como UTC).
    /// </summary>
    public static DateTime NormalizeToUtc(DateTime value, DateTime fallback)
    {
        if (value == default)
        {
            return fallback.Kind == DateTimeKind.Utc ? fallback : DateTime.SpecifyKind(fallback, DateTimeKind.Utc);
        }

        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    /// <summary>
    /// Garante que o DateTime? seja UTC, se tiver valor.
    /// </summary>
    public static DateTime? NormalizeNullableToUtc(DateTime? value)
    {
        if (value is null) return null;

        var date = value.Value;
        return NormalizeToUtc(date, DateTime.MinValue);
    }
}