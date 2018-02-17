using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Data.SqlClient;
using System.Data;
using System.IO;

namespace ExportEtran
{
	static class ExportTxtToTable
	{
		#region Экспорт исходного текстового файла, сначала в DataTable, а затем и в конечную таблицу
		/// <summary>
		/// Метод записывает текстовый файл в DataTable
		/// </summary>
		/// <param name="file">Файл для чтения</param>
		/// <param name="tableName">name of the DataTable we want to add</param>
		/// <param name="delimeter">Разделитель полей (чаще всего знак табуляции)</param>
		/// <returns>Заполненный DataTable из текстового файла</returns>
		public static DataTable CreateDataTableFromFile(string file, string delimeter)
		{
			DataTable dt = new DataTable();
			try
			{
				// Проверка существования файла
				if (File.Exists(file))
				{
					// Создание StreamReader и открытие файла
					StreamReader reader = new StreamReader(file, Encoding.GetEncoding("windows-1251"));
					// Чтение первой строки и разделение её на колонки
					string[] columns = reader.ReadLine().Split(delimeter.ToCharArray());
					// Добавление полей в DataTable ///now add our columns (we will check to make sure the column doesnt exist before adding it)                
					foreach (string col in columns)
					{
						// Переменная определяет, что поле было добавлено
						bool added = false;
						// Счетчик
						int i = 0;
						while (!(added))
						{
							string columnName = col;
							// Если такоего поля в DataTable еще нет
							if (!(dt.Columns.Contains(columnName)))
							{
								// Добавление поля, все полня будут определены как string
								dt.Columns.Add(columnName, typeof(string));
								added = true;
							}
							else
							{
								// Поле не было добавлено, инкримент счетчика
								i++;
							}
						}
					}

					//
					// Чтение остальной части файла (строк)
					string data = reader.ReadToEnd();
					// Разделение файла по возврату каретки/переводу строки и запись данных в массив
					string[] rows = data.Split("\r".ToCharArray());
					// Добавление строк в DataDable в цикле
					foreach (string r in rows)
					{
						if ((r.IndexOf("\nИтого") < 0) & (r != "\n") & (r.IndexOf("\n\t") < 0)) // Если строка не начинается на Итого или строка не содержит больше данных кроме символа новой строки // && (r != "\n\t\t\t\t")
						{
							// Разделить строку разделителеми
							string[] items = r.Replace("\n", string.Empty).Split(delimeter.ToCharArray()); // Сначала удалить знак новой строки, затем раделить строку на массив
							dt.Rows.Add(items);
						}
					}
				}
				else
				{
					throw new FileNotFoundException("Файл " + file + " не найден");
				}
			}
			catch (FileNotFoundException ex)
			{
				MessageBox.Show(ex.Message);
				return null;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
				return null;
			}

			// Вернуть получившийся DataTable
			return dt;
		}

		/// <summary>
		/// Экспорт DataTable в таблицу MSSQL
		/// </summary>
		/// <param name="dt">DataTable источник</param>
		/// <param name="tableName">Таблица в базе данных, в которую необходимо преобразовать DataTable (таблица перезаписывается если такая она уже существует)</param>
		public static void ExportDataTableToSQLServer(DataTable dt, string tableName, SqlConnection connection)
		{
			try
			{
				SqlBulkCopy bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.FireTriggers | SqlBulkCopyOptions.UseInternalTransaction, null);
				bulkCopy.DestinationTableName = tableName;
				connection.Open();

				// Проверка существования таблицы
				SqlCommand command = new SqlCommand();
				command.Connection = connection;
				command.CommandText = string.Format("exec sp_tables @table_name = '{0}'", tableName);
				command.CommandType = CommandType.Text;
				var res = command.ExecuteScalar();
				if (res != null) // Если такая таблица существует, вернется имя базы в которой она находится (значение первой строки первого поля результата хранимой процедуры sp_tables)
				{
					command.CommandText = string.Format("DROP TABLE [dbo].[{0}]", tableName);
					command.ExecuteNonQuery();
				}

				// Создание таблицы
				command.CommandText = GetCreateFromDataTableSQL(tableName, dt);
				command.ExecuteNonQuery();

				// Вставка данных в таблицу
				bulkCopy.WriteToServer(dt);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
			finally
			{
				connection.Close();
			}
		}

		/// <summary>
		/// Метод на основе DataTable генерирует CREATE TABLE SQL код. Исходный вариант http://social.msdn.microsoft.com/Forums/en-US/adodotnetdataproviders/thread/4929a0a8-0137-45f6-86e8-d11e220048c3/
		/// </summary>
		/// <param name="tableName">Имя создаваемой таблицы</param>
		/// <param name="table">DataTable из которой необходимо создать таблицу в MSSQL</param>
		/// <returns>Возращает сформированный на основе DataTable SQL запрос</returns>
		public static string GetCreateFromDataTableSQL(string tableName, DataTable table)
		{
			string sql = "CREATE TABLE [" + tableName + "] (";
			// Список столбцов
			foreach (DataColumn column in table.Columns)
			{
				sql += "[" + column.ColumnName + "] NVARCHAR(255),";

			}
			sql = sql.TrimEnd(new char[] { ',', '\n' }) + ")";
			return sql;
		}
		#endregion
	}
}
