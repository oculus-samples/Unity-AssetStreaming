// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.IO;
using System;
using System.Linq;
using Unity.Jobs;
using Unity.Collections;

public struct Polygon
{
    public Vector3 v0;
    public Vector3 v1;
    public Vector3 v2;
    public Vector2 t0;
    public Vector2 t1;
    public Vector2 t2;
    public Vector3 n0;
    public Vector3 n1;
    public Vector3 n2;
}

public struct PolygonLightmapUV
{
    public Vector2 v0;
    public Vector2 v1;
    public Vector2 v2;
}

// Copies the lightmap UVs from the lightmap mesh to the LOD meshes.
// Looks for identical vertex then copies the lightmap UVs.
public struct CopyLightmapUVsJob : IJob
{
    [ReadOnly]
    public NativeHashMap<Hash128, PolygonLightmapUV> polygonLightmapUVs; // Holds lightmap UVs of every triangle.
    [ReadOnly]
    public NativeArray<Vector3> vertices;
    [ReadOnly]
    public NativeArray<Vector2> texcoords;
    [ReadOnly]
    public NativeArray<Vector3> normals;
    public NativeList<int> triangles;
    public NativeList<Vector3> newVertices;
    public NativeList<Vector2> newTexcoords;
    public NativeList<Vector3> newNormals;
    public NativeList<Vector2> newLightmapUVs;

    public void Execute()
    {
        // Returns the index of a vertex
        Func<NativeList<Vector3>, NativeList<Vector2>, NativeList<Vector3>, NativeList<Vector2>, Vector3, Vector2, Vector3, Vector2, int> FindVertex = 
            (NativeList<Vector3> newVertices, NativeList<Vector2> newTexcoords, NativeList<Vector3> newNormals, NativeList<Vector2> newLightmapUVs, Vector3 vertex, Vector2 uv, Vector3 normal, Vector2 lightmap) =>
        {
            for (int i = 0; i < newVertices.Length; ++i)
            {
                if (vertex == newVertices[i] && uv == newTexcoords[i] && normal == newNormals[i] && lightmap == newLightmapUVs[i])
                {
                    return i;
                }
            }

            return -1;
        };

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i0 = triangles[i];
            int i1 = triangles[i + 1];
            int i2 = triangles[i + 2];

            Polygon p = new Polygon();
            p.v0 = vertices[i0];
            p.v1 = vertices[i1];
            p.v2 = vertices[i2];
            p.t0 = texcoords[i0];
            p.t1 = texcoords[i1];
            p.t2 = texcoords[i2];
            p.n0 = normals[i0];
            p.n1 = normals[i1];
            p.n2 = normals[i2];

            // Find lightmap UVs of matching triangle
            Hash128 hash = new Hash128();
            HashUtilities.ComputeHash128(ref p, ref hash);
            PolygonLightmapUV uvs = new PolygonLightmapUV();
            if (!polygonLightmapUVs.TryGetValue(hash, out uvs))
            {
                Debug.LogError("Vertex not found");
                continue;
            }

            // Get the index of vertex 0
            i0 = FindVertex(newVertices, newTexcoords, newNormals, newLightmapUVs, p.v0, p.t0, p.n0, uvs.v0);
            if (i0 == -1)
            {
                // Add vertex with correct lightmap UVs
                i0 = newVertices.Length;
                newVertices.Add(p.v0);
                newTexcoords.Add(p.t0);
                newNormals.Add(p.n0);
                newLightmapUVs.Add(uvs.v0);
            }
            triangles[i] = i0;

            // Get the index of vertex 1
            i1 = FindVertex(newVertices, newTexcoords, newNormals, newLightmapUVs, p.v1, p.t1, p.n1, uvs.v1);
            if (i1 == -1)
            {
                // Add vertex with correct lightmap UVs
                i1 = newVertices.Length;
                newVertices.Add(p.v1);
                newTexcoords.Add(p.t1);
                newNormals.Add(p.n1);
                newLightmapUVs.Add(uvs.v1);
            }
            triangles[i + 1] = i1;

            // Get the index of vertex 2
            i2 = FindVertex(newVertices, newTexcoords, newNormals, newLightmapUVs, p.v2, p.t2, p.n2, uvs.v2);
            if (i2 == -1)
            {
                // Add vertex with correct lightmap UVs
                i2 = newVertices.Length;
                newVertices.Add(p.v2);
                newTexcoords.Add(p.t2);
                newNormals.Add(p.n2);
                newLightmapUVs.Add(uvs.v2);
            }
            triangles[i + 2] = i2;
        }
    }
}

// Edge for edge collapse algorithm
struct Edge : IComparable
{
    public int i0; // vertex index
    public int i1;
    public float length; // length of edge

    public Edge(int i0, int i1, float length)
    {
        this.i0 = i0;
        this.i1 = i1;
        this.length = length;
    }

