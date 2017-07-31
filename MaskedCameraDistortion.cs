using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

// Remember to ajust the main camera transform matrix according to the tracker data 

public class MaskedCameraDistortion : MonoBehaviour 
{
	// color correction shader parameters (composition.shader)
	public Vector4 liftColor = new Vector4(0.0f,0.0f,0.0f,1.0f);
	public Vector4 gammaColor = new Vector4(0.5f,0.5f,0.5f,1.0f);
	public Vector4 gainColor = new Vector4(1.0f,1.0f,1.0f,1.0f);
	public float saturation = 0.5f;
	// displacement shader parameter (displacementblit.shader)
	public Vector2 offset = new Vector2(0.0f,0.0f);
    public float animSpeed = 1.0f;

	public Texture2D mTexture;                                         // feed the video texture here. Check that the dimensios of the the used render targets match.
    public Texture2D mDisplacementTexture;

    private RenderTexture mMaskRenderTexture;
    private RenderTexture mMaskedAreaCompositionRenderTexture;
    private RenderTexture mDispacedImageRenderTexture;
	private RenderTexture mIntermediateRT;
    
	private GameObject mMaskCamera;
	public GameObject mMaskObject;
  
    private Material mMaskBlurMaterial;
    private Material mMaskedAreaCompositionMaterial;
    private Material mDisplacementMaterial;
    private Material mFinalCompositionMaterial;

	public Shader SeparableGlassBlurShader;
	public Shader CompositionShader;
	public Shader DisplacementBlitShader;
	public Shader FinalCompositionShader;
	public Shader MaskGenerationShader;

    void Start () 
	{
        //The 3D object for masking needs to be named as "MaskObject". 
        if (mMaskObject == null)
        {
            mMaskObject = GameObject.Find("MaskObject");
		}

		if (mMaskObject != null) {
			setLayer (mMaskObject, 12);
		}

        if (mTexture == null)
        { 
			mTexture = new Texture2D (Screen.width, Screen.height);
            mTexture.filterMode = FilterMode.Point;
            mTexture.wrapMode = TextureWrapMode.Clamp;
        }

		if (mIntermediateRT == null)
			mIntermediateRT = new RenderTexture (Screen.width, Screen.height, 0, RenderTextureFormat.Default);
        
		if (mDisplacementTexture == null) {
			mDisplacementTexture = new Texture2D (256, 256);
		}
  
        if (mMaskRenderTexture == null)
        {
            mMaskRenderTexture = new RenderTexture(Camera.main.pixelWidth, Camera.main.pixelHeight, 16);
            mMaskRenderTexture.filterMode = FilterMode.Bilinear;
        }
  
		if (mMaskedAreaCompositionRenderTexture == null)
        {
            mMaskedAreaCompositionRenderTexture = new RenderTexture(Camera.main.pixelWidth, Camera.main.pixelHeight, 0);
            mMaskedAreaCompositionRenderTexture.filterMode = FilterMode.Bilinear;
        }
        if (mDispacedImageRenderTexture == null)
        {
            mDispacedImageRenderTexture = new RenderTexture(Camera.main.pixelWidth, Camera.main.pixelHeight, 0);
            mDispacedImageRenderTexture.filterMode = FilterMode.Point;
            mDispacedImageRenderTexture.wrapMode = TextureWrapMode.Repeat;
        }

        if (mMaskCamera == null)
        {
            mMaskCamera = new GameObject("MaskCamera", typeof(MeshRenderer), typeof(MeshFilter));
            mMaskCamera.AddComponent<Camera>();
            mMaskCamera.GetComponent<Camera>().enabled = false;
            mMaskCamera.hideFlags = HideFlags.HideAndDontSave;
        }

        if (mMaskBlurMaterial == null)
			mMaskBlurMaterial = new Material(SeparableGlassBlurShader);

        if (mMaskedAreaCompositionMaterial == null)
            mMaskedAreaCompositionMaterial = new Material(CompositionShader);

        if (mDisplacementMaterial == null)
            mDisplacementMaterial = new Material(DisplacementBlitShader);
        
        if (mFinalCompositionMaterial == null)
            mFinalCompositionMaterial = new Material(FinalCompositionShader);

        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = Color.black;
        Camera.main.cullingMask = ~(1 << 12);
    }
	
