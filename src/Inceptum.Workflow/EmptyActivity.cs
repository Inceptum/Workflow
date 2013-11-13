using System;

namespace Inceptum.Workflow
{
    public class EmptyActivity : ActivityBase<object, object, object>
    {
        public override ActivityResult Execute(object input, Action<object> processOutput, Action<object> processFailOutput)
        {
            return ActivityResult.Succeeded;
        }
    }
}