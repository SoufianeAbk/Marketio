using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Marketio_WPF.Converters
{
    /// <summary>
    /// Converter die zichtbaarheid bepaalt op basis van de aanwezigheid van een gebruikersrol.
    /// Ontvangt een verzameling rollen en controleert of een van deze rollen overeenkomt met de vereiste rol.
    /// </summary>
    public class RoleToVisibilityConverter : IMultiValueConverter
    {
        /// <summary>
        /// Zet gebruikersrollen en een vereiste rol om naar zichtbaarheid
        /// </summary>
        /// <param name="values">[0] = IList<string> of user roles, [1] = string of required role</param>
        /// <param name="targetType">Visibility</param>
        /// <param name="parameter">Not used</parameter>
        /// <param name="culture">Culture info</param>
        /// <returns>Visibility.Visible als user een rol heeft, Visibility.Collapsed anders</returns>
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

            // Controleert of de gebruiker de vereiste rol heeft
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