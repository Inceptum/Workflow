using System;

namespace Inceptum.Workflow.Executors
{
    internal class ResumeWorkflowExecutor<TContext> : WorkflowExecutorBase<TContext>
    {
        private readonly object m_Closure;
        private readonly ActivityExecution m_ResumingActivityExecution;

        public ResumeWorkflowExecutor(Execution<TContext> execution, TContext context, INodesResolver<TContext> nodes, IActivityFactory factory, IExecutionObserver observer, ActivityExecution resumingActivityExecution, object closure) 
            : base(execution, context, nodes, factory, observer)
        {
            m_ResumingActivityExecution = resumingActivityExecution;
            m_Closure = closure;
        }

        protected override ActivityExecution GetActivityExecution(IGraphNode<TContext> node)
        {
            return m_ResumingActivityExecution;
        }

        protected override ActivityResult VisitNode(IGraphNode<TContext> node, Guid activityExecutionId, out object activityOutput)
        {
            return node.ActivitySlot.Resume(activityExecutionId, Factory, Context, m_Closure, out activityOutput);
        }

        protected override WorkflowExecutorBase<TContext> GetNextNodeVisitor()
        {
            return new WorkflowExecutor<TContext>(Execution,Context,Nodes,Factory,ExecutionObserver);
        }
    }
}