using System;
using System.Collections.Generic;

namespace Inceptum.Workflow
{
    class ExecutionState<TContext>
    {
        public TContext Context { get; set; }
        public ActivityResult State { get; set; }
    }

    internal class GraphNode<TContext, TActivity> : GraphNode<TContext> where TActivity : IActivity<TContext>
    {
        public GraphNode(string name) : base(name,typeof(TActivity).Name)
        {
        }
    }


    internal class GraphNode<TContext> : IGraphNode<TContext> 
    {
        
        public GraphNode(string name,string activityType)
        {
            Name = name;
            ActivityType = activityType;
        }

        public string Name { get; private set; }
        public string ActivityType { get; private set; }

        private readonly List<GraphEdge<TContext>> m_Constraints = new List<GraphEdge<TContext>>();

        public virtual void AddConstraint(string node, Func<TContext, ActivityResult, bool> condition, string description)
        {
            m_Constraints.Add(new GraphEdge<TContext>(node, condition, description));
        }

        public IEnumerable<GraphEdge<TContext>> Edges
        {
            get { return m_Constraints; }
        }

        public T Accept<T>(IWorflowVisitor<TContext, T> workflowExecutor)
        {
            return workflowExecutor.Visit(this);
        }
    }
}