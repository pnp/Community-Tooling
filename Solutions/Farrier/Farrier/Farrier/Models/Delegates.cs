using System;
using System.Collections.Generic;
using System.Text;

namespace Farrier.Models
{
    delegate bool DelRunRule(string ruleName, int prefix, InspectionRule parentRule = null);
}
