using System.Windows;
using System.Windows.Data;

namespace Rdr.Gui.Converters
{
	[ValueConversion(typeof(bool), typeof(Visibility))]
	public class HasEnclosureConverter : BooleanConverterBase<Visibility> { }
}
