using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RoboBankOmniKassa.Models
{
    public class RobobankViewModel
    {
        public string OrderID { get; set; }

        public double Amount { get; set; }

        public string Response { get; set; }

    }
}