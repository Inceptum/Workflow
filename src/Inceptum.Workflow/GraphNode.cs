using System;
using System.Collections.Generic;

namespace Inceptum.Workflow
{
    interface IActivitySlot<TContext>
    {
        string ActivityType { get; }
        ActivityResult Execute(IActivityFactory factory, TContext context, out object activityOutput, Action<object> beforeExecute);
        ActivityResult Resume<TClosure>(IActivityFactory factory, TContext context, TClosure closure, out object activityOutput);
    }

    public interface IActivitySlot<TContext, TInput, TOutput, TFailOutput> : IHideObjectMembers
    {
        IActivitySlot<TContext, TInput, TOutput, TFailOutput> ProcessOutput(Action<TContext, TOutput> processOutput);
        IActivitySlot<TContext, TInput, TOutput, TFailOutput> ProcessFailOutput(Action<TContext, TFailOutput> processFailOutput);
    }

    internal class EmptyActivitySlot<TContext> :  IActivitySlot<TContext>
    {
        public EmptyActivitySlot(string activityType)
        {
            if (activityType == null) throw new ArgumentNullException("activityType");
            m_ActivityType = activityType;
        }

        private readonly string m_ActivityType;

        public string ActivityType
        {
            get { return m_ActivityType; }
        }

        public ActivityResult Execute(IActivityFactory factory, TContext context, out object activityOutput, Action<object> beforeExecute)
        {
            beforeExecute(null);
            activityOutput = null;
            return ActivityResult.Succeeded;
        }

        public ActivityResult Resume<TClosure>(IActivityFactory factory, TContext context, TClosure closure, out object activityOutput)
        {
            activityOutput = null;
            return ActivityResult.Succeeded;
        }

    }

    internal class ActivitySlot<TContext, TInput, TOutput, TFailOutput> : IActivitySlot<TContext>, IActivitySlot<TContext, TInput, TOutput, TFailOutput>
        where TInput : class
        where TOutput : class
        where TFailOutput : class
    {
        private readonly Func<TContext, TInput> m_GetActivityInput;
        private Action<TContext, TOutput> m_ProcessOutput= (context, output) => { };
        private Action<TContext, TFailOutput> m_ProcessFailOutput= (context, output) => { };
        private readonly Func<IActivityFactory,IActivity<TInput, TOutput, TFailOutput>> m_ActivityCreation;

        public ActivitySlot(Func<IActivityFactory, IActivity<TInput, TOutput, TFailOutput>> activityCreation, Func<TContext, TInput> getInput,string activityType)
        {
            m_ActivityCreation = activityCreation;
            m_GetActivityInput = getInput;
            ActivityType = activityType;
        }

        public  IActivitySlot<TContext, TInput, TOutput, TFailOutput> ProcessOutput( Action<TContext, TOutput> processOutput)
        {
            m_ProcessOutput = processOutput;
            return this;
        }
        public  IActivitySlot<TContext, TInput, TOutput, TFailOutput> ProcessFailOutput( Action<TContext, TFailOutput> processFailOutput)
        {
            m_ProcessFailOutput = processFailOutput;
            return this;
        }

        public string ActivityType
        {
            get; private set;
        }

        public ActivityResult Execute(IActivityFactory factory, TContext context, out object activityOutput, Action<object> beforeExecute)
        {
            IActivity<TInput, TOutput, TFailOutput> activity=null;
            try
            {
                activity = m_ActivityCreation(factory);
                object actout = null;
                var activityInput = m_GetActivityInput(context);
                beforeExecute(activity.IsInputSerializable?activityInput:null);
                var result = activity.Execute(activityInput, output =>
                {
                    actout = output;
                    m_ProcessOutput(context, output);
                }, output =>
                {
                    actout = output;
                    m_ProcessFailOutput(context, output);
                });
                activityOutput = actout;
                return result;
            }
            finally
            {
                if(activity!=null)
                factory.Release(activity);
            }
        }

        public ActivityResult Resume<TClosure>(IActivityFactory factory, TContext context, TClosure closure, out object activityOutput)
        {
            var activity = m_ActivityCreation(factory);
            object actout = null;
            var result = activity.Resume(output =>
            {
                actout = output;
                m_ProcessOutput(context, output);
            }, output =>
            {
                actout = output;
                m_ProcessFailOutput(context, output);
            },
            closure);
            activityOutput = actout;
            return result;
        
        }
    }
     



