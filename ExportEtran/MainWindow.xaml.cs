using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data;
using System.IO;
using System.Data.SqlClient;
using System.ComponentModel;
using System.Configuration;
using System.Windows.Threading;

namespace ExportEtran
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

            //SrcField.FillSrcField(@"C:\WS\Job 2018\ExportEtran\DB\TXT\Накладные\Грузоотправитель\N150512.txt", "\t");
        }

        private void btnOpenSourceFile_Click(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
			dlg.DefaultExt = ".txt";
			dlg.Filter = "Текстовый документ (.txt)|*.txt";

			Nullable<bool> result = dlg.ShowDialog();

			if (result == true)
			{
				this.txtSourceFile.Text = dlg.FileName;
			}
		}

		private void btnExportBill_Click(object sender, RoutedEventArgs e)
		{
			string srcTable = "SrcTableBill";

			// 0. Создание таблицы в базе данных на основе текстового файла
			SqlConnection connection = new SqlConnection(ConfigurationManager.AppSettings["ConString"]);
			ExportTxtToTable.ExportDataTableToSQLServer(ExportTxtToTable.CreateDataTableFromFile(txtSourceFile.Text, "\t"), srcTable, connection); // В качестве первого параметра метода ExportDataTableToSQLServer вызывается метод CreateDataTableFromFile возрощающий созданный им DataTable, на оснвое которого будет создана таблица в базе методом ExportDataTableToSQLServer

			// Проверить существуют ли поля указанные в tbl_ConfRef в SrcTable
			string lstField = Model.ConfTable.CheckConfRef(connection, srcTable);
			if (lstField != "")
			{
				MessageBox.Show("В конфигурационной таблице перечислены поля, которых нет в исходном текстовом файле:\n\n" + lstField + "\n\n Экспорт прерван!", "Внимание!");
				return;
			}

			EtranParsingServiceReference.ParsingServiceClient client = new EtranParsingServiceReference.ParsingServiceClient(ConfigurationManager.AppSettings["WcfEndpoint"]);
			client.Endpoint.Binding.SendTimeout = new TimeSpan(0, 0, 5, 0);
			//MessageBox.Show(client.ParseBill(connection));
			EtranParsingServiceReference.ParsingResult[] parsingResult = client.ParseBill(connection);

			#region Лог
			string log = ""; // Текстовое представление результата импорта

			DataTable insertDataTable = new DataTable(); // Результат вставки в таблицу tbl_Bill
			DataTable updateDataTable = new DataTable(); // Результат обновления таблицы tbl_Bill
			DataTable insertDataTableBillCar = new DataTable(); // Результат вставки в таблицу tbl_CarBill

			// Перебор массива результатов
			for (int i = 0; i < parsingResult.Length; i++)
			{
				// Формирование тектового лога
				if (parsingResult[i].Operation == EtranParsingServiceReference.OperationList.I)
					if (parsingResult[i].ExeptionMsg == null)
						log += string.Format("Добавлено {0} записей в таблицу {1}. ", parsingResult[i].CountProcessedRecords, parsingResult[i].TableName);
					else
						log += string.Format("Ошибка во время добавления записей в таблицу {1}: {0}. ", parsingResult[i].ExeptionMsg, parsingResult[i].TableName);
				else if (parsingResult[i].Operation == EtranParsingServiceReference.OperationList.U)
					if (parsingResult[i].ExeptionMsg == null)
						log += string.Format("Обновлено {0} записей в таблице {1}. ", parsingResult[i].CountProcessedRecords, parsingResult[i].TableName);
					else
						log += string.Format("Ошибка во время обновления записей в таблице {1}: {0}. ", parsingResult[i].ExeptionMsg, parsingResult[i].TableName);

				// Получение табличного представления изменений. Определение отношения таблица-изменения
				if ((parsingResult[i].TableName == "tbl_Bill") & (parsingResult[i].Operation == EtranParsingServiceReference.OperationList.I))
					insertDataTable = parsingResult[i].DataTableResult;

				if ((parsingResult[i].TableName == "tbl_Bill") & (parsingResult[i].Operation == EtranParsingServiceReference.OperationList.U))
					updateDataTable = parsingResult[i].DataTableResult;

				if ((parsingResult[i].TableName == "tbl_BillCar") & (parsingResult[i].Operation == EtranParsingServiceReference.OperationList.I))
					insertDataTableBillCar = parsingResult[i].DataTableResult;
			}

			this.textBlockCarNumLog.Text = "";
			this.textBlockCarNumLog.Text += log;

			// Формирование табличного лога по tbl_Bill
			IEnumerable<DataRow> unionLog = null;
			if ((insertDataTable != null) & (updateDataTable != null))
			{
				// 1. Выбор из таблицы updateDataTable идешников, которых нету в insertDataTable
				var exceptedRecords = (from ut in updateDataTable.AsEnumerable() select ut[0])
						  .Except(from it in insertDataTable.AsEnumerable() select it[0]);
				// 2. Выбор полной строки из updateDataTable на основе идешников которых нет в insertDataTable
				var join = from ut in updateDataTable.AsEnumerable()
						   join r in exceptedRecords
						   on ut.ItemArray[0] equals r
						   select ut;
				// 3. Совмещение полного списка insertDataTable с сформированным join
				unionLog = (from it in insertDataTable.AsEnumerable() select it).Union(join);
			}
			else if (insertDataTable != null)
			{
				unionLog = from it in insertDataTable.AsEnumerable() select it;
			}
			else if (updateDataTable != null)
			{
				unionLog = from it in updateDataTable.AsEnumerable() select it;
			}

			if ((unionLog != null) && (unionLog.ToArray().Count() != 0))
			{
				DataRow[] dataRowArray = unionLog.ToArray();
				DataTable dataTable = dataRowArray.CopyToDataTable();

				this.dataGridBillLog.ItemsSource = dataTable.DefaultView;
			}

			// Формирование табличного лога по tbl_CarBill
			IEnumerable<DataRow> insertBillCarLog = null;
			if (insertDataTableBillCar != null)
				insertBillCarLog = from it in insertDataTableBillCar.AsEnumerable() select it;

			if ((insertBillCarLog != null) && (insertBillCarLog.ToArray().Count() != 0))
			{
				DataRow[] billCarDataRowArray = insertBillCarLog.ToArray();
				DataTable billCarDataTable = billCarDataRowArray.CopyToDataTable();

				this.dataGridBillCarLog.ItemsSource = billCarDataTable.DefaultView;
			}
			#endregion
		}

		private void btnExportVPU_Click(object sender, RoutedEventArgs e)
		{
			string srcTable = "SrcTableVPU";

			// 0. Создание таблицы в базе данных на основе текстового файла
			SqlConnection connection = new SqlConnection(ConfigurationManager.AppSettings["ConString"]);
			ExportTxtToTable.ExportDataTableToSQLServer(ExportTxtToTable.CreateDataTableFromFile(txtSourceFile.Text, "\t"), srcTable, connection); // В качестве первого параметра метода ExportDataTableToSQLServer вызывается метод CreateDataTableFromFile возрощающий созданный им DataTable, на оснвое которого будет создана таблица в базе методом ExportDataTableToSQLServer

			// Проверить существуют ли поля указанные в tbl_ConfRef в SrcTable
			string lstField = Model.ConfTable.CheckConfRef(connection, srcTable);
			if (lstField != "")
			{
				MessageBox.Show("В конфигурационной таблице перечислены поля, которых нет в исходном текстовом файле:\n\n" + lstField + "\n\n Экспорт прерван!", "Внимание!");
				return;
			}

			EtranParsingServiceReference.ParsingServiceClient client = new EtranParsingServiceReference.ParsingServiceClient();
			client.Endpoint.Binding.SendTimeout = new TimeSpan(0, 0, 5, 0);
			//MessageBox.Show(client.ParseVPU(connection));
			EtranParsingServiceReference.ParsingResult[] parsingResult = client.ParseVPU(connection);

			#region Лог
			string log = ""; // Текстовое представление результата импорта

			DataTable insertDataTable = new DataTable(); // Результат вставки в таблицу tbl_Bill
			DataTable updateDataTable = new DataTable(); // Результат обновления таблицы tbl_Bill
			DataTable insertDataTableRgdExpense = new DataTable(); // Результат вставки в таблицу tbl_CarBill

			// Перебор массива результатов
			for (int i = 0; i < parsingResult.Length; i++)
			{
				// Формирование тектового лога
				if (parsingResult[i].Operation == EtranParsingServiceReference.OperationList.I)
					if (parsingResult[i].ExeptionMsg == null)
						log += string.Format("Добавлено {0} записей в таблицу {1}. ", parsingResult[i].CountProcessedRecords, parsingResult[i].TableName);
					else
						log += string.Format("Ошибка во время добавления записей в таблицу {1}: {0}. ", parsingResult[i].ExeptionMsg, parsingResult[i].TableName);
				else if (parsingResult[i].Operation == EtranParsingServiceReference.OperationList.U)
					if (parsingResult[i].ExeptionMsg == null)
						log += string.Format("Обновлено {0} записей в таблице {1}. ", parsingResult[i].CountProcessedRecords, parsingResult[i].TableName);
					else
						log += string.Format("Ошибка во время обновления записей в таблице {1}: {0}. ", parsingResult[i].ExeptionMsg, parsingResult[i].TableName);

				// Получение табличного представления изменений. Определение отношения таблица-изменения
				if ((parsingResult[i].TableName == "tbl_DocPU_RGD") & (parsingResult[i].Operation == EtranParsingServiceReference.OperationList.I))
					insertDataTable = parsingResult[i].DataTableResult;

				if ((parsingResult[i].TableName == "tbl_DocPU_RGD") & (parsingResult[i].Operation == EtranParsingServiceReference.OperationList.U))
					updateDataTable = parsingResult[i].DataTableResult;

				if ((parsingResult[i].TableName == "tbl_DocPU_RGDExpense") & (parsingResult[i].Operation == EtranParsingServiceReference.OperationList.I))
					insertDataTableRgdExpense = parsingResult[i].DataTableResult;
			}

			this.textBlockVpuLog.Text = "";
			this.textBlockVpuLog.Text += log;

			// Формирование табличного лога по tbl_DocPU_RGD
			IEnumerable<DataRow> unionLog = null;
			if ((insertDataTable != null) & (updateDataTable != null))
			{
				// 1. Выбор из таблицы updateDataTable идешников, которых нету в insertDataTable
				var exceptedRecords = (from ut in updateDataTable.AsEnumerable() select ut[0])
						  .Except(from it in insertDataTable.AsEnumerable() select it[0]);
				// 2. Выбор полной строки из updateDataTable на основе идешников которых нет в insertDataTable
				var join = from ut in updateDataTable.AsEnumerable()
						   join r in exceptedRecords
						   on ut.ItemArray[0] equals r
						   select ut;
				// 3. Совмещение полного списка insertDataTable с сформированным join
				unionLog = (from it in insertDataTable.AsEnumerable() select it).Union(join);
			}
			else if (insertDataTable != null)
			{
				unionLog = from it in insertDataTable.AsEnumerable() select it;
			}
			else if (updateDataTable != null)
			{
				unionLog = from it in updateDataTable.AsEnumerable() select it;
			}

			if ((unionLog != null) && (unionLog.ToArray().Count() != 0))
			{
				DataRow[] dataRowArray = unionLog.ToArray();
				DataTable dataTable = dataRowArray.CopyToDataTable();

				this.dataGridRgdLog.ItemsSource = dataTable.DefaultView;
			}

			// Формирование табличного лога по tbl_DocPU_RGDExpense
			IEnumerable<DataRow> insertRgdExpenseLog = null;
			if (insertDataTableRgdExpense != null)
				insertRgdExpenseLog = from it in insertDataTableRgdExpense.AsEnumerable() select it;

			if ((insertRgdExpenseLog != null) && (insertRgdExpenseLog.ToArray().Count() != 0))
			{
				DataRow[] rgdExpenseDataRowArray = insertRgdExpenseLog.ToArray();
				DataTable rgdExpenseDataTable = rgdExpenseDataRowArray.CopyToDataTable();

				this.dataGridRgdExpenseLog.ItemsSource = rgdExpenseDataTable.DefaultView;
			}
			#endregion
		}

		private void btnExportNK_Click(object sender, RoutedEventArgs e)
		{
			string srcTable = "SrcTableNK";

			// 0. Создание таблицы в базе данных на основе текстового файла
			SqlConnection connection = new SqlConnection(ConfigurationManager.AppSettings["ConString"]);
			ExportTxtToTable.ExportDataTableToSQLServer(ExportTxtToTable.CreateDataTableFromFile(txtSourceFile.Text, "\t"), srcTable, connection); // В качестве первого параметра метода ExportDataTableToSQLServer вызывается метод CreateDataTableFromFile возрощающий созданный им DataTable, на оснвое которого будет создана таблица в базе методом ExportDataTableToSQLServer

			// Проверить существуют ли поля указанные в tbl_ConfRef в SrcTable
			string lstField = Model.ConfTable.CheckConfRef(connection, srcTable);
			if (lstField != "")
			{
				MessageBox.Show("В конфигурационной таблице перечислены поля, которых нет в исходном текстовом файле:\n\n" + lstField + "\n\n Экспорт прерван!", "Внимание!");
				return;
			}

			EtranParsingServiceReference.ParsingServiceClient client = new EtranParsingServiceReference.ParsingServiceClient();
			client.Endpoint.Binding.SendTimeout = new TimeSpan(0, 0, 5, 0);
			//MessageBox.Show(client.ParseNK(connection));
			EtranParsingServiceReference.ParsingResult[] parsingResult = client.ParseNK(connection);

			#region Лог
			string log = ""; // Текстовое представление результата импорта

			DataTable insertDataTable = new DataTable(); // Результат вставки в таблицу tbl_DocNK_RGD
			DataTable updateDataTable = new DataTable(); // Результат обновления таблицы tbl_DocNK_RGD

			// Перебор массива результатов
			for (int i = 0; i < parsingResult.Length; i++)
			{
				// Формирование тектового лога
				if (parsingResult[i].Operation == EtranParsingServiceReference.OperationList.I)
					if (parsingResult[i].ExeptionMsg == null)
						log += string.Format("Добавлено {0} записей в таблицу {1}. ", parsingResult[i].CountProcessedRecords, parsingResult[i].TableName);
					else
						log += string.Format("Ошибка во время добавления записей в таблицу {1}: {0}. ", parsingResult[i].ExeptionMsg, parsingResult[i].TableName);
				else if (parsingResult[i].Operation == EtranParsingServiceReference.OperationList.U)
					if (parsingResult[i].ExeptionMsg == null)
						log += string.Format("Обновлено {0} записей в таблице {1}. ", parsingResult[i].CountProcessedRecords, parsingResult[i].TableName);
					else
						log += string.Format("Ошибка во время обновления записей в таблице {1}: {0}. ", parsingResult[i].ExeptionMsg, parsingResult[i].TableName);

				// Получение табличного представления изменений. Определение отношения таблица-изменения
				if ((parsingResult[i].TableName == "tbl_DocNK_RGD") & (parsingResult[i].Operation == EtranParsingServiceReference.OperationList.I))
					insertDataTable = parsingResult[i].DataTableResult;

				if ((parsingResult[i].TableName == "tbl_DocNK_RGD") & (parsingResult[i].Operation == EtranParsingServiceReference.OperationList.U))
					updateDataTable = parsingResult[i].DataTableResult;
			}

			this.textBlockNkLog.Text = "";
			this.textBlockNkLog.Text += log;

			// Формирование табличного лога по tbl_DocNK_RGD
			IEnumerable<DataRow> unionLog = null;
			if ((insertDataTable != null) & (updateDataTable != null))
			{
				// 1. Выбор из таблицы updateDataTable идешников, которых нету в insertDataTable
				var exceptedRecords = (from ut in updateDataTable.AsEnumerable() select ut[0])
						  .Except(from it in insertDataTable.AsEnumerable() select it[0]);
				// 2. Выбор полной строки из updateDataTable на основе идешников которых нет в insertDataTable
				var join = from ut in updateDataTable.AsEnumerable()
						   join r in exceptedRecords
						   on ut.ItemArray[0] equals r
						   select ut;
				// 3. Совмещение полного списка insertDataTable с сформированным join
				unionLog = (from it in insertDataTable.AsEnumerable() select it).Union(join);
			}
			else if (insertDataTable != null)
			{
				unionLog = from it in insertDataTable.AsEnumerable() select it;
			}
			else if (updateDataTable != null)
			{
				unionLog = from it in updateDataTable.AsEnumerable() select it;
			}

			if ((unionLog != null) && (unionLog.ToArray().Count() != 0))
			{
				DataRow[] dataRowArray = unionLog.ToArray();
				DataTable dataTable = dataRowArray.CopyToDataTable();

				this.dataGridNkLog.ItemsSource = dataTable.DefaultView;
			}
			#endregion
		}

		private void btnExportZ_Click(object sender, RoutedEventArgs e)
		{
			string srcTable = "SrcTableZ";

			// 0. Создание таблицы в базе данных на основе текстового файла
			SqlConnection connection = new SqlConnection(ConfigurationManager.AppSettings["ConString"]);
			ExportTxtToTable.ExportDataTableToSQLServer(ExportTxtToTable.CreateDataTableFromFile(txtSourceFile.Text, "\t"), srcTable, connection); // В качестве первого параметра метода ExportDataTableToSQLServer вызывается метод CreateDataTableFromFile возрощающий созданный им DataTable, на оснвое которого будет создана таблица в базе методом ExportDataTableToSQLServer

			// Проверить существуют ли поля указанные в tbl_ConfRef в SrcTable
			string lstField = Model.ConfTable.CheckConfRef(connection, srcTable);
			if (lstField != "")
			{
				MessageBox.Show("В конфигурационной таблице перечислены поля, которых нет в исходном текстовом файле:\n\n" + lstField + "\n\n Экспорт прерван!", "Внимание!");
				return;
			}

			EtranParsingServiceReference.ParsingServiceClient client = new EtranParsingServiceReference.ParsingServiceClient();
			client.Endpoint.Binding.SendTimeout = new TimeSpan(0, 0, 5, 0);
			//MessageBox.Show(client.ParseZ(connection));
			EtranParsingServiceReference.ParsingResult[] parsingResult = client.ParseZ(connection);

			#region Лог
			string log = "";

			DataTable insertDataTable = new DataTable();
			DataTable updateDataTable = new DataTable();

			for (int i = 0; i < parsingResult.Length; i++)
			{
				/*if ((parsingResult[i].CountProcessedRecords > 0) & (dt.Rows.Count == 0))
				{
					dt = parsingResult[i].DataTableResult;
				}
				else if ((parsingResult[i].CountProcessedRecords > 0) & (dt.Rows.Count != 0))
				{
					dt.Merge(parsingResult[i].DataTableResult);
				}*/
				
				if (parsingResult[i].Operation == EtranParsingServiceReference.OperationList.I)//ParsingResult.OperationList.I)//'I')
					if (parsingResult[i].ExeptionMsg == null)
						log += string.Format("Добавлено {0} записей в таблицу {1}. ", parsingResult[i].CountProcessedRecords, parsingResult[i].TableName);
					else
						log += string.Format("Ошибка во время добавления заявок: {0}. ", parsingResult[i].ExeptionMsg);
				else if (parsingResult[i].Operation == EtranParsingServiceReference.OperationList.U)// ParsingResult.OperationList.U)//'U')
					if (parsingResult[i].ExeptionMsg == null)
						log += string.Format("Обновлено {0} записей в таблице {1}. ", parsingResult[i].CountProcessedRecords, parsingResult[i].TableName);
					else
						log += string.Format("Ошибка во время обновления заявок: {0}. ", parsingResult[i].ExeptionMsg);

				if ((parsingResult[i].TableName == "tbl_Zayavka") & (parsingResult[i].Operation == EtranParsingServiceReference.OperationList.I))
					insertDataTable = parsingResult[i].DataTableResult;

				if ((parsingResult[i].TableName == "tbl_Zayavka") & (parsingResult[i].Operation == EtranParsingServiceReference.OperationList.U))
					updateDataTable = parsingResult[i].DataTableResult;
			}

			textBlockZayavkaLog.Text += log;
			/*dataGrid1.ItemsSource = dt.DefaultView;*/

			IEnumerable<DataRow> unionLog = null;
			if (insertDataTable != null)
			{
				// 1. Выбор из таблицы updateDataTable идешников, которых нету в insertDataTable
				var exceptedRecords = (from ut in updateDataTable.AsEnumerable() select ut[0])
						  .Except(from it in insertDataTable.AsEnumerable() select it[0]);
				// 2. Выбор полной строки из updateDataTable на основе идешников которых нет в insertDataTable
				var join = from ut in updateDataTable.AsEnumerable()
						   join r in exceptedRecords
						   on ut.ItemArray[0] equals r
						   select ut;
				// 3. Совмещение полного списка insertDataTable с сформированным join
				unionLog = (from it in insertDataTable.AsEnumerable() select it).Union(join);
			}
			else if (insertDataTable != null)
			{
				unionLog = from it in insertDataTable.AsEnumerable() select it;
			}
			else if ((updateDataTable != null) & (updateDataTable != null))
			{
				unionLog = from it in updateDataTable.AsEnumerable() select it;
			}

			if ((unionLog != null) && (unionLog.ToArray().Count() != 0))
			{
				DataRow[] dataRowArray = unionLog.ToArray();
				DataTable dataTable = dataRowArray.CopyToDataTable();

				this.dataGridZayavkaLog.ItemsSource = dataTable.DefaultView;
			}
			#endregion
		}

		private void btnExportDislocationExcel_Click(object sender, RoutedEventArgs e)
		{
			/*string srcTable = "SrcTableLocationExcel";

			// 0. Создание таблицы в базе данных на основе текстового файла
			SqlConnection connection = new SqlConnection(ConfigurationManager.AppSettings["ConString"]);
			ExportTxtToTable.ExportDataTableToSQLServer(ExportTxtToTable.CreateDataTableFromFile(txtSourceFile.Text, "\t"), srcTable, connection); // В качестве первого параметра метода ExportDataTableToSQLServer вызывается метод CreateDataTableFromFile возрощающий созданный им DataTable, на оснвое которого будет создана таблица в базе методом ExportDataTableToSQLServer

			// Проверить существуют ли поля указанные в tbl_ConfRef в SrcTable
			string lstField = Model.ConfTable.CheckConfRef(connection, srcTable);
			if (lstField != "")
			{
				MessageBox.Show("В конфигурационной таблице перечислены поля, которых нет в исходном текстовом файле:\n\n" + lstField + "\n\n Экспорт прерван!", "Внимание!");
				return;
			}

			EtranParsingServiceReference.ParsingServiceClient client = new EtranParsingServiceReference.ParsingServiceClient();
			client.Endpoint.Binding.SendTimeout = new TimeSpan(0, 0, 5, 0);
			//MessageBox.Show(client.ParseZ(connection));
			EtranParsingServiceReference.ParsingResult[] parsingResult = client.ParseZ(connection);*/
		}

		private System.Data.Objects.ObjectQuery<tbl_ConfRef> Gettbl_ConfRefQuery(ExportEtranEntities exportEtranEntities)
		{
			// Автоматически созданный код

			System.Data.Objects.ObjectQuery<ExportEtran.tbl_ConfRef> tbl_ConfRefQuery = exportEtranEntities.tbl_ConfRef;
			// Возвращает ObjectQuery.
			return tbl_ConfRefQuery;
		}

		ExportEtran.ExportEtranEntities exportEtranEntities = new ExportEtran.ExportEtranEntities();
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			// Загрузить данные в tbl_ConfRef. Можно изменить этот код как требуется.
			System.Windows.Data.CollectionViewSource tbl_ConfRefViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("tbl_ConfRefViewSource")));
			System.Data.Objects.ObjectQuery<ExportEtran.tbl_ConfRef> tbl_ConfRefQuery = this.Gettbl_ConfRefQuery(exportEtranEntities);
			tbl_ConfRefViewSource.Source = tbl_ConfRefQuery.Execute(System.Data.Objects.MergeOption.AppendOnly);

			//this.tbl_ConfRefDataGrid.SelectionChanged += new SelectionChangedEventHandler(tbl_ConfRefDataGrid_SelectionChanged);
			List<string> lst = ResultFromSQL.GetListFromSQL(new SqlConnection(ConfigurationManager.AppSettings["ConString"]), "SELECT SrcTable FROM dbo.tbl_ConfRef GROUP BY SrcTable UNION SELECT '</>'");
			this.cboSrcTableList.ItemsSource = lst;//new DstTableList();

			// Вариант с виртуализацией данных
			//tbl_ConfRefProvider customerProvider = new tbl_ConfRefProvider(97, 100);
			//tbl_ConfRefViewSource.Source = new VirtualizingCollection<ExportEtran.tbl_ConfRef>(customerProvider, 80).ToList<ExportEtran.tbl_ConfRef>();

			this.btnExportBill.MouseEnter += (s, ev) => { this.StatusBarItemLeft.Text = "Экспортировать накладную"; };
			this.btnExportBill.MouseLeave += (s, ev) => { this.StatusBarItemLeft.Text = " "; };
			//this.btnExportBill.MouseEnter += delegate(object sender2, MouseEventArgs e2) { this.StatusBarItemLeft.Text = "left"; };
			/*this.btnExportBill.MouseEnter += new MouseEventHandler(HintShow);
			this.btnExportNK.MouseLeave += new MouseEventHandler(HintHide);*/

			//this.StatusBarItemRight.Dispatcher.BeginInvoke(new Action<TextBlock>((textBlock) => { textBlock.Text = DateTime.Now.ToLongTimeString(); }), StatusBarItemRight);
			this.StatusBarItemRight.Dispatcher.BeginInvoke(new Action(() => { StatusBarItemRight.Text = DateTime.Now.ToLongTimeString(); }));

			DispatcherTimer timer = new DispatcherTimer();
			timer.Interval = new TimeSpan(0, 0, 1);
			timer.Tick += (s, ev) => { this.StatusBarItemRight.Text = DateTime.Now.ToLongTimeString(); };
			timer.Start(); 
		}

		private void HintShow(object sender, MouseEventArgs e)
		{
			this.StatusBarItemLeft.Text = "Экспортировать накладную";
		}
		private void HintHide(object sender, MouseEventArgs e)
		{
			this.StatusBarItemLeft.Text = "";
		}

		private string textData = "123";

		public string TextData
		{
			get { return textData; }
			set { textData = value; }
		}

		//public static readonly MainWindow Instance = new MainWindow(); - синглтон, удалить так как не используется

		// Проверка возможности забиндить за комбобоксом список в классе основной формы - успешно
		private List<string> destFields = new List<string>();

		/// <summary>
		/// Возращает список полей указанной таблицы
		/// </summary>
		public List<string> DestFields
		{
			get
			{

				if (tbl_ConfRefDataGrid.SelectedItem != null)
				{
					tbl_ConfRef row = (tbl_ConfRef)tbl_ConfRefDataGrid.SelectedItem;

					SqlDataReader reader = FieldList(row.DstTable);
					if (reader.HasRows)
					{
						destFields.Clear();
						while (reader.Read())
						{
							destFields.Add(reader["COLUMN_NAME"].ToString());
						}
					}
					reader.Close();
				}
				return destFields;
			}

			//set { destFields = value; }
		}

		private List<string> srcFields = new List<string>();

		/// <summary>
		/// Возращает список полей указанной таблицы
		/// </summary>
		public List<string> SrcFields
		{
			get
			{

				if (tbl_ConfRefDataGrid.SelectedItem != null)
				{
					tbl_ConfRef row = (tbl_ConfRef)tbl_ConfRefDataGrid.SelectedItem;

					SqlDataReader reader = FieldList(row.SrcTable);
					if (reader.HasRows)
					{
						srcFields.Clear();
						while (reader.Read())
						{
							srcFields.Add(reader["COLUMN_NAME"].ToString());
						}
					}
					reader.Close();
				}
				return srcFields;
			}

			//set { destFields = value; }
		}

		private SqlDataReader FieldList(string tableName)
		{
			string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=ExportEtran;Integrated Security=True";
			SqlConnection connection = new SqlConnection();
			connection.ConnectionString = connectionString;
			connection.Open();
			SqlCommand command = new SqlCommand();
			command.Connection = connection;
			command.CommandText = "exec sp_columns @table_name = '" + tableName + "'";
			command.CommandType = CommandType.Text;
			return command.ExecuteReader();
		}
		//

		private void button1_Click(object sender, RoutedEventArgs e)
		{
			var tt = VisualTreeHelpers.FindChild<ComboBox>(tbl_ConfRefDataGrid, "cboDstField");
			(tt as ComboBox).ItemsSource = new string[] { "one1", "two2", "three3" };
		}

		private void button2_Click(object sender, RoutedEventArgs e)
		{
			List<string> sd = new List<string>(new string[] { "1", "2", "3", "4", "5", "6", "7" });
			TestFieldList.Instance.MyList2 = sd;
		}

		private void SaveConfig()
		{
			try
			{
				exportEtranEntities.SaveChanges();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void tbl_ConfRefDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SaveConfig();
		}

		private void cboCostName_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			((tbl_ConfRef)tbl_ConfRefDataGrid.SelectedValue).CostName2_ID = (int?)((ComboBox)sender).SelectedValue;
		}

		private void cboSrcTableList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			System.Windows.Data.CollectionViewSource tbl_ConfRefViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("tbl_ConfRefViewSource")));
			System.Data.Objects.ObjectQuery<ExportEtran.tbl_ConfRef> tbl_ConfRefQuery = this.Gettbl_ConfRefQuery(exportEtranEntities);
			if (((ComboBox)sender).SelectedValue.ToString() != "</>")
				tbl_ConfRefQuery = tbl_ConfRefQuery.Where("it.SrcTable = '" + ((ComboBox)sender).SelectedValue + "'");
			tbl_ConfRefViewSource.Source = tbl_ConfRefQuery.Execute(System.Data.Objects.MergeOption.AppendOnly);
		}

		private void tbl_ConfRefDataGrid_CurrentCellChanged(object sender, EventArgs e)
		{
			tbl_ConfRefDataGrid.SelectedValue = tbl_ConfRefDataGrid.CurrentItem;
		}

		/* Тестирование функционала получения логов отслеживания изменений
		private void button3_Click(object sender, RoutedEventArgs e)
		{
			string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=ExportEtran;Integrated Security=True";
			SqlConnection connection = new SqlConnection();
			connection.ConnectionString = connectionString;
			connection.Open();
			SqlCommand command = new SqlCommand();
			command.Connection = connection;
			command.CommandType = CommandType.Text;
			command.CommandText = "Select Zayavka_ID, 'I, U' As Operation From tempTbl_Insert";
			SqlDataReader insertReader = command.ExecuteReader();
			DataTable insertDataTable = new DataTable();
			insertDataTable.Load(insertReader);

			command.CommandText = "Select Zayavka_ID, 'U' As Operation From tempTbl_Update";
			SqlDataReader updateReader = command.ExecuteReader();
			DataTable updateDataTable = new DataTable();
			updateDataTable.Load(updateReader);

			/*var res = from ut in updateDataTable.AsEnumerable()
					  join it in insertDataTable.AsEnumerable()
					  on ut.Field<int?>("Zayavka_ID") equals it.Field<int?>("Zayavka_ID") into join_result
					  from jr in join_result.DefaultIfEmpty()
					  select new
					  {
						  Zayavka_ID = ut.Field<int>("Zayavka_ID")
					  };
			object tt;
			var res2 = from ut in updateDataTable.AsEnumerable()
					  join it in insertDataTable.AsEnumerable()
					  on ut.Field<int?>("Zayavka_ID") equals it.Field<int?>("Zayavka_ID") into join_result
					  from jr in join_result.DefaultIfEmpty()
					  //into JoinedEmpDept 
					  //from dept in JoinedEmpDept.DefaultIfEmpty()
					  //where it.Field<int?>("Zayavka_ID") == null
					  select new
					  {
						  //Zayavka_ID = ut.Field<int>("Zayavka_ID"),
						  Zayavka_ID2 = string.IsNullOrEmpty(jr.Field<int?>("Zayavka_ID").ToString()) == true ? 0 : 1
					  };*/
		/*
			// 1. Выбор из таблицы updateDataTable идешников, которых нету в insertDataTable
			var exceptedRecords = (from ut in updateDataTable.AsEnumerable() select ut[0])
					  .Except(from it in insertDataTable.AsEnumerable() select it[0]);
			// 2. Выбор полной строки из updateDataTable на основе идешников которых нет в insertDataTable
			var join = from ut in updateDataTable.AsEnumerable()
						   join r in exceptedRecords
						   on ut.ItemArray[0] equals r
						   select new { Zayavka_ID = ut.ItemArray[0], Operation = ut.ItemArray[1] };
			// 3. Совмещение полного списка insertDataTable с сформированным asd
			var result = (from it in insertDataTable.AsEnumerable() select new { Zayavka_ID = it.ItemArray[0], Operation = it.ItemArray[1] }).Union(join);
			
			this.dataGridZayavkaLog.ItemsSource = result.ToList();
		}*/

		private void btnCheckConfTable_Click(object sender, RoutedEventArgs e)
		{
			string lstField = Model.ConfTable.CheckConfRef(new SqlConnection(ConfigurationManager.AppSettings["ConString"]));
			if (lstField != "")
			{
				MessageBox.Show("В конфигурационной таблице перечислены поля, которых нет в таблицах источниках:\n\n" + lstField, "Внимание!");
				return;
			}
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
			MessageBox.Show("Этран", "О программе", MessageBoxButton.OK,MessageBoxImage.Information);
		}
	}

	public class MyList : List<string>
	{
		public MyList()
		{
			this.Add("DstField");
			this.Add("DstField1");
			this.Add("DstField2");
			this.Add("DstTable");
			this.Add("DstTable1");
			this.Add("DstTable2");
			this.Add("lol");
			this.Add("SrcField");
			this.Add("SrcField1");
			this.Add("SrcField2");
			this.Add("SrcTable");
			this.Add("SrcTable1");
			this.Add("SrcTable2");

		}
	}

	public class SrcFieldList : List<string>
	{
		public void FillSrcField(string file, string delimeter)
		{
			try
			{
				if (File.Exists(file))
				{
					StreamReader reader = new StreamReader(file, Encoding.GetEncoding("windows-1251"));
					this.AddRange(reader.ReadLine().Split(delimeter.ToCharArray()).ToList());
				}
				else
				{
					throw new FileNotFoundException("The file " + file + " could not be found");
				}
			}
			catch (FileNotFoundException ex)
			{
				MessageBox.Show(ex.Message);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		public SrcFieldList()
		{
			FillSrcField(@"C:\WS\Job 2018\ExportEtran\DB\TXT\Накладные\Грузоотправитель\N110912 С датой создания документа.txt", "\t");
		}
	}

	public class DstTableList : List<string>
	{
		string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=ExportEtran;Integrated Security=True";

		public DstTableList()
		{
			SqlConnection connection = new SqlConnection();
			connection.ConnectionString = connectionString;
			connection.Open();

			SqlCommand command = new SqlCommand();
			command.Connection = connection;
			command.CommandText = "exec sp_tables @table_owner = 'dbo', @table_type=\"'TABLE'\"";
			command.CommandType = CommandType.Text;

			SqlDataReader reader = command.ExecuteReader();
			if (reader.HasRows)
			{
				while (reader.Read())
				{
					if ((reader["TABLE_NAME"].ToString() != "sysdiagrams") & (reader["TABLE_NAME"].ToString() != "dtproperties"))
						this.Add(reader["TABLE_NAME"].ToString());
				}
			}
			reader.Close();
		}
	}

	public class CostDic : Dictionary<int, string>
	{
		string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=TransCalc;Integrated Security=True";

		public CostDic()
		{
			SqlConnection connection = new SqlConnection();
			connection.ConnectionString = connectionString;
			connection.Open();

			SqlCommand command = new SqlCommand();
			command.Connection = connection;
			command.CommandText = "SELECT CostName2_ID, CostName FROM dbo.CostName2 ORDER BY CostName";
			command.CommandType = CommandType.Text;

			SqlDataReader reader = command.ExecuteReader();
			if (reader.HasRows)
			{
				while (reader.Read())
				{
					this.Add(Int32.Parse(reader["CostName2_ID"].ToString()), reader["CostName"].ToString());
				}
			}
			reader.Close();
		}
	}

	public static class ResultFromSQL
	{
		public static SqlDataReader GetReaderFromSQL(SqlConnection connection, string sql)
		{
			using (connection)
			{
				connection.Open();
				SqlCommand command = new SqlCommand();
				command.Connection = connection;
				command.CommandText = sql;
				command.CommandType = CommandType.Text;
				return command.ExecuteReader();
			}
		}

		public static List<string> GetListFromSQL(SqlConnection connection, string sql)
		{
			List<string> lst = new List<string>();
			using (connection)
			{
				connection.Open();
				SqlCommand command = new SqlCommand();
				command.Connection = connection;
				command.CommandText = sql;
				command.CommandType = CommandType.Text;
				SqlDataReader reader = command.ExecuteReader();
				if (reader.HasRows)
				{
					while (reader.Read())
					{
						lst.Add(reader[0].ToString());
					}
				}
				return lst;
			}
		}
	}

	public class TestFieldList : List<string>, INotifyPropertyChanged
	{
		public static readonly TestFieldList Instance = new TestFieldList();

		List<string> myList2 = new List<string>(new string[] { "1", "2", "3", "4" });

		public List<string> MyList2
		{
			get
			{
				return myList2;
			}

			set
			{
				myList2 = value;
				OnPropertyChanged("MyList2");
			}
		}

		#region Implementation of INotifyPropertyChanged
		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}


	//
	public class LogRecord
	{
		public int SYS_CHANGE_VERSION;

		public int? SYS_CHANGE_CREATION_VERSION;

		public int SYS_CHANGE_OPERATION;

		public int? SYS_CHANGE_COLUMNS;

		public int? SYS_CHANGE_CONTEXT;

		public int Zayavka_ID;
	}
}
