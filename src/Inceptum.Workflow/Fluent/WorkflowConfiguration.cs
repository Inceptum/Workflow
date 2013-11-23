using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Inceptum.Workflow.Fluent
{
   /* public static class ExecutionFlowExtensions 
    {
        public static WorkflowConfiguration<TContext> Do<TOutput, TContext>(this IExecutionFlow<TContext> flow, string name,
           Expression<Func<TContext, TOutput>> method) where TOutput : class
        {
            var methodCall = method.Body as MethodCallExpression;
            string activityType = null;

            if (methodCall != null)
            {
                activityType = methodCall.Method.Name;
            }
            var activityMethod = method.Compile();
            return flow.Do<DelegateActivity<TOutput>, Func<TOutput>, TOutput,Exception>(name, activityType, context => (() => activityMethod(context)), ((context, output) => { }) ,((context, output) => { }));
        }


        public static WorkflowConfiguration<TContext> Do<TContext>(this IExecutionFlow<TContext> flow, string activity, Func<TContext, object> getActivityInput,
            Action<TContext, dynamic> processOutput=null,
            Action<TContext, dynamic> processFailOutput=null)
        {
            return flow.Do(activity, activity, getActivityInput, processOutput ?? ((context, o) => { }), processFailOutput ?? ((context, o) => { }));
        }



    }*/

    public interface IExecutionFlow<TContext>: IHideObjectMembers
    {
/*        WorkflowConfiguration<TContext> Do<TActivity, TInput, TOutput, TFailOutput>(string name, string activityType, Func<TContext, TInput> getActivityInput, Action<TContext, TOutput> processOutput, Action<TContext, TFailOutput> processFailOutput, params object[] activityCreationParams)
            where TActivity : IActivity<TInput, TOutput, TFailOutput>
            where TInput : class
            where TOutput : class
            where TFailOutput : class;
        WorkflowConfiguration<TContext> Do<TActivity, TInput, TOutput, TFailOutput>(string name, Func<TContext, TInput> getActivityInput, Action<TContext, TOutput> processOutput, Action<TContext, TFailOutput> processFailOutput, params object[] activityCreationParams)
            where TActivity : IActivity<TInput, TOutput, TFailOutput>
            where TInput : class
            where TOutput : class
            where TFailOutput : class;
        WorkflowConfiguration<TContext> Do<TActivity, TInput, TOutput, TFailOutput>(string name, Func<TContext, TInput> getActivityInput, params object[] activityCreationParams)
            where TActivity : IActivity<TInput, TOutput, TFailOutput>
            where TInput : class
            where TOutput : class
            where TFailOutput : class;
        WorkflowConfiguration<TContext> Do(string activity, string name, Func<TContext, object> getActivityInput, Action<TContext, dynamic> processOutput, Action<TContext, dynamic> processFailOutput=null);

        WorkflowConfiguration<TContext> Do<TActivity, TInput, TOutput>(string name, string activityType, Func<TContext, TInput> getActivityInput, Action<TContext, TOutput> processOutput, Action<TContext, TOutput> processFailOutput, params object[] activityCreationParams)
            where TActivity : IActivity<TInput, TOutput, TOutput>
            where TInput : class
            where TOutput : class;
        WorkflowConfiguration<TContext> Do<TActivity, TInput, TOutput>(string name, Func<TContext, TInput> getActivityInput, Action<TContext, TOutput> processOutput, Action<TContext, TOutput> processFailOutput = null, params object[] activityCreationParams)
            where TActivity : IActivity<TInput, TOutput, TOutput>
            where TInput : class
            where TOutput : class;
        WorkflowConfiguration<TContext> Do<TActivity, TInput, TOutput>(string name, Func<TContext, TInput> getActivityInput, params object[] activityCreationParams)
            where TActivity : IActivity<TInput, TOutput, TOutput>
            where TInput : class
            where TOutput : class;*/

        WorkflowConfiguration<TContext> Do(string name);
    }

    public interface IBranchingPoint<TContext>: IHideObjectMembers
    {
        IExecutionFlow<TContext> WithBranch();
    }

    class DelegateActivity<TOutput> : ActivityBase<Func<TOutput>, TOutput,Exception> where TOutput : class
    {
        public DelegateActivity()
        {
        }

        public override ActivityResult Execute(Func<TOutput> activityMethod, Action<TOutput> processOutput, Action<Exception> processFailOutput)
        {
            try
            {
                processOutput(activityMethod());

            }
            catch (Exception e)
            {
                processFailOutput(e);
                return ActivityResult.Failed;

            }
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

        internal  Workflow<TContext> Workflow
        {
            get { return m_Workflow; }
        }

        internal Stack<IGraphNode<TContext>> Nodes
        {
            get { return m_Nodes; }
        }

/*
        public WorkflowConfiguration<TContext> Do<TActivity, TInput, TOutput>(string name, string activityType, Func<TContext, TInput> getActivityInput, Action<TContext, TOutput> processOutput, Action<TContext, TOutput> processFailOutput=null,
            params object[] activityCreationParams)
            where TActivity : IActivity<TInput, TOutput, TOutput>
            where TInput : class
            where TOutput : class
            
        {
            return Do<TActivity, TInput, TOutput, TOutput>(name, activityType, getActivityInput, processOutput, processFailOutput ?? ((context, output) => { }), activityCreationParams);
        }
 
        public WorkflowConfiguration<TContext> Do<TActivity, TInput, TOutput>(string name, Func<TContext, TInput> getActivityInput, Action<TContext, TOutput> processOutput, Action<TContext, TOutput> processFailOutput=null,
            params object[] activityCreationParams)
            where TActivity : IActivity<TInput, TOutput, TOutput>
            where TInput : class
            where TOutput : class
            
        {
            return Do<TActivity, TInput, TOutput, TOutput>(name, getActivityInput, processOutput, processFailOutput ?? ((context, output) => { }), activityCreationParams);
        }

        public WorkflowConfiguration<TContext> Do<TActivity, TInput, TOutput>(string name, Func<TContext, TInput> getActivityInput, params object[] activityCreationParams) where TActivity : IActivity<TInput, TOutput, TOutput>
            where TInput : class
            where TOutput : class
            
        {
            return Do<TActivity, TInput, TOutput, TOutput>(name, getActivityInput, activityCreationParams);
        }

        public WorkflowConfiguration<TContext> Do(string activity, string name, Func<TContext, object> getActivityInput, Action<TContext, dynamic> processOutput, Action<TContext, dynamic> processFailOutput=null) 
        {
            var activityNode = m_Workflow.CreateNode(name, activity, getActivityInput, processOutput,processFailOutput??((context, o) => { }) );
            if (Nodes.Count > 0)
                Nodes.Peek().AddConstraint(name, (context, state) => state == ActivityResult.Succeeded, "Success");
            Nodes.Push(activityNode);
            return this;
        }

        public WorkflowConfiguration<TContext> Do<TActivity, TInput, TOutput, TFailOutput>(string name, Func<TContext, TInput> getActivityInput, params object[] activityCreationParams)
            where TActivity : IActivity<TInput, TOutput, TFailOutput>
            where TInput : class
            where TOutput : class
            where TFailOutput : class
        {
            return Do<TActivity, TInput, TOutput, TFailOutput>(name, getActivityInput, (c, o) => { }, (c, o) => { });
        }

        public WorkflowConfiguration<TContext> Do<TActivity, TInput, TOutput, TFailOutput>(string name, string activityType, Func<TContext, TInput> getActivityInput, Action<TContext, TOutput> processOutput, Action<TContext, TFailOutput> processFailOutput, params object[] activityCreationParams)
            where TActivity : IActivity<TInput, TOutput, TFailOutput>
            where TInput : class
            where TOutput : class
            where TFailOutput : class
        {
            var activityNode = m_Workflow.CreateNode<TActivity, TInput, TOutput, TFailOutput>(name, activityType, getActivityInput, processOutput, processFailOutput,activityCreationParams);
            if (Nodes.Count > 0)
                Nodes.Peek().AddConstraint(name, (context, state) => state == ActivityResult.Succeeded, "Success");
            Nodes.Push(activityNode);
            return this;
        }
*/

        public WorkflowConfiguration<TContext> Do(string name)
        {

            var activityNode = m_Workflow.CreateNode(name);
            if (Nodes.Count > 0)
                Nodes.Peek().AddConstraint(name, (context, state) => state == ActivityResult.Succeeded, "Success");
            Nodes.Push(activityNode);
            return this;
        }

      /*  public WorkflowConfiguration<TContext> Do<TActivity, TInput, TOutput, TFailOutput>(string name, Func<TContext, TInput> getActivityInput, Action<TContext, TOutput> processOutput, Action<TContext, TFailOutput> processFailOutput, params object[] activityCreationParams)
            where TActivity : IActivity<TInput, TOutput, TFailOutput>
            where TInput : class
            where TOutput : class
            where TFailOutput : class
        {
            return Do<TActivity, TInput, TOutput, TFailOutput>(name, null, getActivityInput, processOutput,processFailOutput, activityCreationParams);
        }*/

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



        public WorkflowConfiguration<TContext> Fail()
        {
            Nodes.Peek().AddConstraint("fail", (context, state) => state == ActivityResult.Succeeded, "Fail");
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