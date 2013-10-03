using System;

namespace Inceptum.Workflow
{
    internal class GraphEdge<TContext>
    {
        public string Node { get; private set; }
        public string Description { get; set; }
        public Func<TContext, ActivityResult, bool> Condition { get; private set; }

        public GraphEdge(string node, Func<TContext, ActivityResult, bool> condition, string description)
        {
            Node = node;
            Description = description;
            Condition = condition ?? ((context, state) => true);
        }
    }
}