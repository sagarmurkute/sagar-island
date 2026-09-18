using System;
using System.Collections.Generic;

namespace SagarIsland;

public enum IslandEventPriority
{
    Clipboard = 1,
    VolumeBrightnessBattery = 2,
    Media = 3,
    NormalNotification = 4,
    CriticalNotification = 5
}

public class IslandEvent
{
    public IslandEventPriority Priority { get; }
    public IslandState State { get; }
    public double Width { get; }
    public double Height { get; }
    public double AutoCollapseSeconds { get; }
    public Action ApplyUI { get; }

    public IslandEvent(IslandEventPriority priority, IslandState state, double width, double height, double autoCollapseSeconds, Action applyUI)
    {
        Priority = priority;
        State = state;
        Width = width;
        Height = height;
        AutoCollapseSeconds = autoCollapseSeconds;
        ApplyUI = applyUI;
    }
}

public class IslandEventManager
{
    private readonly Queue<IslandEvent> _eventQueue = new();
    private readonly Action<IslandEvent> _presentHandler;
    private IslandEvent? _currentActiveEvent;

    public IslandEventManager(Action<IslandEvent> presentHandler)
    {
        _presentHandler = presentHandler;
    }

    public void PostEvent(IslandEvent newEvent, IslandState currentWindowVisualState)
    {
        // If window is currently collapsed or idle, present immediately
        if (currentWindowVisualState == IslandState.Collapsed || _currentActiveEvent == null)
        {
            _currentActiveEvent = newEvent;
            _presentHandler(newEvent);
            return;
        }

        // If currently in a user-pinned full expanded card, don't interrupt unless critical
        if (currentWindowVisualState == IslandState.ExpandedCard || currentWindowVisualState == IslandState.MediaExpanded)
        {
            if (newEvent.Priority == IslandEventPriority.CriticalNotification)
            {
                _currentActiveEvent = newEvent;
                _presentHandler(newEvent);
            }
            return;
        }

        // Priority Preemption Check:
        if (newEvent.Priority >= _currentActiveEvent.Priority)
        {
            // If the incoming event is higher priority or same priority update
            _currentActiveEvent = newEvent;
            _presentHandler(newEvent);
        }
        else
        {
            // For notifications or important items, queue them up to display after higher-priority item finishes
            if (newEvent.Priority >= IslandEventPriority.NormalNotification && _eventQueue.Count < 3)
            {
                _eventQueue.Enqueue(newEvent);
            }
            // Transient events (like clipboard or volume when a higher priority notification is on screen) are safely dropped
        }
    }

    public IslandEvent? GetNextQueuedEvent()
    {
        if (_eventQueue.Count > 0)
        {
            _currentActiveEvent = _eventQueue.Dequeue();
            return _currentActiveEvent;
        }
        _currentActiveEvent = null;
        return null;
    }

    public void ClearCurrent()
    {
        _currentActiveEvent = null;
    }
}
