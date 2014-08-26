using System;

namespace Inceptum.Workflow.Executors
{
    internal class ResumeAfterWorkflowExecutor<TContext> : WorkflowExecutorBase<TContext>, IActivityOutputProvider
    {
        private readonly string m_Node;
        private readonly IActivityOutputProvider m_OutputProvider;
        private readonly object m_Output;

        public ResumeAfterWorkflowExecutor(Execution<TContext> execution, TContext context, INodesResolver<TContext> nodes, IActivityFactory factory, IExecutionObserver observer,  string node, object output) 
            : base(execution, context, nodes, factory, observer)
        {
            m_Node = node;
            m_Output = output;
        }

        public  ResumeAfterWorkflowExecutor(Execution<TContext> execution, TContext context, INodesResolver<TContext> nodes, IActivityFactory factory, IExecutionObserver observer, string node, IActivityOutputProvider outputProvider) 
            : base(execution, context, nodes, factory, observer)
        {
            m_Node = node;
            m_OutputProvider = outputProvider;
        }

        protected override ActivityResult VisitNode(IGraphNode<TContext> node, Guid activityExecutionId, out object activityOutput)
        {
            return node.ActivitySlot.Complete(activityExecutionId, Factory, Context, m_OutputProvider ?? this, out activityOutput);
        }

        public TOutput GetOuput<TOutput>()
        {
            return (TOutput) m_Output;
        }

        protected override WorkflowExecutorBase<TContext> GetNextNodeVisitor()
        {
            return new WorkflowExecutor<TContext>(Execution,Context,Nodes,Factory,ExecutionObserver);
        }

        protected override ActivityExecution GetActivityExecution(IGraphNode<TContext> node)
        {
            return Execution.ExecutingActivities.FindLast(ae => ae.Node.Equals(m_Node));
        }
    }
}