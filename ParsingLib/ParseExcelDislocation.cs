using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParsingLib
{
	class ParseExcelDislocation
	{
		/* 1. Создать запрос добавления накладных в таблицу tbl_CarBillDislocation у которых состояние "Порожние" или "Груженые" и которых нет в таблице tbl_CarBillDislocation
		 * 2. Обновить накладные в таблице tbl_CarBillDislocation на соответствующие записи (по номеру накладной и номеру вагона) у которых состояние "Ремонт"
		 * 3. Добавить операции в таблицу tbl_CarBillDislocationOperation (сравнивать с тем что уже есть по текщей станции и даты операции)
		 * 4. Обновлять операции (сравнивать с тем что уже есть по текщей станции и даты операции)
		 * 
		 * 
		 */
	}
}
