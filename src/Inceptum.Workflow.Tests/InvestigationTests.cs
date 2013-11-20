using System;
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
            GraphNode<string, ActivityBase<TInput, TOutput, TFailOutput>, TInput, TOutput, TFailOutput> node = null;
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
        }

    }

    class MyActivity : ActivityBase<InvalidCastException, string, Exception>
    {
        public override ActivityResult Execute(InvalidCastException input, Action<string> processOutput, Action<Exception> processFailOutput)
        {
            throw new NotImplementedException();
        }
 
    }

   

}