using System;
using System.Collections.Generic;
using System.Text;

namespace Farrier.Models
{
    delegate InspectionRule DelRunRule(string ruleName, Dictionary<string, string> ruleTokens, int prefix, InspectionRule parentRule);
}
