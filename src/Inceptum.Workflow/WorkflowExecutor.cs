using System;
using System.Configuration;
using System.Linq;

namespace Inceptum.Workflow
{
    public class ActivityState
    {
        public dynamic Values { get; set; }
        public string NodeName { get; set; }
        public ActivityResult Status { get; set; }

        public override string ToString()
        {
            return string.Format("ActivityState for node name {0} status {1}", NodeName, Status);
        }
    }

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

    internal class WorkflowExecutor<TContext> : IWorkflowVisitor<TContext, WorkflowState>
    {
        private readonly IActivityFactory m_Factory;
        private readonly INodesResolver<TContext> m_Nodes;
        private readonly Execution<TContext> m_Execution;
        private readonly TContext m_Context;
        private readonly object m_Closure;
        private readonly IExecutionObserver m_ExecutionObserver;
        private ActivityExecution m_ResumingActivityExecution;

        public WorkflowExecutor(Execution<TContext> execution, TContext context, INodesResolver<TContext> nodes, IActivityFactory factory, IExecutionObserver observer, ActivityExecution resumingActivityExecution, object closure)
            :this(execution,context,nodes,factory,observer)
        {
            if (!m_Execution.ExecutingActivities.Contains(resumingActivityExecution))
                throw new ArgumentException("resumingActivityExecution does not belong to provided execution ", "resumingActivityExecution");
            m_ResumingActivityExecution = resumingActivityExecution;
            
            m_Closure = closure;

        }

        public WorkflowExecutor(Execution<TContext> execution, TContext context, INodesResolver<TContext> nodes, IActivityFactory factory,   IExecutionObserver observer)
        {
            m_ExecutionObserver = observer??new NullExecutionObserver();
            m_Context = context;
            m_Factory = factory;
            m_Execution = execution;
            m_Nodes = nodes;
        }


        public WorkflowState Visit(IGraphNode<TContext> node)
        {
            object activityOutput = null;
            ActivityResult result;
            ActivityExecution activityExecution;
            if (m_ResumingActivityExecution != null)
            {
                activityExecution = m_ResumingActivityExecution;
                result = node.ActivitySlot.Resume(activityExecution.Id,m_Factory, m_Context, m_Closure, out activityOutput);
                m_ResumingActivityExecution = null;
            }
            else
            {
                activityExecution = new ActivityExecution(node.Name);
                m_Execution.ExecutingActivities.Add(activityExecution);
                result = node.ActivitySlot.Execute(activityExecution.Id,m_Factory, m_Context, out activityOutput, activityInput => m_ExecutionObserver.ActivityStarted(activityExecution.Id, node.Name, node.ActivityType, activityInput));
            }


            if (result == ActivityResult.Pending)
            {
                m_Execution.State = WorkflowState.InProgress;
                return WorkflowState.InProgress;
            }

            if (result == ActivityResult.None)
            {
                m_Execution.State = WorkflowState.Corrupted;
                m_ExecutionObserver.ActivityCorrupted(activityExecution.Id, node.Name, node.ActivityType);
                return WorkflowState.Corrupted;
            }

            if (result == ActivityResult.Failed)
            {
                m_ExecutionObserver.ActivityFailed(activityExecution.Id, node.Name, node.ActivityType, activityOutput);
                m_Execution.ExecutingActivities.Remove(activityExecution);
            }

            if (result == ActivityResult.Succeeded)
            {
                m_ExecutionObserver.ActivityFinished(activityExecution.Id, node.Name, node.ActivityType, activityOutput);
                m_Execution.ExecutingActivities.Remove(activityExecution);
            }

            var edges = node.Edges.Where(e => e.Condition(m_Context, result)).ToArray();

            if(edges.Length>1){
                m_Execution.Error = "Failed to get next node - more then one transition condition was met: " + Environment.NewLine+string.Join(Environment.NewLine, edges.Select(e => string.Format("[{0}]-{1}-> [{2}]", node.Name, e.Description, e.Node)));
                m_Execution.State = WorkflowState.Corrupted;
                return WorkflowState.Corrupted;
            }
            if (edges.Length == 0 && node.Name != "end" && node.Name != "fail")
            {
                m_Execution.Error = "Failed to get next node - none of transition condition was met: " + Environment.NewLine + string.Join(Environment.NewLine,node.Edges.Select(e => string.Format("[{0}]-{1}-> [{2}]", node.Name, e.Description, e.Node)));
                m_Execution.State = WorkflowState.Corrupted;
                return WorkflowState.Corrupted;
            }

            var transition = edges.FirstOrDefault();

            if (transition != null)
            {
                var nextNode = m_Nodes[transition.Node];
                var nextResult = nextNode.Accept(this);
                return nextResult;
            }

            //TODO: =="end" is not good idea
            if (node.Name == "end" && result == ActivityResult.Succeeded)
            {
                m_Execution.State = WorkflowState.Complete;
                return WorkflowState.Complete;
            }

            //TODO: =="end" is not good idea
            if (node.Name == "fail")
            {
                m_Execution.State = WorkflowState.Failed;
                return WorkflowState.Failed;
            }

            m_Execution.State = WorkflowState.Corrupted;
            return WorkflowState.Corrupted;
        }
    }
}