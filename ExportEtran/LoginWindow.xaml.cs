using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ExportEtran
{
	/// <summary>
	/// Логика взаимодействия для LoginWindow.xaml
	/// </summary>
	public partial class LoginWindow : Window
	{
		public LoginWindow()
		{
			InitializeComponent();
		}

		private void MenuItemExit_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void MenuItemOpenSettings_Click(object sender, RoutedEventArgs e)
		{
			Settings settings = new Settings();
			settings.Show();
		}

		private void MenuItemAbout_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("Этран", "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void btnSubmit_Click(object sender, RoutedEventArgs e)
		{
			using (AuthServiceReference.AuthClient authClient = new AuthServiceReference.AuthClient())
			{

				authClient.ClientCredentials.UserName.UserName = "Admin";

				authClient.ClientCredentials.UserName.Password = "Admin1";

				MessageBox.Show(authClient.GetUserName()); //GetUserName());

			}
		}
	}
}
