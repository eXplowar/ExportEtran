using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Data.SqlClient;
using System.Collections;

namespace EtranWcfService
{
	[ServiceContract]
	public interface IParsingService
	{
		[OperationContract]
		ParsingLib.ParsingResult[] ParseBill(SqlConnection connection);

		[OperationContract]
		ParsingLib.ParsingResult[] ParseVPU(SqlConnection connection);

		[OperationContract]
		ParsingLib.ParsingResult[] ParseNK(SqlConnection connection);

		[OperationContract]
		ParsingLib.ParsingResult[] ParseZ(SqlConnection connection);

		/*[OperationContract]
		OperationTypeList GetOperationTypeList();*/

		[OperationContract]
		ParsingLib.OperationList GetOperationList();
	}
}
