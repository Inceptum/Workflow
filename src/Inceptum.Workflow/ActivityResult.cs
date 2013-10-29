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
        None,
        Corrupted,
        Failed,
        InProgress,
        Complete
    }


}