    internal class GraphNode<TContext> : IGraphNode<TContext>
    {
        private IActivitySlot<TContext> m_ActivitySlot;
        private readonly List<GraphEdge<TContext>> m_Constraints = new List<GraphEdge<TContext>>();
        
        public string Name { get; private set; }

        public string ActivityType
        {
            get
            {
                return ActivitySlot != null ? ActivitySlot.ActivityType : "";
            }
        }

        public IActivitySlot<TContext> ActivitySlot
        {
            get { return m_ActivitySlot; }
        }
 
        public GraphNode(string name)
        {
            Name = name;
            m_ActivitySlot = new EmptyActivitySlot<TContext>(name);
        }

        public T Accept<T>(IWorkflowVisitor<TContext, T> workflowExecutor)
        {
            return workflowExecutor.Visit(this);
        }

        public virtual void AddConstraint(string node, Func<TContext, ActivityResult, bool> condition, string description)
        {

            m_Constraints.Add(new GraphEdge<TContext>(node, condition, description));
        }

        public ISlotCreationHelper<TContext, TActivity> Activity<TActivity>(string activityType,object activityCreationParams=null) where TActivity : IActivityWithOutput<object, object, object>
        {
            m_ActivitySlot = new EmptyActivitySlot<TContext>(activityType);
            return new SlotCreationHelper<TContext, TActivity>(this, activityType, activityCreationParams);
        }

        public IEnumerable<GraphEdge<TContext>> Edges
        {
            get { return m_Constraints; }
        }

        public void AddActivitySlot(IActivitySlot<TContext> activitySlot)
        {
            m_ActivitySlot = activitySlot;
        }
    }


    public interface ISlotCreationHelper<TContext, out TActivity> : IHideObjectMembers
    {
       
       
    }

    interface ISlotCreationHelperWithNode<TContext>
    {
        GraphNode<TContext> GraphNode{get;}
        string ActivityType { get; }
        IActivity<TInput, TOutput, TFailOutput> CreateActivity<TInput, TOutput, TFailOutput>(IActivityFactory activityFactory) where TInput : class where TOutput : class where TFailOutput : class;
    }

    internal class SlotCreationHelper<TContext, TActivity> :ISlotCreationHelper<TContext,  TActivity>, ISlotCreationHelperWithNode<TContext>
        where TActivity : IActivityWithOutput<object, object, object>
    {
        private readonly object m_ActivityCreationParams;
        private readonly string m_ActivityType;

        public SlotCreationHelper(GraphNode<TContext> graphNode, string activityType, object activityCreationParams)
        {
            m_ActivityType = activityType;
            m_ActivityCreationParams = activityCreationParams;
            GraphNode = graphNode;
        }

        public GraphNode<TContext> GraphNode
        {
            get; private set;
        }


        public IActivity<TInput, TOutput, TFailOutput> CreateActivity<TInput, TOutput, TFailOutput>(IActivityFactory activityFactory) where TInput : class where TOutput : class where TFailOutput : class
        {
            return (IActivity<TInput, TOutput, TFailOutput>) activityFactory.Create<TActivity>(m_ActivityCreationParams);
        }

        public string ActivityType
        {
            get
            {
                return m_ActivityType;
            }
        }
    }
     
    public static class SlotCreationHelperExtensions
    {

        public static IActivitySlot<TContext, TInput, TOutput, TFailOutput> WithInput<TContext, TInput, TOutput, TFailOutput>(this ISlotCreationHelper<TContext, IActivity<TInput, TOutput, TFailOutput>> n, Func<TContext, TInput> getInput)
            where TInput : class
            where TOutput : class
            where TFailOutput : class
        {
            var helper = (n as ISlotCreationHelperWithNode<TContext>);
            var activitySlot = new ActivitySlot<TContext, TInput, TOutput, TFailOutput>(activityFactory => helper.CreateActivity<TInput, TOutput, TFailOutput>(activityFactory), getInput, helper.ActivityType);
            helper.GraphNode.AddActivitySlot(activitySlot);
            return activitySlot;
        }

    }
}