#pragma once

typedef struct UnityXRNativeSession_1
{
    int version;
    void* sessionPtr;
} UnityXRNativeSession_1;

typedef struct UnityXRNativeCamera_1
{
    int version;
    void* framePtr;
} UnityXRNativeCamera_1;

typedef struct UnityXRNativePlane_1
{
    int version;
    void* planePtr;
} UnityXRNativePlane_1;

typedef struct UnityXRNativeReferencePoint_1
{
    int version;
    void* referencePointPtr;
} UnityXRNativeReferencePoint_1;

static const int kUnityXRNativeSessionVersion = 1;
static const int kUnityXRNativeCameraVersion = 1;
static const int kUnityXRNativePlaneVersion = 1;
static const int kUnityXRNativeReferencePointVersion = 1;

typedef UnityXRNativeSession_1 UnityXRNativeSession;
typedef UnityXRNativeCamera_1 UnityXRNativeCamera;
typedef UnityXRNativePlane_1 UnityXRNativePlane;
typedef UnityXRNativeReferencePoint_1 UnityXRNativeReferencePoint;
