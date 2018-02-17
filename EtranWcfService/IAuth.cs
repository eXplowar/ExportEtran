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
	[ServiceContract]
	public interface IAuth
	{
		[OperationContract]
		string GetSecretCode();

		[OperationContract]
		string GetUserName();
	}
}