using System;
using System.Collections.Generic;

namespace Inceptum.Workflow
{
    class ExecutionState<TContext>
    {
        public TContext Context { get; set; }
        public ActivityResult State { get; set; }
    }

  


    internal class GraphNode<TContext> : GraphNode<TContext,GenericActivity, dynamic, dynamic>
    {
        public GraphNode(string name, string activityType, Func<TContext, dynamic> getActivityInput, Action<TContext, dynamic> processOutput)
            : base(name, activityType, getActivityInput, processOutput)
        {
        }
    }
   

          
    internal class GraphNode<TContext, TActivity, TInput, TOutput> : IGraphNode<TContext> where TActivity : IActivity<TInput, TOutput>
    {
        private readonly Func<TContext, TInput> m_GetActivityInput;
        private readonly Action<TContext, TOutput> m_ProcessOutput;

        public string Name { get; private set; }
        public string ActivityType { get; private set; }


        public GraphNode(string name, Func<TContext, TInput> getActivityInput, Action<TContext, TOutput> processOutput)
            : this(name, typeof(TActivity).Name, getActivityInput, processOutput)
        {
        }

        protected GraphNode(string name, string activityType, Func<TContext, TInput> getActivityInput, Action<TContext, TOutput> processOutput)
           
        {
            Name = name;
            ActivityType = activityType;
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

        private readonly List<GraphEdge<TContext>> m_Constraints = new List<GraphEdge<TContext>>();

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