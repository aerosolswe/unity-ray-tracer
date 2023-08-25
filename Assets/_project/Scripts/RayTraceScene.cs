using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class RayTraceScene : MonoBehaviour
{
    public static RayTraceScene Instance;

    struct Sphere
    {
        public float3 center;
        public float radius;
        public float3 albedo;
        public float3 specular;
        public float smoothness;
        public float3 emission;
    }

    struct MeshObject
    {
        public Matrix4x4 localToWorldMatrix;
        public int indices_offset;
        public int indices_count;
        public float3 albedo;
        public float3 specular;
        public float smoothness;
        public float3 emission;
    }

    private static List<RayTraceObject> rayTraceObjects = new List<RayTraceObject>();

    public RayTraceObject ballPrefab;
    public Light sun;
    public ReflectionProbe reflectionProbe;
    public RenderTargetIdentifier ProbeIdentifier { get; private set; }

    private ComputeBuffer meshObjectBuffer;
    private ComputeBuffer vertexBuffer;
    private ComputeBuffer indexBuffer;
    private ComputeBuffer sphereBuffer;

    public ComputeBuffer MeshObjectBuffer => meshObjectBuffer;
    public ComputeBuffer VertexBuffer => vertexBuffer;
    public ComputeBuffer IndexBuffer => indexBuffer;
    public ComputeBuffer SphereBuffer => sphereBuffer;


    private void OnEnable()
    {
        Application.targetFrameRate = 45;
        ProbeIdentifier = new RenderTargetIdentifier(reflectionProbe.bakedTexture);
        Instance = this;
    }

    private void OnDisable()
    {
        Instance = null;

        MeshObjectBuffer?.Release();
        VertexBuffer?.Release();
        IndexBuffer?.Release();
        SphereBuffer?.Release();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            RayTraceObject rto = Instantiate(ballPrefab, transform);
            rto.transform.position = new Vector3(UnityEngine.Random.Range(-5, 5), UnityEngine.Random.Range(20, 25), UnityEngine.Random.Range(-5, 5));
            float randomScale = UnityEngine.Random.Range(0.2f, 4.0f);
            rto.transform.localScale = new Vector3(randomScale, randomScale, randomScale);
            rto.albedo = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
            rto.specular = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
            rto.smoothness = UnityEngine.Random.value;
            rto.emission = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
            rto.GetComponent<Rigidbody>().mass = 0.7f * randomScale/2;
        }
    }

    private void FixedUpdate()
    {
        List<Sphere> spheres = new List<Sphere>();
        List<MeshObject> _meshObjects = new List<MeshObject>();
        List<float3> _vertices = new List<float3>();
        List<int> _indices = new List<int>();

        foreach (RayTraceObject go in rayTraceObjects)
        {
            Vector3 pos = go.transform.position;

            float radius = go.transform.localScale.x / 2;
            Vector3 albedo = new Vector3(go.albedo.r, go.albedo.g, go.albedo.b);
            Vector3 specular = new Vector3(go.specular.r, go.specular.g, go.specular.b);
            Vector3 emission = new Vector3(go.emission.r, go.emission.g, go.emission.b);

            if (go.isMesh)
            {
                Mesh mesh = go.GetComponent<MeshFilter>().sharedMesh;
                // Add vertex data
                int firstVertex = _vertices.Count;
                for(int i = 0; i < mesh.vertices.Length; i++)
                {
                    _vertices.Add(mesh.vertices[i]);
                }
                // Add index data - if the vertex buffer wasn't empty before, the
                // indices need to be offset
                int firstIndex = _indices.Count;
                var indices = mesh.GetIndices(0);
                _indices.AddRange(indices.Select(index => index + firstVertex));
                // Add the object itself
                _meshObjects.Add(new MeshObject()
                {
                    localToWorldMatrix = go.transform.localToWorldMatrix,
                    indices_offset = firstIndex,
                    indices_count = indices.Length,
                    albedo = albedo,
                    specular = specular,
                    smoothness = go.smoothness,
                    emission = emission,
                });
            }
            else
            {
                spheres.Add(new Sphere
                {
                    center = pos,
                    radius = radius,
                    albedo = albedo,
                    specular = specular,
                    smoothness = go.smoothness,
                    emission = emission
                });
            }
        }

        // Assign to compute buffer
        CreateComputeBuffer(ref vertexBuffer, _vertices, 12);
        CreateComputeBuffer(ref sphereBuffer, spheres, 56);
        CreateComputeBuffer(ref meshObjectBuffer, _meshObjects, 112);
        CreateComputeBuffer(ref indexBuffer, _indices, 4);
    }

    public void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data, int stride)
    where T : struct
    {

        //buffer = new ComputeBuffer(data.Count, stride);
        //buffer.SetData(data);

        //return;

        // Do we already have a compute buffer?
        if (buffer != null)
        {
            // If no data or buffer doesn't match the given criteria, release it
            if (data.Count == 0 || buffer.count != data.Count || buffer.stride != stride)
            {
                buffer.Release();
                buffer = null;
            }
        }
        if (data.Count != 0)
        {
            // If the buffer has been released or wasn't there to
            // begin with, create it
            if (buffer == null)
            {
                buffer = new ComputeBuffer(data.Count, stride);
            }
            // Set data on the buffer
            buffer.SetData(data);
        }
    }

    public static void RegisterObject(RayTraceObject obj)
    {
        rayTraceObjects.Add(obj);
    }

    public static void UnregisterObject(RayTraceObject obj)
    {
        rayTraceObjects.Remove(obj);
    }

}
