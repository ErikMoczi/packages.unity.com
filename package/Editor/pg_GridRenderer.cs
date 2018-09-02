using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Object = UnityEngine.Object;

namespace ProGrids.Editor
{
	static class pg_GridRenderer
	{
		const HideFlags k_SceneObjectHideFlags = HideFlags.HideAndDontSave;

		const string k_PreviewObjectName = "ProGridsGridObject";

		const string k_MaterialObjectName = "ProGridsMaterialObject+8b6ad2eb-aa3f-4f18-ae2d-f289932e3e33";
		const string k_MeshObjectName = "ProGridsMeshObject+134da923-cff2-4dda-a804-b927ccac4459";
		const string k_GridShader = "Hidden/ProGrids/pg_GridShader";
		const int k_MaxLines = 256;

		static Mesh s_GridMesh;
		static Material s_GridMaterial;
		static int tan_iter, bit_iter, max = k_MaxLines, div = 1;

		public static int majorLineIncrement = 10;

		/// <summary>
		/// Destroy any existing render objects, then initialize new ones.
		/// </summary>
		public static void Init()
		{
			majorLineIncrement = EditorPrefs.GetInt(pg_PreferenceKeys.MajorLineIncrement, 10);

			if(majorLineIncrement < 2)
				majorLineIncrement = 2;

			if (s_GridMesh == null)
			{
				s_GridMesh = Resources.FindObjectsOfTypeAll<Mesh>().FirstOrDefault(x => x.name.Equals(k_MeshObjectName));

				if (s_GridMesh == null)
				{
					s_GridMesh = new Mesh();
					s_GridMesh.name = k_MeshObjectName;
					s_GridMesh.hideFlags = k_SceneObjectHideFlags;
				}
			}

			s_GridMesh.hideFlags = k_SceneObjectHideFlags;

			if (s_GridMaterial == null)
			{
				s_GridMaterial = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(x => x.name.Equals(k_MaterialObjectName));

				if (s_GridMaterial == null)
				{
					s_GridMaterial = new Material(Shader.Find(k_GridShader));
					s_GridMaterial.name = k_MaterialObjectName;
					s_GridMaterial.hideFlags = k_SceneObjectHideFlags;
				}
			}
		}

		public static void Destroy()
		{
			if (s_GridMesh != null)
				Object.DestroyImmediate(s_GridMesh);

			if(s_GridMaterial != null)
				Object.DestroyImmediate(s_GridMaterial);
		}

		public static void DoGUI(SceneView view)
		{
			if (Event.current.type == EventType.Repaint &&
			    s_GridMesh != null &&
			    s_GridMaterial != null)
			{
				s_GridMaterial.SetPass(0);
				Graphics.DrawMeshNow(s_GridMesh, Matrix4x4.identity);
			}
		}

		/// <summary>
		/// Returns the distance this grid is drawing
		/// </summary>
		/// <param name="cam"></param>
		/// <param name="pivot"></param>
		/// <param name="tangent"></param>
		/// <param name="bitangent"></param>
		/// <param name="snapValue"></param>
		/// <param name="color"></param>
		/// <param name="alphaBump"></param>
		/// <returns></returns>
		public static float DrawPlane(Camera cam, Vector3 pivot, Vector3 tangent, Vector3 bitangent, float snapValue, Color color, float alphaBump)
		{
			if(!s_GridMesh || !s_GridMaterial)
				Init();

			s_GridMaterial.SetFloat("_AlphaCutoff", .1f);
			s_GridMaterial.SetFloat("_AlphaFade", .6f);

			pivot = pg_Util.SnapValue(pivot, snapValue);

			Vector3 p = cam.WorldToViewportPoint(pivot);
			bool inFrustum = (p.x >= 0f && p.x <= 1f) &&
							 (p.y >= 0f && p.y <= 1f) &&
							  p.z >= 0f;

			float[] distances = GetDistanceToFrustumPlanes(cam, pivot, tangent, bitangent, 24f);

			if(inFrustum)
			{
				tan_iter = (int)(Mathf.Ceil( (Mathf.Abs(distances[0]) + Mathf.Abs(distances[2]))/snapValue ));
				bit_iter = (int)(Mathf.Ceil( (Mathf.Abs(distances[1]) + Mathf.Abs(distances[3]))/snapValue ));

				max = Mathf.Max( tan_iter, bit_iter );

				// if the max is around 3x greater than min, we're probably skewing the camera at near-plane
				// angle, so use the min instead.
				if(max > Mathf.Min(tan_iter, bit_iter) * 2)
					max = (int) Mathf.Min(tan_iter, bit_iter) * 2;

				div = 1;

				float dot = Vector3.Dot( cam.transform.position-pivot, Vector3.Cross(tangent, bitangent) );

				if(max > k_MaxLines)
				{
					if(Vector3.Distance(cam.transform.position, pivot) > 50f * snapValue && Mathf.Abs(dot) > .8f)
					{
						while(max/div > k_MaxLines)
							div += div;
					}
					else
					{
						max = k_MaxLines;
					}
				}
			}

			// origin, tan, bitan, increment, iterations, divOffset, color, primary alpha bump
			DrawFullGrid(cam, pivot, tangent, bitangent, snapValue*div, max/div, div, color, alphaBump);

			return ((snapValue*div)*(max/div));
		}

