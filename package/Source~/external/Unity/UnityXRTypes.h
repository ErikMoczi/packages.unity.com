#pragma once

/// The maximum length of a string, used in some structs with the XR headers
const unsigned int kUnityXRStringSize = 128;

/// Simple 2-Element Float Vector
struct UnityXRVector2
{
    float x; ///< The x coordinate
    float y; ///< The y coordinate
};

/// Simple 3-Element float vector
struct UnityXRVector3
{
    float x; ///< The x coordinate
    float y; ///< The y coordinate
    float z; ///< The z coordinate
};

/// Simple 4 Element Quaternion with indices ordered x, y, z, and w in order
struct UnityXRVector4
{
    float x; ///< The x coordinate
    float y; ///< The y coordinate
    float z; ///< The z coordinate
    float w; ///< The w coordinate
};

/// A simple struct representing a point in space with position and orientation
struct UnityXRPose
{
    UnityXRVector3 position; ///< The position of the pose
    UnityXRVector4 rotation; ///< The rotation, stored as a quaternion
};

/// A 4x4 column-major matrix
struct UnityXRMatrix4x4
{
    UnityXRVector4 columns[4]; ///< The columns of the matrix
};
