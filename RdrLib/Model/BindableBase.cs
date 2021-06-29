using System.ComponentModel;

namespace RdrLib.Model
{
	public abstract class BindableBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		protected bool SetProperty<T>(ref T storage, T value, string propertyName)
		{
			if (Equals(storage, value))
			{
				return false;
			}

			storage = value;

			RaisePropertyChanged(propertyName);

			return true;
		}

		protected virtual void RaisePropertyChanged(string propertyName)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
