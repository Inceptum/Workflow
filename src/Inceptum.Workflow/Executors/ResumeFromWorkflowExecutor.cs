using System;

namespace Inceptum.Workflow.Executors
{
    internal class ResumeFromWorkflowExecutor<TContext> : WorkflowExecutorBase<TContext>, IActivityInputProvider
    {
        private readonly object m_Input;
        private readonly IActivityInputProvider m_InputProvider;

           
        public ResumeFromWorkflowExecutor(Execution<TContext> execution, TContext context, INodesResolver<TContext> nodes, IActivityFactory factory, IExecutionObserver observer, object input) 
            : base(execution, context, nodes, factory, observer)
        {
            m_Input = input;
        }

        public ResumeFromWorkflowExecutor(Execution<TContext> execution, TContext context, INodesResolver<TContext> nodes, IActivityFactory factory, IExecutionObserver observer, IActivityInputProvider inputProvider)
            : base(execution, context, nodes, factory, observer)
        {
            m_InputProvider = inputProvider;
        }
 
        protected override ActivityResult VisitNode(IGraphNode<TContext> node, Guid activityExecutionId, out object activityOutput)
        {
            return node.ActivitySlot.Execute(activityExecutionId, Factory, Context, m_InputProvider??this, out activityOutput, activityInput => ExecutionObserver.ActivityStarted(activityExecutionId, node.Name, node.ActivityType, activityInput));
        }

        protected override WorkflowExecutorBase<TContext> GetNextNodeVisitor()
        {
            return new WorkflowExecutor<TContext>(Execution,Context,Nodes,Factory,ExecutionObserver);
        }

        public TInput GetInput<TInput>()
        {
            return (TInput) m_Input;
        }
    }
}