using System;
using System.Collections.Generic;

namespace Inceptum.Workflow
{
    public class ResumeFromActivityExection:ActivityExecution
    {
        private readonly object m_Input;

        public ResumeFromActivityExection(string node, Guid id, object input) : base(node, id)
        {
            m_Input = input;
        }

        public ResumeFromActivityExection(string node, object input) : base(node)
        {
            m_Input = input;
        }

        public object Input
        {
            get { return m_Input; }
        }
    }

    public class ActivityExecution
    {
        public Guid Id { get; private set; }
        public string Node { get; private set; }

        public ActivityExecution(string node, Guid id)
        {
            Id = id;
            Node = node;
        }

        public ActivityExecution(string node)
            : this(node, Guid.NewGuid())
        {
            
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