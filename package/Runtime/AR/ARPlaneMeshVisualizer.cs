using System;
using System.Collections.Generic;
using UnityEngine.Experimental.XR;

namespace UnityEngine.XR.ARFoundation
{
    /// <summary>
    /// Generates a mesh for an <see cref="ARPlane"/>.
    /// </summary>
    /// <remarks>
    /// If this <c>GameObject</c> has a <c>MeshFilter</c> and/or <c>MeshCollider</c>,
    /// this component will generate a mesh from the underlying <c>BoundedPlane</c>.
    /// 
    /// It will also update a <c>LineRenderer</c> with the boundary points, if present.
    /// </remarks>
    [RequireComponent(typeof(ARPlane))]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@1.0/api/UnityEngine.XR.ARFoundation.ARPlaneMeshVisualizer.html")]
    public sealed class ARPlaneMeshVisualizer : MonoBehaviour
    {
        /// <summary>
        /// Get the <c>Mesh</c> that this visualizer creates and manages.
        /// </summary>
        public Mesh mesh { get; private set; }

        /// <summary>
        /// Generates a <c>Mesh</c> from the given parameters. The <paramref name="convexPolygon"/> is assumed to be convex.
        /// </summary>
        /// <remarks>
        /// <paramref name="convexPolygon"/> is not checked for its convexness. Concave polygons will produce incorrect results.
        /// </remarks>
        /// <param name="mesh">The <c>Mesh</c> to write results to.</param>
        /// <param name="center">The plane's center, in plane-space.</param>
        /// <param name="normal">The plane's normal, in plane-space.</param>
        /// <param name="convexPolygon">The vertices of the plane's boundary, in plane-space.</param>
        /// <param name="areaTolerance">If any triangle in the generated mesh is less than this, then the entire mesh is ignored.
        /// This handles an edge case which prevents degenerate or very small triangles. Units are meters-squared.</param>
        public static void GenerateMesh(Mesh mesh, Vector3 center, Vector3 normal, List<Vector3> convexPolygon, float areaTolerance = 1e-6f)
        {
            if (mesh == null)
                throw new ArgumentNullException("mesh");

            if (convexPolygon == null)
                throw new ArgumentNullException("convexPolygon");

            mesh.Clear();

            if (convexPolygon.Count < 3)
                return;

            var areaToleranceSquared = areaTolerance * areaTolerance;

            convexPolygon.Add(center);
            mesh.SetVertices(convexPolygon);

            // Create a triangle fan
            var indices = s_Indices;
            indices.Clear();

            var numBoundaryVertices = convexPolygon.Count - 1;
            var centerIndex = numBoundaryVertices;

            for (int i = 0; i < numBoundaryVertices; ++i)
            {
                int j = (i + 1) % numBoundaryVertices;

                // Stop if the area of the triangle is too small
                var a = convexPolygon[i] - convexPolygon[centerIndex];
                var b = convexPolygon[j] - convexPolygon[centerIndex];
                var areaSquared = Vector3.Cross(a, b).sqrMagnitude * 0.25f;
                if (areaSquared < areaToleranceSquared)
                {
                    mesh.Clear();
                    return;
                }

                indices.Add(centerIndex);
                indices.Add(i);
                indices.Add(j);
            }

            // Reuse the same list for normals
            var normals = convexPolygon;
            for (int i = 0; i < normals.Count; ++i)
                normals[i] = normal;

            mesh.SetNormals(normals);

            const int subMesh = 0;
            const bool calculateBounds = true;
            mesh.SetTriangles(indices, subMesh, calculateBounds);
        }

        void OnBoundaryChanged(ARPlaneBoundaryChangedEventArgs eventArgs)
        {
            // Ignore subsumed planes
            if (m_Plane.boundedPlane.SubsumedById != TrackableId.InvalidId)
            {
                DisableComponents();
                return;
            }

            var center = eventArgs.center;
            var normal = eventArgs.normal;
            var polygon = eventArgs.convexBoundary;

            var lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = polygon.Count;
                for (int i = 0; i < polygon.Count; ++i)
                {
                    lineRenderer.SetPosition(i, polygon[i]);
                }
            }

            GenerateMesh(mesh, center, normal, polygon);

            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null)
                meshFilter.sharedMesh = mesh;

            var meshCollider = GetComponent<MeshCollider>();
            if (meshCollider != null)
                meshCollider.sharedMesh = mesh;
        }

        void DisableComponents()
        {
            enabled = false;

            var meshCollider = GetComponent<MeshCollider>();
            if (meshCollider != null)
                meshCollider.enabled = false;

            UpdateVisibility();
        }

        void SetVisible(bool visible)
        {
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
                meshRenderer.enabled = visible;

            var lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer != null)
                lineRenderer.enabled = visible;
        }

        void UpdateVisibility()
        {
            var visible = enabled &&
                (m_Plane.trackingState != TrackingState.Unavailable) &&
                (ARSubsystemManager.systemState > ARSystemState.Ready);

            SetVisible(visible);
        }

        void OnUpdated(ARPlane plane)
        {
            UpdateVisibility();
        }

        void OnSystemStateChanged(ARSystemStateChangedEventArgs eventArgs)
        {
            UpdateVisibility();
        }

        void Awake()
        {
            mesh = new Mesh();
            m_Plane = GetComponent<ARPlane>();
        }

        void OnEnable()
        {
            m_Plane.boundaryChanged += OnBoundaryChanged;
            m_Plane.updated += OnUpdated;
            ARSubsystemManager.systemStateChanged += OnSystemStateChanged;
            UpdateVisibility();
        }

        void OnDisable()
        {
            m_Plane.boundaryChanged -= OnBoundaryChanged;
            m_Plane.updated -= OnUpdated;
            ARSubsystemManager.systemStateChanged -= OnSystemStateChanged;
        }

        ARPlane m_Plane;

        static List<int> s_Indices = new List<int>();
    }
}