		public static void DrawGridPerspective(Camera cam, Vector3 pivot, float snapValue, Color[] colors, float alphaBump)
		{
			if(!s_GridMesh || !s_GridMaterial)
				Init();

			s_GridMaterial.SetFloat("_AlphaCutoff", 0f);
			s_GridMaterial.SetFloat("_AlphaFade", 0f);

			Vector3 camDir = (pivot - cam.transform.position).normalized;
			pivot = pg_Util.SnapValue(pivot, snapValue);

			// Used to flip the grid to match whatever direction the cam is currently
			// coming at the pivot from
			Vector3 right = camDir.x < 0f ? Vector3.right : Vector3.right * -1f;
			Vector3 up = camDir.y < 0f ? Vector3.up : Vector3.up * -1f;
			Vector3 forward = camDir.z < 0f ? Vector3.forward : Vector3.forward * -1f;

			// Get intersecting point for each axis, if it exists
			Ray ray_x = new Ray(pivot, right);
			Ray ray_y = new Ray(pivot, up);
			Ray ray_z = new Ray(pivot, forward);

			float x_dist = 10f, y_dist = 10f, z_dist = 10f;
			bool x_intersect = false, y_intersect = false, z_intersect = false;

			Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
			foreach(Plane p in planes)
			{
				float dist;
				float t = 0;

				if(p.Raycast(ray_x, out dist))
				{
					t = Vector3.Distance(pivot, ray_x.GetPoint(dist));
					if(t < x_dist || !x_intersect)
					{
						x_intersect = true;
						x_dist = t;
					}
				}

				if(p.Raycast(ray_y, out dist))
				{
					t = Vector3.Distance(pivot, ray_y.GetPoint(dist));
					if(t < y_dist || !y_intersect)
					{
						y_intersect = true;
						y_dist = t;
					}
				}

				if(p.Raycast(ray_z, out dist))
				{
					t = Vector3.Distance(pivot, ray_z.GetPoint(dist));
					if(t < z_dist || !z_intersect)
					{
						z_intersect = true;
						z_dist = t;
					}
				}
			}

			int x_iter = (int)(Mathf.Ceil(Mathf.Max(x_dist, y_dist))/snapValue);
			int y_iter = (int)(Mathf.Ceil(Mathf.Max(x_dist, z_dist))/snapValue);
			int z_iter = (int)(Mathf.Ceil(Mathf.Max(z_dist, y_dist))/snapValue);

			int max = Mathf.Max( Mathf.Max(x_iter, y_iter), z_iter );
			int div = 1;
			while(max/div> k_MaxLines)
			{
				div++;
			}

			Vector3[] vertices_t = null;
			Vector3[] normals_t = null;
			Color[] colors_t = null;
			int[] indices_t = null;

			List<Vector3> vertices_m = new List<Vector3>();
			List<Vector3> normals_m = new List<Vector3>();
			List<Color> colors_m = new List<Color>();
			List<int> indices_m = new List<int>();

			// X plane
			DrawHalfGrid(cam, pivot, up, right, snapValue*div, x_iter/div, colors[0], alphaBump, out vertices_t, out normals_t, out colors_t, out indices_t, 0);
			vertices_m.AddRange(vertices_t);
			normals_m.AddRange(normals_t);
			colors_m.AddRange(colors_t);
			indices_m.AddRange(indices_t);

			// Y plane
			DrawHalfGrid(cam, pivot, right, forward, snapValue*div, y_iter/div, colors[1], alphaBump, out vertices_t, out normals_t, out colors_t, out indices_t, vertices_m.Count);
			vertices_m.AddRange(vertices_t);
			normals_m.AddRange(normals_t);
			colors_m.AddRange(colors_t);
			indices_m.AddRange(indices_t);

			// Z plane
			DrawHalfGrid(cam, pivot, forward, up, snapValue*div, z_iter/div, colors[2], alphaBump, out vertices_t, out normals_t, out colors_t, out indices_t, vertices_m.Count);
			vertices_m.AddRange(vertices_t);
			normals_m.AddRange(normals_t);
			colors_m.AddRange(colors_t);
			indices_m.AddRange(indices_t);

			s_GridMesh.Clear();
			s_GridMesh.vertices = vertices_m.ToArray();
			s_GridMesh.normals = normals_m.ToArray();
			s_GridMesh.subMeshCount = 1;
			s_GridMesh.uv = new Vector2[vertices_m.Count];
			s_GridMesh.colors = colors_m.ToArray();
			s_GridMesh.SetIndices(indices_m.ToArray(), MeshTopology.Lines, 0);

		}

