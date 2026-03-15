using Azure;
using PRN232.NMS.Repo.Basic;
using PRN232.NMS.Repo.DBContext;
using PRN232.NMS.Repo.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.NMS.Repo.Repositories
{
    public class GradingResultRepository : GenericRepository<GradingResult>
    {
        public GradingResultRepository() { }

        public GradingResultRepository(Prn232lab3Context context) : base(context) { }
    }
}
