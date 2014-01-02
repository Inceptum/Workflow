namespace Inceptum.Workflow
{
    public interface IActivityFactory
    {
        TActivity Create<TActivity>(object activityCreationParams) where TActivity : IActivityWithOutput<object, object, object>;
    }
}