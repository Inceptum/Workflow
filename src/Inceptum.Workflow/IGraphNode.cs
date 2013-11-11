using System;
using System.Collections.Generic;
using Inceptum.Workflow.Fluent;

namespace Inceptum.Workflow
{

    internal interface IWorkflowVisitor<TContext,  out TResult>
    {
        TResult Visit<TActivity, TInput, TOutput>(GraphNode<TContext, TActivity, TInput, TOutput> node) where TActivity : IActivity<TInput, TOutput>;
    }

    internal interface IGraphNode<TContext>
    {
        string Name { get; }
        string ActivityType { get;  }
        IEnumerable<GraphEdge<TContext>> Edges { get; }
        T Accept<T>(IWorkflowVisitor<TContext, T> workflowExecutor);
        void AddConstraint(string node, Func<TContext, ActivityResult, bool> condition, string description);
    }
}