		static void DrawHalfGrid(Camera cam, Vector3 pivot, Vector3 tan, Vector3 bitan, float increment, int iterations, Color secondary, float alphaBump,
			out Vector3[] vertices,
			out Vector3[] normals,
			out Color[] colors,
			out int[] indices, int offset)
		{
			Color primary = secondary;
			primary.a += alphaBump;

			float len = increment * iterations;

			int highlightOffsetTan 		= (int)((pg_Util.ValueFromMask(pivot, tan) % (increment * majorLineIncrement)) / increment);
			int highlightOffsetBitan	= (int)((pg_Util.ValueFromMask(pivot, bitan) % (increment * majorLineIncrement)) / increment);

			iterations++;

			// this could only use 3 verts per line
			float fade = .75f;
			float fadeDist = len * fade;
			Vector3 nrm = Vector3.Cross(tan, bitan);

			vertices = new Vector3[iterations*6-3];
			normals = new Vector3[iterations*6-3];
			indices = new int[iterations*8-4];
			colors = new Color[iterations*6-3];

			vertices[0] = pivot;
			vertices[1] = (pivot + bitan*fadeDist);
			vertices[2] = (pivot + bitan*len);

			normals[0] = nrm;
			normals[1] = nrm;
			normals[2] = nrm;

			indices[0] = 0 + offset;
			indices[1] = 1 + offset;
			indices[2] = 1 + offset;
			indices[3] = 2 + offset;

			colors[0] = primary;
			colors[1] = primary;
			colors[2] = primary;
			colors[2].a = 0f;


			int n = 4;
			int v = 3;

			for(int i = 1; i < iterations; i++)
			{
				// MeshTopology doesn't exist prior to Unity 4
				vertices[v+0] = pivot + i * tan * increment;
				vertices[v+1] = (pivot + bitan*fadeDist) + i * tan * increment;
				vertices[v+2] = (pivot + bitan*len) + i * tan * increment;

				vertices[v+3] = pivot + i * bitan * increment;
				vertices[v+4] = (pivot + tan*fadeDist) + i * bitan * increment;
				vertices[v+5] = (pivot + tan*len) + i * bitan * increment;

				normals[v+0] = nrm;
				normals[v+1] = nrm;
				normals[v+2] = nrm;
				normals[v+3] = nrm;
				normals[v+4] = nrm;
				normals[v+5] = nrm;

				indices[n+0] = v + 0 + offset;
				indices[n+1] = v + 1 + offset;
				indices[n+2] = v + 1 + offset;
				indices[n+3] = v + 2 + offset;
				indices[n+4] = v + 3 + offset;
				indices[n+5] = v + 4 + offset;
				indices[n+6] = v + 4 + offset;
				indices[n+7] = v + 5 + offset;

				float alpha = (i/(float)iterations);
				alpha = alpha < fade ? 1f : 1f - ( (alpha-fade)/(1-fade) );

				Color col = (i+highlightOffsetTan) % majorLineIncrement == 0 ? primary : secondary;
				col.a *= alpha;

				colors[v+0] = col;
				colors[v+1] = col;
				colors[v+2] = col;
				colors[v+2].a = 0f;

				col = (i+highlightOffsetBitan) % majorLineIncrement == 0 ? primary : secondary;
				col.a *= alpha;

				colors[v+3] = col;
				colors[v+4] = col;
				colors[v+5] = col;
				colors[v+5].a = 0f;

				n += 8;
				v += 6;
			}
		}

