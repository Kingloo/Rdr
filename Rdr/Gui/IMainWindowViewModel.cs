using System.Collections.Generic;
using System.Windows;
using Rdr.Common;
using RdrLib;
using RdrLib.Model;

namespace Rdr.Gui
{
	public interface IMainWindowViewModel
	{
		public DelegateCommandAsync RefreshAllCommand { get; }		
		public DelegateCommandAsync<Feed> RefreshCommand { get; }
		public DelegateCommand<Feed> GoToFeedCommand { get; }
		public DelegateCommand<Item> GoToItemCommand { get; }
		public DelegateCommand MarkAsReadCommand { get; }
		public DelegateCommand OpenFeedsFileCommand { get; }
		public DelegateCommandAsync ReloadCommand { get; }
		public DelegateCommandAsync SeeUnreadCommand { get; }
		public DelegateCommandAsync SeeAllCommand { get; }
		public DelegateCommandAsync<Feed?> ViewFeedItemsCommand { get; }
		public DelegateCommandAsync<Enclosure> DownloadEnclosureCommand { get; }
		public DelegateCommand<Window> ExitCommand { get; }

		public bool Activity { get; set; }
		public string StatusMessage { get; set; }
		public bool IsRefreshTimerRunning { get; }
		public bool HasActiveDownloads { get; }
		public IRdrService RdrService { get; }
		public IReadOnlyCollection<Item> ViewedItems { get; }

		public void StartTimer();
		public void StopTimer();
	}
}