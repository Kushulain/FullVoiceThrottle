﻿using UnityEngine;
using System.Collections;


public class ColoredFog : PostEffectsBaseCS {
	
	public enum FogMode {
		AbsoluteYAndDistance = 0,
		AbsoluteY = 1,
		Distance = 2,
		RelativeYAndDistance = 3,
	}
	
	public FogMode fogMode  = FogMode.AbsoluteYAndDistance;
	
	private float CAMERA_NEAR  = 0.5f;
	private float CAMERA_FAR  = 50.0f;
	private float CAMERA_FOV  = 60.0f;	
	private float CAMERA_ASPECT_RATIO  = 1.333333f;
	
	public float startDistance  = 200.0f;
	public float globalDensity  = 1.0f;
	public float heightScale  = 100.0f;
	public float height = 0.0f;
	
	public Color globalFogColor  = Color.grey;
	public Color globalFogColor2  = Color.grey;
	public Color globalFogColor3  = Color.grey;
	public Color globalFogColor4  = Color.grey;
	public Color globalFogColor5  = Color.grey;
	public float fogMaxDistance = 1000f;
	public Color skyColor  = Color.grey;
	public bool takeAlphaIntoAccount = false;
	
	public Shader fogShader;
	private Material fogMaterial = null;	
	
	public override bool CheckResources ()
	{	
		CheckSupport (true);
		
		fogMaterial = CheckShaderAndCreateMaterial (fogShader, fogMaterial);
		
		if(!isSupported)
			ReportAutoDisable ();
		return isSupported;				
	}
	
	void OnRenderImage (RenderTexture source, RenderTexture destination ) {	
//		if (takeAlphaIntoAccount)
//		{
//			camera.pixelRect = new Rect(0f,0f,Screen.width*0.5f,Screen.height*0.5f);
//
//		}

		if(CheckResources()==false) {
			Graphics.Blit (source, destination);
			return;
		}


		CAMERA_NEAR = GetComponent<Camera>().nearClipPlane;
		CAMERA_FAR = GetComponent<Camera>().farClipPlane;
		CAMERA_FOV = GetComponent<Camera>().fieldOfView;
		CAMERA_ASPECT_RATIO = GetComponent<Camera>().aspect;
		
		Matrix4x4 frustumCorners = Matrix4x4.identity;		
		Vector4 vec;
		Vector3 corner;
		
		float fovWHalf = CAMERA_FOV * 0.5f;
		
		Vector3 toRight = GetComponent<Camera>().transform.right * CAMERA_NEAR * Mathf.Tan (fovWHalf * Mathf.Deg2Rad) * CAMERA_ASPECT_RATIO;
		Vector3 toTop = GetComponent<Camera>().transform.up * CAMERA_NEAR * Mathf.Tan (fovWHalf * Mathf.Deg2Rad);
		
		Vector3 topLeft = (GetComponent<Camera>().transform.forward * CAMERA_NEAR - toRight + toTop);
		float CAMERA_SCALE = topLeft.magnitude * CAMERA_FAR/CAMERA_NEAR;	
		
		topLeft.Normalize();
		topLeft *= CAMERA_SCALE;
		
		Vector3 topRight = (GetComponent<Camera>().transform.forward * CAMERA_NEAR + toRight + toTop);
		topRight.Normalize();
		topRight *= CAMERA_SCALE;
		
		Vector3 bottomRight = (GetComponent<Camera>().transform.forward * CAMERA_NEAR + toRight - toTop);
		bottomRight.Normalize();
		bottomRight *= CAMERA_SCALE;
		
		Vector3 bottomLeft = (GetComponent<Camera>().transform.forward * CAMERA_NEAR - toRight - toTop);
		bottomLeft.Normalize();
		bottomLeft *= CAMERA_SCALE;
		
		frustumCorners.SetRow (0, topLeft); 
		frustumCorners.SetRow (1, topRight);		
		frustumCorners.SetRow (2, bottomRight);
		frustumCorners.SetRow (3, bottomLeft);		
		
		fogMaterial.SetMatrix ("_FrustumCornersWS", frustumCorners);
		fogMaterial.SetVector ("_CameraWS", GetComponent<Camera>().transform.position);
		fogMaterial.SetVector ("_StartDistance", new Vector4 (1.0f / startDistance, (CAMERA_SCALE-startDistance)));
		fogMaterial.SetVector ("_Y", new Vector4 (height, 1.0f / heightScale));

		fogMaterial.SetFloat ("_GlobalDensity", globalDensity * 0.01f);
		fogMaterial.SetColor ("_FogColor", globalFogColor);
		fogMaterial.SetColor ("_FogColor2", globalFogColor2);
		fogMaterial.SetColor ("_FogColor3", globalFogColor3);
		fogMaterial.SetColor ("_FogColor4", globalFogColor4);
		fogMaterial.SetColor ("_FogColor5", globalFogColor5);
		fogMaterial.SetColor ("_SkyColor", skyColor);
		fogMaterial.SetFloat ("_FogMaxDistance", 1f/fogMaxDistance);
		fogMaterial.SetFloat ("_takeAlpha", takeAlphaIntoAccount ? 1f : 0f);

		CustomGraphicsBlit (source, destination, fogMaterial, (int)fogMode);
	}
	
	static void CustomGraphicsBlit (RenderTexture source, RenderTexture dest , Material fxMaterial,int passNr ) {
		RenderTexture.active = dest;
		
		fxMaterial.SetTexture ("_MainTex", source);	        
		
		GL.PushMatrix ();
		GL.LoadOrtho ();	
		
		fxMaterial.SetPass (passNr);	
		
		GL.Begin (GL.QUADS);
		
		GL.MultiTexCoord2 (0, 0.0f, 0.0f); 
		GL.Vertex3 (0.0f, 0.0f, 3.0f); // BL
		
		GL.MultiTexCoord2 (0, 1.0f, 0.0f); 
		GL.Vertex3 (1.0f, 0.0f, 2.0f); // BR
		
		GL.MultiTexCoord2 (0, 1.0f, 1.0f); 
		GL.Vertex3 (1.0f, 1.0f, 1.0f); // TR
		
		GL.MultiTexCoord2 (0, 0.0f, 1.0f); 
		GL.Vertex3 (0.0f, 1.0f, 0.0f); // TL
		
		GL.End ();
		GL.PopMatrix ();
	}		
}
