using System;
using System.Collections.Generic;
using Inceptum.Workflow.Fluent;

namespace Inceptum.Workflow
{

    internal interface IWorkflowVisitor<TContext,  out TResult>
    {
        TResult Visit(IGraphNode<TContext> node);
    }

    internal interface IGraphNode<TContext>
    {
        string Name { get; }
        string ActivityType { get;  }
        IEnumerable<GraphEdge<TContext>> Edges { get; }
        T Accept<T>(IWorkflowVisitor<TContext, T> workflowExecutor);
        void AddConstraint(string node, Func<TContext, ActivityResult, bool> condition, string description);
        ISlotCreationHelper<TContext, TActivity> Activity<TActivity>(string activityName,params object[] activityCreationParams) where TActivity : IActivityWithOutput<object, object, object>;
        IActivitySlot<TContext> ActivitySlot
        {
            get;
        }
    }
}