    // Generate the blurred mask and dispaced image
    void OnPreRender()
    {       
        mMaskCamera.GetComponent<Camera>().CopyFrom(Camera.main);
        mMaskCamera.GetComponent<Camera>().backgroundColor = new Color(0, 0, 0, 1);
        mMaskCamera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;

//        mMaskCamera.SetActive(true);
        
        RenderDisplacedImage();
        GenerateSharpMask();
        BlurSharpMask();
        CompositeMaskArea();
                
//        mMaskCamera.SetActive(false);
    }

    // OnRenderImage is called after all rendering is complete to render image
    void OnRenderImage (RenderTexture source, RenderTexture destination)
    {
		if (mTexture == null || source == null)
			return;

		source.filterMode = FilterMode.Point;
        mFinalCompositionMaterial.SetTexture("_MainCameraTex", source);
        mFinalCompositionMaterial.SetTexture("_MaskedAreaTex", mMaskedAreaCompositionRenderTexture);
		Graphics.Blit(source, mIntermediateRT, mFinalCompositionMaterial, 0);
		Graphics.Blit(mIntermediateRT, destination);

		mIntermediateRT.DiscardContents();
		mMaskRenderTexture.DiscardContents();
		mMaskedAreaCompositionRenderTexture.DiscardContents();
		mDispacedImageRenderTexture.DiscardContents();
  }   
    
    // A helper function to set layer recursively to all child objects
    private void setLayer(GameObject g, int layer)
    {
        g.layer = layer;
        foreach(Transform t in g.transform)
        {
            t.gameObject.layer = 2;
            setLayer(t.gameObject, layer);
        }
    }

    // Render mask object to generate sharp mask for camera image
    public void GenerateSharpMask()
    {
        RenderTexture attachedRenderTexture = RenderTexture.active; 
        mMaskCamera.GetComponent<Camera>().targetTexture = mMaskRenderTexture;
        RenderTexture.active = mMaskRenderTexture;
        GL.Clear(true, true, Color.clear);

        // Render the mask object only
        int maskCameraCullingMask = mMaskCamera.GetComponent<Camera>().cullingMask;
        mMaskCamera.GetComponent<Camera>().cullingMask = 1 << 12;

		mMaskCamera.GetComponent<Camera>().RenderWithShader(MaskGenerationShader, null);
        
        mMaskCamera.GetComponent<Camera>().cullingMask = maskCameraCullingMask;
        RenderTexture.active = attachedRenderTexture;
    }

    // Render displaced image that is a base to fill the mask area
    public void RenderDisplacedImage()
    {
        RenderTexture attachedRenderTexture = RenderTexture.active; 
        mMaskCamera.GetComponent<Camera>().targetTexture = mDispacedImageRenderTexture;
        RenderTexture.active = mDispacedImageRenderTexture;
        GL.Clear(false, true, Color.clear);

        if (mDisplacementMaterial != null)
        {
            mDisplacementMaterial.SetVector("uvOffsetInvScales", new Vector2(1.0f / Screen.width, 1.0f / Screen.height));
            mDisplacementMaterial.SetTexture("_DisplacementTex", mDisplacementTexture);
            mDisplacementMaterial.SetVector("_Offset", offset);
            mDisplacementMaterial.SetFloat("_AnimSpeed", animSpeed);
            Graphics.Blit(mTexture, mDispacedImageRenderTexture, mDisplacementMaterial);
        }

        mMaskCamera.GetComponent<Camera>().targetTexture = null;
        RenderTexture.active = attachedRenderTexture; 
    }
    
