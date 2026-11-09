using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Expecto.Utils
{
	public class AlphaHelper : MonoBehaviour
	{
		[SerializeField] private SpriteRenderer[] spriteRenderers;
		[SerializeField] private TextMeshPro[] textMeshPros;
		[Range(0f, 1f)] [SerializeField] private float globalAlpha = 0f;

		private float[] originalSpriteAlphas;
		private float[] originalTextAlphas;
		private bool isFollowing;
		private bool isInitialized;

		public float GlobalAlpha
		{
			get => globalAlpha;
			set => SetGlobalAlpha(value);
		}

		public bool IsFollowing => isFollowing;
		public SpriteRenderer[] SpriteRenderers => spriteRenderers;
		public TextMeshPro[] TextMeshPros => textMeshPros;

		private void Awake()
		{
			InitializeAndCache();
			globalAlpha = 0f;
			ApplyAlpha(globalAlpha);
		}

		private void LateUpdate()
		{
			if (isFollowing)
			{
				ApplyAlpha(globalAlpha);
			}
		}

#if UNITY_EDITOR
		[ContextMenu("Find All Renderers")]
		public void FindAllRenderers()
		{
			Undo.RecordObject(this, "Find All Renderers");
			spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
			textMeshPros = GetComponentsInChildren<TextMeshPro>(true);

			EditorUtility.SetDirty(this);
		}
#endif

		[ContextMenu("Start Follow")]
		public void StartFollow()
		{
			if (!isInitialized)
			{
				InitializeAndCache();
			}

			isFollowing = true;
			ApplyAlpha(globalAlpha);
		}

		[ContextMenu("Stop Follow")]
		public void StopFollow()
		{
			isFollowing = false;
		}

		public void SetGlobalAlpha(float alpha)
		{
			globalAlpha = Mathf.Clamp01(alpha);
			if (isFollowing)
			{
				ApplyAlpha(globalAlpha);
			}
		}

		public void CacheOriginalAlphas()
		{
			InitializeAndCache();
		}

		private void InitializeAndCache()
		{
			int spriteCount = spriteRenderers != null ? spriteRenderers.Length : 0;
			originalSpriteAlphas = new float[spriteCount];
			for (int i = 0; i < spriteCount; i++)
			{
				if (spriteRenderers[i] != null)
				{
					originalSpriteAlphas[i] = spriteRenderers[i].color.a;
				}
			}

			int textCount = textMeshPros != null ? textMeshPros.Length : 0;
			originalTextAlphas = new float[textCount];
			for (int i = 0; i < textCount; i++)
			{
				if (textMeshPros[i] != null)
				{
					originalTextAlphas[i] = textMeshPros[i].color.a;
				}
			}

			isInitialized = true;
		}

		private void ApplyAlpha(float alphaRatio)
		{
			if (!isInitialized)
			{
				return;
			}

			if (spriteRenderers != null && originalSpriteAlphas != null)
			{
				int count = Mathf.Min(spriteRenderers.Length, originalSpriteAlphas.Length);
				for (int i = 0; i < count; i++)
				{
					SpriteRenderer sr = spriteRenderers[i];
					if (sr == null)
					{
						continue;
					}

					Color c = sr.color;
					c.a = originalSpriteAlphas[i] * alphaRatio;
					sr.color = c;
				}
			}

			if (textMeshPros != null && originalTextAlphas != null)
			{
				int count = Mathf.Min(textMeshPros.Length, originalTextAlphas.Length);
				for (int i = 0; i < count; i++)
				{
					TextMeshPro tmp = textMeshPros[i];
					if (tmp == null)
					{
						continue;
					}

					Color c = tmp.color;
					c.a = originalTextAlphas[i] * alphaRatio;
					tmp.color = c;
				}
			}
		}
	}
}
