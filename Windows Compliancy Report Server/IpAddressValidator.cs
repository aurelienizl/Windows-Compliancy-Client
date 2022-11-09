﻿using System;

namespace Windows_Compliancy_Report_Server
{
    public static class IpAddressValidator
    {
        public static bool IsValid(string ip)
        {
            int dotCount = 0;
            bool helper = false;

            foreach (var item in ip)
            {
                if (item == '.' && helper)
                {
                    dotCount++;
                    helper = false;
                }
                if (45 < (int)item && 58 > (int)item)
                {
                    helper = true;
                }
                else
                {
                    dotCount--;
                }
            }

            return dotCount == 3;
        }
    }
}