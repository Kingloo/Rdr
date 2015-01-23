using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Rdr
{
    class RdrBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnNotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChangedEventHandler pceh = this.PropertyChanged;

            if (pceh != null)
            {
                PropertyChangedEventArgs args = new PropertyChangedEventArgs(propertyName);

                pceh(this, args);
            }
        }
    }
}
