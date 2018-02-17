using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;

namespace ParsingLib
{
	public static class ParseZ
	{
		/// <summary>
		/// Добавление в указанную таблицу данных из исходной таблицы на основе конфигурационной таблицы tbl_ConfRef в базе
		/// </summary>
		/// <param name="sourceTable">Таблица источник. Таблица сформированная на основе исходного текстового файла</param>
		/// <param name="distTable">Таблица приемник. Распердиление информации происходит на оснвое таблицы соответствий tbl_ConfRef</param>
		public static ParsingResult InsertSourceTableToTargetTable(string sourceTable, string distTable, SqlConnection connection)
		{
			// как на самом деле должен выглядеть запрос на вставку:
			/*
			
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
								strSubQueryJoinField = string.Format("{0}.[{1}]=ActualZ.[{1}]", dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"]);
							}
							else
							{
								strSubQueryJoinField += string.Format(" AND {0}.[{1}]=ActualZ.[{1}]", dicConfRefRow["SrcTable"], dicConfRefRow["SrcField"]);
							}
						}
						strInnerJoin = string.Format("INNER JOIN (SELECT {0}, {2} FROM {1} GROUP BY {2}) AS ActualZ ON {3}", strSubQueryGroupByField, dicConfRefRow["SrcTable"], strSubQuerySelectField, strSubQueryJoinField);
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

								strWhere = string.Format("WHERE ({0}.[Состояние заявки] <> N'Испорчен') AND ({1}.{2} IS NULL)", dicConfRefRow["SrcTable"], dicConfRefRow["DstTable"], dicConfRefRow["DstField"]);
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
				int insertedRecords = sdrCongRef.RecordsAffected;
				sdrCongRef.Close();
				// -> Выборка истории изменения над таблицей
				ParsingResult parsingResult = new ParsingResult();
				if (insertedRecords > 0)
				{
					/*command.CommandText = string.Format(@"DECLARE @lastU bigint;
													set @lastU = change_tracking_current_version() - 1;
													select * from changetable(changes {0}, @lastU) as ct", distTable);*/
					command.CommandType = CommandType.StoredProcedure;
					command.CommandText = string.Format("pr_GetLogLastChanges");
					command.Parameters.Add("@Table", SqlDbType.NVarChar).Value = distTable;
					SqlDataReader res = command.ExecuteReader();
					DataTable dt = new DataTable("tmpTblResultInserting");
					dt.Load(res);
					parsingResult.DataTableResult = dt;
				}
				// Тип произведенной операции над таблицей
				parsingResult.Operation = ParsingLib.OperationList.I;// 'I';
				// Таблица, в которую вставлялись записи
				parsingResult.TableName = distTable;
				// Количество обработанных (вставленных/обновленных) зайписей
				parsingResult.CountProcessedRecords = insertedRecords;
				// <-
				return parsingResult; //"В таблицу " + distTable + " успешно были добавлены " + insertedRecords + " записи из исходного текстового файла.";
			}
			catch (Exception ex)
			{
				return new ParsingLib.ParsingResult(ex.Message, distTable, ParsingLib.OperationList.I);
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
				int updatedRecords = 0;
				if (strFirstPartOfJoin != "") // Если в конфигурационой таблице не одно из полей не было помещено как обновляемое, тогда переменная пустая
				{
					sdrCongRef = command.ExecuteReader();
					updatedRecords = sdrCongRef.RecordsAffected;
				}
				sdrCongRef.Close();
				// -> Выборка истории изменения над таблицей
				ParsingResult parsingResult = new ParsingResult();
				if (updatedRecords > 0)
				{
					/*command.CommandText = string.Format(@"DECLARE @lastU bigint;
														set @lastU = change_tracking_current_version() - 1;
														select * from changetable(changes {0}, @lastU) as ct", distTable);*/
					command.CommandType = CommandType.StoredProcedure;
					command.CommandText = string.Format("pr_GetLogLastChanges");
					command.Parameters.Add("@Table", SqlDbType.NVarChar).Value = distTable;
					SqlDataReader res = command.ExecuteReader();
					DataTable dt = new DataTable("tmpTblResultUpdating");
					dt.Load(res);
					parsingResult.DataTableResult = dt;
				}
				// Тип произведенной операции над таблицей
				parsingResult.Operation = ParsingLib.OperationList.U;//'U';
				// Таблица, в которой обновлялись записи
				parsingResult.TableName = distTable;
				/*// Тип таблицы................................................................................возможно тип таблицы ненужно..................................
				parsingResult.TableType = ParsingLib.TableTypeList.ParentTable;*/
				// Количество обработанных (вставленных/обновленных) зайписей
				parsingResult.CountProcessedRecords = updatedRecords;
				// <-
				return parsingResult;// updatedRecords;
			}
			catch (Exception ex)
			{
				return new ParsingLib.ParsingResult(ex.Message, distTable, ParsingLib.OperationList.U);
			}
			finally
			{
				connection.Close();
			}
		}
	}
}