    public int CompareTo(object obj)
    {
        // Sort edges by length
        Edge other = (Edge)obj;
        if ((i0 == other.i0 || i0 == other.i1) &&
            (i1 == other.i0 || i1 == other.i1))
            return 0;

        int lComp = length.CompareTo(other.length);
        if (lComp != 0)
            return lComp;

        int i0Comp = i0.CompareTo(other.i0);
        if (i0Comp != 0)
            return i0Comp;

        return i1.CompareTo(other.i1);
    }
}

public struct DecimateMeshJob : IJob
{
    public NativeArray<Vector3> vertices;
    public NativeList<int> triangles;
    public float decimation; // percentage of triangles of original mesh to target

    public void Execute()
    {
        // Calculate at which amount of triangles we're allowed to stop decimating
        int finalTriangleCount = (int)(triangles.Length * decimation * 0.01f);
        finalTriangleCount -= finalTriangleCount % 3;

        // Collect all edges
        SortedSet<Edge> edges = new SortedSet<Edge>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i0 = triangles[i];
            int i1 = triangles[i + 1];
            int i2 = triangles[i + 2];

            Vector3 v0 = vertices[i0];
            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];

            float m0 = (v0 - v1).magnitude;
            float m1 = (v1 - v2).magnitude;
            float m2 = (v2 - v0).magnitude;

