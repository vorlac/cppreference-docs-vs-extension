﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace CppReferenceDocsExtension.Core.Converters {
    public sealed class GetEnumValuesConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value is Type type ? Enum.GetValues(type) : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }
}
