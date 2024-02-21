using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Kesa.Japanese.AvaloniaExtensions;

public class ValueComparisonExtension : MarkupExtension
{
    public Binding Current { get; set; }

    public Binding Desired { get; set; }

    public object EqualValue { get; set; }

    public object NotEqualValue { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new MultiBinding()
        {
            Converter = new ValueComparisonConverter(EqualValue, NotEqualValue),
            Bindings = [Current, Desired],
        };
    }
}

public class ValueComparisonConverter(object equalValue, object notEqualValue) : IMultiValueConverter
{
    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is [{ } value1, { } value2])
        {
            return Equals(value1, value2) ? equalValue : notEqualValue;
        }

        return notEqualValue;
    }
}