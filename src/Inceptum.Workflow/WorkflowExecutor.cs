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
    }

    public enum ActivityStatus
    {
        None,
        Failed,
        Complete
    }

    public class ActivityState
    {
        public string Values { get; set; }
        public string NodeName { get; set; }
        public ActivityStatus Status { get; set; }
    }

    public interface IActivityExecutor<TContext>
    {
        ActivityResult Execute(TContext context, string activityType, string nodeName);
    }

    internal class WorkflowExecutor<TContext> : IWorflowVisitor<TContext, WorkflowState>
    {
        private readonly IActivityFactory<TContext> m_Factory;
        private readonly INodesResolver<TContext> m_Nodes;
        private Func<IActivity<TContext>, TContext, ActivityResult> m_Resume;
        private readonly Execution<TContext> m_Execution;
        private readonly TContext m_Context;
        private readonly IActivityExecutor<TContext> m_ActivityExecutor;


        public WorkflowExecutor(Execution<TContext> execution, TContext context, INodesResolver<TContext> nodes, IActivityFactory<TContext> factory,IActivityExecutor<TContext> activityExecutor,
            Func<IActivity<TContext>, TContext, ActivityResult> resume = null)
        {
            m_ActivityExecutor = activityExecutor;
            m_Context = context;
            m_Resume = resume;
            m_Factory = factory;
            m_Execution = execution;
            m_Nodes = nodes;
        }

        public WorkflowState Visit<TActivity>(GraphNode<TContext, TActivity> node) where TActivity : IActivity<TContext>
        {
            var activity = m_Factory.Create<TActivity>();
            ActivityResult result = m_Resume != null ? m_Resume(activity, m_Context) : activity.Execute(m_Context);
            return visit(node, result);
        }

        public WorkflowState Visit(GraphNode<TContext> node)
        {

            ActivityResult result = m_Resume != null ? m_Resume(null, m_Context) : m_ActivityExecutor.Execute(m_Context,node.ActivityType,node.Name);
            return visit(node, result);

        }

        private WorkflowState visit(GraphNode<TContext> node,ActivityResult result)  
        {
            var logItem = m_Execution.AddLog(node.Name);
            Console.WriteLine(node.Name + " (" + node.ActivityType + "): ");
            m_Resume = null;
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