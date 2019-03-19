# BaseAgent

We have provided a BaseAgent class which handles the initialization of the [PlannerSystem](xref:Unity.AI.Planner.PlannerSystem) and [Controller](xref:Unity.AI.Planner.Agent.Controller`1). Futhermore, this class possesses a serialized field for the initial domain data, which users may set in the inspector window in the editor. The BaseAgent class inherits from MonoBehaviour and, as a default, updates the controller on each frame.

In order to make use of the planner, you must implement your own agent class, which inherits from [BaseAgent<TAgent>](xref:UnityEngine.AI.Planner.Agent.BaseAgent`1). Ex:

```csharp
public class YourAgentClass : BaseAgent<YourAgentClass>
{
}
```

Once you have an agent class, then you can define [operational actions](OperationalActions.md), which will allow you to control what happens within the game/simulation as a result of the controller selecting a planner action.