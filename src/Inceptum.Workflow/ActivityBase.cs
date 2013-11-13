using System;

namespace Inceptum.Workflow
{

    public abstract class ActivityBase<TInput, TOutput> : ActivityBase<TInput, TOutput, TOutput>
    {
    }

    public abstract class ActivityBase<TInput, TOutput, TFailOutput> : IActivity<TInput, TOutput, TFailOutput>
    {
        public abstract ActivityResult Execute(TInput input, Action<TOutput> processOutput, Action<TFailOutput> processFailOutput);

        public virtual ActivityResult Resume<TClosure>(Action<TOutput> processOutput, TClosure closure)
        {
            return ActivityResult.None;
        }

    }
}