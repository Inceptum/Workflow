using System;
using System.Collections.Generic;

namespace Inceptum.Workflow
{
    public class Execution<TContext>
    {
        public string ActiveNode { get;  set; }
        public WorkflowState State { get; set; }
    }

    public interface IExecutionLogger
    {
        void ActivityStarted(string node, string activityType, string inputValues);
        void ActivityFinished(string node, string activityType, string outputValues);
        void ActivityFailed(string node, string activityType);
        void ActivityCorrupted(string node, string activityType);
    }
}