		/// <summary>
		/// Draws a plane grid using pivot point, the right and forward directions, and how far each direction should extend
		/// </summary>
		/// <param name="cam"></param>
		/// <param name="pivot"></param>
		/// <param name="tan"></param>
		/// <param name="bitan"></param>
		/// <param name="increment"></param>
		/// <param name="iterations"></param>
		/// <param name="div"></param>
		/// <param name="secondary"></param>
		/// <param name="alphaBump"></param>
		static void DrawFullGrid(Camera cam, Vector3 pivot, Vector3 tan, Vector3 bitan, float increment, int iterations, int div, Color secondary, float alphaBump)
		{
			Color primary = secondary;
			primary.a += alphaBump;

			float len = iterations * increment;

			iterations++;

			Vector3 start = pivot - tan*(len/2f) - bitan*(len/2f);
			start = pg_Util.SnapValue(start, bitan+tan, increment);

			float inc = increment;
			int highlightOffsetTan = (int)((pg_Util.ValueFromMask(start, tan) % (inc*majorLineIncrement)) / inc);
			int highlightOffsetBitan = (int)((pg_Util.ValueFromMask(start, bitan) % (inc*majorLineIncrement)) / inc);

			Vector3[] lines = new Vector3[iterations * 4];
			int[] indices = new int[iterations * 4];
			Color[] colors = new Color[iterations * 4];

			int v = 0, t = 0;

			for(int i = 0; i < iterations; i++)
			{
				Vector3 a = start + tan * i * increment;
				Vector3 b = start + bitan * i * increment;

				lines[v+0] = a;
				lines[v+1] = a + bitan * len;

				lines[v+2] = b;
				lines[v+3] = b + tan * len;

				indices[t++] = v;
				indices[t++] = v+1;
				indices[t++] = v+2;
				indices[t++] = v+3;

				Color col = (i + highlightOffsetTan) % majorLineIncrement == 0 ? primary : secondary;

				// tan
				colors[v+0] = col;
				colors[v+1] = col;

				col = (i + highlightOffsetBitan) % majorLineIncrement == 0 ? primary : secondary;

				// bitan
				colors[v+2] = col;
				colors[v+3] = col;

				v += 4;
			}

			Vector3 nrm = Vector3.Cross(tan, bitan);
			Vector3[] nrms = new Vector3[lines.Length];
			for(int i = 0; i < lines.Length; i++)
				nrms[i] = nrm;


			s_GridMesh.Clear();
			s_GridMesh.vertices = lines;
			s_GridMesh.normals = nrms;
			s_GridMesh.subMeshCount = 1;
			s_GridMesh.uv = new Vector2[lines.Length];
			s_GridMesh.colors = colors;
			s_GridMesh.SetIndices(indices, MeshTopology.Lines, 0);
		}

		/// <summary>
		/// Returns the distance from pivot to frustum plane in the order of float[] { tan, bitan, -tan, -bitan }
		/// </summary>
		/// <param name="cam"></param>
		/// <param name="pivot"></param>
		/// <param name="tan"></param>
		/// <param name="bitan"></param>
		/// <param name="minDist"></param>
		/// <returns></returns>
		static float[] GetDistanceToFrustumPlanes(Camera cam, Vector3 pivot, Vector3 tan, Vector3 bitan, float minDist)
		{
			Ray[] rays = new Ray[4]
			{
				new Ray(pivot, tan),
				new Ray(pivot, bitan),
				new Ray(pivot, -tan),
				new Ray(pivot, -bitan)
			 };

			float[] intersects = new float[4] { minDist, minDist, minDist, minDist };
			bool[] intersection_found = new bool[4] { false, false, false, false };

			Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
			foreach(Plane p in planes)
			{
				float dist;
				float t = 0;

				for(int i = 0; i < 4; i++)
				{
					if(p.Raycast(rays[i], out dist))
					{
						t = Vector3.Distance(pivot, rays[i].GetPoint(dist));

						if(t < intersects[i] || !intersection_found[i])
						{
							intersection_found[i] = true;
							intersects[i] = Mathf.Max(minDist, t);
						}
					}
				}
			}
			return intersects;
		}
	}
}
