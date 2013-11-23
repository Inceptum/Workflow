namespace Inceptum.Workflow
{
    public interface IActivityFactory
    {
        TActivity Create<TActivity>(params object[] activityCreationParams) where TActivity : IActivityWithOutput<object, object, object>;
    }
}