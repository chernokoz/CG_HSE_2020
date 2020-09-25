using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
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
        private MetaBallField field;
        private float size;
        
    
        public Cube(float beginX, float beginY, float beginZ, float size, 
            List<Vector3> verts, List<Vector3> norms, List<int> indxs,
            MetaBallField field)
        {
            points = new List<Vector3>
            {
                new Vector3(beginX + 0 * size,  beginY + 0 * size, beginZ + 0 * size), // 0
                new Vector3(beginX + 0 * size,  beginY + 1 * size, beginZ + 0 * size), // 1
                new Vector3(beginX + 1 * size,  beginY + 1 * size, beginZ + 0 * size), // 2
                new Vector3(beginX + 1 * size,  beginY + 0 * size, beginZ + 0 * size), // 3
                new Vector3(beginX + 0 * size,  beginY + 0 * size, beginZ + 1 * size), // 4
                new Vector3(beginX + 0 * size,  beginY + 1 * size, beginZ + 1 * size), // 5
                new Vector3(beginX + 1 * size,  beginY + 1 * size, beginZ + 1 * size), // 6
                new Vector3(beginX + 1 * size,  beginY + 0 * size, beginZ + 1 * size), // 7
            };
            
            F_values[0] = field.F(points[0]);
            F_values[1] = field.F(points[1]);
            F_values[2] = field.F(points[2]);
            F_values[3] = field.F(points[3]);
            F_values[4] = field.F(points[4]);
            F_values[5] = field.F(points[5]);
            F_values[6] = field.F(points[6]);
            F_values[7] = field.F(points[7]);
    
            vertices = verts;
            normals = norms;
            indices = indxs;
            this.field = field;
            this.size = size;
        }
        public void addTriangles()
        {
            var currentCaseMask = 0;
            for (var i = 0; i < 8; i++)
            {
                if (F_values[i] > 0)
                {
                    currentCaseMask |= (1 << i);
                }
            }
    
            var trianglesCount = MarchingCubes.Tables.CaseToTrianglesCount[currentCaseMask];
            var currentCase =  MarchingCubes.Tables.CaseToVertices[currentCaseMask]; // int3 [] case
    
            for (var i = 0; i < trianglesCount; i++)
            {
                var currentTriangle = currentCase[i]; // int3 with edges of current triangle
                
                for (var j = 0; j < 3; j++)
                {
                    // add triangle point
                    indices.Add(vertices.Count);
                    vertices.Add(getEdgeZeroPoint(currentTriangle[j]));
                    // vertices.Add(points[j]);
                    normals.Add(getNormal(vertices.Last()));
                    
                }

            }
        }
        
        private Vector3 getEdgeZeroPoint(int edgeNumber)
        {
            var edge = MarchingCubes.Tables._cubeEdges[edgeNumber]; // current edge
            var firstVertex = points[0] + MarchingCubes.Tables._cubeVertices[edge[0]] * size;
            var secondVertex = points[0] + MarchingCubes.Tables._cubeVertices[edge[1]] * size;
            var a = F_values[edge[0]];
            var b = F_values[edge[1]];

            return (firstVertex * b - secondVertex * a) / (b - a);
        }
    
        private Vector3 getNormal(Vector3 p)
        {
            var deltaX = new Vector3(0.1f, 0, 0);
            var deltaY = new Vector3(0, 0.1f, 0);
            var deltaZ = new Vector3(0, 0, 0.1f);
            var nx = field.F(p + deltaX) - field.F(p - deltaX);
            var ny = field.F(p + deltaY) - field.F(p - deltaY);
            var nz = field.F(p + deltaZ) - field.F(p - deltaZ);
    
            return -Vector3.Normalize(new Vector3(nx, ny, nz));
        }
    }

    private void Update()
    {
        vertices.Clear();
        indices.Clear();
        normals.Clear();

        Field.Update();


        var centerCoord = Field.getMassCenter();
        // var meshCubes = new List<Cube>();
        const float begin = -3f;
        const float end = 3f;
        const float cubeSize = 0.1f;
        
        
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
        //
        // List<Vector3> cubeVertices = new List<Vector3>
        // {
        //     new Vector3(centerCoord[0] + begin, centerCoord[1] + begin, centerCoord[2] + begin), // 0
        //     new Vector3(centerCoord[0] + begin, centerCoord[1] + end, centerCoord[2] + begin), // 1
        //     new Vector3(centerCoord[0] + end, centerCoord[1] + end, centerCoord[2] + begin), // 3
        //     new Vector3(centerCoord[0] + end, centerCoord[1] + begin, centerCoord[2] + begin), // 2
        //     new Vector3(centerCoord[0] + begin, centerCoord[1] + begin, centerCoord[2] + end), // 4
        //     new Vector3(centerCoord[0] + begin, centerCoord[1] + end, centerCoord[2] + end), // 5
        //     new Vector3(centerCoord[0] + end, centerCoord[1] + end, centerCoord[2] + end), // 6
        //     new Vector3(centerCoord[0] + end, centerCoord[1] + begin, centerCoord[2] + end), // 7
        // };
        //
        // int[] sourceTriangles =
        // {
        //     0, 1, 2, 2, 3, 0, // front
        //     3, 2, 6, 6, 7, 3, // right
        //     7, 6, 5, 5, 4, 7, // back
        //     0, 4, 5, 5, 1, 0, // left
        //     0, 3, 7, 7, 4, 0, // bottom
        //     1, 5, 6, 6, 2, 1, // top
        // };




        // var cube = new Cube(centerCoord[0], centerCoord[1], centerCoord[2], cube_size * 10,
        //     vertices, normals, indices, Field);
        // meshCubes.Add(cube);
        // cube.addTriangles();
        
        
        
        for (var i = centerCoord[0] + begin; i < centerCoord[0] + end; i += cubeSize)
        {
            for (var j = centerCoord[1] + begin; j < centerCoord[1] + end; j += cubeSize)
            {
                for (var k = centerCoord[2] + begin; k < centerCoord[2] + end; k += cubeSize)
                {
                    var cube = new Cube(i, j, k, cubeSize,
                        vertices, normals, indices, Field);
                    cube.addTriangles();
                }
            }
        }
        
        // foreach (var meshCube in meshCubes)
        // {
        //     meshCube.addTriangles();
        // }

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