// RopeGPU.cs
using UnityEngine;
using System.Runtime.InteropServices;
using Unity.Properties;
using System.Collections.Generic;

public class RopeGPU : MonoBehaviour
{
    struct Point { public Vector2 position, prevPosition; public float friction; public int isFixed; }
    struct Constraint { public int idxA, idxB; public float restLength; }

    [Header("Compute Settings")]
    public ComputeShader compute;
    public int numPoints;
    public int numConstraints;
    public Vector2 gravity = new Vector2(0, -9.81f);
    public int constraintPasses = 16;
    [Range(0, 1)]
    public float airFriction = 0.90f;
    public Transform rootObj;

    [Header("GPU Rendering")]
    [Tooltip("Material using the RopeLine shader below")]
    public Material ropeMaterial;
    [Tooltip("Color of the rope")]
    public Color ropeColor = Color.white;

    public ComputeBuffer pointBuffer, constraintBuffer, debugBuffer, pinBuffer;
    Point[] points;
    Constraint[] constraints;

    public float thickness = 1.0f;

    public 
    List<GameObject> pinnedObjs;
    
    Vector2[] pinPositions;
    uint[] pinIds;
    float[] debugs;
    int frameCount;

    int integrateK, constrainK;
    public static readonly List<RopeGPU> Ropes = new List<RopeGPU>();

    void OnEnable() => Ropes.Add(this);       // register this instance
    void OnDisable() => Ropes.Remove(this);    // deregister (editor & runtime)
    
    void Start()
    {
        compute = Instantiate(compute);
        ropeMaterial = Instantiate(ropeMaterial);
        // find kernels
        integrateK = compute.FindKernel("Integrate");
        constrainK = compute.FindKernel("Constrain");
        
        // allocate + init
        points = new Point[numPoints];
        constraints = new Constraint[numConstraints];
        debugs = new float[numPoints];
        pinPositions = new Vector2[numPoints];

        float spacing = .1f;



        for (int i = 0; i < numPoints; i++)
        {
            points[i].position = new Vector2(0, i*spacing);
            points[i].prevPosition = points[i].position;
            points[i].friction = 0.999999f;
            points[i].isFixed = (i == 0) ? 1 : 0;
            Debug.Log("point " + i + "is " + points[i].isFixed + "fixed");
        }

        for (int i = 0; i < numConstraints; i++)
        {
            constraints[i].idxA = i;
            Debug.Log("I: " + (i));
            constraints[i].idxB = i + 1;
            Debug.Log("I  with e  " + (i+1));
            constraints[i].restLength = spacing;
        }

        for (int i = 0;i < numPoints; i++)
        {
            debugs[i] = 0f;
        }

        // create GPU buffers
        int pStride = sizeof(float) * 2 * 2 + sizeof(float) + sizeof(int);
        int cStride = sizeof(int) * 2 + sizeof(float);

        debugBuffer = new ComputeBuffer(numPoints, sizeof(float));
        pointBuffer = new ComputeBuffer(numPoints, pStride);
        constraintBuffer = new ComputeBuffer(numConstraints, cStride);
        pinBuffer = new ComputeBuffer(numPoints, (sizeof(float) * 2));
        pointBuffer.SetData(points);
        constraintBuffer.SetData(constraints);
        debugBuffer.SetData(debugs);
        pinBuffer.SetData(pinPositions);

        // bind to compute
        compute.SetBuffer(integrateK, "points", pointBuffer);
        compute.SetBuffer(integrateK, "pinPositions", pinBuffer);
        compute.SetBuffer(constrainK, "points", pointBuffer);
        compute.SetBuffer(constrainK, "constraints", constraintBuffer);
        compute.SetBuffer(constrainK, "debug", debugBuffer);
        compute.SetInt("numPoints", numPoints);
        compute.SetInt("numConstraints", numConstraints);
        compute.SetFloat("stiffness", 0.9999f);



        

        // bind to render material
        if (ropeMaterial == null)
        {
            Debug.LogError("Assign a Material using the RopeLine shader.");
            enabled = false;
            return;
        }

        compute.SetFloats("gravity", gravity.x, gravity.y);
        compute.SetFloat("airFriction", airFriction);
        ropeMaterial.SetBuffer("_Points", pointBuffer);
        ropeMaterial.SetColor("_Color", ropeColor);

        frameCount = 0;

    }

    void Update()
    {


        //OTHER LOGIC 

        for (int i = 0; i < pinnedObjs.Count; i++)
        {
            pinPositions[i] = new Vector2(pinnedObjs[i].transform.position.x, pinnedObjs[i].transform.position.y);

        }
        pinBuffer.SetData(pinPositions);





        //COMPUTE 
        // run simulation
        compute.SetFloat("deltaTime", Time.fixedDeltaTime);


        compute.SetBuffer(integrateK, "pinPositions", pinBuffer);

        int pg = Mathf.CeilToInt(numPoints / 64f);
        int cg = Mathf.CeilToInt(numConstraints / 64f);
        float dt = Time.fixedDeltaTime * .5f;

        compute.SetFloat("deltaTime", dt);
        compute.Dispatch(integrateK, pg, 1, 1);

        for (int j = 0; j < constraintPasses; j++)
        {
            compute.SetInt("evenPass", 0);
            compute.Dispatch(constrainK, cg, 1, 1);


            compute.SetInt("evenPass", 1);
            compute.Dispatch(constrainK, cg, 1, 1);

            // pointBuffer.GetData(points);
            //  foreach (Point p in points){
            //     Debug.Log(p.position.x + " + " +  p.position.y);

            // }

            // constraintBuffer.GetData(constraints);

            //        foreach (Constraint c in constraints) {
            //           Debug.Log("DISTANCE: " +  (points[c.idxA].position - points[c.idxB].position)); 


            //      }


            frameCount++;

        }
    }
    void OnRenderObject()
    {
        ropeMaterial.SetBuffer("_Points", pointBuffer);
        ropeMaterial.SetBuffer("_Constraints", constraintBuffer);
        ropeMaterial.SetFloat("_Thickness", thickness);
        ropeMaterial.SetColor("_Color", ropeColor);
        ropeMaterial.SetPass(0);                // <--- bind “ForwardRope”

        // draw one line strip of `numPoints` verts
        Graphics.DrawProceduralNow(MeshTopology.Triangles, 6,  numConstraints);
    }

    void OnDestroy()
    {
        pointBuffer?.Release();
        constraintBuffer?.Release();
    }
}

