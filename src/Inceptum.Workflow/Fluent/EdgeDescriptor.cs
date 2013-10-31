using System;

namespace Inceptum.Workflow.Fluent
{

    public interface INamedEdge<TContext> : IHideObjectMembers
    {
        EdgeDescriptor<TContext> DeterminedAs(Func<TContext, bool> func);
    }    
    
    public interface IDecisionPoint<TContext> : IBranchingPoint<TContext>,IHideObjectMembers
    {
        INamedEdge<TContext> On (string name);
    }



    public class EdgeDescriptor<TContext> : IHideObjectMembers, INamedEdge<TContext>
    {
        private readonly WorkflowConfiguration<TContext> m_Config;
        private readonly string m_Name;
        private Func<TContext, ActivityResult, bool> m_Func;

        public EdgeDescriptor(WorkflowConfiguration<TContext> config,string name)
        {
            m_Name = name;
            m_Config = config;
        }


        internal EdgeDescriptor<TContext> DeterminedAs(Func<TContext, ActivityResult, bool> func)
        {
            m_Func = func;
            return this;
        }


        public EdgeDescriptor<TContext> DeterminedAs(Func<TContext, bool> func)
        {
            m_Func = (context, state) => state == ActivityResult.Succeeded && func(context);
            return this;
        }

        public WorkflowConfiguration<TContext> ContinueWith(string name)
        {
            m_Config.Nodes.Peek().AddConstraint(name, m_Func, m_Name);
            return m_Config;
        }

        public WorkflowConfiguration<TContext> Fail()
        {
            return ContinueWith("fail");
        }

        public IDecisionPoint<TContext> End()
        {
            return ContinueWith("end");
        }
    }
}