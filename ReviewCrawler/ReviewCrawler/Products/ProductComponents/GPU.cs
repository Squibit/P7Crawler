﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReviewCrawler.Products.ProductComponents
{
    class GPU : ComputerComponents
    {
        string processorManufacturer;
        string chipset;
        string model;
        string architecture;
        int pciSlots;
        string cooling;
        string memSize;
        string manufacturer;

        protected override void AddInformation(Dictionary<string, string> productInformation)
        {

        }
    }
}
