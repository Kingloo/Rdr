using System.Windows;
using System.Windows.Data;

namespace Rdr.Gui.Converters
{
    [ValueConversion(typeof(bool), typeof(Style))]
    public class IsUnreadConverter : BooleanConverterBase<Style> { }
}
