using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Marketio_WPF.Converters
{
    /// <summary>
    /// Converter that returns visibility based on user role presence.
    /// Takes a collection of roles and checks if any match the required role.
    /// </summary>
    public class RoleToVisibilityConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts user roles and required role to visibility.
        /// </summary>
        /// <param name="values">[0] = IList<string> of user roles, [1] = string of required role</param>
        /// <param name="targetType">Visibility</param>
        /// <param name="parameter">Not used</parameter>
        /// <param name="culture">Culture info</param>
        /// <returns>Visibility.Visible if user has role, Visibility.Collapsed otherwise</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
            {
                return Visibility.Collapsed;
            }

            if (values[0] is not IList<string> userRoles || values[1] is not string requiredRole)
            {
                return Visibility.Collapsed;
            }

            if (string.IsNullOrWhiteSpace(requiredRole))
            {
                return Visibility.Collapsed;
            }

            // Check if user has the required role
            var hasRole = userRoles.Any(role =>
                role.Equals(requiredRole, StringComparison.OrdinalIgnoreCase));

            return hasRole ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}