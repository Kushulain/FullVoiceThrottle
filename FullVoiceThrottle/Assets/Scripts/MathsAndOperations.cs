using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public static class MathsAndOperations {

	//public static float 
	
	public static int GetClosestHit(RaycastHit[] hits, Vector3 targetPosition)
	{
		int closestId = -1;
		for(int i=0; i<hits.Length;i++)
		{
			if (  ( closestId == -1 ||
			                                      (hits[i].point - targetPosition).sqrMagnitude < 
			                                      (hits[closestId].point - targetPosition).sqrMagnitude ))
			{
				closestId = i;
			}
		}
		
		return closestId;
	}
	
	public static Vector3 Multiply(this Vector3 VecA, Vector3 VecB)
	{
		return new Vector3(VecA.x*VecB.x,VecA.y*VecB.y,VecA.z*VecB.z);
	}

	public static Vector3 RotateAround(this Vector3 VecA, Vector3 axisPosition, Vector3 AxisDirection, float angle)
	{
		return Quaternion.AngleAxis(angle,AxisDirection) * ( VecA - axisPosition) + axisPosition;
	}


	public static void AddForceDebug(this Rigidbody RB, Vector3 force,  ForceMode forceMode = ForceMode.Force)
	{
		/*
		if (force.sqrMagnitude > 30000)
			Debug.LogWarning("Suspicious force : " + force);*/
		RB.AddForce(force,forceMode);
	}

	public static void AddForceAtPositionDebug(this Rigidbody RB, Vector3 force, Vector3 position,  ForceMode forceMode = ForceMode.Force)
	{
		/*
		if (force.sqrMagnitude > 30000)
			Debug.LogWarning("Suspicious force : " + force);*/
		RB.AddForceAtPosition(force,position,forceMode);
	}

	public static Vector3 Divide(this Vector3 VecA, Vector3 VecB)
	{
		return new Vector3(VecA.x/VecB.x,VecA.y/VecB.y,VecA.z/VecB.z);
	}

	public static float Pow(float A, float B)
	{
		if (A < 0f)
		{
			return -Mathf.Pow(Mathf.Abs(A),B);
		}
		return Mathf.Pow(A,B);
	}

	public static Vector3 GetFallPositionAtTime(Vector3 startPos, Vector3 JumpVel, float time)
	{
		return startPos + time * JumpVel + time * time * Physics.gravity * 0.5f;
	}

	public static float ClampMinus1To1(float val)
	{
		return Mathf.Max(-1f,Mathf.Min(1f,val));
	}

	public static Transform FindRecursivly(Transform transform, string name)
	{
		Transform res = null;
		foreach (Transform child in transform)
		{
			if (child.name == name)
				return child;
			else 
				res = FindRecursivly(child, name);
			
			if (res != null)
				break;
		}
		
		return res;
	}

	public static T GetParentComponent<T>(Transform transform) where T : Component
	{
		do
		{
			if (transform.GetComponent<T>() != null)
				return transform.GetComponent<T>();
			
			transform = transform.parent;
			
		}  while (transform != null);

		return null;
	}

	public static int GetLayerMaskForLayer(int layerId)
	{
		int layerMask = 0;

		for (int i=0; i<32; i++)
		{
			if (!Physics.GetIgnoreLayerCollision(layerId,i))
				layerMask |= 1<<i;
		}

		return layerMask;
	}

	public static void SetSubMesh(this Mesh meshA, Mesh meshB, int subMeshID)
	{
		if (meshA.triangles.Length == 0)
			Debug.LogError("Merge an empty mesh ? Well ... can do ...");

		int currentVerticesCount = meshA.vertices.Length;

		Vector3[] newVertices = new Vector3[currentVerticesCount+meshB.vertices.Length];
		Array.Copy(meshA.vertices, newVertices, currentVerticesCount);
		Array.Copy(meshB.vertices, 0, newVertices, currentVerticesCount, meshB.vertices.Length);
		meshA.vertices = newVertices;

		BoneWeight[] newBoneWeight = new BoneWeight[currentVerticesCount+meshB.boneWeights.Length];
		Array.Copy(meshA.boneWeights, newBoneWeight, currentVerticesCount);
		Array.Copy(meshB.boneWeights, 0, newBoneWeight, currentVerticesCount, meshB.boneWeights.Length);
		meshA.boneWeights = newBoneWeight;

		Vector3[] newNormals = new Vector3[currentVerticesCount+meshB.normals.Length];
		Array.Copy(meshA.normals, newNormals, currentVerticesCount);
		Array.Copy(meshB.normals, 0, newNormals, currentVerticesCount, meshB.normals.Length);
		meshA.normals = newNormals;

		Vector4[] newTangents = new Vector4[currentVerticesCount+meshB.tangents.Length];
		Array.Copy(meshA.tangents, newTangents, currentVerticesCount);
		Array.Copy(meshB.tangents, 0, newTangents, currentVerticesCount, meshB.tangents.Length);
		meshA.tangents = newTangents;

		Vector2[] newUV = new Vector2[currentVerticesCount+meshB.uv.Length];
		Array.Copy(meshA.uv, newUV, currentVerticesCount);
		Array.Copy(meshB.uv, 0, newUV, currentVerticesCount, meshB.uv.Length);
		meshA.uv = newUV;

		int[] newTriangles = new int[meshB.triangles.Length];

		for (int triId=0; triId<newTriangles.Length; triId++)
		{
			newTriangles[triId] = meshB.triangles[triId] + currentVerticesCount;
		}
		meshA.subMeshCount = Mathf.Max(meshA.subMeshCount,subMeshID+1);
		meshA.SetTriangles(newTriangles,subMeshID);
		//meshA.vertices.ad
	}
}
