using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using Inceptum.Workflow.Fluent;

namespace Inceptum.Workflow
{
    internal interface INodesResolver<TContext>
    {
        IGraphNode<TContext> this[string name] { get; }
    }

    public class Workflow<TContext> : IActivityFactory , INodesResolver<TContext> ,IDisposable
    {
        private readonly IGraphNode<TContext> m_End;
        private readonly IGraphNode<TContext> m_Fail;
        private readonly Dictionary<string, IGraphNode<TContext>> m_Nodes = new Dictionary<string, IGraphNode<TContext>>();
        private readonly IGraphNode<TContext> m_Start;
        private readonly IWorkflowPersister<TContext> m_Persister;
        private readonly IActivityFactory  m_ActivityFactory;
        private readonly IExecutionObserver m_ExecutionObserver;

        public Workflow(string name, IWorkflowPersister<TContext> persister, IActivityFactory  activityFactory = null,IExecutionObserver executionObserver=null )
        {
            m_ExecutionObserver = executionObserver;
            m_ActivityFactory = activityFactory ?? this;
            m_Persister = persister;
            Name = name;
            m_Start = new GraphNode<TContext>("start");
            m_End = new GraphNode<TContext>("end");
            m_Fail = new GraphNode<TContext>("fail");
            registerNode(m_End);
            registerNode(m_Fail);
        }

        public string Name { get; set; }


        internal Dictionary<string, IGraphNode<TContext>> Nodes
        {
            get { return m_Nodes; }
        }

        internal IGraphNode<TContext> Start
        {
            get { return m_Start; }
        }

        #region IActivityFactory<TContext> Members

        TActivity IActivityFactory.Create<TActivity>(object activityCreationParams)
        {

            var values = new Dictionary<string, object>();
            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(activityCreationParams))
            {
                var value = descriptor.GetValue(activityCreationParams);
                values.Add(descriptor.Name, value);
            }
           



            var constructor = typeof(TActivity)
                .GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault(info => info.GetParameters().All(p=>values.ContainsKey(p.Name) && values[p.Name].GetType()==p.ParameterType));
            if (constructor == null)
                throw new MissingMethodException("No public constructor defined for this object");


            var instance = constructor.Invoke(constructor.GetParameters().Select(p=>values[p.Name]).ToArray());
            return (TActivity)instance;
        }

        public void Release<TActivity>(TActivity activity) where TActivity : IActivityWithOutput<object, object, object>
        {
            m_ActivityFactory.Release(activity);
        }

        #endregion

        #region INodesResolver<TContext> Members

        IGraphNode<TContext> INodesResolver<TContext>.this[string name]
        {
            get { return m_Nodes[name]; }
        }

        #endregion

        public void Configure(Action<WorkflowConfiguration<TContext>> configure)
        {
            var conf = new WorkflowConfiguration<TContext>(this);


            configure(conf);

            string[] errors = Nodes.Values.SelectMany(n => n.Edges.Where(e => !Nodes.ContainsKey(e.Node))
                                                               .Select(e => string.Format("Node '{0}' references unknown node '{1}'", n.Name, e.Node))
                ).Union(
                    Nodes.Values.Where(n => n.Name != "end" &&  n.Name != "fail" && !n.Edges.Any()).Select(
                        n => string.Format("Node '{0}' is not connected with any other node.", n.Name))
                ).ToArray();
            if (errors.Any())
                throw new ConfigurationErrorsException(string.Join(Environment.NewLine, errors));
        }


        public virtual Execution<TContext> Run(TContext context)
        {
            var execution = new Execution<TContext> { State = WorkflowState.InProgress };
            var executor = new WorkflowExecutor<TContext>(execution, context, this, m_ActivityFactory,  m_ExecutionObserver);
            accept(executor);
            m_Persister.Save(context, execution);
            return execution;
        }


        public virtual Execution<TContext> Resume<TClosure>(TContext context, TClosure closure)
        {
            var execution = m_Persister.Load(context);
            var executor = new WorkflowExecutor<TContext>(execution, context, this, m_ActivityFactory,  m_ExecutionObserver, closure);
            string node = execution.ActiveNode;
            accept(executor, node);
            m_Persister.Save(context, execution);
            return execution;
        }

        public virtual Execution<TContext> ResumeFrom(TContext context, string node)
        {
            var execution = m_Persister.Load(context);
            var executor = new WorkflowExecutor<TContext>(execution, context, this, m_ActivityFactory,  m_ExecutionObserver);
            accept(executor, node);
            m_Persister.Save(context, execution);
            return execution;
        }


        internal IGraphNode<TContext> CreateNode(string name,  params string[] aliases)
        {
            var node = new GraphNode<TContext>(name);
            registerNode(node, aliases);
            return node;
        }

        private void registerNode(IGraphNode<TContext> node, params string[] aliases)
        {
            m_Nodes.Add(node.Name, node);
            foreach (string alias in aliases)
            {
                m_Nodes.Add(alias, node);
            }
        }


        private T accept<T>(IWorkflowVisitor<TContext, T> workflowExecutor, string startFrom = null)
        {
            IGraphNode<TContext> node = startFrom == null ? m_Start : m_Nodes[startFrom];
            return node.Accept(workflowExecutor);
        }
 


        public ActivityResult Execute(string activityType, string nodeName, dynamic input, Action<dynamic> processOutput, Action<dynamic> processFailOutput)
        {
            return ActivityResult.Failed;
        }
 
        public override string ToString()
        {
            var generator = new GraphvizGenerator<TContext>(this);
            return string.Format(@"digraph {{
graph [ resolution=64];

{0}
}}", accept(generator));
 
        }

        public void Dispose()
        {
//TODO[KN]: release activity
        }

        public ISlotCreationHelper<TContext, TActivity> Node<TActivity>(string name, object activityCreationParams=null) where TActivity : IActivityWithOutput<object, object, object>
        {
            return Nodes[name].Activity<TActivity>(typeof(TActivity).Name, activityCreationParams);
        }
 
        public ISlotCreationHelper<TContext, DelegateActivity<TInput, TOutput>> Node<TInput, TOutput>(string name, Expression<Func<TInput, TOutput>> method) 
            where TInput : class where TOutput : class
        
        {
            var methodCall = method.Body as MethodCallExpression;
            string activityType = null;

            if (methodCall != null)
            {
                activityType = methodCall.Method.Name;
            }
            var activityMethod = method.Compile();
            return Nodes[name].Activity<DelegateActivity<TInput, TOutput>>(activityType, new {activityMethod});
        }
    }

    public class DelegateActivity<TInput, TOutput> :ActivityBase<TInput, TOutput,Exception> where TInput : class where TOutput : class
    {
        private readonly Func<TInput, TOutput> m_ActivityMethod;

        public DelegateActivity(Func<TInput, TOutput> activityMethod)
        {
            m_ActivityMethod = activityMethod;
        }

        public override ActivityResult Execute(TInput input, Action<TOutput> processOutput, Action<Exception> processFailOutput)
        {
            try
            {
                var output = m_ActivityMethod(input);
                processOutput(output);
                return ActivityResult.Succeeded;
            }
            catch (Exception e)
            {
                processFailOutput(e);
                return ActivityResult.Failed;
            }

        }
    }
}