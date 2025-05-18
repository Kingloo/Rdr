using System.Collections.Generic;
using System.Windows;
using Rdr.Common;
using RdrLib.Model;

namespace Rdr.Gui
{
	public interface IMainWindowViewModel
	{
		public DelegateCommandAsync RefreshAllCommand { get; }
		public DelegateCommandAsync<Feed> RefreshCommand { get; }
		public DelegateCommandAsync<Feed> RefreshForceCommand { get; }
		public DelegateCommand<Feed> GoToFeedCommand { get; }
		public DelegateCommand<Item> GoToItemCommand { get; }
		public DelegateCommand MarkAsReadCommand { get; }
		public DelegateCommand OpenFeedsFileCommand { get; }
		public DelegateCommandAsync ReloadCommand { get; }
		public DelegateCommandAsync SeeUnreadCommand { get; }
		public DelegateCommandAsync<int> SeeRecentCommand { get; }
		public DelegateCommandAsync SeeAllCommand { get; }
		public DelegateCommandAsync<Feed?> ViewFeedItemsCommand { get; }
		public DelegateCommandAsync<Enclosure> DownloadEnclosureCommand { get; }
		public DelegateCommand<Window> ExitCommand { get; }

		public string StatusMessage { get; set; }
		public bool Activity { get; set; }
		public IReadOnlyCollection<Feed> Feeds { get; }
		public IReadOnlyCollection<Item> ViewedItems { get; }

		public void StartRefreshTimer();
		public void StopRefreshTimer();
	}
}
