using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Collections;

namespace ParsingLib
{
	public static class ParseVPU
	{
		/// <summary>
		/// Добавление ВПУ в указанную таблицу данных из исходной таблицы на основе конфигурационной таблицы tbl_ConfRef в базе
		/// </summary>
		/// <param name="sourceTable">Таблица источник. Таблица сформированная на основе исходного текстового файла</param>
		/// <param name="distTable">Таблица приемник. Распердиление информации происходит на оснвое таблицы соответствий tbl_ConfRef</param>
		public static ParsingResult InsertRGD(string sourceTable, string distTable, SqlConnection connection)
		{
			// как на самом деле должен выглядеть запрос на вставку:
			/*
			INSERT INTO tbl_DocPU_RGD ( [ETRAN_PU_ID], [N_DocPU_RGD], [PUState], ..., [Total] )
			SELECT        dbo.SrcTableVPU.[Идентификатор ведомости], dbo.SrcTableVPU.[Номер ведомости], dbo.SrcTableVPU.Состояние, ..., dbo.SrcTableVPU.[Сумма сбора по ведомости (ру]
			FROM            dbo.SrcTableVPU INNER JOIN
										 (SELECT        [Номер ведомости], MAX(CAST([Дата создания ведомости] AS datetime)) AS [Max-Дата создания ведомости]
										   FROM            dbo.SrcTableVPU AS SrcTableVPU_1
										   GROUP BY [Номер ведомости]) AS LastVPU ON dbo.SrcTableVPU.[Номер ведомости] = LastVPU.[Номер ведомости] AND 
									 dbo.SrcTableVPU.[Дата создания ведомости] = LastVPU.[Max-Дата создания ведомости] LEFT OUTER JOIN
									 dbo.tbl_DocPU_RGD ON dbo.SrcTableVPU.[Номер ведомости] = dbo.tbl_DocPU_RGD.N_DocPU_RGD
			WHERE        (dbo.SrcTableVPU.Состояние <> N'Испорчена') AND (dbo.tbl_DocPU_RGD.N_DocPU_RGD IS NULL)

			 * "INSERT INTO {0} ( {1} ) SELECT {2} FROM {3} {4} {5}" // где,
			 * {0} : distTable
			 * {1} : strDstField
			 * {2} : strSrcField
			 * {3} : sourceTable
			 * {4} : 
					INNER JOIN
						 (SELECT        MAX([Дата создания документа]) AS [Max-Дата создания документа], [№ накладной]
						   FROM            dbo.SrcTable1
						   GROUP BY [№ накладной]) AS ActualBill ON dbo.SrcTable1.[Дата создания документа] = ActualBill.[Max-Дата создания документа] AND 
					 dbo.SrcTable1.[№ накладной] = ActualBill.[№ накладной]
			 * {5} : strJoin
			
			ВПУ:
			
			
			
			*/

			try
			{
				string strDstField = ""; // Список полей через запятую, в которые выполняется вставка
				string strSrcField = ""; // Список полей через запятую, которые необходимо вставить
				string strLeftJoin = ""; // Итоговая строка LEFT OUTER JOIN
				string strWhere = ""; // Подстрока условия для LEFT OUTER JOIN
				string strSubLeftJoin = ""; // Подстрока для LEFT OUTER JOIN
				string strInnerJoin = ""; // Итоговая строка INNER JOIN
				string strSubQuerySelectField = ""; // В подстроку SELECT в INNER JOIN входит MAX(Дата создания документа)
				string strSubQueryGroupByField = ""; // GROUP BY часть подзапроса, входят поля над которыми не производится вычисления максимума, а только группировка
				string strSubQueryJoinField = ""; // Сами связи между подззапросом и таблицей источником
				SqlCommand command = new SqlCommand();
				command.Connection = connection;
				command.CommandType = CommandType.Text;
				// Основной запрос предоставляющий исчерпывающию информацию по экспорту - откуда-куда и типы полей. При этом поля откуда и куда фильтруются жестко. Так как на основе этого запроса будет построен запрос на вставку, нельзя из двух таблиц разных исходных вставлять в одну таблицу назначения. Темболие нескольких таблиц назначения не может быть.
				command.CommandText = string.Format(@"SELECT        dbo.tbl_ConfRef.ConfRef_ID, dbo.tbl_ConfRef.SrcTable, dbo.tbl_ConfRef.SrcField, dbo.tbl_ConfRef.DstTable, dbo.tbl_ConfRef.DstField, dbo.tbl_ConfRef.IdField, dbo.tbl_ConfRef.UpdateField, dbo.tbl_ConfRef.ActualField,
													                         isc.DATA_TYPE AS FieldDataTypeInDstTable, isc.CHARACTER_MAXIMUM_LENGTH AS FieldCharMaxLengthInDstTable 
													FROM            INFORMATION_SCHEMA.COLUMNS AS isc INNER JOIN 
													                         dbo.tbl_ConfRef ON isc.TABLE_NAME = dbo.tbl_ConfRef.DstTable AND isc.COLUMN_NAME = dbo.tbl_ConfRef.DstField 
													WHERE        (dbo.tbl_ConfRef.SrcTable = N'{0}') AND (dbo.tbl_ConfRef.DstTable = N'{1}')", sourceTable, distTable);

				connection.Open();
				SqlDataReader sdrCongRef = command.ExecuteReader();
				if (sdrCongRef.HasRows)
				{
					while (sdrCongRef.Read())
					{
						// Для упращения читаемости кода, при форомировании строки запроса, dicConfRefFields выполняет роль текущей строки
						Dictionary<string, string> dicConfRefRow = new Dictionary<string, string>();
						for (int i = 0; i < sdrCongRef.FieldCount; i++)
						{
							dicConfRefRow.Add(sdrCongRef.GetName(i).ToString(), sdrCongRef[i].ToString().Trim());
						}

						// 1.1 Создать список соответствия поля и его типа
						if (strDstField == "")
							strDstField = "[" + dicConfRefRow["DstField"] + "]";
						else
							strDstField += ", [" + dicConfRefRow["DstField"] + "]";

						// 2. Список полей через запятую, которые необходимо вставить (последовательность полей должна соответствовать полям в которые происходит вставка)
						// Каждое поле в этом списке полей необходимо привести к типу который соответствует типу поля приемника, например CAST([№ вагона] AS int), CAST(REPLACE([Итого НДС по прибытию;1], ',','.') AS float)
						// Также необходимо проверить каждое поле на наличие пустого значения используя инструкцию IIF (IIF(SrcTable1.[ОКПО Грузополучателя] = '', NULL, CAST(SrcTable1.[ОКПО Грузополучателя] AS int)))
						if (strSrcField == "")
						{
							if (dicConfRefRow["FieldDataTypeInDstTable"] == "nvarchar") // Если nvarchar
								strSrcField += string.Format("IIF({0}.[{1}] = '', NULL, CAST({0}.[{1}] AS {2}({3})))", dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["FieldDataTypeInDstTable"], dicConfRefRow["FieldCharMaxLengthInDstTable"]); // Указать максимульную длинну поля nvarchar
							else if (dicConfRefRow["FieldDataTypeInDstTable"] == "float")
								strSrcField += string.Format("IIF({0}.[{1}] = '', NULL, CAST(REPLACE({0}.[{1}], ',','.') AS {2}))", dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["FieldDataTypeInDstTable"]);
							else
								strSrcField += string.Format("IIF({0}.[{1}] = '', NULL, CAST({0}.[{1}] AS {2}))", dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["FieldDataTypeInDstTable"]);
						}
						else
						{
							if (dicConfRefRow["FieldDataTypeInDstTable"] == "nvarchar")
								strSrcField += string.Format(", IIF({0}.[{1}] = '', NULL, CAST({0}.[{1}] AS {2}({3})))", dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["FieldDataTypeInDstTable"], dicConfRefRow["FieldCharMaxLengthInDstTable"]);
							else if (dicConfRefRow["FieldDataTypeInDstTable"] == "float")
								strSrcField += string.Format(", IIF({0}.[{1}] = '', NULL, CAST(REPLACE({0}.[{1}], ',','.') AS {2}))", dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["FieldDataTypeInDstTable"]);
							else
								strSrcField += string.Format(", IIF({0}.[{1}] = '', NULL, CAST({0}.[{1}] AS {2}))", dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["FieldDataTypeInDstTable"]);
						}

						///
						// 3. Составление подстроки для формирования строки INNER JOIN						
						/* Формирование строки
						INNER JOIN
                             (SELECT        MAX([Дата создания документа]) AS [Max-Дата создания документа], [№ накладной]
                               FROM            dbo.SrcTable1
                               GROUP BY [№ накладной]) AS ActualBill ON dbo.SrcTable1.[Дата создания документа] = ActualBill.[Max-Дата создания документа] AND 
                         dbo.SrcTable1.[№ накладной] = ActualBill.[№ накладной]
						*/
						if (dicConfRefRow["ActualField"] == "1")
						{
							if (strSubQuerySelectField == "")
							{
								strSubQuerySelectField = "[" + dicConfRefRow["SrcField"] + "]";
							}
							else
							{
								strSubQuerySelectField += ", [" + dicConfRefRow["SrcField"] + "]";
							}
						}
						else if (dicConfRefRow["ActualField"] == "2")
						{
							if (strSubQueryGroupByField == "")
							{
								strSubQueryGroupByField = "MAX([" + dicConfRefRow["SrcField"] + "]) AS [" + dicConfRefRow["SrcField"] + "]";
							}
							else
							{
								strSubQueryGroupByField += ", MAX([" + dicConfRefRow["SrcField"] + "]) AS [" + dicConfRefRow["SrcField"] + "]";
							}
						}

						if ((dicConfRefRow["ActualField"] == "1") || (dicConfRefRow["ActualField"] == "2"))
						{
							if (strSubQueryJoinField == "")
							{
								strSubQueryJoinField = string.Format("{0}.[{1}]=LastVPU.[{1}]", dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"]);
							}
							else
							{
								strSubQueryJoinField += string.Format(" AND {0}.[{1}]=LastVPU.[{1}]", dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"]);
							}
						}
						strInnerJoin = string.Format("INNER JOIN (SELECT {0}, {2} FROM {1} GROUP BY {2}) AS LastVPU ON {3}", strSubQueryGroupByField, dicConfRefRow["SrcTable"], strSubQuerySelectField, strSubQueryJoinField);
						///

						// 4. Составление подстроки для формирования строки LEFT OUTER JOIN
						if (dicConfRefRow["IdField"] == "True")
						{
							// Если ключевое поле встретилось впервый раз, сформировать строку вроде этой: "LEFT OUTER JOIN tbl_Bill ON MyTable.[№ накладной] = tbl_Bill.BillNum", сдесь же сформировать WHERE часть: "WHERE (((tbl_Bill.BillNum) Is Null))"
							if (strLeftJoin == "")
							{
								if (dicConfRefRow["FieldDataTypeInDstTable"] == "nvarchar")
									strLeftJoin = string.Format("(IIF({0}.[{1}] = '', NULL, CAST({0}.[{1}] AS {4}({5}))) = {2}.[{3}])", dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["DstTable"], dicConfRefRow["DstField"], dicConfRefRow["FieldDataTypeInDstTable"], dicConfRefRow["FieldCharMaxLengthInDstTable"]);
								else if (dicConfRefRow["FieldDataTypeInDstTable"] == "float")
									strLeftJoin = string.Format("(IIF({0}.[{1}] = '', NULL, CAST(REPLACE({0}.[{1}], ',','.') AS {4})) = {2}.[{3}])", dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["DstTable"], dicConfRefRow["DstField"], dicConfRefRow["FieldDataTypeInDstTable"]);
								else
									strLeftJoin = string.Format("(IIF({0}.[{1}] = '', NULL, CAST({0}.[{1}] AS {4})) = {2}.[{3}])", dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["DstTable"], dicConfRefRow["DstField"], dicConfRefRow["FieldDataTypeInDstTable"]);

								// Условие WHERE написано жестко, тк в конфигурационной таблице не определяется какое поле описывает статус состояния и какое состояние должно быть исключено
								strWhere = string.Format("WHERE ({0}.Состояние <> N'Испорчена') AND ({1}.{2} IS NULL)", dicConfRefRow["SrcTable"], dicConfRefRow["DstTable"], dicConfRefRow["DstField"]);
							}
							else // Если ключевых полей несколько, соответственно переписать строку на: "LEFT OUTER JOIN tbl_Bill ON (MyTable.[Код станции отправления] = tbl_Bill.DepSt) AND (MyTable.[№ накладной] = tbl_Bill.BillNum)"
							{
								if (dicConfRefRow["FieldDataTypeInDstTable"] == "nvarchar")
									strLeftJoin += string.Format(" AND (IIF({0}.[{1}] = '', NULL, CAST({0}.[{1}] AS {4}({5}))) = {2}.[{3}])", dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["DstTable"], dicConfRefRow["DstField"], dicConfRefRow["FieldDataTypeInDstTable"], dicConfRefRow["FieldCharMaxLengthInDstTable"]);
								else if (dicConfRefRow["FieldDataTypeInDstTable"] == "float")
									strLeftJoin += string.Format(" AND (IIF({0}.[{1}] = '', NULL, CAST(REPLACE({0}.[{1}], ',','.') AS {4})) = {2}.[{3}])", dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["DstTable"], dicConfRefRow["DstField"], dicConfRefRow["FieldDataTypeInDstTable"]);
								else
									strLeftJoin += string.Format(" AND (IIF({0}.[{1}] = '', NULL, CAST({0}.[{1}] AS {4})) = {2}.[{3}])", dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["DstTable"], dicConfRefRow["DstField"], dicConfRefRow["FieldDataTypeInDstTable"]);
							}
						}
						strSubLeftJoin = string.Format("LEFT OUTER JOIN {0} ON ", dicConfRefRow["DstTable"]);
					}
					// Формирование полной строки: "LEFT OUTER JOIN tbl_Bill ON (MyTable.[Код станции отправления] = tbl_Bill.DepSt) AND (MyTable.[№ накладной] = tbl_Bill.BillNum) WHERE (((tbl_Bill.BillNum) Is Null))"
					strLeftJoin = strSubLeftJoin + strLeftJoin + " " + strWhere;
				}

				// 3. Итоговый запрос на вставку в таблицу distTable
				command.CommandText = string.Format("INSERT INTO {0} ( {1} ) SELECT {2} FROM {3} {4} {5}", distTable, strDstField, strSrcField, sourceTable, strInnerJoin, strLeftJoin);

				sdrCongRef.Close();
				sdrCongRef = command.ExecuteReader();
				//MessageBox.Show("В таблицу " + distTable + " успешно были добавлены " + sdrCongRef.RecordsAffected + " записи из исходного текстового файла.");
				int insertedRecords = sdrCongRef.RecordsAffected;
				sdrCongRef.Close();
				// -> Выборка истории изменения над таблицей
				ParsingResult parsingResult = new ParsingResult();
				if (insertedRecords > 0)
				{
					command.CommandType = CommandType.StoredProcedure;
					command.CommandText = string.Format("pr_GetLogLastChanges");
					command.Parameters.Add("@Table", SqlDbType.NVarChar).Value = distTable;
					SqlDataReader res = command.ExecuteReader();
					DataTable dt = new DataTable("tmpTblResultInserting");
					dt.Load(res);
					parsingResult.DataTableResult = dt;
				}
				// Тип произведенной операции над таблицей
				parsingResult.Operation = ParsingLib.OperationList.I;
				// Таблица, в которую вставлялись записи
				parsingResult.TableName = distTable;
				// Количество обработанных (вставленных/обновленных) зайписей
				parsingResult.CountProcessedRecords = insertedRecords;
				// <-
				return parsingResult;// insertedRecords; //"В таблицу " + distTable + " успешно были добавлены " + insertedRecords + " записи из исходного текстового файла.";
			}
			catch (Exception ex)
			{
				//MessageBox.Show(ex.Message);
				return new ParsingLib.ParsingResult(ex.Message, distTable, ParsingLib.OperationList.I);// return ex.Message;
			}
			finally
			{
				connection.Close();
			}
		}

		/// <summary>
		/// Обновление в указанной таблице данных из исходной таблицы на основе конфигурационной таблицы tbl_ConfRef в базе
		/// </summary>
		/// <param name="sourceTable">Таблица источник. Таблица сформированная на основе исходного текстового файла</param>
		/// <param name="distTable">Таблица приемник. Распердиление информации происходит на оснвое таблицы соответствий tbl_ConfRef</param>
		public static ParsingResult UpdateTargetTable(string sourceTable, string distTable, SqlConnection connection)
		{
			try
			{
				string strFirstPartOfJoin = "";
				string strFrom = "";
				string strSecondPartOfJoin = "";
				SqlCommand command = new SqlCommand();
				command.Connection = connection;
				command.CommandType = CommandType.Text;
				// Основной запрос предоставляющий исчерпывающию информацию по экспорту - откуда-куда и типы полей. При этом поля откуда и куда фильтруются жестко. Так как на основе этого запроса будет построен запрос на вставку, нельзя из двух таблиц разных исходных вставлять в одну таблицу назначения. Темболие нескольких таблиц назначения не может быть. Из таблицы выбираются только те записи которые перечисляют поля необходимые для обновления
				command.CommandText = string.Format(@"SELECT        dbo.tbl_ConfRef.ConfRef_ID, dbo.tbl_ConfRef.SrcTable, dbo.tbl_ConfRef.SrcField, dbo.tbl_ConfRef.DstTable, dbo.tbl_ConfRef.DstField, dbo.tbl_ConfRef.IdField, dbo.tbl_ConfRef.UpdateField, 
													                         isc.DATA_TYPE AS FieldDataTypeInDstTable, isc.CHARACTER_MAXIMUM_LENGTH AS FieldCharMaxLengthInDstTable 
													FROM            INFORMATION_SCHEMA.COLUMNS AS isc INNER JOIN 
													                         dbo.tbl_ConfRef ON isc.TABLE_NAME = dbo.tbl_ConfRef.DstTable AND isc.COLUMN_NAME = dbo.tbl_ConfRef.DstField 
													WHERE        (dbo.tbl_ConfRef.DstTable = N'{0}') AND (dbo.tbl_ConfRef.SrcTable = N'{1}') AND ((dbo.tbl_ConfRef.IdField = 1)  OR (dbo.tbl_ConfRef.UpdateField = 1))", distTable, sourceTable);
				connection.Open();
				SqlDataReader sdrCongRef = command.ExecuteReader();
				if (sdrCongRef.HasRows)
				{
					while (sdrCongRef.Read())
					{
						// Для упращения читаемости кода, при форомировании строки запроса, dicConfRefFields выполняет роль текущей строки
						Dictionary<string, string> dicConfRefRow = new Dictionary<string, string>();
						for (int i = 0; i < sdrCongRef.FieldCount; i++)
						{
							dicConfRefRow.Add(sdrCongRef.GetName(i).ToString(), sdrCongRef[i].ToString().Trim());
						}

						// Пример результирующего запроса
						/*
						UPDATE tbl_Bill SET tbl_Bill.[BillState] = CAST(SrcTable1.[Состояние накладной] AS nvarchar(255))
						FROM tbl_Bill
						INNER JOIN SrcTable1
						ON tbl_Bill.[BillNum] = CAST(SrcTable1.[№ накладной] AS nvarchar(255)) AND tbl_Bill.[DepSt] = CAST(SrcTable1.[Код станции отправления] AS int) 
						 */

						// Первая часть строки запроса: "UPDATE tbl_Bill SET tbl_Bill.[BillState] = CAST(SrcTable1.[Состояние накладной] AS nvarchar(255))"
						if (dicConfRefRow["UpdateField"] == "True") // Если поле обновляемое
						{
							if (strFirstPartOfJoin == "")
							{
								if (dicConfRefRow["FieldDataTypeInDstTable"] == "nvarchar") // Если nvarchar
									strFirstPartOfJoin = string.Format("UPDATE {0} SET {0}.[{1}] = IIF({2}.[{3}] = '', NULL, CAST({2}.[{3}] AS {4}({5})))", dicConfRefRow["DstTable"], dicConfRefRow["DstField"], dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["FieldDataTypeInDstTable"], dicConfRefRow["FieldCharMaxLengthInDstTable"]); // Указать максимульную длинну поля nvarchar
								else if (dicConfRefRow["FieldDataTypeInDstTable"] == "float")
									strFirstPartOfJoin = string.Format("UPDATE {0} SET {0}.[{1}] = IIF({2}.[{3}] = '', NULL, CAST(REPLACE({2}.[{3}], ',','.') AS {4}))", dicConfRefRow["DstTable"], dicConfRefRow["DstField"], dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["FieldDataTypeInDstTable"]);
								else
									strFirstPartOfJoin = string.Format("UPDATE {0} SET {0}.[{1}] = IIF({2}.[{3}] = '', NULL, CAST({2}.[{3}] AS {4}))", dicConfRefRow["DstTable"], dicConfRefRow["DstField"], dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["FieldDataTypeInDstTable"]);

								// Вторая часть запроса
								strFrom = string.Format(" FROM {0}", dicConfRefRow["DstTable"]);
							}
							else
							{
								if (dicConfRefRow["FieldDataTypeInDstTable"] == "nvarchar") // Если nvarchar
									strFirstPartOfJoin += string.Format(", {0}.[{1}] = IIF({2}.[{3}] = '', NULL, CAST({2}.[{3}] AS {4}({5})))", dicConfRefRow["DstTable"], dicConfRefRow["DstField"], dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["FieldDataTypeInDstTable"], dicConfRefRow["FieldCharMaxLengthInDstTable"]); // Указать максимульную длинну поля nvarchar
								else if (dicConfRefRow["FieldDataTypeInDstTable"] == "float")
									strFirstPartOfJoin += string.Format(", {0}.[{1}] = IIF({2}.[{3}] = '', NULL, CAST(REPLACE({2}.[{3}], ',','.') AS {4}))", dicConfRefRow["DstTable"], dicConfRefRow["DstField"], dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["FieldDataTypeInDstTable"]);
								else
									strFirstPartOfJoin += string.Format(", {0}.[{1}] = IIF({2}.[{3}] = '', NULL, CAST({2}.[{3}] AS {4}))", dicConfRefRow["DstTable"], dicConfRefRow["DstField"], dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["FieldDataTypeInDstTable"]);
							}
						}

						// Третья часть запроса: "INNER JOIN SrcTable1 ON tbl_Bill.[BillNum] = CAST(SrcTable1.[№ накладной] AS nvarchar(255)) AND tbl_Bill.[DepSt] = CAST(SrcTable1.[Код станции отправления] AS int)"
						if (dicConfRefRow["IdField"] == "True") // Если поле является идентифицирующим накладную, оно будет поподать в строку джоин
						{
							if (strSecondPartOfJoin == "")
							{
								if (dicConfRefRow["FieldDataTypeInDstTable"] == "nvarchar") // Если nvarchar
									strSecondPartOfJoin = string.Format(" INNER JOIN {0} ON {1}.[{2}] = IIF({0}.[{3}] = '', NULL, CAST({0}.[{3}] AS {4}({5})))", dicConfRefRow["SrcTable"], dicConfRefRow["DstTable"], dicConfRefRow["DstField"], dicConfRefRow["SrcField"], dicConfRefRow["FieldDataTypeInDstTable"], dicConfRefRow["FieldCharMaxLengthInDstTable"]); // Указать максимульную длинну поля nvarchar
								else if (dicConfRefRow["FieldDataTypeInDstTable"] == "float")
									strSecondPartOfJoin = string.Format(" INNER JOIN {0} ON {1}.[{2}] = IIF({0}.[{3}] = '', NULL, CAST(REPLACE({0}.[{3}], ',','.') AS {4}))", dicConfRefRow["SrcTable"], dicConfRefRow["DstTable"], dicConfRefRow["DstField"], dicConfRefRow["SrcField"], dicConfRefRow["FieldDataTypeInDstTable"]);
								else
									strSecondPartOfJoin = string.Format(" INNER JOIN {0} ON {1}.[{2}] = IIF({0}.[{3}] = '', NULL, CAST({0}.[{3}] AS {4}))", dicConfRefRow["SrcTable"], dicConfRefRow["DstTable"], dicConfRefRow["DstField"], dicConfRefRow["SrcField"], dicConfRefRow["FieldDataTypeInDstTable"]);
							}
							else // Если ключевых полей несколько " AND tbl_Bill.[DepSt] = CAST(SrcTable1.[Код станции отправления] AS int)""
							{
								if (dicConfRefRow["FieldDataTypeInDstTable"] == "nvarchar") // Если nvarchar
									strSecondPartOfJoin += string.Format(" AND {0}.[{1}] = IIF({2}.[{3}] = '', NULL, CAST({2}.[{3}] AS {4}({5})))", dicConfRefRow["DstTable"], dicConfRefRow["DstField"], dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["FieldDataTypeInDstTable"], dicConfRefRow["FieldCharMaxLengthInDstTable"]); // Указать максимульную длинну поля nvarchar
								else if (dicConfRefRow["FieldDataTypeInDstTable"] == "float")
									strSecondPartOfJoin += string.Format(" AND {0}.[{1}] = IIF({2}.[{3}] = '', NULL, CAST(REPLACE({2}.[{3}], ',','.') AS {4}))", dicConfRefRow["DstTable"], dicConfRefRow["DstField"], dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["FieldDataTypeInDstTable"]);
								else
									strSecondPartOfJoin += string.Format(" AND {0}.[{1}] = IIF({2}.[{3}] = '', NULL, CAST({2}.[{3}] AS {4}))", dicConfRefRow["DstTable"], dicConfRefRow["DstField"], dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["FieldDataTypeInDstTable"]);
							}
						}
					}
				}

				// 3. Итоговый запрос на вставку в таблицу distTable
				command.CommandText = strFirstPartOfJoin + strFrom + strSecondPartOfJoin;
				sdrCongRef.Close();
				sdrCongRef = command.ExecuteReader();
				//MessageBox.Show("В таблице " + distTable + " успешно были обновлены " + sdrCongRef.RecordsAffected + " записи на значения из исходного текстового файла.");
				int updatedRecords = sdrCongRef.RecordsAffected;
				sdrCongRef.Close();
				// -> Выборка истории изменения над таблицей
				ParsingResult parsingResult = new ParsingResult();
				if (updatedRecords > 0)
				{
					command.CommandType = CommandType.StoredProcedure;
					command.CommandText = string.Format("pr_GetLogLastChanges");
					command.Parameters.Add("@Table", SqlDbType.NVarChar).Value = distTable;
					SqlDataReader res = command.ExecuteReader();
					DataTable dt = new DataTable("tmpTblResultUpdating");
					dt.Load(res);
					parsingResult.DataTableResult = dt;
				}
				// Тип произведенной операции над таблицей
				parsingResult.Operation = ParsingLib.OperationList.U;
				// Таблица, в которой обновлялись записи
				parsingResult.TableName = distTable;
				// Количество обработанных (вставленных/обновленных) зайписей
				parsingResult.CountProcessedRecords = updatedRecords;
				// <-
				return parsingResult;// updatedRecords; //"В таблице " + distTable + " успешно были обновлены " + updatedRecords + " записи на значения из исходного текстового файла.";
			}
			catch (Exception ex)
			{
				//MessageBox.Show(ex.Message);
				return new ParsingLib.ParsingResult(ex.Message, distTable, ParsingLib.OperationList.U); //return ex.Message;
			}
			finally
			{
				connection.Close();
			}
		}

		// Вставка вагонов

		/// <summary>
		/// Добавление в указанную таблицу данных из исходной таблицы на основе конфигурационной таблицы tbl_ConfRef в базе
		/// </summary>
		/// <param name="sourceTable">Таблица источник. Таблица сформированная на основе исходного текстового файла</param>
		/// <param name="distTable">Таблица приемник. Распердиление информации происходит на оснвое таблицы соответствий tbl_ConfRef</param>
		public static ParsingResult InsertRGDExpense(string sourceTable, string distTable, string parentTable, SqlConnection connection)
		{
			try
			{
				string strDstField = ""; // Список полей через запятую, в которые выполняется вставка
				//string strSrcField = ""; // Список полей через запятую, которые необходимо вставить
				ArrayList strSrcFieldArray = new ArrayList(); // Массив Select части запросов
				//string strWhere = ""; // Подстрока условия для LEFT OUTER JOIN
				ArrayList strWhereArray = new ArrayList(); // Подстрока условия для LEFT OUTER JOIN
				string strInnerJoin = "";
				//string strLeftJoin = "";
				ArrayList strLeftJoinArray = new ArrayList();
				string strSubQuerySelectField = ""; // В подстроку SELECT в INNER JOIN входит MAX(Дата создания документа)
				string strSubQueryGroupByField = ""; // GROUP BY часть подзапроса, входят поля над которыми не производится вычисления максимума, а только группировка
				string strSubQueryJoinField = ""; // Сами связи между подззапросом и таблицей источником
				string strInnerJoinActualVPU = ""; // Итоговый INNER JOIN с подзапросом ActualBill
				SqlCommand command = new SqlCommand();
				command.Connection = connection;
				command.CommandType = CommandType.Text;

				// В следующий список записей входят как запись в которых присутствует информация необходимая непосредственно для вставки в определенную таблицу, а также информация о соответствующая родительской таблице
				// Версия с инфорацией о типе родительского ключа, а также в результат запроса входят строки из конфигурационной таблицы, у которых 1. таблица источник совподает с таблицей источником; 2. Таблица назначения равна родительской таблице в строке описывающей дочернию запис; 3. То что запись относится к родительскому узлу говорит то что у него нет значения в поле ParentField
				// Также сюда входят поля у которых ActualField > 0. Эти поля используются для формирования подзапроса ActualBill
				command.CommandText = string.Format(@"SELECT        dbo.tbl_ConfRef.ConfRef_ID, dbo.tbl_ConfRef.SrcTable, dbo.tbl_ConfRef.SrcField, dbo.tbl_ConfRef.DstTable, dbo.tbl_ConfRef.DstField, dbo.tbl_ConfRef.ParentTable, 
																				dbo.tbl_ConfRef.IdField, dbo.tbl_ConfRef.ActualField, dbo.tbl_ConfRef.CostName2_ID, isc.DATA_TYPE AS FieldDataTypeInDstTable, 
																				isc.CHARACTER_MAXIMUM_LENGTH AS FieldCharMaxLengthInDstTable, isk.COLUMN_NAME AS PrimaryFieldInParentTable, 
																				iscpt.DATA_TYPE AS DataTypePrimaryKeyInParentTable
													FROM            INFORMATION_SCHEMA.COLUMNS AS isc INNER JOIN
																				dbo.tbl_ConfRef ON isc.TABLE_NAME = dbo.tbl_ConfRef.DstTable AND isc.COLUMN_NAME = dbo.tbl_ConfRef.DstField LEFT OUTER JOIN
																				INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS isk INNER JOIN
																				INFORMATION_SCHEMA.COLUMNS AS iscpt ON isk.TABLE_NAME = iscpt.TABLE_NAME AND isk.COLUMN_NAME = iscpt.COLUMN_NAME ON 
																				dbo.tbl_ConfRef.ParentTable = isk.TABLE_NAME
													WHERE        (dbo.tbl_ConfRef.SrcTable = N'{0}') AND (dbo.tbl_ConfRef.DstTable = N'{1}') AND 
																				(dbo.tbl_ConfRef.ParentTable = N'{2}') OR
																				(dbo.tbl_ConfRef.SrcTable = N'{0}') AND (dbo.tbl_ConfRef.DstTable = N'{2}') AND (dbo.tbl_ConfRef.ParentTable IS NULL) AND 
																				(dbo.tbl_ConfRef.IdField = 1) OR
																				(dbo.tbl_ConfRef.SrcTable = N'{0}') AND (dbo.tbl_ConfRef.DstTable = N'{2}') AND (dbo.tbl_ConfRef.ParentTable IS NULL) AND 
																				(dbo.tbl_ConfRef.ActualField > 0)", sourceTable, distTable, parentTable);

				connection.Open();
				SqlDataReader sdrCongRef = command.ExecuteReader();
				if (sdrCongRef.HasRows)
				{
					while (sdrCongRef.Read())
					{
						// Для упращения читаемости кода, при форомировании строки запроса, dicConfRefFields выполняет роль текущей строки
						Dictionary<string, string> dicConfRefRow = new Dictionary<string, string>();
						for (int i = 0; i < sdrCongRef.FieldCount; i++)
						{
							dicConfRefRow.Add(sdrCongRef.GetName(i).ToString(), sdrCongRef[i].ToString().Trim());
						}

						// Запрос на вставку в таблицу tbl_DocPU_RGDExpense
						/*
						В разделе SELECT должно присутствовать только одно поле из шести. Для этого будет выделена отдельная переменная например strInsertRGDExpense перед формирования которой будет проверятся пустая она или нет, если пустая, то присвоить ей SrcField, где DstTable == tbl_DocPU_RGDExpense. Если не пустая, ничего не присваивать
						Так же за прос SELECT формируется 
			
						INSERT INTO tbl_DocPU_RGDExpense ( [DocPU_RGD_ID], [CostName2_ID], [Sum] )
						SELECT        dbo.tbl_DocPU_RGD.DocPU_RGD_ID, 1, dbo.SrcTableVPU.[Плата за пользование]
						FROM            dbo.SrcTableVPU INNER JOIN
														(SELECT        [Номер ведомости], MAX(CAST([Дата создания ведомости] AS datetime)) AS [Max-Дата создания ведомости]
														FROM            dbo.SrcTableVPU AS SrcTableVPU_1
														GROUP BY [Номер ведомости]) AS LastVPU ON dbo.SrcTableVPU.[Номер ведомости] = LastVPU.[Номер ведомости] AND 
													dbo.SrcTableVPU.[Дата создания ведомости] = LastVPU.[Max-Дата создания ведомости] INNER JOIN
													dbo.tbl_DocPU_RGD ON dbo.SrcTableVPU.[Номер ведомости] = dbo.tbl_DocPU_RGD.N_DocPU_RGD LEFT OUTER JOIN
													dbo.tbl_DocPU_RGDExpense ON dbo.tbl_DocPU_RGD.DocPU_RGD_ID = dbo.tbl_DocPU_RGDExpense.DocPU_RGD_ID
						WHERE        (dbo.SrcTableVPU.Состояние <> N'Испорчена') AND (dbo.SrcTableVPU.[Плата за пользование] <> N'0') AND (dbo.tbl_DocPU_RGDExpense.DocPU_RGD_ID IS NULL)						
						*/
						
						// 2. Список полей через запятую, которые необходимо вставить (последовательность полей должна соответствовать полям в которые происходит вставка)
						// Каждое поле в этом списке полей необходимо привести к типу который соответствует типу поля приемника, например CAST([№ вагона] AS int), CAST(REPLACE([Итого НДС по прибытию;1], ',','.') AS float)
						// Также необходимо проверить каждое поле на наличие пустого значения используя инструкцию IIF (IIF(SrcTable1.[ОКПО Грузополучателя] = '', NULL, CAST(SrcTable1.[ОКПО Грузополучателя] AS int)))
						if (dicConfRefRow["ParentTable"] != "")// Отфильтровываются записи описывающие родительскую таблицу. Формирование остальной части запроса на вставку
						{
							// Важная особенность первого и второго пунтка: в перечислениях откуда и куда обязательно необходимо добавить ключевое поле родительского поля. Ключевое поле не может быть nvarchar
							// 1. На основе конфигурационной таблицы состовить список полей в которые происходит вставка: [DocPU_RGD_ID], [CostName2_ID], [Sum]
							if (strDstField == "")
								strDstField = "[" + dicConfRefRow["PrimaryFieldInParentTable"] + "], [CostName2_ID], [" + dicConfRefRow["DstField"] + "]";

							// 2. Список полей через запятую, из которых вытягиваются данные (последовательность полей должна соответствовать полям в которые происходит вставка): CAST(tbl_Bill.[Bill_ID] AS int), CAST(SrcTable1.[№ вагона] AS varchar), CAST(SrcTable1.[Масса груза погруженая (кг);1] AS int)
							// Каждое поле в этом списке полей необходимо привести к типу который соответствует типу поля приемника, например CAST([№ вагона] AS int), CAST(REPLACE([Итого НДС по прибытию;1], ',','.') AS float)
							strSrcFieldArray.Add(string.Format("[{0}].[{1}], {2}, IIF({3}.[{4}] = '', NULL, CAST(REPLACE({3}.[{4}], ',','.') AS {5}))", dicConfRefRow["ParentTable"], dicConfRefRow["PrimaryFieldInParentTable"], dicConfRefRow["CostName2_ID"], dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["FieldDataTypeInDstTable"]));
						}
						
						// INNER JOIN:
						// INNER JOIN dbo.tbl_DocPU_RGD ON dbo.SrcTableVPU.[Номер ведомости] = dbo.tbl_DocPU_RGD.N_DocPU_RGD
						if ((dicConfRefRow["DstTable"] == parentTable) & (dicConfRefRow["ParentTable"] == "") & (dicConfRefRow["IdField"] == "True")) // Если у текущей записи поле 'таблица назначения' = 'родительской таблице' и 'родительский код' = пустой и эта запись идентифицируюещее
						{
							// На основе этой строки формируется INNER JOIN dbo.tbl_Bill ON dbo.SrcTable1.[№ накладной] = dbo.tbl_Bill.BillNum AND dbo.SrcTable1.[Код станции отправления] = dbo.tbl_Bill.DepSt
							if (strInnerJoin == "")
							{
								strInnerJoin = string.Format("INNER JOIN {0} ON IIF({1}.[{2}] = '', NULL, CAST({1}.[{2}] AS {4})) = {0}.{3}", dicConfRefRow["DstTable"], dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["DstField"], dicConfRefRow["FieldDataTypeInDstTable"]);
							}
							else
							{
								if (dicConfRefRow["FieldDataTypeInDstTable"] == "nvarchar")
									strInnerJoin += string.Format(" AND IIF({0}.[{1}] = '', NULL, CAST({0}.[{1}] AS {4}({5}))) = {2}.{3}", dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["DstTable"], dicConfRefRow["DstField"], dicConfRefRow["FieldDataTypeInDstTable"], dicConfRefRow["FieldCharMaxLengthInDstTable"]);
								else if (dicConfRefRow["FieldDataTypeInDstTable"] == "float")
									strInnerJoin += string.Format(" AND IIF({0}.[{1}] = '', NULL, CAST(REPLACE({0}.[{1}], ',','.') AS {4})) = {2}.{3}", dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["DstTable"], dicConfRefRow["DstField"], dicConfRefRow["FieldDataTypeInDstTable"]);
								else
									strInnerJoin += string.Format(" AND IIF({0}.[{1}] = '', NULL, CAST({0}.[{1}] AS {4})) = {2}.{3}", dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"], dicConfRefRow["DstTable"], dicConfRefRow["DstField"], dicConfRefRow["FieldDataTypeInDstTable"]);
							}
						}
						else if ((dicConfRefRow["DstTable"] == distTable) & (dicConfRefRow["ParentTable"] == parentTable))
						{
							// Например LEFT OUTER JOIN в два этапа 1. Сам лефт джоин, 2. Условие обеспечивающее проверку отсутствия такой записи в таблице назначения:
							// LEFT OUTER JOIN (SELECT DocPU_RGDExpense_ID, DocPU_RGD_ID, CostName2_ID, Sum FROM dbo.tbl_DocPU_RGDExpense WHERE (CostName2_ID = 3)) AS DocPU_RGDExpense ON dbo.tbl_DocPU_RGD.DocPU_RGD_ID = DocPU_RGDExpense.DocPU_RGD_ID
							// WHERE (dbo.SrcTableVPU.Состояние <> N'Испорчена') AND (dbo.SrcTableVPU.[Сбор за подачу/уборку] <> N'0') AND (DocPU_RGDExpense.Sum IS NULL)
							
							strLeftJoinArray.Add(string.Format("LEFT OUTER JOIN (SELECT * FROM {0} WHERE (CostName2_ID = {3})) AS DocPU_RGDExpense ON {1}.{2} = DocPU_RGDExpense.{2}", dicConfRefRow["DstTable"], dicConfRefRow["ParentTable"], dicConfRefRow["PrimaryFieldInParentTable"], dicConfRefRow["CostName2_ID"]));
							
							strWhereArray.Add(string.Format("WHERE ({0}.Состояние <> N'Испорчена') AND ({0}.[{2}] <> N'0') AND (DocPU_RGDExpense.{1} IS NULL);", dicConfRefRow["SrcTable"], dicConfRefRow["DstField"], dicConfRefRow["SrcField"]));
						}

						///
						// Составление подстроки для формирования строки INNER JOIN						
						/* Формирование строки
						INNER JOIN
							(SELECT        [Номер ведомости], MAX(CAST([Дата создания ведомости] AS datetime)) AS [Max-Дата создания ведомости]
							FROM            dbo.SrcTableVPU AS SrcTableVPU_1
							GROUP BY [Номер ведомости]) AS LastVPU ON dbo.SrcTableVPU.[Номер ведомости] = LastVPU.[Номер ведомости] AND 
						dbo.SrcTableVPU.[Дата создания ведомости] = LastVPU.[Max-Дата создания ведомости]
						*/
						if (dicConfRefRow["ActualField"] == "1")
						{
							if (strSubQuerySelectField == "")
							{
								strSubQuerySelectField = "[" + dicConfRefRow["SrcField"] + "]";
							}
							else
							{
								strSubQuerySelectField += ", [" + dicConfRefRow["SrcField"] + "]";
							}
						}
						else if (dicConfRefRow["ActualField"] == "2")
						{
							if (strSubQueryGroupByField == "")
							{
								strSubQueryGroupByField = "MAX([" + dicConfRefRow["SrcField"] + "]) AS [" + dicConfRefRow["SrcField"] + "]";
							}
							else
							{
								strSubQueryGroupByField += ", MAX([" + dicConfRefRow["SrcField"] + "]) AS [" + dicConfRefRow["SrcField"] + "]";
							}
						}

						if ((dicConfRefRow["ActualField"] == "1") || (dicConfRefRow["ActualField"] == "2"))
						{
							if (strSubQueryJoinField == "")
							{
								// dbo.SrcTable1.[Дата создания документа] = ActualBill.[Max-Дата создания документа] AND dbo.SrcTable1.[№ накладной] = ActualBill.[№ накладной]
								strSubQueryJoinField = string.Format("{0}.[{1}]=LastVPU.[{1}]", dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"]);
							}
							else
							{
								strSubQueryJoinField += string.Format(" AND {0}.[{1}]=LastVPU.[{1}]", dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"]);
							}
						}
						strInnerJoinActualVPU = string.Format("INNER JOIN (SELECT {0}, {2} FROM {1} GROUP BY {2}) AS LastVPU ON {3}", strSubQueryGroupByField, dicConfRefRow["SrcTable"], strSubQuerySelectField, strSubQueryJoinField);
						///
					}
				}


				int insertedRecords = 0;
				// 3. Итоговые запросы на вставку в таблицу distTable
				// -> Формирование истории изменения над таблицей
				ParsingResult parsingResult = new ParsingResult();
				SqlCommand logCmd = new SqlCommand();
				logCmd.Connection = connection;
				logCmd.CommandType = CommandType.StoredProcedure;
				logCmd.CommandText = string.Format("pr_GetLogLastChanges");
				logCmd.Parameters.Add("@Table", SqlDbType.NVarChar).Value = distTable;
				
				for (int i = 0; i < strSrcFieldArray.Count; i++)
				{
					command.CommandText = string.Format("INSERT INTO {0} ( {1} ) SELECT {2} FROM {3} {4} {5} {6} {7}", distTable, strDstField, strSrcFieldArray[i], sourceTable, strInnerJoinActualVPU, strInnerJoin, strLeftJoinArray[i], strWhereArray[i]);
					sdrCongRef.Close();
					sdrCongRef = command.ExecuteReader();
					insertedRecords += sdrCongRef.RecordsAffected;

					if (sdrCongRef.RecordsAffected > 0)
					{
						SqlDataReader res = logCmd.ExecuteReader();
						DataTable dt = new DataTable("tmpTblResultInserting");
						dt.Load(res);
						if (parsingResult.DataTableResult == null)
							parsingResult.DataTableResult = dt;
						else
							parsingResult.DataTableResult.Merge(dt);
					}
				}
				// Тип произведенной операции над таблицей
				parsingResult.Operation = ParsingLib.OperationList.I;
				// Таблица, в которую вставлялись записи
				parsingResult.TableName = distTable;
				// Количество обработанных (вставленных/обновленных) зайписей
				parsingResult.CountProcessedRecords = insertedRecords;
				// <-
				return parsingResult;// insertedRecords;
			}
			catch (Exception ex)
			{
			    //MessageBox.Show(ex.Message);
				return new ParsingLib.ParsingResult(ex.Message, distTable, ParsingLib.OperationList.I); //return ex.Message;
			}
			finally
			{
			    connection.Close();
			}
		}
	}
}
