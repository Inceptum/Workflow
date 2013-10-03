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
        private Func<TContext, bool> m_Func;

        public EdgeDescriptor(WorkflowConfiguration<TContext> config,string name)
        {
            m_Name = name;
            m_Config = config;
        }


        EdgeDescriptor<TContext> INamedEdge<TContext>.DeterminedAs(Func<TContext, bool> func)
        {
            m_Func = func;
            return this;
        }

        public IDecisionPoint<TContext> ContinueWith(string name)
        {
            m_Config.Nodes.Peek().AddConstraint(name, (context,state) =>state==ActivityResult.Succeeded && m_Func(context), m_Name);
            return m_Config;
        }

        public IDecisionPoint<TContext> End()
        {
            return ContinueWith("end");
        }
    }
}