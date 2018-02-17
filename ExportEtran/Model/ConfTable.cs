using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;

namespace ExportEtran.Model
{
	static class ConfTable
	{
		public static string CheckConfRef(SqlConnection connection, string srcTable = null)
		{
			string lstField = "";
			/*using (connection)
			{
				using (SqlCommand command = new SqlCommand())
				{
					command.Connection = connection;
					command.CommandType = CommandType.StoredProcedure;
					command.CommandText = string.Format("pr_CheckTblConfRef");
					if (srcTable != null)
						command.Parameters.Add("@SrcTable", SqlDbType.NVarChar).Value = srcTable;
					connection.Open();
					using (SqlDataReader drListField = command.ExecuteReader())
					{
						while (drListField.Read())
						{
							if (lstField == "")
								lstField += drListField["SrcField"];
							else
								lstField += ", " + drListField["SrcField"];
						}
					}
				}
			}*/
			SqlCommand command = new SqlCommand();
			command.Connection = connection;
			command.CommandType = CommandType.StoredProcedure;
			command.CommandText = string.Format("pr_CheckTblConfRef");
			if (srcTable != null)
				command.Parameters.Add("@SrcTable", SqlDbType.NVarChar).Value = srcTable;
			connection.Open();
			SqlDataReader drListField = command.ExecuteReader();
			while (drListField.Read())
			{
				if (lstField == "")
					lstField += drListField["SrcField"];
				else
					lstField += ", " + drListField["SrcField"];
			}
			return lstField;
		}
	}
}
