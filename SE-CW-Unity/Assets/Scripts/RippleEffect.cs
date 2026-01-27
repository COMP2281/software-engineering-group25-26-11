using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class RippleEffect : MonoBehaviour
{
    public int TextureSize = 512;
    public RenderTexture ObjectsRT;
    private RenderTexture CurrRT, PrevRT, TempRT;
    public Shader RippleShader, AddShader;
    private Material RippleMat, AddMat;
    // Start is called before the first frame update
    void Start()
    {
        //Creating render textures and materials
        CurrRT = new RenderTexture(TextureSize, TextureSize, 0, RenderTextureFormat.RFloat);
        PrevRT = new RenderTexture(TextureSize, TextureSize, 0, RenderTextureFormat.RFloat);
        TempRT = new RenderTexture(TextureSize, TextureSize, 0, RenderTextureFormat.RFloat);
        RippleMat = new Material(RippleShader);
        AddMat = new Material(AddShader);

        //Change the texture in the material of this object to the render texture calculated by the ripple shader.
        GetComponent<Renderer>().material.SetTexture("_RippleTex", CurrRT);

        StartCoroutine(ripples());
    }

    // Update is called once per frame
    IEnumerator ripples()
    {
        // compute object scale vector and pass to shaders
        // use the two axes that map to mesh's UV U and V:
        // - For a typical horizontal plane mesh, U maps to transform.lossyScale.x and V maps to transform.lossyScale.z.
        // - If mesh uses X/Y for UVs, change .z to .y below.
        float scaleU = transform.lossyScale.x;
        float scaleV = transform.lossyScale.z; // change to .y if your mesh maps V to Y
        // protect against zero scale
        if (Mathf.Abs(scaleU) < 1e-6f) scaleU = 1e-6f;
        if (Mathf.Abs(scaleV) < 1e-6f) scaleV = 1e-6f;

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