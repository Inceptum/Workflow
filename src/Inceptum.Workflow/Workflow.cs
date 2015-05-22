﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using Inceptum.Workflow.Executors;
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
            registerNode(m_Start);
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
            try
            {
                accept(executor);
            }
            catch (Exception e)
            {
                execution.Error = e.ToString();
                execution.State = WorkflowState.Corrupted;
            }
            m_Persister.Save(context, execution);
            return execution;
        }


        public virtual Execution<TContext> Resume<TClosure>(TContext context, Guid activityExecutionId, TClosure closure)
        {
            var execution = m_Persister.Load(context);
            
            var activityExecution = execution.ExecutingActivities.FirstOrDefault(a => a.Id == activityExecutionId);
            if (activityExecution == null)
            {
                execution.Error = string.Format("Failed to resume. Provided activity execution id '{0}' not found", activityExecutionId);
                execution.State = WorkflowState.Corrupted;
            }
            else
            {
                var executor = new ResumeWorkflowExecutor<TContext>(execution, context, this, m_ActivityFactory, m_ExecutionObserver, activityExecution, closure);
                try
                {
                    string node = activityExecution.Node;
                    accept(executor, node);
                }
                catch (Exception e)
                {
                    execution.Error = e.ToString();
                    execution.State = WorkflowState.Corrupted;
                }
            }
            m_Persister.Save(context, execution);
            return execution;
        }

        public virtual Execution<TContext> ResumeAfter(TContext context, string node, IActivityOutputProvider outputProvider)
        {
            var execution = m_Persister.Load(context);

            var executor = new ResumeAfterWorkflowExecutor<TContext>(execution, context, this, m_ActivityFactory, m_ExecutionObserver, outputProvider);
            try
            {
                accept(executor, node);
            }
            catch (Exception e)
            {
                execution.Error = e.ToString();
                execution.State = WorkflowState.Corrupted;
            }
            m_Persister.Save(context, execution);
            return execution;
        }

        public virtual Execution<TContext> ResumeFrom(TContext context, string node, IActivityInputProvider inputProvider)
        {
            var execution = m_Persister.Load(context);
            var executor = new ResumeFromWorkflowExecutor<TContext>(execution, context, this, m_ActivityFactory, m_ExecutionObserver, inputProvider);
            try
            {
                accept(executor, node);
            }
            catch (Exception e)
            {
                execution.Error = e.ToString();
                execution.State = WorkflowState.Corrupted;
            }
            m_Persister.Save(context, execution);
            return execution;
        }

        public virtual Execution<TContext> ResumeFrom(TContext context, string node, object input = null)
        {
            var execution = m_Persister.Load(context);
            var executor = new ResumeFromWorkflowExecutor<TContext>(execution, context, this, m_ActivityFactory, m_ExecutionObserver, input);
            try
            {
                accept(executor, node);
            }
            catch (Exception e)
            {
                execution.Error = e.ToString();
                execution.State = WorkflowState.Corrupted;
            }
            m_Persister.Save(context, execution);
            return execution;
        }


        internal IGraphNode<TContext> CreateNode(string name,  params string[] aliases)
        {
            if (m_Nodes.ContainsKey(name))
                throw new ConfigurationErrorsException(string.Format("Can not create node '{0}', node with this name already exists", name));
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

        public ISlotCreationHelper<TContext, TActivity> Node<TActivity>(string name, object activityCreationParams=null,string activityType=null) where TActivity : IActivityWithOutput<object, object, object>
        {
            return Nodes[name].Activity<TActivity>(activityType??typeof(TActivity).Name, activityCreationParams);
        }

        public ISlotCreationHelper<TContext, DelegateActivity<TInput, TOutput>> DelegateNode<TInput, TOutput>(string name, Expression<Func<TInput, TOutput>> method) 
            where TInput : class where TOutput : class
        
        {
            var methodCall = method.Body as MethodCallExpression;
            string activityType = "DelegateActivity";

            if (methodCall != null)
            {
                activityType += " " + methodCall.Method.Name;
            }
            var activityMethod = method.Compile();

            return Nodes[name].Activity<DelegateActivity<TInput, TOutput>>(activityType, new { activityMethod, isInputSerializable = true});
        }

        public IActivitySlot<TContext, object, TOutput, Exception> DelegateNode<TOutput>(string name, Expression<Func<TContext, TOutput>> method) where TOutput : class
        {
            var methodCall = method.Body as MethodCallExpression;
            string activityType = "DelegateActivity";

            if (methodCall != null)
            {
                activityType += " " + methodCall.Method.Name;
            }
            Func<TContext, TOutput> compiled = method.Compile();
            Func<object, TOutput> activityMethod = context => compiled((TContext)context);
            return Nodes[name].Activity<DelegateActivity<object, TOutput>>(activityType, new { activityMethod, isInputSerializable = false }).WithInput(context => (object)context);
        }

        public IActivitySlot<TContext, object, object, Exception> DelegateNode(string name, Expression<Action<TContext>> method) 
        {
            var methodCall = method.Body as MethodCallExpression;
            string activityType = "DelegateActivity";

            if (methodCall != null)
            {
                activityType += " " + methodCall.Method.Name;
            }
            Action<TContext> compiled = method.Compile();
            Func<object,object> activityMethod = context =>
            {
                compiled((TContext) context);
                return null;
            };
            return Nodes[name].Activity<DelegateActivity<object, object>>(activityType, new { activityMethod, isInputSerializable = false }).WithInput(context => (object)context);
        }

         public ISlotCreationHelper<TContext, DelegateActivity<TInput, object>> DelegateNode<TInput>(string name, Expression<Action<TInput>> method) 
            where TInput : class
        
        {
            var methodCall = method.Body as MethodCallExpression;
            string activityType = "DelegateActivity";

            if (methodCall != null)
            {
                activityType += " " + methodCall.Method.Name;
            }
            Action<TInput> compiled = method.Compile();
            Func<object,object> activityMethod = input =>
            {
                compiled((TInput) input);
                return null;
            };

            return Nodes[name].Activity<DelegateActivity<TInput, object>>(activityType, new { activityMethod, isInputSerializable = true});
        }
    }

    public class DelegateActivity<TInput, TOutput> :ActivityBase<TInput, TOutput,Exception> where TInput : class where TOutput : class
    {
        private readonly Func<TInput, TOutput> m_ActivityMethod;
        private readonly bool m_IsInputSerializable;

        public DelegateActivity(Func<TInput, TOutput> activityMethod, bool isInputSerializable)
        {
            m_ActivityMethod = activityMethod;
            m_IsInputSerializable = isInputSerializable;
        }

        public override bool IsInputSerializable
        {
            get { return m_IsInputSerializable; }
        }

        public override ActivityResult Execute(Guid activityExecutionId, TInput input, Action<TOutput> processOutput, Action<Exception> processFailOutput)
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