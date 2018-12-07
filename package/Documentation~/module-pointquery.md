# PointQuery Module

The PointQuery module provides a convenience support service for certain applications. It is not essential and should only be included in applications that use point queries. 

It provides a service to create an acceleration data structure to efficiently query the n-closest points from a large number of points in 3D.

It is also an example Module for future, more data processing oriented, modules and unlikely to be needed by most 2D games or ads.

The point query module does not contain any systems. Everything is done via the static PointQueryService. 

Using it is a two step process: 
1. Create a PointQuery acceleration structure and fill it with points once. Typically the number of points added here is very large (hundreds of thousands). 

2. Many times query the structure for the n-closest points in the structure around a changing query point. 


For (1) use **createPointQueryStruct** to initialize the structure and assign points using **addPointsToQueryStruct**. When done adding points, use **buildPointQueryStruct** to accelerate future queries. **buildPointQueryStruct** is also called automatically when the first query is run.

For (2) use **queryClosestPoint** or **queryNClosestPoints** repeatedly.

An example use case would be implementing flocking behavior for a flock of 10000 birds: In (1) All the  birds in the swarm are added to a new query structure. In (2) every bird can now efficiently search for its 10 closest neighbours and adjust speed and heading accordingly.

(See this module's API documentation for more information)