            edges.Add(new Edge(i0, i1, m0));
            edges.Add(new Edge(i1, i2, m1));
            edges.Add(new Edge(i2, i0, m2));
        }

        int connectivityCheck = 0;
        int geometryCheck = 0;
        int interiorCheck = 0;
        
        // Decimate until final triangle count has been reached
        while (triangles.Length > finalTriangleCount)
        {
            int triangleCount = triangles.Length;

            // It's possible no edges can be collapsed due to the shape and connectivity constraints.
            // When noEdgesToDecimate is true we stop decimating.
            bool noEdgesToDecimate = true;

            // Go over all edges
            for (int e = 0; e < edges.Count; ++e)
            {
                Edge edge = edges.ElementAt(e);

                Func<NativeList<int>, int, List<int>> GetConnectedTriangles = (NativeList<int> triangles, int i) =>
                {
                    // Look for triangles which contain vertex index i
                    List<int> connectedTriangles = new List<int>();

                    for (int j = 0; j < triangles.Length; j += 3)
                    {
                        int i0 = triangles[j];
                        int i1 = triangles[j + 1];
                        int i2 = triangles[j + 2];

                        if (i0 == i || i1 == i || i2 == i)
                        {
                            connectedTriangles.Add(j);
                        }
                    }

                    return connectedTriangles;
                };

                // Get a list of all triangles connected to either vertex of this edge
                List<int> connectedTriangles0 = GetConnectedTriangles(triangles, edge.i0);
                List<int> connectedTriangles1 = GetConnectedTriangles(triangles, edge.i1);

                // Find all vertices both edge vertices are connected to
                HashSet<int> sharedNeighbours = new HashSet<int>();
                for (int i = 0; i < connectedTriangles0.Count; ++i)
                {
                    int[] t0 = new int[3];
                    t0[0] = triangles[connectedTriangles0[i]];
                    t0[1] = triangles[connectedTriangles0[i] + 1];
                    t0[2] = triangles[connectedTriangles0[i] + 2];

                    for (int j = 0; j < connectedTriangles1.Count; ++j)
                    {
                        int[] t1 = new int[3];
                        t1[0] = triangles[connectedTriangles1[j]];
                        t1[1] = triangles[connectedTriangles1[j] + 1];
                        t1[2] = triangles[connectedTriangles1[j] + 2];

                        for (int x = 0; x < 3; ++x)
                        {
                            if (t0[x] == edge.i0)
                                continue;

                            for (int y = 0; y < 3; ++y)
                            {
                                if (t1[y] == edge.i1)
                                    continue;

                                if (t0[x] == t1[y])
                                    sharedNeighbours.Add(t0[x]);
                            }
                        }
                    }
                }

                if (sharedNeighbours.Count != 2)
                {
                    ++connectivityCheck;
                    edges.Remove(edge);
                    --e;

                    // Edge can't be collapsed because this would create non manifold triangles.
                    // Continue to next shortest edge.
                    continue;
                }

                Func<NativeList<int>, NativeArray<Vector3>, List<int>, bool> CheckGeometryValidity = (NativeList<int> triangles, NativeArray<Vector3> vertices, List<int> uniqueTriangles) =>
                {
                    // Check if rotation of triangle changes if edge it collapsed
                    for (int i = 0; i < uniqueTriangles.Count; ++i)
                    {
                        int i0 = triangles[uniqueTriangles[i]];
                        int i1 = triangles[uniqueTriangles[i] + 1];
                        int i2 = triangles[uniqueTriangles[i] + 2];

                        Vector3 v0 = vertices[i0];
                        Vector3 v1 = vertices[i1];
                        Vector3 v2 = vertices[i2];

                        // Create planes from triangles.
                        // The triangle is flipped if the plane's normal points in the opposite direction.
                        Plane beforePlane;
                        if (!GeometryUtility.TryCreatePlaneFromPolygon(new Vector3[] { v0, v1, v2 }, out beforePlane))
                        {
                            return false;
                        }

                        if (i0 == edge.i1)
                            v0 = vertices[edge.i0];
                        else if (i1 == edge.i1)
                            v1 = vertices[edge.i0];
                        else if (i2 == edge.i1)
                            v2 = vertices[edge.i0];

                        Plane afterPlane;
                        if (!GeometryUtility.TryCreatePlaneFromPolygon(new Vector3[] { v0, v1, v2 }, out afterPlane))
                        {
                            return false;
                        }

                        if (Vector3.Dot(beforePlane.normal, afterPlane.normal) <= 0.0f)
                        {
                            return false;
                        }
                    }

                    return true;
                };

                List<int> sharedTriangles = connectedTriangles0.Intersect(connectedTriangles1).ToList();
                List<int> i0UniqueTriangles = new List<int>(connectedTriangles0);
                i0UniqueTriangles.RemoveAll(x => sharedTriangles.Contains(x));
                List<int> i1UniqueTriangles = new List<int>(connectedTriangles1);
                i1UniqueTriangles.RemoveAll(x => sharedTriangles.Contains(x));

                // Check if any of the connected triangles flip if edge is collapsed.
                bool i0GeometryValid = CheckGeometryValidity(triangles, vertices, i0UniqueTriangles);
                bool i1GeometryValid = CheckGeometryValidity(triangles, vertices, i1UniqueTriangles);

                if (!i0GeometryValid && !i1GeometryValid)
                {
                    ++geometryCheck;
                    edges.Remove(edge);
                    --e;

                    // Edge can't be collapsed because triangles would get flipped.
                    // Continue to next shortest edge.
                    continue;
                }

                Func<NativeList<int>, List<int>, bool> IsInteriorVertex = (NativeList<int> triangles, List<int> connectedTriangles) =>
                {
                    // Check if vertex is surrounded by other vertex to create a closed fan.
                    // If not the vertex can't be moved without potentially creating a hole in the original mesh.
                    HashSet<int> uniqueSurroundingVertices = new HashSet<int>();
                    for (int i = 0; i < connectedTriangles.Count; ++i)
                    {
                        int[] t = new int[3];
                        t[0] = triangles[connectedTriangles[i]];
                        t[1] = triangles[connectedTriangles[i] + 1];
                        t[2] = triangles[connectedTriangles[i] + 2];

                        for (int x = 0; x < 3; ++x)
                        {
                            uniqueSurroundingVertices.Add(t[x]);
                        }
                    }

                    return uniqueSurroundingVertices.Count == (connectedTriangles.Count + 1);
                };

                // Check if vertices are interior vertices
                bool i0IsInteriorVertex = i0GeometryValid ? IsInteriorVertex(triangles, connectedTriangles0) : false;
                bool i1IsInteriorVertex = i1GeometryValid ? IsInteriorVertex(triangles, connectedTriangles1) : false;

                if (!i0IsInteriorVertex && !i1IsInteriorVertex)
                {
                    ++interiorCheck;
                    edges.Remove(edge);
                    --e;

                    // Edge can't be collapsed because it's not an interior vertex.
                    // Continue to next shortest edge.
                    continue;
                }

                // Collapse the edge by removing 1 of the vertices and changing all surrounding triangles to use the other vertex.
                List<int> validUniqueTriangles = i0IsInteriorVertex ? i0UniqueTriangles : i1UniqueTriangles;
                int vertexToRemove = i0IsInteriorVertex ? edge.i0 : edge.i1;
                int moveToVertex = i0IsInteriorVertex ? edge.i1 : edge.i0;

                // Remove edges which contain the vertex that's been removed.
                edges.RemoveWhere(x => x.i0 == vertexToRemove || x.i1 == vertexToRemove);

                // Add new edges for the new triangles
                for (int i = 0; i < validUniqueTriangles.Count; ++i)
                {
                    int i0 = triangles[validUniqueTriangles[i]];
                    int i1 = triangles[validUniqueTriangles[i] + 1];
                    int i2 = triangles[validUniqueTriangles[i] + 2];

                    if (i0 == vertexToRemove)
                    {
                        i0 = moveToVertex;
                        triangles[validUniqueTriangles[i]] = i0;
                    }
                    else if (i1 == vertexToRemove)
                    {
                        i1 = moveToVertex;
                        triangles[validUniqueTriangles[i] + 1] = i1;
                    }
                    else if (i2 == vertexToRemove)
                    {
                        i2 = moveToVertex;
                        triangles[validUniqueTriangles[i] + 2] = i2;
                    }

                    Vector3 v0 = vertices[i0];
                    Vector3 v1 = vertices[i1];
                    Vector3 v2 = vertices[i2];

                    float m0 = (v0 - v1).magnitude;
                    float m1 = (v1 - v2).magnitude;
                    float m2 = (v2 - v0).magnitude;

                    edges.Add(new Edge(i0, i1, m0));
                    edges.Add(new Edge(i1, i2, m1));
                    edges.Add(new Edge(i2, i0, m2));
                }

                // Remove the triangles from the mesh
                for (int i = 0; i < sharedTriangles.Count; ++i)
                {
                    triangles.RemoveRange(sharedTriangles[i] - i * 3, sharedTriangles[i] - i * 3 + 3);
                }

                noEdgesToDecimate = false;
                break;
            }

            if (noEdgesToDecimate)
                break;
        }
    }
}

