using Flecto.Core.Models.Filters;

namespace Flecto.Core.Validators;

internal static class CommonValidator
{
    internal static IEnumerable<(string Field, string Error)> ValidateNullOr<TFilter>(
        TFilter? filter,
        bool allowNullable,
        Func<TFilter, IEnumerable<(string Field, string Error)>> validateNotNull)
        where TFilter : class, IFilter
    {
        if (filter == null)
        {
            foreach (var error in ValidateNull(filter, allowNullable))
                yield return error;
            yield break;
        }

        foreach (var error in validateNotNull(filter))
            yield return error;
    }

    private static IEnumerable<(string Field, string Error)> ValidateNull<TFilter>(
        TFilter? filter,
        bool allowNullable)
        where TFilter : class, IFilter
    {
        if (filter == null && allowNullable) yield break;

        yield return (typeof(TFilter).Name, "Filter must not be null");
    }

    internal static IEnumerable<(string Field, string Error)> ValidateEqAndNotEq<T>(
        T? eq,
        T? notEq,
        string fieldName)
    {
        if (eq is not null && notEq is not null)
            yield return (fieldName, "Cannot specify both Eq and NotEq simultaneously");
    }

    internal static IEnumerable<(string Field, string Error)> ValidateArrayIfNeeded<T>(
        T[]? arr,
        string fieldName)
    {
        if (arr == null) yield break;

        if (arr.Length == 0)
            yield return (fieldName, "Array cannot be empty if specified");
        else if (arr.Length != arr.Distinct().Count())
            yield return (fieldName, "Array contains duplicate values");
    }

    internal static IEnumerable<(string Field, string Error)> ValidateViaCustomValidatorIfNeeded<TFilter>(
        TFilter filter,
        Func<TFilter, (bool IsValid, string? ErrorMessage)>? customValidator)
        where TFilter : class, IFilter
    {
        if (customValidator == null) yield break;

        var result = customValidator(filter);

        if (!result.IsValid)
            yield return (typeof(TFilter).Name, result.ErrorMessage ?? "Filter failed custom validation");
    }

    internal static void ThrowIfErrors(
        (string Field, string Error)[] errors,
        string? prefix = null)
    {
        if (errors.Length == 0) return;

        var msg = string.Join("\n", errors.Select(e => $"{e.Field}: {e.Error}"));

        if (!string.IsNullOrWhiteSpace(prefix))
        {
            msg = $"{prefix}\n{msg}";
        }

        throw new ArgumentException(msg);
    }

    internal static void EnsureValidBindFilter<TFilter>(
        TFilter filter,
        string table,
        string column,
        Func<TFilter, IEnumerable<(string Field, string Error)>> validator)
    where TFilter : class, IQueryFilter
    {
        EnsureValidBind(filter, table, column);
        ThrowIfErrors(
            validator(filter).ToArray(),
            $"{typeof(TFilter).Name}: validation for table: '{table}', column: '{column}' failed:"
        );
    }

    private static void EnsureValidBind(IQueryFilter filter, string table, string column)
    {
        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentException("table should be null or empty");

        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("column should be null or empty");
    }

    internal static IEnumerable<(string Field, string Error)> ValidateRangeConsistency<T>(
        T? gt,
        T? gte,
        T? lt,
        T? lte,
        string filterName)
    where T : struct
    {
        if (gt.HasValue && lt.HasValue && Comparer<T>.Default.Compare(gt.Value, lt.Value) >= 0)
        {
            yield return (filterName, $"Gt ({gt.Value}) must be less than Lt ({lt.Value})");
        }

        if (gt.HasValue && lte.HasValue && Comparer<T>.Default.Compare(gt.Value, lte.Value) > 0)
        {
            yield return (filterName, $"Gt ({gt.Value}) must be less than or equal to Lte ({lte.Value})");
        }

        if (gte.HasValue && lt.HasValue && Comparer<T>.Default.Compare(gte.Value, lt.Value) >= 0)
        {
            yield return (filterName, $"Gte ({gte.Value}) must be less than Lt ({lt.Value})");
        }

        if (gte.HasValue && lte.HasValue && Comparer<T>.Default.Compare(gte.Value, lte.Value) > 0)
        {
            yield return (filterName, $"Gte ({gte.Value}) must be less than or equal to Lte ({lte.Value})");
        }
    }
}
