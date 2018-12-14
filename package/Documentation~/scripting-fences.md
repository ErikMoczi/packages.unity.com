# Fences

Fences are basically empty nodes in the system execution graph: they perform no logic, but you can use them to schedule systems without knowing every other system in your game. Currently, all fences live under **ut.Shared**.

Here's the current fence scheduling order:

1.  InputFence
1.  UserCodeStart
1.  UserCodeEnd
1.  RenderingFence
1.  PlatformRenderingFence

<!-- TO DO : (more detail required here) -->