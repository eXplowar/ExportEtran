using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;

namespace ParsingLib
{
	/// <summary>
	/// Экземпляр этого класса будет предоставлять информацию о последней произведенной операцией парсинга
	/// </summary>
	public class ParsingResult
	{
		string exeptionMsg;

		/// <summary>
		/// Содержит сообщение об ошибке
		/// </summary>
		public string ExeptionMsg
		{
			get { return exeptionMsg; }
			set { exeptionMsg = value; }
		}

		int countProcessedRecords;

		/// <summary>
		/// Количество вставленных/обновленных записей
		/// </summary>
		public int CountProcessedRecords
		{
			get { return countProcessedRecords; }
			set { countProcessedRecords = value; }
		}

		/*char operation;

		/// <summary>
		/// Тип операции: I - Insert, U - Update, D - Delete
		/// </summary>
		public char Operation
		{
			get { return operation; }
			set { operation = value; }
		}*/

		OperationList? operation;

		/// <summary>
		/// Тип операции в перечеслении OperationList: OperationList.I - Insert, OperationList.U - Update, OperationList.D - Delete
		/// </summary>
		public OperationList? Operation
		{
			get { return operation; }
			set { operation = value; }
		}

		string tableName;

		/// <summary>
		/// Таблица с которой производились действия
		/// </summary>
		public string TableName
		{
			get { return tableName; }
			set { tableName = value; }
		}

		TableTypeList tableType;

		/// <summary>
		/// Тип таблицы (родительская/дочерняя) в перечеслении TableTypeList: TableTypeList.ParentTable, TableTypeList.ChildTable
		/// </summary>
		public TableTypeList TableType
		{
			get { return tableType; }
			set { tableType = value; }
		}

		DataTable dataTableResult;

		/// <summary>
		/// Используется для хранения лога отслеживания изменений последнего действия над таблицей
		/// </summary>
		public DataTable DataTableResult
		{
			get { return dataTableResult; }
			set { dataTableResult = value; }
		}

		public ParsingResult()
		{
			this.exeptionMsg = null;
			this.countProcessedRecords = 0;
			this.operation = null;//OperationList.None;//'\0';
			this.dataTableResult = null;
		}

		/// <summary>
		/// Перегрузка конструктора принимает сообщение об ошибке
		/// </summary>
		/// <param name="exeption">Сообщение об ошибке</param>
		/// <param name="operation">Тип операции</param>
		public ParsingResult(string exeption, string tableName, OperationList operation)
		{
			this.exeptionMsg = exeption;
			this.tableName = tableName;
			this.operation = operation;
		}
	}

	/// <summary>
	/// Тип операции: I - Insert, U - Update, D - Delete
	/// </summary>
	public enum OperationList { I, U, D };

	/// <summary>
	/// Тип таблицы: ParentTable - родительская таблица, ChildTable - дочерняя таблица
	/// </summary>
	public enum TableTypeList { ParentTable, ChildTable };
}