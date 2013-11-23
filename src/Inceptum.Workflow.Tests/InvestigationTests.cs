using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Inceptum.Workflow.Tests
{

 
   

    public interface IActivityDescriptor<out T>
    {

    }
    public class ActivityDescriptor<T> : IActivityDescriptor<T>
    {

    }

    static class NodeEx
    {
        public static IActivityDescriptor<IActivity<TInput, TOutput, TFailOutput>> ProcessOutput<TInput, TOutput, TFailOutput>(this IActivityDescriptor<IActivity<TInput, TOutput, TFailOutput>> n, Action<TOutput> processOutput)
            where TInput : class
            where TOutput : class
            where TFailOutput : class
        {
            return n;
        }


        public static IActivityDescriptor<IActivity<TInput, TOutput, TFailOutput>> ProcessFailOutput<TInput, TOutput, TFailOutput>(this IActivityDescriptor<IActivity<TInput, TOutput, TFailOutput>> n, Action<TFailOutput> processOutput)
            where TInput : class
            where TOutput : class
            where TFailOutput : class
        {
            return n;
        }

        public static IActivityDescriptor<IActivity<TInput, TOutput, TFailOutput>> GetInput<TInput, TOutput, TFailOutput>(this IActivityDescriptor<IActivity<TInput, TOutput, TFailOutput>> n, Func<object, TInput> getInput)
            where TInput : class
            where TOutput : class
            where TFailOutput : class
        {
            return n;
        }

    }

    public interface IFactory
    {
        T Create<T>();
    }
    public class InvestigationTests
    {
        public void Node<TInput, TOutput, TFailOutput>(Func<IFactory, IActivity<TInput, TOutput, TFailOutput>> t)
            where TInput : class
            where TOutput : class
            where TFailOutput : class
        {
            GraphNode<string> node = null;
        }


        public ActivityDescriptor<T> Node<T>(string name) where T : IActivityWithOutput<object, object, object> 
        {
            return new ActivityDescriptor<T>();
        }

        [Test]
        public void Test()
        {
            IActivityWithOutput<object, object, object> a = new MyActivity();
            Node<MyActivity>("node name").ProcessOutput(i => { }).ProcessFailOutput(e => { }).GetInput(o => new InvalidCastException());


            var activitySlot = new Node<string>().Activity<MyActivity>().WithInput(s => new InvalidCastException());//.Create<MyActivity>(new object[0]);
        }

    }

    class MyActivity : ActivityBase<InvalidCastException, string, Exception>
    {
        public override ActivityResult Execute(InvalidCastException input, Action<string> processOutput, Action<Exception> processFailOutput)
        {
            throw new NotImplementedException();
        }
 
    }



    class Node<TContext> : IGraphNode<TContext>
    {
        public string Name { get; private set; }
        public string ActivityType { get; private set; }
        public IEnumerable<GraphEdge<TContext>> Edges { get; private set; }
        public T Accept<T>(IWorkflowVisitor<TContext, T> workflowExecutor)
        {
            throw new NotImplementedException();
        }

        public void AddConstraint(string node, Func<TContext, ActivityResult, bool> condition, string description)
        {
            throw new NotImplementedException();
        }

        public ISlotCreationHelper<TContext,TActivity> Activity<TActivity>() where TActivity : IActivityWithOutput<object, object, object>
        {
            return new SlotCreationHelper<TContext, TActivity>(null);
        }

        public IActivitySlot<TContext> ActivitySlot { get; private set; }
    }
   

}