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
using System.Configuration;
using System.ServiceModel.Configuration;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace ExportEtran
{
	/// <summary>
	/// Логика взаимодействия для Settings.xaml
	/// </summary>
	public partial class Settings : Window
	{
		public Settings()
		{
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			this.cboWcfServerList.ItemsSource = GetEndpointNameList();
			this.cboWcfServerList.SelectedValue = ConfigurationManager.AppSettings["WcfEndpoint"];
		}

		private List<string> GetEndpointNameList()
		{
			ConfigurationManager.RefreshSection("system.serviceModel/client");
			ClientSection clientSection = ConfigurationManager.GetSection("system.serviceModel/client") as ClientSection;
			/*ChannelEndpointElementCollection endpointCollection = clientSection.ElementInformation.Properties[string.Empty].Value as ChannelEndpointElementCollection;
			List<string> endpointNames = new List<string>();
			foreach (ChannelEndpointElement endpointElement in endpointCollection)
			{
				endpointNames.Add(endpointElement.Name);
			}*/
			var endpointCollection = clientSection.Endpoints;
			List<string> endpointNameList = (from ChannelEndpointElement item in endpointCollection select item.Name).ToList();
			return endpointNameList;
		}

		private void btnAddEndpoint_Click(object sender, RoutedEventArgs e)
		{
			Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			ServiceModelSectionGroup group = System.ServiceModel.Configuration.ServiceModelSectionGroup.GetSectionGroup(config);
			ChannelEndpointElement channelEndpointElement = new ChannelEndpointElement();
			channelEndpointElement.Address = new Uri(this.textBoxAddress.Text);
			channelEndpointElement.Binding = this.textBoxBinding.Text;
			channelEndpointElement.BindingConfiguration = this.textBoxBindingConfiguration.Text;
			channelEndpointElement.Contract = this.textBoxContract.Text;
			channelEndpointElement.Name = this.textBoxName.Text;
			group.Client.Endpoints.Add(channelEndpointElement);
			config.Save(ConfigurationSaveMode.Modified);
			ConfigurationManager.RefreshSection("system.serviceModel/client");

			this.cboWcfServerList.ItemsSource = GetEndpointNameList();
		}

		private void cboWcfServerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (this.cboWcfServerList.SelectedValue != null)
			{
				System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
				config.AppSettings.Settings["WcfEndpoint"].Value = this.cboWcfServerList.SelectedValue.ToString();
				config.Save(ConfigurationSaveMode.Modified);
				ConfigurationManager.RefreshSection("appSettings");
			}
		}
	}

	/*//
	class ServerElement : ConfigurationElement
	{
		[ConfigurationProperty("name", IsKey = true, IsRequired = true)]
		public string Name
		{
			get { return (string)this["name"]; }
		}
	}

	class ServerElementCollection : ConfigurationElementCollection
	{
		protected override ConfigurationElement CreateNewElement()
		{
			return new ServerElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((ServerElement)element).Name;
		}
	}

	class ListConnectionString : ConfigurationSection
	{
		[ConfigurationProperty("servers")]
		public ServerElementCollection Servers
		{
			get { return (ServerElementCollection)this["servers"]; }
		}
	}*/
}
