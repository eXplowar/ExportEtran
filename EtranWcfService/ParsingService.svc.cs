using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using ParsingLib;
using System.Data.SqlClient;
using System.Collections;
using System.Data;

namespace EtranWcfService
{
	// NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
	//[LoggingServiceBehavior]
	public class ParsingService : IParsingService
	{
		public ParsingResult[] ParseBill(SqlConnection connection)
		{
			#region Вставка накладных
			ParsingResult inseredBillResult = ParsingLib.ParseBill.InsertSourceTableToTargetTable("SrcTableBill", "tbl_Bill", connection);
			#endregion

			#region Обновление накладной
			ParsingResult updatedBillResult = ParsingLib.ParseBill.UpdateTargetTable("SrcTableBill", "tbl_Bill", connection);
			#endregion

			#region Вставка вагонов
			ParsingResult inseredBillCarResult = ParsingLib.ParseBill.InsertSourceTableToTargetChildTable("SrcTableBill", "tbl_BillCar", "tbl_Bill", connection);
			#endregion

			ParsingResult[] parsingResultArray = new[] { inseredBillResult, updatedBillResult, inseredBillCarResult };

			return parsingResultArray;
		}

		public ParsingResult[] ParseVPU(SqlConnection connection)
		{
			#region Вставка ВПУ
			ParsingResult inseredVPUResult = ParsingLib.ParseVPU.InsertRGD("SrcTableVPU", "tbl_DocPU_RGD", connection);
			#endregion

			#region Обновление ВПУ
			ParsingResult updatedVPUResult = ParsingLib.ParseVPU.UpdateTargetTable("SrcTableVPU", "tbl_DocPU_RGD", connection);
			#endregion

			#region Вставка затрат
			ParsingResult inseredVPUExpenseResult = ParsingLib.ParseVPU.InsertRGDExpense("SrcTableVPU", "tbl_DocPU_RGDExpense", "tbl_DocPU_RGD", connection);
			#endregion

			ParsingResult[] parsingResultArray = new[] { inseredVPUResult, updatedVPUResult, inseredVPUExpenseResult };

			return parsingResultArray;
		}

		public ParsingResult[] ParseNK(SqlConnection connection)
		{

			#region Вставка накопительной карточки
			ParsingResult inseredNKResult = ParsingLib.ParseNK.InsertSourceTableToTargetTable("SrcTableNK", "tbl_DocNK_RGD", connection);
			#endregion

			#region Обновление накопительной карточки
			ParsingResult updatedNKResult = ParsingLib.ParseNK.UpdateTargetTable("SrcTableNK", "tbl_DocNK_RGD", connection);
			#endregion

			ParsingResult[] parsingResultArray = new[] { inseredNKResult, updatedNKResult };

			return parsingResultArray;
		}

		public ParsingResult[] ParseZ(SqlConnection connection)
		{
			//List<ParsingLib.ParsingResult> resultsArray = new List<ParsingLib.ParsingResult>();

			#region Вставка накопительной карточки
			ParsingResult inseredZResult = ParsingLib.ParseZ.InsertSourceTableToTargetTable("SrcTableZ", "tbl_Zayavka", connection);
			//resultsArray.Add(inseredZResult);	
			#endregion

			#region Обновление накопительной карточки
			ParsingResult updatedZResult = ParsingLib.ParseZ.UpdateTargetTable("SrcTableZ", "tbl_Zayavka", connection);
			//resultsArray.Add(updatedZResult);			
			#endregion

			//ParsingLib.ParsingResult[] parsingResult = (ParsingLib.ParsingResult[])resultsArray.ToArray();
			ParsingResult[] parsingResultArray = new[] { inseredZResult, updatedZResult };

			return parsingResultArray;
		}

		/*public OperationTypeList GetOperationTypeList()
		{
			return new OperationTypeList();
		}*/

		/// <summary>
		/// Метод не используется, был создан для того чтобы на клиенте можно было получить перечисление ParsingLib.OperationList и тип ParsingResult
		/// </summary>
		/// <returns></returns>
		public ParsingLib.OperationList GetOperationList()
		{
			return new ParsingLib.OperationList();
		}
	}

	/*[DataContract(Name = "OperationTypeList")]
	public enum OperationTypeList
	{
		[EnumMember(Value = "Insert")]
		I,
		[EnumMember(Value = "Update")]
		U,
		[EnumMember]
		D
	}*/
}