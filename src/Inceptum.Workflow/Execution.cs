using System;
using System.Collections.Generic;

namespace Inceptum.Workflow
{
    public class ActivityExecution
    {
        public Guid Id { get; private set; }
        public string Node { get; private set; }
        public ActivityExecution(string node)
        {
            Id = Guid.NewGuid();
            Node = node;
        }
    }

    public class Execution<TContext>
    {
        public WorkflowState State { get; set; }
        public string Error { get; set; }
        public List<ActivityExecution> ExecutingActivities { get; private  set; }

        public Execution()
        {
            ExecutingActivities = new List<ActivityExecution>();
        }
    }

    public interface IExecutionObserver
    {
        void ActivityStarted(Guid activityExecutionId,string node, string activityType, object inputValues);
        void ActivityFinished(Guid activityExecutionId, string node, string activityType, object outputValues);
        void ActivityFailed(Guid activityExecutionId, string node, string activityType, object outputValues);
        void ActivityCorrupted(Guid activityExecutionId, string node, string activityType);
    }
}