using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))][RequireComponent(typeof(MeshFilter))]
public class RayTraceObject : MonoBehaviour
{
    public bool isMesh = false;

    public Color albedo = Color.white;
    public Color specular = Color.white;
    public float smoothness;
    public Color emission = Color.black;

    private void OnEnable()
    {
        RayTraceScene.RegisterObject(this);
    }
    private void OnDisable()
    {
        RayTraceScene.UnregisterObject(this);
    }
}
