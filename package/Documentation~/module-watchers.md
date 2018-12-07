# Watchers Module

The watchers module provides an optional high level helper for getting callback-like functionality in an ECS environment. 

In general callbacks are not a very ECS-friendly concept: All components only store data, and systems contain only code that operates on data.

Sometimes it is desirable for ease of use to have some kind of callback like functionality though: For example watchers can be used for UI event handlers like OnClick or to get notifications about the state of a tween animation like OnEnded or OnLoopPoint.

_Note: Watchers are entirely a user level concept and are implemented entirely in user code without any additional runtime support._

To use watchers, first create a new **WatchGroup**. A watch group is like any other System and _needs to be scheduled to run_. There can be multiple watch groups active at any time, for example all UI watchers could be in one group, and animation watchers in a different one. Because they are scheduled as systems they can be used to determine the order of callbacks. 

To add a new watcher to a watch group call **watchChanged** to watch a specific component field value for changes on particular target entity. Whenever that field value changes, and the watcher system runs the notification callback is invoked.

_Note: Watchers only register changes at the point where the watching system runs. If a component value is changed back to its original value before the watching system runs, the change will not be picked up._

(See this module's API documentation for more information)