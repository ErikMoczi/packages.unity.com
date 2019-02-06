# Demo - Simple Input

This demo shows a comparison of a simple movement behaviour in Unity and uTiny.

**Unity3d Example**

When creating behaviours in unity your data and your logic are merged together in a single file. In the example below the `Speed` variable is placed alongside the logic.

```csharp
using UnityEngine;

public class MovementBehaviour : MonoBehaviour
{
	public float Speed = 100;

	// Invoked once per frame on each object with a MovementBehaviour script
	void Update()
	{
		var dt = Time.deltaTime;
		var direction = new Vector3();

		if (Input.GetKey(KeyCode.UpArrow))
		{
			direction.y += 1;
		}
		
		if (Input.GetKey(KeyCode.DownArrow))
		{
			direction.y -= 1;
		}
		
		if (Input.GetKey(KeyCode.RightArrow))
		{
			direction.x += 1;
		}
		
		if (Input.GetKey(KeyCode.LeftArrow))
		{
			direction.x -= 1;
		}

		direction.Normalize();
		direction *= Speed * dt;

		var position = transform.localPosition;
		position += direction;
		transform.localPosition = position;
	}
}
```

**uTiny Example**

In an ECS model your data and logic are separated. You delcare your component definitions as pure data (through the editor). Add your components to entities in the editor just like you add MonoBehaviours to GameObjects. 

Systems operate statically on component sets with no state.

```javascript
// Invoked once per frame
function (sched, world) {
	// Iterate over all entities with a Transform AND Movement component
	world.forEachEntity([ut.Core2D.Transform, game.Movement], 
		function (entity, transform, movement) {
			var dt = sched.elapsed() / 1000;
			var direction = new Vector3f(0, 0, 0);

			if (Input.getKey(ut.input.KeyCode.UpArrow)) {
			    direction.y += 1;
			}

			if (Input.getKey(ut.input.KeyCode.DownArrow)) {
			    direction.y -= 1;
			}

			if (Input.getKey(ut.input.KeyCode.RightArrow)) {
			    direction.x += 1;
			}

			if (Input.getKey(ut.input.KeyCode.LeftArrow)) {
			    direction.x -= 1;
			}

			direction.normalize();
			direction.multiplyScalar(movement.speed() * dt);

			var position = transform.localPosition();
			position.add(direction);
			transform.setLocalPosition(position);
  		});
	};
};
```