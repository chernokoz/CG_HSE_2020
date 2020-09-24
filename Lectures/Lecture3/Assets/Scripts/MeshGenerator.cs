using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Unity.Mathematics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    public MetaBallField Field = new MetaBallField();
    
    private MeshFilter _filter;
    private Mesh _mesh;
    
    private List<Vector3> vertices = new List<Vector3>();
    private List<Vector3> normals = new List<Vector3>();
    private List<int> indices = new List<int>();

    /// <summary>
    /// Executed by Unity upon object initialization. <see cref="https://docs.unity3d.com/Manual/ExecutionOrder.html"/>
    /// </summary>
    private void Awake()
    {
        // Getting a component, responsible for storing the mesh
        _filter = GetComponent<MeshFilter>();
        
        // instantiating the mesh
        _mesh = _filter.mesh = new Mesh();
        
        // Just a little optimization, telling unity that the mesh is going to be updated frequently
        _mesh.MarkDynamic();
    }

    /// <summary>
    /// Executed by Unity on every frame <see cref="https://docs.unity3d.com/Manual/ExecutionOrder.html"/>
    /// You can use it to animate something in runtime.
    /// </summary>
    
    private class Cube
    {
        private List<Vector3> points;
        private float[] F_values = new float[8];
        private List<Vector3> vertices;
        private List<Vector3> normals;
        private List<int> indices;
        

        public Cube(float beginX, float beginY, float beginZ, float size, 
            List<Vector3> vertices, List<Vector3> normals, List<int> indices)
        {
            points = new List<Vector3>
            {
                new Vector3(beginX + 0,  beginY + 0, beginZ + 0), // 0
                new Vector3(beginX + 0,  beginY + 1, beginZ + 0), // 1
                new Vector3(beginX + 1,  beginY + 1, beginZ + 0), // 2
                new Vector3(beginX + 1,  beginY + 0, beginZ + 0), // 3
                new Vector3(beginX + 0,  beginY + 0, beginZ + 1), // 4
                new Vector3(beginX + 0,  beginY + 1, beginZ + 1), // 5
                new Vector3(beginX + 1,  beginY + 1, beginZ + 1), // 6
                new Vector3(beginX + 1,  beginY + 0, beginZ + 1), // 7
            };
            
            F_values[0] = new MetaBallField().F(points[0]);
            F_values[1] = new MetaBallField().F(points[1]);
            F_values[2] = new MetaBallField().F(points[2]);
            F_values[3] = new MetaBallField().F(points[3]);
            F_values[4] = new MetaBallField().F(points[4]);
            F_values[5] = new MetaBallField().F(points[5]);
            F_values[6] = new MetaBallField().F(points[6]);
            F_values[7] = new MetaBallField().F(points[7]);

            this.vertices = vertices;
            this.normals = normals;
            this.indices = indices;
        }
        public List<Vector3> addTriangles()
        {
            var res = new List<Vector3>();
            var currentCaseMask = 0;
            for (var i = 0; i < 8; i++)
            {
                if (F_values[i] > 0)
                {
                    currentCaseMask |= (1 << i);
                }
            }

            var trianglesCount = MarchingCubes.Tables.CaseToTrianglesCount[currentCaseMask];
            var currentCase =  MarchingCubes.Tables.CaseToVertices[currentCaseMask];

            for (var i = 0; i < trianglesCount; i++)
            {
                var currentVertices = currentCase[i];
                
                for (var j = 0; j <= 2; j++)
                {
                    // add first point
                    indices.Add(vertices.Count);
                    vertices.Add(getEdgeZeroPoint(currentVertices[j]));
                    normals.Add(getNormal(vertices.Last()));
                }

            }
            
            return res;
        }
        
        private Vector3 getEdgeZeroPoint(int edgeNumber)
        {
            var edge = MarchingCubes.Tables._cubeEdges[edgeNumber];
            var firstVertex = MarchingCubes.Tables._cubeVertices[edge[0]];
            var secondVertex = MarchingCubes.Tables._cubeVertices[edge[1]];
            var a = new MetaBallField().F(firstVertex);
            var b = new MetaBallField().F(secondVertex);

            return (firstVertex * a - secondVertex * b) / (b - a);
        }

        private Vector3 getNormal(Vector3 x)
        {
            var instance = new MetaBallField();
            var deltaX = new Vector3(0.1f, 0, 0);
            var deltaY = new Vector3(0, 0.1f, 0);
            var deltaZ = new Vector3(0, 0, 0.1f);
            var nx = instance.F(x + deltaX) - instance.F(x - deltaX);
            var ny = instance.F(x + deltaY) - instance.F(x - deltaY);
            var nz = instance.F(x + deltaZ) - instance.F(x - deltaZ);

            return Vector3.Normalize(new Vector3(nx, ny, nz));
        }
    }

    private void Update()
    {
        // List<Vector3> cubeVertices = new List<Vector3>
        // {
        //     new Vector3(0, 0, 0), // 0
        //     new Vector3(0, 1, 0), // 1
        //     new Vector3(1, 1, 0), // 2
        //     new Vector3(1, 0, 0), // 3
        //     new Vector3(0, 0, 1), // 4
        //     new Vector3(0, 1, 1), // 5
        //     new Vector3(1, 1, 1), // 6
        //     new Vector3(1, 0, 1), // 7
        // };

        // int[] sourceTriangles =
        // {
        //     0, 1, 2, 2, 3, 0, // front
        //     3, 2, 6, 6, 7, 3, // right
        //     7, 6, 5, 5, 4, 7, // back
        //     0, 4, 5, 5, 1, 0, // left
        //     0, 3, 7, 7, 4, 0, // bottom
        //     1, 5, 6, 6, 2, 1, // top
        // };


        vertices.Clear();
        indices.Clear();
        normals.Clear();

        Field.Update();


        var meshCubes = new List<Cube>();
        const float begin = -5;
        const float end = 5;
        const float cube_size = 0.2f;
        for (var i = begin; i < end; i += cube_size)
        {
            for (var j = begin; i < end; i += cube_size)
            {
                for (var k = begin; i < end; i += cube_size)
                {
                    meshCubes.Add(new Cube(i, j, k, cube_size,
                        vertices, normals, indices));
                }
            }
        }
        
        foreach (var meshCube in meshCubes)
        {
            meshCube.addTriangles();
        }

        // ----------------------------------------------------------------
        // Generate mesh here. Below is a sample code of a cube generation.
        // ----------------------------------------------------------------

        // What is going to happen if we don't split the vertices? Check it out by yourself by passing
        // sourceVertices and sourceTriangles to the mesh.
        
        // for (int i = 0; i < sourceTriangles.Length; i++)
        // {
        //     indices.Add(vertices.Count);
        //     Vector3 vertexPos = cubeVertices[sourceTriangles[i]];
        //     
        //     //Uncomment for some animation:
        //     vertexPos += new Vector3
        //     (
        //        Mathf.Sin(Time.time + vertexPos.z),
        //        Mathf.Sin(Time.time + vertexPos.y),
        //        Mathf.Sin(Time.time + vertexPos.x)
        //     );
        //     
        //     vertices.Add(vertexPos);
        // }
        
        print(vertices.Count);

        // Here unity automatically assumes that vertices are points and hence (x, y, z) will be represented as (x, y, z, 1) in homogenous coordinates
        _mesh.Clear();
        _mesh.SetVertices(vertices);
        _mesh.SetTriangles(indices, 0);
        // _mesh.RecalculateNormals(); // Use _mesh.SetNormals(normals) instead when you calculate them
        _mesh.SetNormals(normals);
        
        
        // Upload mesh data to the GPU
        _mesh.UploadMeshData(false);
    }
}