    // Blur boudaries of the sharp mask to generate smooth transition towards non-masked area  
    public void BlurSharpMask()
    {
        int downScale = 8;
        RenderTexture blurredID = RenderTexture.GetTemporary(Camera.main.pixelWidth / downScale, Camera.main.pixelHeight / downScale, 0);
        blurredID.filterMode = FilterMode.Bilinear;
        RenderTexture blurredID2 = RenderTexture.GetTemporary(Camera.main.pixelWidth / downScale, Camera.main.pixelHeight / downScale, 0);
        blurredID2.filterMode = FilterMode.Bilinear;

        // downsample
        Graphics.Blit(mMaskRenderTexture, blurredID);

        // horizontal blur
        mMaskBlurMaterial.SetVector("offsets", new Vector4(2.0f / Screen.width, 0, 0, 0));
        Graphics.Blit(blurredID, blurredID2, mMaskBlurMaterial);
		blurredID.DiscardContents();
        // vertical blur
        mMaskBlurMaterial.SetVector("offsets", new Vector4(0, 2.0f / Screen.height, 0, 0));
        Graphics.Blit(blurredID2, blurredID, mMaskBlurMaterial);
		blurredID2.DiscardContents();
        // horizontal blur
        mMaskBlurMaterial.SetVector("offsets", new Vector4(4.0f / Screen.width, 0, 0, 0));
        Graphics.Blit(blurredID, blurredID2, mMaskBlurMaterial);
		blurredID.DiscardContents();
        // vertical blur
        mMaskBlurMaterial.SetVector("offsets", new Vector4(0, 4.0f / Screen.height, 0, 0));
        Graphics.Blit(blurredID2, blurredID, mMaskBlurMaterial);
		blurredID2.DiscardContents();
        // horizontal blur
        mMaskBlurMaterial.SetVector("offsets", new Vector4(8.0f / Screen.width, 0, 0, 0));
        Graphics.Blit(blurredID, blurredID2, mMaskBlurMaterial);
		blurredID.DiscardContents();
        // vertical blur
        mMaskBlurMaterial.SetVector("offsets", new Vector4(0, 8.0f / Screen.height, 0, 0));
        Graphics.Blit(blurredID2, blurredID, mMaskBlurMaterial);
		blurredID2.DiscardContents();

        // upsample
        Graphics.Blit(blurredID, mMaskRenderTexture);
        blurredID.DiscardContents();

        RenderTexture.ReleaseTemporary(blurredID);
        RenderTexture.ReleaseTemporary(blurredID2);
    }
    
    // Composite the blurred mask and displaced image 
    public void CompositeMaskArea()
    {
        mMaskedAreaCompositionMaterial.SetTexture("_ImageTex", mTexture);
        mMaskedAreaCompositionMaterial.SetTexture("_DispTex", mDispacedImageRenderTexture);
        mMaskedAreaCompositionMaterial.SetTexture("_MaskTex", mMaskRenderTexture);

		mMaskedAreaCompositionMaterial.SetColor("_LiftColor", liftColor);
		mMaskedAreaCompositionMaterial.SetColor("_GammaColor", gammaColor);
		mMaskedAreaCompositionMaterial.SetColor("_GainColor", gainColor);
		mMaskedAreaCompositionMaterial.SetFloat("_Saturation", saturation);

        Graphics.Blit(mMaskRenderTexture, mMaskedAreaCompositionRenderTexture, mMaskedAreaCompositionMaterial);

        mMaskCamera.GetComponent<Camera>().targetTexture = null;
    }

    protected void OnDestroy()
    {
        if (mMaskCamera)
            Destroy(mMaskCamera);

        if (mMaskRenderTexture)
            Destroy(mMaskRenderTexture);

        if (mMaskedAreaCompositionRenderTexture)
            Destroy(mMaskedAreaCompositionRenderTexture);

        if (mDispacedImageRenderTexture)
            Destroy(mDispacedImageRenderTexture);

		if (mIntermediateRT)
			Destroy(mIntermediateRT);

         if (mMaskObject)
             Destroy(mMaskObject);
  
        if (mMaskBlurMaterial)
            Destroy(mMaskBlurMaterial);

        if (mMaskedAreaCompositionMaterial)
            Destroy(mMaskedAreaCompositionMaterial);

        if (mDisplacementMaterial)
            Destroy(mDisplacementMaterial);

        if (mFinalCompositionMaterial)
            Destroy(mFinalCompositionMaterial);
    }

}