// CODE REMOVED
// This class replaces all classes which have been removed.
public class ObjectRemoved
{}

[CustomEditor(typeof(LODGenerator))]
public class LODGeneratorEditor : Editor
{
    private LODGenerator lodGenerator = null;
    private NativeHashMap<Hash128, PolygonLightmapUV> polygonLightmapUVs;

    public override void OnInspectorGUI()
    {
        lodGenerator = (LODGenerator)target;

        // LOD generator inspector
        GUILayout.Label("This component is not functional! Parts of the code have been removed.");
        lodGenerator.cellSize = EditorGUILayout.Vector2Field("Cell Size", lodGenerator.cellSize);
        lodGenerator.lowestPointTerrain = EditorGUILayout.FloatField("Lowest Point Terrain", lodGenerator.lowestPointTerrain);
        lodGenerator.lodLevels = EditorGUILayout.IntField("LOD Levels", lodGenerator.lodLevels);

        // When the LOD levels change, adjust the per lod parameter arrays
        if(lodGenerator.lodMinObjectRadius.Length > lodGenerator.lodLevels || lodGenerator.lodDecimationPercentage.Length > lodGenerator.lodLevels)
        {
            float[] values = new float[lodGenerator.lodLevels];
            for (int i = 0; i < lodGenerator.lodLevels; ++i)
                values[i] = lodGenerator.lodMinObjectRadius[i];
            lodGenerator.lodMinObjectRadius = values;

            values = new float[lodGenerator.lodLevels];
            for (int i = 0; i < lodGenerator.lodLevels; ++i)
                values[i] = lodGenerator.lodDecimationPercentage[i];
            lodGenerator.lodDecimationPercentage = values;
        }
        else if(lodGenerator.lodMinObjectRadius.Length < lodGenerator.lodLevels)
        {
            float[] values = new float[lodGenerator.lodLevels];
            for (int i = 0; i < lodGenerator.lodMinObjectRadius.Length; ++i)
                values[i] = lodGenerator.lodMinObjectRadius[i];
            for (int i = lodGenerator.lodMinObjectRadius.Length; i < lodGenerator.lodLevels; ++i)
                values[i] = lodGenerator.lodMinObjectRadius[lodGenerator.lodMinObjectRadius.Length - 1];
            lodGenerator.lodMinObjectRadius = values;

            values = new float[lodGenerator.lodLevels];
            for (int i = 0; i < lodGenerator.lodDecimationPercentage.Length; ++i)
                values[i] = lodGenerator.lodDecimationPercentage[i];
            for (int i = lodGenerator.lodDecimationPercentage.Length; i < lodGenerator.lodLevels; ++i)
                values[i] = lodGenerator.lodDecimationPercentage[lodGenerator.lodDecimationPercentage.Length - 1];
            lodGenerator.lodDecimationPercentage = values;
        }

        ++EditorGUI.indentLevel;
        GUILayout.Label("LOD Parameters");
        for (int i = 0; i < lodGenerator.lodLevels; ++i)
        {
            lodGenerator.lodMinObjectRadius[i] = EditorGUILayout.FloatField(i.ToString() + ": Minimum Object Radius", lodGenerator.lodMinObjectRadius[i]);
            lodGenerator.lodDecimationPercentage[i] = EditorGUILayout.Slider(i.ToString() + ": Decimation Percentage", lodGenerator.lodDecimationPercentage[i], 10.0f, 100.0f);
        }
        --EditorGUI.indentLevel;

        SerializedObject serializedObject = new SerializedObject(lodGenerator);
        SerializedProperty additionalObjectsProperty = serializedObject.FindProperty("additionalObjects");
        serializedObject.Update();
        EditorGUILayout.PropertyField(additionalObjectsProperty, true);
        serializedObject.ApplyModifiedProperties();

        if (GUILayout.Button("Generate LODs"))
            GenerateLODs();
    }

