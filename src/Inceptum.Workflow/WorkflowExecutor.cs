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

    internal class WorkflowExecutor<TContext> : WorkflowExecutorBase<TContext>
    {
        public WorkflowExecutor(Execution<TContext> execution, TContext context, INodesResolver<TContext> nodes, IActivityFactory factory, IExecutionObserver observer) 
            : base(execution, context, nodes, factory, observer)
        {
        }

        protected override ActivityResult VisitNode(IGraphNode<TContext> node, Guid activityExecutionId, out object activityOutput)
        {
            return node.ActivitySlot.Execute(activityExecutionId, Factory, Context, null, out activityOutput, activityInput => ExecutionObserver.ActivityStarted(activityExecutionId, node.Name, node.ActivityType, activityInput));
        }
    }

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

    public interface IActivityInputProvider
    {
        TInput GetInput<TInput>();
    }

    internal class ResumeFromWorkflowExecutor<TContext> : WorkflowExecutorBase<TContext>, IActivityInputProvider
    {
        private readonly object m_Input;
        private IActivityInputProvider m_InputProvider;

        public ResumeFromWorkflowExecutor(Execution<TContext> execution, TContext context, INodesResolver<TContext> nodes, IActivityFactory factory, IExecutionObserver observer) 
            : base(execution, context, nodes, factory, observer)
        {
        }
        public ResumeFromWorkflowExecutor(Execution<TContext> execution, TContext context, INodesResolver<TContext> nodes, IActivityFactory factory, IExecutionObserver observer
            , object input) 
            : base(execution, context, nodes, factory, observer)
        {
            m_Input = input;
        }
       public ResumeFromWorkflowExecutor(Execution<TContext> execution, TContext context, INodesResolver<TContext> nodes, IActivityFactory factory, IExecutionObserver observer
            , IActivityInputProvider inputProvider) 
            : base(execution, context, nodes, factory, observer)
       {
           m_InputProvider = inputProvider;
       }

        T getInput<T>()
        {
            return default(T);
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


    internal abstract class WorkflowExecutorBase<TContext> : IWorkflowVisitor<TContext, WorkflowState>
    {
        private readonly IActivityFactory m_Factory;
        private readonly INodesResolver<TContext> m_Nodes;
        private readonly Execution<TContext> m_Execution;
        private readonly TContext m_Context;
        private readonly IExecutionObserver m_ExecutionObserver;
        private ActivityExecution m_ResumingActivityExecution;
        private ResumeFromActivityExection m_ResumeFromActivityExecution;

        public INodesResolver<TContext> Nodes
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