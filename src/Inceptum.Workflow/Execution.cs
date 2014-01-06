using System;
using System.Collections.Generic;

namespace Inceptum.Workflow
{
    public class Execution<TContext>
    {
        public string ActiveNode { get;  set; }
        public WorkflowState State { get; set; }
        public string Error { get; set; }
    }

    public interface IExecutionObserver
    {
        void ActivityStarted(string node, string activityType, object inputValues);
        void ActivityFinished(string node, string activityType, object outputValues);
        void ActivityFailed(string node, string activityType, object outputValues);
        void ActivityCorrupted(string node, string activityType);
    }
}