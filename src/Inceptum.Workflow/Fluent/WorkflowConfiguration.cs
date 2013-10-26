using System;
using System.Collections.Generic;

namespace Inceptum.Workflow.Fluent
{
    public static class IExecutionFlowExtensions
    {
        public static WorkflowConfiguration<TContext> Do<TOutput, TContext>(this IExecutionFlow<TContext> flow, string name,
            Func<TContext, TOutput> activityMethod,
            Action<TContext, TOutput> processOutput = null)
        {
            return flow.Do<DelegateActivity<TContext, TOutput>, TContext, TOutput>(name, context => context, processOutput ?? ((context, output) => { }));
        }


    }

    public interface IExecutionFlow<TContext>: IHideObjectMembers
    {
        WorkflowConfiguration<TContext> Do<TActivity, TInput, TOutput>(string name, Func<TContext, TInput> getActivityInput,Action<TContext, TOutput> processOutput) where TActivity : IActivity<TInput, TOutput>;
        WorkflowConfiguration<TContext> Do<TActivity, TInput, TOutput>(string name, Func<TContext, TInput> getActivityInput) where TActivity : IActivity<TInput, TOutput>;
        WorkflowConfiguration<TContext> Do(string activity, string name, Func<TContext, object> getActivityInput, Action<TContext, dynamic> processOutput);
    }

    public interface IBranchingPoint<TContext>: IHideObjectMembers
    {
        IExecutionFlow<TContext> WithBranch();
    }

    class DelegateActivity<TContext, TOutput> : ActivityBase<TContext, TOutput>
    {
        private readonly Func<TContext, TOutput> m_ActivityMethod;

        public DelegateActivity(Func<TContext,TOutput> activityMethod)
        {
            if (activityMethod == null) throw new ArgumentNullException("activityMethod");
            m_ActivityMethod = activityMethod;
        }

        public override ActivityResult Execute(TContext input, Action<TOutput> processOutput)
        {
            processOutput(m_ActivityMethod(input));
            return ActivityResult.Succeeded;
        }
    }

    public class WorkflowConfiguration<TContext> : IExecutionFlow<TContext>, IBranchingPoint<TContext>, IDecisionPoint<TContext>
    {
        private readonly Workflow<TContext> m_Workflow;
        private readonly Stack<IGraphNode<TContext>> m_Nodes = new Stack<IGraphNode<TContext>>();
        private readonly Stack<GraphEdge<TContext>> m_Edges = new Stack<GraphEdge<TContext>>();

        public WorkflowConfiguration(Workflow<TContext> workflow)
        {
            m_Workflow = workflow;
            m_Nodes.Push(m_Workflow.Start);
        }

        internal Stack<IGraphNode<TContext>> Nodes
        {
            get { return m_Nodes; }
        }

        public WorkflowConfiguration<TContext> Do(string activity,string name,Func<TContext,object> getActivityInput,Action<TContext,dynamic> processOutput ) 
        {
            var activityNode = m_Workflow.CreateNode(name, activity, getActivityInput, processOutput);
            if (Nodes.Count > 0)
                Nodes.Peek().AddConstraint(name, (context, state) => state == ActivityResult.Succeeded, "Success");
            Nodes.Push(activityNode);
            return this;
        }

        public WorkflowConfiguration<TContext> Do<TActivity, TInput, TOutput>(string name, Func<TContext, TInput> getActivityInput ) where TActivity : IActivity<TInput, TOutput>
        {
            return Do<TActivity, TInput, TOutput>(name, getActivityInput, (c, o) => { });
        }


        public WorkflowConfiguration<TContext> Do<TActivity, TInput, TOutput>(string name, Func<TContext, TInput> getActivityInput, Action<TContext, TOutput> processOutput) where TActivity : IActivity<TInput, TOutput>
        {
            var activityNode = m_Workflow.CreateNode<TActivity, TInput, TOutput>(name, getActivityInput,processOutput);
            if(Nodes.Count>0)
                Nodes.Peek().AddConstraint(name, (context, state) => state == ActivityResult.Succeeded, "Success");
            Nodes.Push(activityNode);
            return this;
        }

        public WorkflowConfiguration<TContext> ContinueWith(string node)
        {
            Nodes.Peek().AddConstraint(node, (context, state) => state == ActivityResult.Succeeded, "Success");
            return this;
        }
     
        public WorkflowConfiguration<TContext> End()
        {
            Nodes.Peek().AddConstraint("end", (context, state) => state == ActivityResult.Succeeded, "Success");
            return this;
        }

        public IExecutionFlow<TContext> WithBranch()
        {
            Nodes.Clear();
            return this;
        }

        public INamedEdge<TContext> On(string name)
        {
            return new EdgeDescriptor<TContext>(this, name);
        }
    }
}