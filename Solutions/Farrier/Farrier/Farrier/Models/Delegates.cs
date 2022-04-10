using System;
using System.Collections.Generic;
using System.Text;

namespace Farrier.Models
{
    delegate bool DelRunRule(string ruleName, Dictionary<string, string> ruleTokens = null, int prefix = 0, InspectionRule parentRule = null);
}
