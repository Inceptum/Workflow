namespace Inceptum.Workflow.Fluent
{
    public static class WorkflowConfigExtensions
    {
        public static WorkflowConfiguration<TContext> OnFail<TContext>(this WorkflowConfiguration<TContext> config, string name)
        {
            var node = config.Nodes.Peek();
            node.AddConstraint(name, (context, state) => state == ActivityResult.Failed, "Fail");
            return config;
        }

        public static EdgeDescriptor<TContext> OnFail<TContext>(this WorkflowConfiguration<TContext> config)
        {
            var edgeDescriptor = new EdgeDescriptor<TContext>(config, "Fail");
            edgeDescriptor.DeterminedAs((context, result) => result==ActivityResult.Failed);
            return edgeDescriptor;
        }
    }
}