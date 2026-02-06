using OpenRdpGuard.Models;
using OpenRdpGuard.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using WpfListBox = System.Windows.Controls.ListBox;
using System.Windows.Input;

namespace OpenRdpGuard.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Ensure all function lists show the same initial selection.
            var lists = new[] { FunctionList };
            NavigationItem? firstItem = null;
            foreach (var list in lists)
            {
                if (list != null && list.Items.Count > 0)
                {
                    list.SelectedIndex = 0;
                    firstItem ??= list.SelectedItem as NavigationItem;
                }
            }

            if (firstItem != null)
            {
                NavigateTo(firstItem.PageType);
            }
        }

        private void FunctionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox list && list.SelectedItem is NavigationItem item)
            {
                ClearOtherLists(list, SettingsList);
                NavigateTo(item.PageType);
            }
        }

        private void SettingsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox list && list.SelectedItem is NavigationItem item)
            {
                ClearOtherLists(list, FunctionList);
                NavigateTo(item.PageType);
            }
        }

        private void NavigateTo(Type pageType)
        {
            var page = App.Services.GetService(pageType) as Page;
            if (page != null)
            {
                ContentFrame.Navigate(page);
            }
        }

        private static void ClearOtherLists(WpfListBox active, params WpfListBox?[] lists)
        {
            foreach (var list in lists)
            {
                if (list != null && !ReferenceEquals(list, active))
                {
                    list.SelectedItem = null;
                }
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (WindowState == WindowState.Maximized)
                {
                    SystemCommands.RestoreWindow(this);
                }
                else
                {
                    SystemCommands.MaximizeWindow(this);
                }
                return;
            }

            DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                SystemCommands.RestoreWindow(this);
            }
            else
            {
                SystemCommands.MaximizeWindow(this);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }
    }
}
