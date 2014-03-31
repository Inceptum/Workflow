using System;

namespace Inceptum.Workflow
{

    public abstract class ActivityBase<TInput, TOutput> : ActivityBase<TInput, TOutput, TOutput>
        where TInput : class
        where TOutput : class
    {
    }

    public abstract class ActivityBase<TInput, TOutput, TFailOutput> : IActivity<TInput, TOutput, TFailOutput>
        where TInput : class
        where TOutput : class
        where TFailOutput : class
    {
        public virtual bool IsInputSerializable
        {
            get { return true; }
        }

        public abstract ActivityResult Execute(Guid activityExecutionId, TInput input, Action<TOutput> processOutput, Action<TFailOutput> processFailOutput);

        public virtual ActivityResult Resume<TClosure>(Guid activityExecutionId, Action<TOutput> processOutput, Action<TFailOutput> processFailOutput, TClosure closure)
        {
            return ActivityResult.None;
        }

    }
}