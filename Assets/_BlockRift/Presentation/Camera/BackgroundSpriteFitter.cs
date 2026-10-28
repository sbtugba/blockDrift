using UnityEngine;

namespace BlockRift.Presentation
{
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer)), DefaultExecutionOrder(10000)]
    public sealed class BackgroundSpriteFitter : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private MeshRenderer backgroundRenderer;
        [SerializeField] private Texture2D backgroundTexture;
        [SerializeField, Min(0.1f)] private float distanceFromCamera = 40f;

        private bool hasLoggedConfigurationError;

        private void Awake()
        {
            if (!backgroundRenderer)
                backgroundRenderer = GetComponent<MeshRenderer>();

            if (!targetCamera)
                targetCamera = Camera.main;
        }

        private void OnEnable() => FitToCamera();

        private void LateUpdate() => FitToCamera();

        private void FitToCamera()
        {
            if (!targetCamera)
            {
                LogConfigurationError("BackgroundSpriteFitter has no gameplay camera assigned.");
                return;
            }

            if (!backgroundRenderer || !backgroundTexture)
            {
                LogConfigurationError("BackgroundSpriteFitter has no background renderer or texture assigned.");
                return;
            }

            hasLoggedConfigurationError = false;
            float depth = Mathf.Clamp(distanceFromCamera, targetCamera.nearClipPlane + 0.01f,
                targetCamera.farClipPlane - 0.01f);
            transform.position = targetCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, depth));
            transform.rotation = targetCamera.transform.rotation;

            float textureAspect = (float)backgroundTexture.width / backgroundTexture.height;
            // Görüntü penceresi kaydırıldığında da aynı arka plan görseli tam ekran konumunu korur.
            float viewHeight = Vector3.Distance(targetCamera.ViewportToWorldPoint(new Vector3(.5f,0,depth)),
                targetCamera.ViewportToWorldPoint(new Vector3(.5f,1,depth)));
            float viewWidth = Vector3.Distance(targetCamera.ViewportToWorldPoint(new Vector3(0,.5f,depth)),
                targetCamera.ViewportToWorldPoint(new Vector3(1,.5f,depth)));
            // Küçük overscan, perspective matris hassasiyeti/texture filtrelemesinin kenarda çizgi açmasını önler.
            float height = Mathf.Max(viewHeight, viewWidth / textureAspect) * 1.002f;
            transform.localScale = new Vector3(height * textureAspect, height, 1f);
        }

        private void LogConfigurationError(string message)
        {
            if (hasLoggedConfigurationError)
                return;

            Debug.LogError(message, this);
            hasLoggedConfigurationError = true;
        }
    }
}
