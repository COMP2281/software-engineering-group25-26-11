using Seb.Fluid2D.Rendering;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.Rendering;

public class RippleEffect : MonoBehaviour
{
    public static RippleEffect Instance;

    public float timeBetweenRipples = 0.2f;
    public Collider waterCollider;
    public ParticleSystem splashParticleSystem;

    public int TextureWidth = 1024;
    public int TextureHeight = 400;
    public RenderTexture ObjectsRT;
    private RenderTexture CurrRT, PrevRT, TempRT;
    public Shader RippleShader, AddShader;
    private Material RippleMat, AddMat;
    private float lastRippleTime;
    private Camera _mainCam;
    // Start is called before the first frame update

    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        // Ray casting camera
        _mainCam = Camera.main;
        //Creating render textures and materials
        CurrRT = new RenderTexture(TextureWidth, TextureHeight, 0, RenderTextureFormat.RFloat);
        PrevRT = new RenderTexture(TextureWidth, TextureHeight, 0, RenderTextureFormat.RFloat);
        TempRT = new RenderTexture(TextureWidth, TextureHeight, 0, RenderTextureFormat.RFloat);
        RippleMat = new Material(RippleShader);
        AddMat = new Material(AddShader);

        //Change the texture in the material of this object to the render texture calculated by the ripple shader.
        GetComponent<Renderer>().material.SetTexture("_RippleTex", CurrRT);

        StartCoroutine(ripples());
    }

    // Update is called once per frame
    public void RippleAtPoint(Vector3 point)
    {
        //Move the particle system to the hit point and emit a splash.
        splashParticleSystem.transform.position = point;    
        splashParticleSystem.Emit(1);
    }
    private void Update()
    {
        // GetMouseButton instead of GetMouseButtonDown for continuous drawing while dragging
        if (Input.GetMouseButton(0) && Time.time - lastRippleTime >= timeBetweenRipples)
        {
            Ray ray = _mainCam.ScreenPointToRay(Input.mousePosition);
            if (waterCollider.Raycast(ray, out RaycastHit hit, float.MaxValue))
            {
                RippleAtPoint(hit.point);
                lastRippleTime = Time.time;
            }
        }
    }

    IEnumerator ripples()
    {
        // compute object scale vector and pass to shaders
        // use the two axes that map to mesh's UV U and V:
        float scaleU = transform.lossyScale.x;
        float scaleV = transform.lossyScale.z;
        Vector4 objScale = new Vector4(scaleU, scaleV, 0f, 0f);

        //Copy the result of blending the render textures to TempRT.
        AddMat.SetTexture("_ObjectsRT", ObjectsRT);
        AddMat.SetTexture("_CurrentRT", CurrRT);
        AddMat.SetVector("_ObjectScale", objScale);
        Graphics.Blit(null, TempRT, AddMat);

        RenderTexture rt0 = TempRT;
        TempRT = CurrRT;
        CurrRT = rt0;

        //Calculate the ripple animation using ripple shader.
        RippleMat.SetTexture("_PrevRT", PrevRT);
        RippleMat.SetTexture("_CurrentRT", CurrRT);
        RippleMat.SetVector("_ObjectScale", objScale);
        Graphics.Blit(null, TempRT, RippleMat);
        Graphics.Blit(TempRT, PrevRT);

        //Swap PrevRT and CurrentRT to calculate the result for the next frame.
        RenderTexture rt = PrevRT;
        PrevRT = CurrRT;
        CurrRT = rt;

        //Wait for one frame and then execute again.
        yield return null;
        StartCoroutine(ripples());
    }
}