using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Inceptum.Workflow.Tests
{

    interface IActivityReference
    {
        Tuple<string, object, Exception> RSBCheck();
    }

    internal static class WorkflowConfigurationExtensions
    {
        public static Tuple<string, object, Exception> Test(this IActivityReference reference)
        {
            return null;
        }

        public static ISlotCreationHelper<JObject, ActivityBase<TInput, TOutput, TFailOutput>> RemoteNode
            <TInput, TOutput, TFailOutput>(
            this Workflow<JObject> workflow,
            string name,
            Expression<Func<IActivityReference, Tuple<TInput, TOutput, TFailOutput>>> method)
            where TInput : class where TOutput : class where TFailOutput : class
        {
            var methodCallExpression = (method.Body as MethodCallExpression);
            if(methodCallExpression==null)
                throw new InvalidOperationException("IActivityReference method or extension method should be called");
            Console.WriteLine(((MethodCallExpression)method.Body).Method.Name);
            return workflow.Node<ActivityBase<TInput, TOutput, TFailOutput>>(name);
        }

        
    }

    [TestFixture]
    public class InvestigationTests
    {
        [Test]
        [Ignore]
        public void Test()
        {
            var executor = new FakeExecutor();
            var wf = new Workflow<JObject>("", new InMemoryPersister<JObject>(), activityExecutor: executor);
            wf.Configure(cfg => cfg.Do("node").End());
            wf.RemoteNode("node", i => i.RSBCheck());
            wf.RemoteNode("node", i => i.Test());
        }
    }
}