using System;
using System.Collections.Generic;
using Inceptum.Workflow.Fluent;

namespace Inceptum.Workflow
{

    internal interface IWorflowVisitor<TContext, out TResult>
    {
        TResult Visit<TActivity>(GraphNode<TContext, TActivity> node) where TActivity : IActivity<TContext>;
        TResult Visit(GraphNode<TContext> node);
    }

    internal interface IGraphNode<TContext>
    {
        string Name { get; }
        IEnumerable<GraphEdge<TContext>> Edges { get; }
        T Accept<T>(IWorflowVisitor<TContext, T> workflowExecutor);
        void AddConstraint(string node, Func<TContext, ActivityResult, bool> condition, string description);
    }
}