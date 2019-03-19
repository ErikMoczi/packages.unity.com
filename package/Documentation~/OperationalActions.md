# Operational Actions

While the [plan definition](PlanDefinition.md) contains specifications for the requirements and effects of actions, operational actions govern the execution of actions within the game/simulation. 

All operational action scripts must implement the [`IOperationalAction<TAgent>`](xref:Unity.AI.Planner.Agent.IOperationalAction`1) interface, which requires methods for beginning, continuing, ending, and monitoring the status of the actions. Each method is defined over the following arguments:
* State Entity - An ECS entity for the state in which the action is being taken.
* Action Context - The context of the action within the plan.
* Agent - The agent which will perform the action. This is the same Agent class in [BaseAgent](BaseAgent.md


## Operational Action Status

```csharp
OperationalActionStatus Status(Entity stateEntity, ActionContext action, TAgent agent)
```

Operational actions are responsible for reporting the status of the action to the [Controller](xref:Unity.AI.Planner.Agent.Controller`1). This method is called each frame until the action is completed or determined no longer valid. The possible values of the OperationalActionStatus are:
* InProgress 
* NoLongerValid
* Complete


## Begin Execution

```csharp
void BeginExecution(Entity stateEntity, ActionContext action, TAgent agent)
```

BeginExecution is called once, at the start of each action.


## Continue Execution

```csharp
void ContinueExecution(Entity stateEntity, ActionContext action, TAgent agent)
```

ContinueExecution is called each frame until the action is determined to be Complete or NoLongerValid.

## End Execution

```csharp
void EndExecution(Entity stateEntity, ActionContext action, TAgent agent)
```

EndExecution is called once, after the action is reported Complete or NoLongerValid. 