    private void GenerateLODs()
    {
        Debug.Log("Starting LOD generation");

        // Destroy LOD generator object if present in scene.
        // We'll create a new one.
        GameObject go = GameObject.Find("LODGenerator");
        if (go != null)
            DestroyImmediate(go);
        go = GameObject.Find("LODManager");
        if (go != null)
            DestroyImmediate(go);

        // Get the output path for the generated LODs
        Scene scene = SceneManager.GetActiveScene();
        string outputPath = Path.Combine(Path.GetDirectoryName(scene.path), scene.name, "prefabs");

        // Create LOD generator object. All objects used to generate the LODs will be children on this object.
        go = new GameObject("LODGenerator");

        // Create lightmap object
        GameObject lightmapObject = new GameObject("Lightmap");
        lightmapObject.transform.parent = go.transform;

        // CODE REMOVED
        ObjectRemoved lightmapMeshBaker = new ObjectRemoved();
        // Create 1 big mesh from all meshes in the scene. This is the lightmap mesh.
        // It's used to generate the lightmap. All LOD meshes will copy their lightmap LODs from this mesh.
        // CODE_REMOVED

        // Remove triangles under the terrain to create more space in the lightmap
        RemoveTrianglesUnderTerrain(lightmapMeshBaker);
        // Unwrap mesh to get lightmap UVs
        GenerateLightmapUVs(lightmapMeshBaker);

        // Bake the lightmap
        LightmapParameters lightmapParameters;
        GenerateLightmap(lightmapMeshBaker, out lightmapParameters);
        if (lightmapParameters == null)
            throw new Exception("Failed to bake lightmap.");

        // Create lightmap objects
        GameObject[] lodObjects = new GameObject[lodGenerator.lodLevels];

        // Create LOD manager
        GameObject lodManagerObj = new GameObject("LODManager");
        LODManager lodManager = lodManagerObj.AddComponent<LODManager>();
        List<List<LODTreeNode>> lodNodes = new List<List<LODTreeNode>>(new List<LODTreeNode>[lodGenerator.lodLevels]);

        // Generate LOD meshes for each LOD level
        for (int i = 0; i < lodGenerator.lodLevels; ++i)
        {
            Debug.LogFormat("Starting LOD {0} generation", i);
            lodObjects[i] = new GameObject("LOD" + i.ToString());
            lodObjects[i].transform.parent = go.transform;

            // CODE REMOVED
            // Create a list of meshes that should be used in this LOD level. to remove small meshes you can use the following code:
            // // Remove meshes smaller than the lodMinObjectRadius
            // for (int j = 0; j < meshes.Count; ++j)
            // {
            //     Renderer renderer = meshes[j].GetComponent<Renderer>();
            //     if (renderer.bounds.size.sqrMagnitude < lodGenerator.lodMinObjectRadius[i] * lodGenerator.lodMinObjectRadius[i])
            //     {
            //         lodTextureBakers[i].objsToMesh.RemoveAt(j);
            //         --j;
            //     }
            // }
            // Then split into grid cells based on lodGenerator.cellSize. Multiply the cell size by Mathf.Pow(2, i) for every lod level.
            // Then merge all meshes in the same cell.
            // CODE REMOVED

            // Start jobs to copy lightmap UVs from the lightmap mesh to the LOD meshes
            ObjectRemoved[] meshBakers = new ObjectRemoved[1];
            List<CopyLightmapUVsJob> copyLightmapUVsJobs = new List<CopyLightmapUVsJob>();
            List<JobHandle> copyLightmapUVsHandles = new List<JobHandle>();
            for (int j = 0; j < meshBakers.Length; ++j)
            {
                RemoveTrianglesUnderTerrain(meshBakers[j]);
                GameObject prefab = (GameObject)PrefabUtility.InstantiatePrefab(new UnityEngine.Object()); // CODE REMOVED: Create an instance of the generate LOD mesh here so it can be modified.
                if (prefab != null)
                {
                    MeshFilter[] mfs = prefab.GetComponentsInChildren<MeshFilter>();
                    foreach (MeshFilter mf in mfs)
                    {
                        Mesh m = mf.sharedMesh;
                        CopyLightmapUVsJob job = new CopyLightmapUVsJob();
                        job.polygonLightmapUVs = polygonLightmapUVs;
                        job.vertices = new NativeArray<Vector3>(m.vertices, Allocator.Persistent);
                        job.texcoords = new NativeArray<Vector2>(m.uv, Allocator.Persistent);
                        job.normals = new NativeArray<Vector3>(m.normals, Allocator.Persistent);
                        int[] triangles = m.triangles;
                        NativeList<int> nativeTriangles = new NativeList<int>(m.triangles.Length, Allocator.Persistent);
                        for (int x = 0; x < triangles.Length; ++x)
                            nativeTriangles.Add(triangles[x]);
                        job.triangles = nativeTriangles;
                        job.newVertices = new NativeList<Vector3>(Allocator.Persistent);
                        job.newTexcoords = new NativeList<Vector2>(Allocator.Persistent);
                        job.newNormals = new NativeList<Vector3>(Allocator.Persistent);
                        job.newLightmapUVs = new NativeList<Vector2>(Allocator.Persistent);
                        copyLightmapUVsHandles.Add(job.Schedule());
                        copyLightmapUVsJobs.Add(job);
                        JobHandle.ScheduleBatchedJobs();
                    }
                }
                DestroyImmediate(prefab);
            }
            
            // Collect results from copy lightmap UVs jobs
            int jobIndex = 0;
            for (int j = 0; j < meshBakers.Length; ++j)
            {
                GameObject prefab = (GameObject)PrefabUtility.InstantiatePrefab(new UnityEngine.Object()); // CODE REMOVED: Create an instance of the generate LOD mesh here so it can be modified.
                if (prefab != null)
                {
                    MeshFilter[] mfs = prefab.GetComponentsInChildren<MeshFilter>();
                    foreach (MeshFilter mf in mfs)
                    {
                        Mesh m = mf.sharedMesh;
                        copyLightmapUVsHandles[jobIndex].Complete();

                        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                        m.triangles = new int[] { 0, 1, 2 }; // NOTE: Stops error when newVertices has less vertices than the original mesh
                        m.vertices = copyLightmapUVsJobs[jobIndex].newVertices.ToArray();
                        m.uv = copyLightmapUVsJobs[jobIndex].newTexcoords.ToArray();
                        m.normals = copyLightmapUVsJobs[jobIndex].newNormals.ToArray();
                        m.uv2 = copyLightmapUVsJobs[jobIndex].newLightmapUVs.ToArray();
                        m.triangles = copyLightmapUVsJobs[jobIndex].triangles.ToArray();

                        copyLightmapUVsJobs[jobIndex].vertices.Dispose();
                        copyLightmapUVsJobs[jobIndex].texcoords.Dispose();
                        copyLightmapUVsJobs[jobIndex].normals.Dispose();
                        copyLightmapUVsJobs[jobIndex].triangles.Dispose();
                        copyLightmapUVsJobs[jobIndex].newVertices.Dispose();
                        copyLightmapUVsJobs[jobIndex].newTexcoords.Dispose();
                        copyLightmapUVsJobs[jobIndex].newNormals.Dispose();
                        copyLightmapUVsJobs[jobIndex].newLightmapUVs.Dispose();
                        ++jobIndex;
                    }
                }
                DestroyImmediate(prefab);
            }
            Debug.LogFormat("Finished copying lightmap uvs for LOD {0}", i);


            if (lodGenerator.lodDecimationPercentage[i] < 100.0f)
            {
                // Start jobs to decimate meshes.
                List<DecimateMeshJob> decimateMeshJobs = new List<DecimateMeshJob>();
                List<JobHandle> decimateMeshHandles = new List<JobHandle>();
                for (int j = 0; j < meshBakers.Length; ++j)
                {
                    GameObject prefab = (GameObject)PrefabUtility.InstantiatePrefab(new UnityEngine.Object()); // CODE REMOVED: Create an instance of the generate LOD mesh here so it can be modified.
                    if (prefab != null)
                    {
                        MeshFilter[] mfs = prefab.GetComponentsInChildren<MeshFilter>();
                        foreach (MeshFilter mf in mfs)
                        {
                            Mesh m = mf.sharedMesh;
                            DecimateMeshJob job = new DecimateMeshJob();
                            job.vertices = new NativeArray<Vector3>(m.vertices, Allocator.Persistent);
                            int[] triangles = m.triangles;
                            NativeList<int> nativeTriangles = new NativeList<int>(m.triangles.Length, Allocator.Persistent);
                            for (int x = 0; x < triangles.Length; ++x)
                                nativeTriangles.Add(triangles[x]);
                            job.triangles = nativeTriangles;
                            job.decimation = lodGenerator.lodDecimationPercentage[i];
                            decimateMeshHandles.Add(job.Schedule());
                            decimateMeshJobs.Add(job);
                            JobHandle.ScheduleBatchedJobs();
                        }
                    }
                    DestroyImmediate(prefab);
                }

                // Collect results from mesh decimation jobs.
                jobIndex = 0;
                for (int j = 0; j < meshBakers.Length; ++j)
                {
                    GameObject prefab = (GameObject)PrefabUtility.InstantiatePrefab(new UnityEngine.Object()); // CODE REMOVED: Create an instance of the generate LOD mesh here so it can be modified.
                    if (prefab != null)
                    {
                        MeshFilter[] mfs = prefab.GetComponentsInChildren<MeshFilter>();
                        foreach (MeshFilter mf in mfs)
                        {
                            Mesh m = mf.sharedMesh;
                            decimateMeshHandles[jobIndex].Complete();
                            m.triangles = decimateMeshJobs[jobIndex].triangles.ToArray();
                            decimateMeshJobs[jobIndex].vertices.Dispose();
                            decimateMeshJobs[jobIndex].triangles.Dispose();
                            m.Optimize();
                            ++jobIndex;
                        }
                    }
                    DestroyImmediate(prefab);
                }
            }

            Debug.LogFormat("Finished generating LOD {0}", i);

            // Add the generated LOD meshes to the scene & make them static.
            GameObject combinedPrefab = new GameObject(); // CODE_REMOVED: The combined prefab is an empty prefab asset.
            GameObject combinedObj = Instantiate(combinedPrefab, lodManagerObj.transform);
            combinedObj.name = "LOD" + i;
            MakeStatic(combinedObj);

            // Create nodes for each of the LOD meshes.
            // This is how we populate the LOD Managers' quadtree.
            lodNodes[i] = new List<LODTreeNode>();
            for (int j = 0; j < meshBakers.Length; ++j)
            {
                string name = "CODE REMOVED";
                LODTreeNode node = new LODTreeNode();
                node.cell = Vector2Int.zero; // CODE REMOVED: Input the grid cell.
                node.mesh = combinedObj.transform.Find(name).gameObject;
                lodNodes[i].Add(node);
            }
        }

        // Initialize LOD Manager.
        lodManager.gridCellSize = lodGenerator.cellSize;
        // Populate LOD Managers' quadtree. 
        lodManager.SetLOD(lodNodes);
        lodManager.lightmapParameters = lightmapParameters;
        EditorUtility.SetDirty(lodManager);

        // Copy colliders from the original meshes.
        // All colliders are added to a single object which itself is not visible.
        // The LOD meshes don't have colliders.
        Collider[] colliders = FindObjectsOfType<Collider>();
        GameObject colliderObj = new GameObject("Colliders");
        colliderObj.transform.parent = lodManagerObj.transform;
        foreach(Collider collider in colliders)
        {
            GameObject newCollider = Instantiate(collider.gameObject, colliderObj.transform);
            newCollider.transform.position = collider.transform.position;
            newCollider.transform.rotation = collider.transform.rotation;
            newCollider.transform.localScale = collider.transform.lossyScale;
            RemoveNonPhysicsComponents(newCollider);
        }
        MakeStatic(colliderObj);

        // Copy any additional objects which should also be in the resulting scene but are not part of the LOD system.
        GameObject additionalObjects = new GameObject("AdditionalObjects");
        additionalObjects.transform.parent = lodManagerObj.transform;
        foreach (GameObject objToCopy in lodGenerator.additionalObjects)
        {
            GameObject copy = Instantiate(objToCopy, additionalObjects.transform);
            AssignLightmapToCopy(objToCopy, copy);
        }

        // Save the entire level with LODs to a prefab
        PrefabUtility.SaveAsPrefabAssetAndConnect(lodManagerObj, Path.Combine(outputPath, scene.name + "_Final.prefab"), InteractionMode.AutomatedAction);

        // Cleanup
        polygonLightmapUVs.Dispose();
        AssetDatabase.SaveAssets();
    }

