using System;
using System.Collections.Generic;

namespace Inceptum.Workflow
{
    class ExecutionState<TContext>
    {
        public TContext Context { get; set; }
        public ActivityResult State { get; set; }
    }

    internal class GraphNode<TContext, TActivity> : IGraphNode<TContext> where TActivity : IActivity<TContext>
    {
        public GraphNode(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

        private readonly List<GraphEdge<TContext>> m_Constraints =
            new List<GraphEdge<TContext>>();

        public virtual void AddConstraint(string node, Func<TContext, ActivityResult, bool> condition, string description)
        {
            m_Constraints.Add(new GraphEdge<TContext>(node, condition, description));
        }

        public IEnumerable<GraphEdge<TContext>> Edges
        {
            get { return m_Constraints; }
        }

        public T Accept<T>(IWorflowVisitor<TContext, T> worflowExecutor)
        {
            return worflowExecutor.Visit(this);
        }
    }
}