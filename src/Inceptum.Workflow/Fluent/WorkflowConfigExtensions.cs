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
    }
}