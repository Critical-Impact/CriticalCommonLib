using System;

namespace CriticalCommonLib.Time;

public interface ISeTime : IDisposable
{
    TimeStamp ServerTime { get; }
    TimeStamp EorzeaTime { get; }
    long EorzeaTotalMinute { get; }
    long EorzeaTotalHour { get; }
    short EorzeaMinuteOfDay { get; }
    byte EorzeaHourOfDay { get; }
    byte EorzeaMinuteOfHour { get; }
    event Action? Updated;
    event Action? HourChanged;
    event Action? WeatherChanged;
}