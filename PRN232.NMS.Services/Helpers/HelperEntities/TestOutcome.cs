using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.NMS.API.Helpers.HelperEntities
{
    public class TestOutcome
    {
        public string Name { get; set; } = null!;
        public bool Passed { get; set; }
        public int Points { get; set; }
        public string? Message { get; set; }
    }
}
