using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynSecurityGuard.Analyzers.Taint
{
    public abstract class BaseCodeEvaluation
    {

        public MethodBehaviorRepository behaviorRepo { get; set; }
    }
}
