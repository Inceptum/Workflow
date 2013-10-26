using System;
using System.Collections.Generic;
using System.Linq;

namespace Inceptum.Workflow
{
    public class Execution<TContext>
    {
        private readonly List<WorkflowLogItem> m_Log = new List<WorkflowLogItem>();
        public WorkflowState State { get; set; }
        public IEnumerable<WorkflowLogItem> Log
        {
            get { return m_Log; }
        }

        public WorkflowLogItem AddLog(string node)
        {
            var logItem = new WorkflowLogItem(node) { Start = DateTime.Now };
            m_Log.Add(logItem);
            return logItem;
        }

        public Execution()
        {
        }

        public Execution(WorkflowState state, IEnumerable<WorkflowLogItem> log)
        {
            m_Log.AddRange(log);
            State = state;
        }
    }

    public enum ActivityStatus
    {
        None,
        Failed,
        Complete
    }

    public class ActivityState
    {
        public dynamic Values { get; set; }
        public string NodeName { get; set; }
        public ActivityStatus Status { get; set; }
    }

    public interface IActivityExecutor 
    {
        ActivityResult Execute(string activityType, string nodeName,dynamic input, Action<dynamic> processOutput);
    }

    public class GenericActivity:ActivityBase<dynamic,dynamic>
    {
        private readonly IActivityExecutor m_Executor;
        private readonly string m_ActivityType;
        private readonly string m_NodeName;

        public GenericActivity(IActivityExecutor executor, string activityType, string nodeName)
        {
            m_NodeName = nodeName;
            m_ActivityType = activityType;
            m_Executor = executor;
        }

        public override ActivityResult Execute(dynamic input, Action<dynamic> processOutput)
        {
            return m_Executor.Execute(m_ActivityType, m_NodeName,input, processOutput);
        }

        public override ActivityResult Resume<TClosure>(Action<dynamic> processOutput, TClosure closure)
        {
            if (closure.GetType()== typeof(ActivityState))
            {
                var state = ((ActivityState)(object)closure);
                if(state.NodeName!=m_NodeName)
                    return ActivityResult.Pending;

                if (state.Status == ActivityStatus.Complete)
                {
                    processOutput(state.Values);
                    return ActivityResult.Succeeded;
                }

                if (state.Status == ActivityStatus.Failed)
                {
                    return ActivityResult.Failed;
                }
            }
            return ActivityResult.Pending;
        }
    }

    internal class WorkflowExecutor<TContext> : IWorkflowVisitor<TContext, WorkflowState>
    {
        private readonly IActivityFactory m_Factory;
        private readonly INodesResolver<TContext> m_Nodes;
        private readonly Execution<TContext> m_Execution;
        private readonly TContext m_Context;
        private readonly IActivityExecutor m_ActivityExecutor;
        private bool m_Resuming = false;
        private readonly object m_Closure;

        public WorkflowExecutor(Execution<TContext> execution, TContext context, INodesResolver<TContext> nodes, IActivityFactory factory,IActivityExecutor  activityExecutor,object closure)
            :this(execution,context,nodes,factory,activityExecutor)
        {
            m_Resuming = true;
            m_Closure = closure;
        }

        public WorkflowExecutor(Execution<TContext> execution, TContext context, INodesResolver<TContext> nodes, IActivityFactory factory,IActivityExecutor  activityExecutor)
        {
            m_Context = context;
            m_Factory = factory;
            m_Execution = execution;
            m_Nodes = nodes;
            m_ActivityExecutor=activityExecutor;
        }




        public WorkflowState Visit<TActivity, TInput, TOutput>(GraphNode<TContext, TActivity, TInput, TOutput> node) where TActivity : IActivity<TInput, TOutput>
        {
            TActivity activity;
            if (typeof (TActivity) == typeof (GenericActivity))
                activity = (TActivity)(object)new GenericActivity(m_ActivityExecutor, node.ActivityType, node.Name);
            else
                activity = m_Factory.Create<TActivity, TInput, TOutput>();

            ActivityResult result = m_Resuming ? activity.Resume(output => node.ProcessOutput(m_Context, output), m_Closure) : activity.Execute(node.GetActivityInput(m_Context), output => node.ProcessOutput(m_Context, output));
       


            var logItem = m_Execution.AddLog(node.Name);
            Console.WriteLine(node.Name + " (" + node.ActivityType + "): ");
            m_Resuming = false;
            Console.WriteLine("\t" + result);

            logItem.Status = result;
            if (result == ActivityResult.Pending)
            {
                m_Execution.State = WorkflowState.InProgress;
                return WorkflowState.InProgress;
            }

            if (result == ActivityResult.None)
            {
                m_Execution.State = WorkflowState.Corrupted;
                return WorkflowState.Corrupted;
            }



            logItem.End = DateTime.Now;

            var next = node.Edges.SingleOrDefault(e => e.Condition(m_Context, result));

            if (next != null)
            {
                var nextNode = m_Nodes[next.Node];
                var nextResult = nextNode.Accept(this);
                return nextResult;
            }

            //TODO: =="end" is not good idea
            if (node.Name == "end" && result == ActivityResult.Succeeded)
            {
                m_Execution.State = WorkflowState.Complete;
                return WorkflowState.Complete;
            }

            m_Execution.State = WorkflowState.Corrupted;
            return WorkflowState.Corrupted;

        }
    }
}