﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReviewCrawler.Products.ProductComponents
{
    class Cooling : ComputerComponents
    {
        string type;
        string speed;
        string size;
        string airflow;
        string noise;
        string connector;

        protected override void AddInformation(Dictionary<string, string> productInformation)
        {

        }
    }
}
