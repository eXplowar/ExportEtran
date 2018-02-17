using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel;
using System.IdentityModel.Selectors;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;

namespace EtranWcfService
{
	public class Auth : IAuth
	{
		public string GetSecretCode()
		{
			return "password";
		}

		public string GetUserName()
		{
			return ServiceSecurityContext.Current.PrimaryIdentity.Name;
		}
	}

	public class CustomUserNameValidator : UserNamePasswordValidator
	{
		public override void Validate(string login, string hashPassword)
		{
			string ip = "none"; string systemUserName = "none";

			if (login == null || hashPassword == null || ip == null || systemUserName == null)
			{
				throw new ArgumentNullException();
			}

			SqlConnection connection = null;
			using (connection = new SqlConnection(ConfigurationManager.AppSettings["ConString"]))
			{
				using (SqlCommand command = new SqlCommand())
				{
					command.Connection = connection;
					command.CommandType = CommandType.StoredProcedure;
					command.CommandText = string.Format("pr_LoginAccount");
					command.Parameters.Add("@Login", SqlDbType.NVarChar).Value = login;
					command.Parameters.Add("@Password", SqlDbType.NVarChar).Value = hashPassword;
					command.Parameters.Add("@Ip", SqlDbType.NVarChar).Value = ip;
					command.Parameters.Add("@SystemUserName", SqlDbType.NVarChar).Value = systemUserName;
					connection.Open();
					using (SqlDataReader sqlDataReader = command.ExecuteReader())
					{
						if (sqlDataReader[0] == null)
						{
							throw new FaultException("Неверный логин или пароль");
						}
					}
				}
			}

			/*try
			{
				connection = new SqlConnection(ConfigurationManager.AppSettings["ConString"]);
				connection.Open();
				/*SqlCommand cmd = new SqlCommand(string.Format("SELECT [Login], [Password] FROM dbo.tbl_Account WHERE [Login]='{0}'", login), sqlCn);
				SqlDataReader reader = cmd.ExecuteReader();
				StringComparer comparer = StringComparer.OrdinalIgnoreCase;
				reader.Read();
				if (!(0 == comparer.Compare(login, reader["Login"])) & (0 == comparer.Compare(HashPassword, reader["Password"].ToString())))
				{
					throw new FaultException("Неверный логин или пароль");
				}*/
			/*
				SqlCommand command = new SqlCommand();
				command.Connection = connection;
				command.CommandType = CommandType.StoredProcedure;
				command.CommandText = string.Format("pr_LoginAccount");
				command.Parameters.Add("@Login", SqlDbType.NVarChar).Value = login;
				command.Parameters.Add("@HashPassword", SqlDbType.NVarChar).Value = hashPassword;
				command.Parameters.Add("@Ip", SqlDbType.NVarChar).Value = ip;
				command.Parameters.Add("@SystemUserName", SqlDbType.NVarChar).Value = systemUserName;
				connection.Open();
				SqlDataReader sqlDataReader = command.ExecuteReader();

			}
			finally
			{
				connection.Close();
			}*/
		}

		/*private bool CheckAccount(string login, string HashPassword)
		{
			SqlConnection sqlCn = null;
			try
			{
				sqlCn = new SqlConnection(ConfigurationManager.AppSettings["ConString"]);
				sqlCn.Open();
				SqlCommand cmd = new SqlCommand(string.Format("SELECT [Login], [Password] FROM dbo.tbl_Account WHERE [Login]='{0}'", login), sqlCn);
				SqlDataReader reader = cmd.ExecuteReader();
				StringComparer comparer = StringComparer.OrdinalIgnoreCase;
				reader.Read();
				if ((0 == comparer.Compare(login, reader["Login"])) & (0 == comparer.Compare(HashPassword, reader["Password"].ToString())))
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			catch (Exception e)
			{
				MessageBox.Show(string.Format("Ошибка в методе CheckAccount(): {0}", e.Message), "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
			finally
			{
				sqlCn.Close();
			}
		}*/

	}
}