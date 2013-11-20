using System;

namespace Inceptum.Workflow
{
    public interface IActivityExecutor
    {
        ActivityResult Execute(string activityType, string nodeName, object input, Action<dynamic> processOutput, Action<dynamic> processFailOutput);
    }
    public class GenericActivity:ActivityBase<object,object,object>
    {
        private readonly IActivityExecutor m_Executor;
        private readonly string m_ActivityType;
        private readonly string m_NodeName;

        public GenericActivity(IActivityExecutor executor, string activityType, string nodeName)
        {
            m_NodeName = nodeName;
            m_ActivityType = activityType;
            m_Executor = executor;
        }

        public override ActivityResult Execute(dynamic input, Action<dynamic> processOutput, Action<dynamic> processFailOutput)
        {
            return m_Executor.Execute(m_ActivityType, m_NodeName, input, processOutput, processFailOutput);
        }

        public override ActivityResult Resume<TClosure>(Action<dynamic> processOutput, Action<dynamic> processFailOutput, TClosure closure)
        {
            if (closure.GetType()== typeof(ActivityState))
            {
                var state = ((ActivityState)(object)closure);
                if(state.NodeName!=m_NodeName)
                    return ActivityResult.Pending;

                if (state.Status == ActivityResult.Succeeded)
                {
                    processOutput(state.Values);
                    return ActivityResult.Succeeded;
                }

                if (state.Status == ActivityResult.Failed)
                {
                    processFailOutput(state.Values);
                    return ActivityResult.Failed;
                }
            }
            return ActivityResult.Pending;
        }
    }
}