using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Documents;
using CppReferenceDocsExtension.Editor.Settings;

namespace CppReferenceDocsExtension.Core.Converters {
    public sealed class GetEnumValuesConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value is Type type ? Enum.GetValues(type) : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }

    public sealed class CppVersionEnumValuesConverter : IValueConverter {
        public object Convert(object value, Type targetType,
                              object parameter, CultureInfo culture) {
            return value is CppVersion ? this.versionStrings : null;
        }

        private readonly List<string> versionStrings = new List<string>() {
            "C++98",
            "C++03",
            "C++11",
            "C++14",
            "C++17",
            "C++20",
            "C++23",
            "Cpp26",
            "Latest"
        };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }
}
