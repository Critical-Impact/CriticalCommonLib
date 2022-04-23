using System;
using System.Reactive.Linq;

namespace CriticalCommonLib.Extensions
{
    public static class ReactiveExtensions
    {
        public static IObservable<T> StepInterval<T>(this IObservable<T> source, TimeSpan minDelay)
        {
            return source.Select(x => 
                Observable.Empty<T>()
                    .Delay(minDelay)
                    .StartWith(x)
            ).Concat();
        }
    }
}