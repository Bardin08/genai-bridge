using System.Globalization;

namespace GenAI.Bridge.Utils.Extensions;

internal static class PromptParamsExtensions
{
    internal static TOut GetParamAs<TOut>(this object parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        if (parameter is TOut outParam)
            return outParam;

        if (parameter is string strParam)
        {
            var targetType = typeof(TOut);
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (string.IsNullOrWhiteSpace(strParam) && Nullable.GetUnderlyingType(targetType) != null)
                return default!;

            if (underlyingType == typeof(string))
                return (TOut)(object)strParam;

            if (underlyingType == typeof(int))
                return (TOut)(object)int.Parse(strParam, CultureInfo.InvariantCulture);

            if (underlyingType == typeof(long))
                return (TOut)(object)long.Parse(strParam, CultureInfo.InvariantCulture);

            if (underlyingType == typeof(short))
                return (TOut)(object)short.Parse(strParam, CultureInfo.InvariantCulture);

            if (underlyingType == typeof(byte))
                return (TOut)(object)byte.Parse(strParam, CultureInfo.InvariantCulture);

            if (underlyingType == typeof(float))
                return (TOut)(object)float.Parse(strParam, CultureInfo.InvariantCulture);

            if (underlyingType == typeof(double))
                return (TOut)(object)double.Parse(strParam, CultureInfo.InvariantCulture);

            if (underlyingType == typeof(decimal))
                return (TOut)(object)decimal.Parse(strParam, CultureInfo.InvariantCulture);

            if (underlyingType == typeof(bool))
                return (TOut)(object)bool.Parse(strParam);

            if (underlyingType == typeof(char))
            {
                if (strParam.Length == 1)
                    return (TOut)(object)strParam[0];
                throw new FormatException($"String '{strParam}' is not a valid char.");
            }

            if (underlyingType == typeof(DateTime))
                return (TOut)(object)DateTime.Parse(strParam, CultureInfo.InvariantCulture);

            if (underlyingType == typeof(TimeSpan))
                return (TOut)(object)TimeSpan.Parse(strParam, CultureInfo.InvariantCulture);

            if (underlyingType == typeof(Guid))
                return (TOut)(object)Guid.Parse(strParam);

            if (underlyingType.IsEnum)
                return (TOut)Enum.Parse(underlyingType, strParam, ignoreCase: true);

            try
            {
                return (TOut)Convert.ChangeType(strParam, underlyingType, CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is InvalidCastException || ex is FormatException || ex is OverflowException)
            {
            }
        }

        throw new InvalidCastException($"Cannot cast parameter of type {parameter.GetType()} to {typeof(TOut)}.");
    }

    internal static bool TryGetParamAs<TOut>(this object parameter, out TOut result, TOut defaultValue = default!) 
    {
        try
        {
            result = parameter.GetParamAs<TOut>();
            return true;
        }
        catch
        {
            result = defaultValue;
            return false;
        }
    }
}