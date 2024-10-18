# Script Nodes

## Tags

### Globally Supported Tags
#### `@trigger (TriggerId)`
This node is a **Trigger Node**. This node will be evaluated with other `@trigger` nodes with the same `TriggerId` during trigger queries. The most specific node that passes the query will be executed. If multiple nodes with the same specificity are discovered, a random one from that set will be chosen instead.

`(TriggerId)` (name): The name of the trigger. This will be specified by the game.
#### `@function (FunctionId)`
This node is a **Function Node**. This node will be evaluated with other `@function` nodes with the same `TriggerId` during trigger queries. All nodes that pass the query will be executed.

`(FunctionId)` (name): The name of the function. This will be specified by the game.
#### `@exposed`
This node can be referenced by name from other `.leaf` files.
#### `@tracked [Scope]`
Visiting this node will be tracked by scripting persistence.

`[Scope]` (scope, optional): Specifies the scope of this tracking.
* Supported values: `session`, `chapter`, `save`
* Default value: `save`

**NOTE:** This is incompatible with the `@once` tag.
#### `@tag [Tag]`
Sets a single metadata tag for this node.
### Supported by Non-Function Nodes
#### `@cutscene`
This node is a cutscene. Only one cutscene is allowed to be running at a time.

### Supported By Both Trigger and Function
#### `@who (ActorId)`
Sets the node's target actor.

If a query has an explicit target, this node will only be allowed to execute if the query's target matches the node's target.

If this is a `@trigger` node, this node will only be allowed to execute if the target actor's current thread is able to be interrupted by this node's `@priority` and `@interrupt` settings.

`(ActorId)` (name): The name of the target actor, or `*`.

**NOTE:** Setting this to `*` will allow this node to be used for any query target.
#### `@when [Conditions]`
This node will only be allowed to execute if all the specified conditions are met.

`[Conditions]` (comma-separated list of leaf expressions, optional): List of conditions. At present, these are logically combined by an AND.

**NOTE:** For `@trigger` nodes, this adjusts the node's "specificity score" by the number of conditions specified. This is used for finding the most specific nodes that satisfy a query.
#### `@once [Scope]`
This node will only allowed to execute once, within a specific history scope. Additionally, visiting this node will be tracked by scripting persistence.

`[Scope]` (scope, optional): Specifies the scope of tracking.
* Supported values: `session`, `chapter`, `save`
* Default value: `save`

**NOTE:** This is incompatible with the `@tracked` tag.
#### `@ignoreDuringCutscene`
This node will be unable to execute when a node marked as `@cutscene` is playing.

### Supported by Trigger Only
#### `@boostScore (Score)`
Adjusts the "specificity score" of this node. This is used for finding the most specific nodes that satisfy a query. Normally this score is set by the number of conditions in the `@when` tag, but this allows you to adjust this to prioritize or deprioritize this node.

`(Score)` (integer): Amount to adjust the specificity score by.
#### `@repeat (Window)`
Specifies the minimum number of nodes that must be visited before this node is allowed to be triggered again.

`(Window)` (integer): Number of nodes to check.

**NOTE:** This accesses an underlying history buffer. Values greater than the size of that buffer (64 at present) will not be respected.
#### `@cooldown (Seconds)`
Specifies the minimum number of seconds that must pass before this node is allowed to be triggered again. 

`(Seconds)` (float): Number of seconds, in scaled game time

**NOTE:** Be aware that this uses the same underlying history buffer from `@repeat`. In rare cases, where this cooldown is set high and many nodes are visited in rapid succession, the node will be allowed to be seen prior to this cooldown completing.
#### `@priority (Priority)`
Sets the priority of this node. If a `@who` target is present, or the query has an explicit target, this node is only allowed to be triggered if there are no threads playing for that target at a higher priority level.

`(Priority)` (priority): Node priority.

Priorities, from least to greatest:
* `None`
* `Low`
* `Medium`
  * (default value if no `@priority` tag is present)
* `High`
* `Cutscene`

#### `@interrupt`
Allows this node to interrupt a currently playing thread at the same priority level.

**NOTE:** This is only valid if a `@who` target is present, or the query has an explicit target.
#### `@weight (Weight)`
Specifies a weighted priority for this node. If multiple nodes with the same score are found for a trigger, this will skew the results of the random selection towards or away from this node.

`(Weight)` (float): Node selection weight.