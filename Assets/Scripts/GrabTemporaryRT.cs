using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class GrabTemporaryRT : MonoBehaviour
{
	private Camera m_camera;
	private Dictionary<CameraEvent, CommandBuffer> m_cameraCommandBuffers = new Dictionary<CameraEvent, CommandBuffer>();
	private RenderTexture m_rtGrab;
	private Material m_matGrab;
		
	void Awake()
	{
		this.m_camera = this.gameObject.GetComponent<Camera>();

		this.m_rtGrab = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
		this.m_rtGrab.filterMode = FilterMode.Bilinear;
		this.m_rtGrab.wrapMode = TextureWrapMode.Clamp;
		this.m_rtGrab.useMipMap = true;
		this.m_rtGrab.antiAliasing = QualitySettings.antiAliasing;
		this.m_rtGrab.Create();
			
		this.m_matGrab = new Material(Shader.Find("Custom/Grab"));
	}
		
	public void OnDisable()
	{
		foreach(KeyValuePair<CameraEvent, CommandBuffer> buffer in this.m_cameraCommandBuffers)
		{
			this.m_camera.RemoveCommandBuffer(buffer.Key, buffer.Value);
		}
	}
		
	//
	// Setting CameraCommandBuffers
	//
	public void OnPreRender()
	{
		bool _flag = this.gameObject.activeInHierarchy && this.enabled;
		if(!_flag)
		{
			this.OnDisable();
			return;
		}

		// Render Layer TemporaryRTObj
		CommandBuffer _bufA = null;
		CameraEvent _evA = CameraEvent.AfterForwardOpaque;

		if(this.m_cameraCommandBuffers.ContainsKey(_evA))
		{
			_bufA = this.m_cameraCommandBuffers[_evA];
			_bufA.Clear();
		}
		else
		{
			_bufA = new CommandBuffer();
			_bufA.name = "GrabTemporary";
			this.m_cameraCommandBuffers.Add(_evA, _bufA);
			this.m_camera.AddCommandBuffer(_evA, _bufA);
		}

		int _temporaryRTObjLayerIndex = LayerMask.NameToLayer("TemporaryRTObj");
		Transform[] _objs = GameObject.FindObjectsOfType<Transform>().Where(t => t.gameObject.layer == _temporaryRTObjLayerIndex).ToArray();
			
		int _temporaryRT = Shader.PropertyToID("_rt0");
		_bufA.GetTemporaryRT(_temporaryRT, this.m_rtGrab.width, this.m_rtGrab.height, this.m_rtGrab.depth, this.m_rtGrab.filterMode, this.m_rtGrab.format, RenderTextureReadWrite.Default, this.m_rtGrab.antiAliasing);
		_bufA.SetRenderTarget(_temporaryRT);
		_bufA.ClearRenderTarget(true, true, new Color(0f, 0f, 0f, 0f));

		foreach(Transform _obj in _objs)
		{
			MeshRenderer _meshRenderer = _obj.GetComponent<MeshRenderer>();
			MeshFilter _meshFilter = _obj.GetComponent<MeshFilter>();
			if(_meshFilter != null && _meshRenderer != null)
			{
				MaterialPropertyBlock _mpb = new MaterialPropertyBlock();
				for(int _i = 0; _i < _meshFilter.sharedMesh.subMeshCount; ++_i)
				{
					// Force the color change
					_mpb.SetColor("_Color", Color.blue);

					_bufA.DrawMesh(_meshFilter.sharedMesh, _obj.localToWorldMatrix, _meshRenderer.materials[_i], _i, 0, _mpb);
				}
			}
		}
		_bufA.SetGlobalTexture("_TemporaryRT", _temporaryRT);


		// Release TemporaryRT
		CommandBuffer _bufB = null;
		CameraEvent _evB = CameraEvent.AfterEverything;

		if(this.m_cameraCommandBuffers.ContainsKey(_evB))
		{
			_bufB = this.m_cameraCommandBuffers[_evB];
			_bufB.Clear();
		}
		else
		{
			_bufB = new CommandBuffer();
			_bufB.name = "ReleaseTemporary";
			this.m_cameraCommandBuffers.Add(_evB, _bufB);
			this.m_camera.AddCommandBuffer(_evB, _bufB);
		}
		_bufB.ReleaseTemporaryRT(_temporaryRT);
	}

	// 
	// TemporaryRT to RenderTexture
	//
	void OnPostRender()
	{
		RenderTexture _base = RenderTexture.active;

		RenderTexture.active = this.m_rtGrab;
		GL.Clear(true, true, Color.clear);
		Graphics.Blit(null, this.m_rtGrab, this.m_matGrab, 0);
			
		RenderTexture.active = _base;
	}

	// 
	// Render RenderTexture to CameraBuffer
	//
	void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		Graphics.Blit(this.m_rtGrab, destination, this.m_matGrab, 1);
	}
}
