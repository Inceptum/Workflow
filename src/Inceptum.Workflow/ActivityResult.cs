using System;

namespace Inceptum.Workflow
{
    public enum ActivityResult
    {
        None,
        Failed,
        Succeeded,
        Pending
    }


    public enum WorkflowState
    {
        Corrupted,
        InProgress,
        Complete
    }


    public class WorkflowLogItem
    {
        public WorkflowLogItem(string node)
        {
            Node = node;
            Start = DateTime.Now;
        }

        public string Node { get; set; }
        public ActivityResult Status { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}