using System;

namespace Inceptum.Workflow
{
    public abstract class ActivityBase<TInput, TOutput> : IActivity<TInput, TOutput>
    {
        public abstract ActivityResult Execute(TInput input, Action<TOutput> processOutput);

        public virtual ActivityResult Resume<TClosure>(Action<TOutput> processOutput, TClosure closure)
        {
            return ActivityResult.None;
        }

    }
}