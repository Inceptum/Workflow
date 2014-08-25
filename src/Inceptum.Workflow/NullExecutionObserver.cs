using System;

namespace Inceptum.Workflow
{
    class NullExecutionObserver : IExecutionObserver
    {
        public void ActivityStarted(Guid activityExecutionId, string node, string activityType, object inputValues)
        {
            
        }

        public void ActivityFinished(Guid activityExecutionId, string node, string activityType, object outputValues)
        {
        }

        public void ActivityFailed(Guid activityExecutionId, string node, string activityType, object outputValues)
        {
        }

        public void ActivityCorrupted(Guid activityExecutionId, string node, string activityType)
        {
        }
    }
}