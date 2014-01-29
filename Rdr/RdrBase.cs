using System;
using System.ComponentModel;

namespace Rdr
{
    class RdrBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler pceh = this.PropertyChanged;
            if (pceh != null)
            {
                pceh(this, new PropertyChangedEventArgs(name));
            }
        }

        private System.Windows.Threading.Dispatcher _dispatcher = System.Windows.Application.Current.Dispatcher;
        public System.Windows.Threading.Dispatcher Disp { get { return this._dispatcher; } }

        private System.Windows.Application _app = System.Windows.Application.Current;
        public System.Windows.Application App { get { return this._app; } }
    }
}
