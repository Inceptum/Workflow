using System;
using System.Collections.Generic;
using Inceptum.Workflow.Fluent;

namespace Inceptum.Workflow
{

    internal interface IWorkflowVisitor<TContext,  out TResult>
    {
        TResult Visit<TActivity, TInput, TOutput, TFailOutput>(GraphNode<TContext, TActivity, TInput, TOutput, TFailOutput> node)
            where TActivity : IActivity<TInput, TOutput, TFailOutput>
            where TInput : class
            where TOutput : class
            where TFailOutput : class;
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