using System;
using System.Collections.Generic;

namespace Inceptum.Workflow
{
    class ExecutionState<TContext>
    {
        public TContext Context { get; set; }
        public ActivityResult State { get; set; }
    }

  


    internal class GraphNode<TContext> : GraphNode<TContext,GenericActivity, dynamic, dynamic,dynamic>
    {
        public GraphNode(string name, string activityType, Func<TContext, dynamic> getActivityInput, Action<TContext, dynamic> processOutput, Action<TContext, dynamic> processFailOutput, params object[] activityCreationParams)
            : base(name, activityType, getActivityInput, processOutput,processFailOutput, activityCreationParams)
        {
        }
    }



    internal class GraphNode<TContext, TActivity, TInput, TOutput, TFailOutput> : IGraphNode<TContext> where TActivity : IActivity<TInput, TOutput, TFailOutput>
        where TInput : class
        where TOutput : class
        where TFailOutput : class
    {
        private readonly Func<TContext, TInput> m_GetActivityInput;
        private readonly Action<TContext, TOutput> m_ProcessOutput;

        public string Name { get; private set; }
        public string ActivityType { get; private set; }
        public object[] ActivityCreationParams { get; set; }


        public GraphNode(string name, Func<TContext, TInput> getActivityInput, Action<TContext, TOutput> processOutput, Action<TContext, TFailOutput> processFailOutput, params object[] activityCreationParams)
            : this(name, null, getActivityInput, processOutput,processFailOutput, activityCreationParams)
        {
        }

        public GraphNode(string name, string activityType, Func<TContext, TInput> getActivityInput, Action<TContext, TOutput> processOutput, Action<TContext, TFailOutput> processFailOutput,params object[] activityCreationParams)

        {
            m_ProcessFailOutput = processFailOutput;
            ActivityCreationParams = activityCreationParams;
            Name = name;
            ActivityType = activityType ?? typeof(TActivity).Name;
            m_ProcessOutput = processOutput;
            if (getActivityInput == null) throw new ArgumentNullException("getActivityInput");
            m_GetActivityInput = getActivityInput;
        }

        public T Accept<T>(IWorkflowVisitor<TContext, T> workflowExecutor)
        {
            return workflowExecutor.Visit(this);
        }
  
 
        public TInput GetActivityInput(TContext context)
        {
            return m_GetActivityInput(context);
        }

        public void ProcessOutput(TContext context, TOutput output)
        {
            m_ProcessOutput(context, output);
        }

  
        public void ProcessFailOutput(TContext context, TFailOutput output)
        {
            m_ProcessFailOutput(context, output);
        }

        private readonly List<GraphEdge<TContext>> m_Constraints = new List<GraphEdge<TContext>>();
        private Action<TContext, TFailOutput> m_ProcessFailOutput;

        public virtual void AddConstraint(string node, Func<TContext, ActivityResult, bool> condition, string description)
        {
            m_Constraints.Add(new GraphEdge<TContext>(node, condition, description));
        }

        public IEnumerable<GraphEdge<TContext>> Edges
        {
            get { return m_Constraints; }
        }

    }
}