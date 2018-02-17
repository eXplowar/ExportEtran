using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Data.Objects;
using System.Linq;

namespace ExportEtran.Model
{
    /// <summary>
    /// Demo implementation of IItemsProvider returning dummy customer items after
    /// a pause to simulate network/disk latency.
    /// </summary>
    public class tbl_ConfRefProvider : IItemsProvider<ExportEtran.tbl_ConfRef>
    {
        private readonly int _count;
        private readonly int _fetchDelay;

        /// <summary>
        /// Initializes a new instance of the <see cref="DemoCustomerProvider"/> class.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <param name="fetchDelay">The fetch delay.</param>
		public tbl_ConfRefProvider(int count, int fetchDelay)
        {
            _count = count;
            _fetchDelay = fetchDelay;
        }

        /// <summary>
        /// Fetches the total number of items available.
        /// </summary>
        /// <returns></returns>
        public int FetchCount()
        {
            Trace.WriteLine("FetchCount");
            Thread.Sleep(_fetchDelay);
            return _count; 
        }

        /// <summary>
        /// Fetches a range of items.
        /// </summary>
        /// <param name="startIndex">The start index.</param>
        /// <param name="count">The number of items to fetch.</param>
        /// <returns></returns>
		public IList<ExportEtran.tbl_ConfRef> FetchRange(int startIndex, int count)
        {
            Trace.WriteLine("FetchRange: "+startIndex+","+count);
            Thread.Sleep(_fetchDelay);

			/*List<ExportEtran.tbl_ConfRef> list = new List<ExportEtran.tbl_ConfRef>();
            for( int i=startIndex; i<startIndex+count; i++ )
            {
				ExportEtran.tbl_ConfRef customer = new ExportEtran.tbl_ConfRef { ConfRef_ID = i + 1, SrcTable = "Customer " + (i + 1) };
				list.Add(customer);
            }*/

			ExportEtranEntities exportEtranEntities = new ExportEtranEntities();
			System.Data.Objects.ObjectQuery<ExportEtran.tbl_ConfRef> tbl_ConfRefQuery = exportEtranEntities.tbl_ConfRef;
			ObjectResult<ExportEtran.tbl_ConfRef> objectResult = tbl_ConfRefQuery.Execute(System.Data.Objects.MergeOption.AppendOnly);
			List<ExportEtran.tbl_ConfRef> list = objectResult.Skip(startIndex).Take(count).ToList<ExportEtran.tbl_ConfRef>();

            return list;
        }
    }
}
