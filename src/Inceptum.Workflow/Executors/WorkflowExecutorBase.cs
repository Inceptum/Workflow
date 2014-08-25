using System;
using System.Linq;

namespace Inceptum.Workflow.Executors
{
    internal abstract class WorkflowExecutorBase<TContext> : IWorkflowVisitor<TContext, WorkflowState>
    {
        private readonly IActivityFactory m_Factory;
        private readonly INodesResolver<TContext> m_Nodes;
        private readonly Execution<TContext> m_Execution;
        private readonly TContext m_Context;
        private readonly IExecutionObserver m_ExecutionObserver;

        protected INodesResolver<TContext> Nodes
        {
            get { return m_Nodes; }
        }

        protected IExecutionObserver ExecutionObserver
        {
            get { return m_ExecutionObserver; }
        }

        protected IActivityFactory Factory
        {
            get { return m_Factory; }
        }

        protected TContext Context
        {
            get { return m_Context; }
        }

        protected Execution<TContext> Execution
        {
            get { return m_Execution; }
        }


        protected WorkflowExecutorBase(Execution<TContext> execution, TContext context, INodesResolver<TContext> nodes, IActivityFactory factory,   IExecutionObserver observer)
        {
            m_ExecutionObserver = observer??new NullExecutionObserver();
            m_Context = context;
            m_Factory = factory;
            m_Execution = execution;
            m_Nodes = nodes;
        }
 

        protected abstract ActivityResult VisitNode(IGraphNode<TContext> node, Guid activityExecutionId, out  object activityOutput);

        protected virtual ActivityExecution GetActivityExecution(IGraphNode<TContext> node)
        {
            var activityExecution = new ActivityExecution(node.Name);
            Execution.ExecutingActivities.Clear();
            Execution.ExecutingActivities.Add(activityExecution);
            return activityExecution;
        }

        public WorkflowState Visit(IGraphNode<TContext> node)
        {
            object activityOutput;
            var activityExecution = GetActivityExecution(node);

            var result = VisitNode(node,activityExecution.Id, out  activityOutput);
 
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
                var nextResult = nextNode.Accept(GetNextNodeVisitor());
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

        protected virtual WorkflowExecutorBase<TContext> GetNextNodeVisitor()
        {
            return this;
        }
    }
}