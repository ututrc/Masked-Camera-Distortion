using UnityEngine;
using System.Collections;

public class TextureLoader : MonoBehaviour {

	public Texture2D tex;

	// Use this for initialization
	void Start () {

		//cam = new WebCamTexture ();
		if (tex != null)
			GetComponent<MaskedCameraDistortion> ().mTexture = tex;
		
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
