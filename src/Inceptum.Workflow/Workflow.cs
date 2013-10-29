using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Inceptum.Workflow.Fluent;

namespace Inceptum.Workflow
{
    internal interface INodesResolver<TContext>
    {
        IGraphNode<TContext> this[string name] { get; }
    }

    public class Workflow<TContext> : IActivityFactory , INodesResolver<TContext>, IActivityExecutor 
    {
        private readonly IGraphNode<TContext> m_End;
        private readonly Dictionary<string, IGraphNode<TContext>> m_Nodes = new Dictionary<string, IGraphNode<TContext>>();
        private readonly IGraphNode<TContext> m_Start;
        private IWorkflowPersister<TContext> m_Persister;
        private IActivityFactory  m_ActivityFactory;
        private IActivityExecutor m_ActivityExecutor;

        public Workflow(string name, IWorkflowPersister<TContext> persister, IActivityFactory  activityFactory = null,IActivityExecutor  activityExecutor=null )
        {
            m_ActivityExecutor = activityExecutor??this;
            m_ActivityFactory = activityFactory ?? this;
            m_Persister = persister;
            Name = name;
            m_Start = new GraphNode<TContext, EmptyActivity, object, object>("start", context => null, (context, o) => { });
            m_End = new GraphNode<TContext, EndActivity, object, object>("end", context => null, (context, o) => { });
            registerNode(m_End);
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

        TActivity IActivityFactory.Create<TActivity, TInput, TOutput>()
        {
            return Activator.CreateInstance<TActivity>();
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
                    Nodes.Values.Where(n => n.Name != "end" && !n.Edges.Any()).Select(
                        n => string.Format("Node '{0}' is not connected with any other node.", n.Name))
                ).ToArray();
            if (errors.Any())
                throw new ConfigurationErrorsException(string.Join(Environment.NewLine, errors));
        }


        public Execution<TContext> Run(TContext context)
        {
            var execution = new Execution<TContext> { State = WorkflowState.InProgress };
            var executor = new WorkflowExecutor<TContext>(execution, context, this, m_ActivityFactory, m_ActivityExecutor);
            accept(executor);
            m_Persister.Save(context, execution);
            return execution;
        }


        public Execution<TContext> Resume<TClosure>(TContext context, TClosure closure)
        {
            var execution = m_Persister.Load(context);
            var executor = new WorkflowExecutor<TContext>(execution, context, this, m_ActivityFactory, m_ActivityExecutor,   closure);
            string node = execution.Log.Select(item => item.Node).LastOrDefault();
            accept(executor, node);
            m_Persister.Save(context, execution);
            return execution;
        }

        public Execution<TContext> ResumeFrom(TContext context, string node)
        {
            var execution = m_Persister.Load(context);
            var executor = new WorkflowExecutor<TContext>(execution, context, this, m_ActivityFactory, m_ActivityExecutor);
            accept(executor, node);
            m_Persister.Save(context, execution);
            return execution;
        }


        internal IGraphNode<TContext> CreateNode<TActivity, TInput, TOutput>(string name, Func<TContext, TInput> getActivityInput, Action<TContext, TOutput> processOutput, params string[] aliases)
            where TActivity : IActivity<TInput, TOutput>
        {
            var node = new GraphNode<TContext, TActivity, TInput, TOutput>(name, getActivityInput, processOutput);
            registerNode(node, aliases);
            return node;
        }

        internal IGraphNode<TContext> CreateNode(string name, string activityType, Func<TContext, dynamic> getActivityInput, Action<TContext, dynamic> processOutput, params string[] aliases)
        {
            var node = new GraphNode<TContext>(name, activityType, getActivityInput, processOutput);
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
 


        public ActivityResult Execute(string activityType, string nodeName, dynamic input, Action<dynamic> processOutput)
        {
            return ActivityResult.Failed;
        }
 
        public override string ToString()
        {
            var yuml = new YumlActivityGenerator<TContext>(this);
            return "paste the following to http://yuml.me/diagram/nofunky/activity/draw\n" + accept(yuml);
            // HttpUtility.UrlEncode(m_Graph.ToString());
        }

    }
}