﻿using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using RdrLib.Model;

namespace Rdr.Gui
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel vm;

        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();

            Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);

            vm = viewModel;

            DataContext = vm;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            vm.ReloadCommand.Execute(null);

            vm.StartTimer();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void SeeUnread(object sender, RoutedEventArgs e)
        {
            vm.SetSelectedFeed(null);
        }

        private void SeeAll(object sender, RoutedEventArgs e)
        {
            vm.SeeAll();
        }

        private void Label_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Label label = (Label)sender;
            Feed feed = (Feed)label.DataContext;

            vm.SetSelectedFeed(feed);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (vm.Activity || vm.HasActiveDownload)
            {
                MessageBoxResult result = MessageBox.Show("I am doing something. Do you really want to quit?", "Activity!", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            vm.StopTimer();

            vm.CleanUp();
        }
    }
}