    // Remove components that aren't colliders/transform
    private void RemoveNonPhysicsComponents(GameObject go)
    {
        Component[] components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; ++i)
        {
            Type type = components[i].GetType();
            if (!type.IsSubclassOf(typeof(Collider)) && type != typeof(Transform))
            {
                DestroyImmediate(components[i]);
            }
            else
            {
                Behaviour behaviour = components[i] as Behaviour;
                if(behaviour != null)
                {
                    if(!behaviour.isActiveAndEnabled)
                    {
                        DestroyImmediate(components[i]);
                    }
                }
            }
        }

        for (int i = 0; i < go.transform.childCount; ++i)
        {
            RemoveNonPhysicsComponents(go.transform.GetChild(i).gameObject);
        }
    }

    // Remove all triangles below lodGenerator.lowestPointTerrain from the mesh
    private void RemoveTrianglesUnderTerrain(ObjectRemoved meshBaker)
    {
        GameObject prefab = (GameObject)PrefabUtility.InstantiatePrefab(new UnityEngine.Object()); // CODE REMOVED: Create an instance of the generate LOD mesh here so it can be modified.
        if (prefab != null)
        {
            MeshFilter[] mfs = prefab.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter mf in mfs)
            {
                Mesh m = mf.sharedMesh;
                Vector3[] vertices = m.vertices;
                List<int> triangles = new List<int>(m.triangles);

                for(int i = 0; i < triangles.Count; i += 3)
                {
                    int i0 = triangles[i];
                    int i1 = triangles[i + 1];
                    int i2 = triangles[i + 2];

                    Vector3 v0 = vertices[i0];
                    Vector3 v1 = vertices[i1];
                    Vector3 v2 = vertices[i2];

                    if(v0.y < lodGenerator.lowestPointTerrain && v1.y < lodGenerator.lowestPointTerrain && v2.y < lodGenerator.lowestPointTerrain)
                    {
                        triangles.RemoveRange(i, 3);
                        i -= 3;
                    }
                }

                m.triangles = triangles.ToArray();
            }
        }
        DestroyImmediate(prefab);
    }

    // Unwrap mesh to generate lightmap UVs & populate polygonLightmapUVs.
    private void GenerateLightmapUVs(ObjectRemoved meshBaker)
    {
        GameObject prefab = (GameObject)PrefabUtility.InstantiatePrefab(new UnityEngine.Object()); // CODE REMOVED: Create an instance of the generate LOD mesh here so it can be modified.
        if (prefab != null)
        {
            MeshFilter[] mfs = prefab.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter mf in mfs)
            {
                Mesh m = mf.sharedMesh;
                UnwrapParam unwrapParam = new UnwrapParam();
                UnwrapParam.SetDefaults(out unwrapParam);
                unwrapParam.packMargin = 1.0f / 1024.0f; // Reduce the amount of padding in lightmap
                // Unwrapping.GenerateSecondaryUVSet is pretty good. Doesn't leave a lot of unused space in the lightmap.
                // It does however create a lot of very small patches.
                Unwrapping.GenerateSecondaryUVSet(m, unwrapParam);

                Vector3[] vertices = m.vertices;
                Vector2[] texcoords = m.uv;
                Vector3[] normals = m.normals;
                Vector2[] lightmapUVs = m.uv2;
                int[] triangles = m.triangles;
                Debug.LogFormat("Test: {0} indices", triangles.Length);

                polygonLightmapUVs = new NativeHashMap<Hash128, PolygonLightmapUV>(triangles.Length / 3, Allocator.Persistent);

                // Populate polygonLightmapUVs
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int i0 = triangles[i];
                    int i1 = triangles[i + 1];
                    int i2 = triangles[i + 2];

                    Polygon p = new Polygon();
                    p.v0 = vertices[i0];
                    p.v1 = vertices[i1];
                    p.v2 = vertices[i2];
                    p.t0 = texcoords[i0];
                    p.t1 = texcoords[i1];
                    p.t2 = texcoords[i2];
                    p.n0 = normals[i0];
                    p.n1 = normals[i1];
                    p.n2 = normals[i2];

                    PolygonLightmapUV uvs = new PolygonLightmapUV();
                    uvs.v0 = lightmapUVs[i0];
                    uvs.v1 = lightmapUVs[i1];
                    uvs.v2 = lightmapUVs[i2];

                    Hash128 hash = new Hash128();
                    HashUtilities.ComputeHash128(ref p, ref hash);
                    polygonLightmapUVs.TryAdd(hash, uvs);
                }
            }
        }
        DestroyImmediate(prefab);
    }

    // Make all objects static.
    private void MakeStatic(GameObject go)
    {
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI | StaticEditorFlags.NavigationStatic | StaticEditorFlags.OccludeeStatic | 
            StaticEditorFlags.OccluderStatic | StaticEditorFlags.OffMeshLinkGeneration | StaticEditorFlags.ReflectionProbeStatic);
        for(int i = 0; i < go.transform.childCount; ++i)
        {
            MakeStatic(go.transform.GetChild(i).gameObject);
        }
    }

    // Bake the lightmap
    private void GenerateLightmap(ObjectRemoved meshBaker, out LightmapParameters lightmapParameters)
    {
        lightmapParameters = null;

        GameObject prefab = (GameObject)PrefabUtility.InstantiatePrefab(new UnityEngine.Object()); // CODE REMOVED: Create an instance of the generate LOD mesh here so it can be modified.
        if (prefab != null)
        {
            MakeStatic(prefab);
            if(Lightmapping.Bake())
            {
                MeshRenderer[] renderers = prefab.GetComponentsInChildren<MeshRenderer>();
                Debug.Assert(renderers.Length == 1);
                lightmapParameters = new LightmapParameters();
                lightmapParameters.lightmap = LightmapSettings.lightmaps[renderers[0].lightmapIndex].lightmapColor;
                lightmapParameters.lightmapScaleOffset = renderers[0].lightmapScaleOffset;
            }
        }
        DestroyImmediate(prefab);
    }

    // Add component to object to serialize which lightmap it uses.
    // Necessary because we're not using the normal lightmapping system.
    private void AssignLightmapToCopy(GameObject objToCopy, GameObject copy)
    {
        MeshRenderer mr = objToCopy.GetComponent<MeshRenderer>();
        if(mr != null)
        {
            if(mr.lightmapIndex >= 0)
            {
                LightmapInfo li = copy.AddComponent<LightmapInfo>();
                li.lightmap = LightmapSettings.lightmaps[mr.lightmapIndex].lightmapColor;
                li.lightmapScaleOffset = mr.lightmapScaleOffset;
            }
        }

        for(int i = 0; i < objToCopy.transform.childCount; ++i)
        {
            AssignLightmapToCopy(objToCopy.transform.GetChild(i).gameObject, copy.transform.GetChild(i).gameObject);
        }
    }
}
