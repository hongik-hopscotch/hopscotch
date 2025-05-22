using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class UIResizer : MonoBehaviour
{
    private CanvasScaler canvasScaler;
    
    [Header("Reference Resolution")]
    [SerializeField] private float referenceWidth = 1920f;
    [SerializeField] private float referenceHeight = 1080f;
    
    [Header("Safe Area Margins")]
    [SerializeField] private float minMarginTop = 50f;
    [SerializeField] private float minMarginBottom = 50f;
    [SerializeField] private float minMarginLeft = 50f;
    [SerializeField] private float minMarginRight = 50f;

    private void Awake()
    {
        canvasScaler = GetComponent<CanvasScaler>();
        if (canvasScaler == null)
        {
            Debug.LogError("CanvasScaler component is required!");
            return;
        }

        SetupCanvasScaler();
        AdjustToSafeArea();
    }

    private void SetupCanvasScaler()
    {
        // 화면 비율 계산
        float screenRatio = Screen.width / (float)Screen.height;
        float targetRatio = referenceWidth / referenceHeight;

        // 모바일 디바이스 체크
        bool isMobile = Application.platform == RuntimePlatform.IPhonePlayer || 
                       Application.platform == RuntimePlatform.Android;

        // 캔버스 스케일러 설정
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(referenceWidth, referenceHeight);
        
        // 모바일의 경우 Match Width or Height 조정
        if (isMobile)
        {
            if (screenRatio > targetRatio)
            {
                // 더 넓은 화면
                canvasScaler.matchWidthOrHeight = 1f; // Height에 맞춤
            }
            else
            {
                // 더 긴 화면
                canvasScaler.matchWidthOrHeight = 0f; // Width에 맞춤
            }
        }
        else
        {
            // PC의 경우 0.5로 설정하여 중간값 사용
            canvasScaler.matchWidthOrHeight = 0.5f;
        }
    }

    private void AdjustToSafeArea()
    {
        Rect safeArea = Screen.safeArea;
        RectTransform canvasRect = GetComponent<RectTransform>();

        // 안전 영역이 전체 화면과 다른 경우 (노치, 홈 바 등이 있는 경우)
        if (safeArea.x != 0 || safeArea.y != 0 || 
            safeArea.width != Screen.width || safeArea.height != Screen.height)
        {
            // 상단 여백
            float topMargin = Screen.height - (safeArea.y + safeArea.height);
            if (topMargin > 0)
            {
                RectTransform[] allRects = GetComponentsInChildren<RectTransform>();
                foreach (RectTransform rect in allRects)
                {
                    // 상단에 있는 UI 요소들의 위치 조정
                    if (rect.anchoredPosition.y > canvasRect.rect.height * 0.8f)
                    {
                        Vector2 position = rect.anchoredPosition;
                        position.y -= Mathf.Max(topMargin, minMarginTop);
                        rect.anchoredPosition = position;
                    }
                }
            }

            // 하단 여백
            float bottomMargin = safeArea.y;
            if (bottomMargin > 0)
            {
                RectTransform[] allRects = GetComponentsInChildren<RectTransform>();
                foreach (RectTransform rect in allRects)
                {
                    // 하단에 있는 UI 요소들의 위치 조정
                    if (rect.anchoredPosition.y < -canvasRect.rect.height * 0.8f)
                    {
                        Vector2 position = rect.anchoredPosition;
                        position.y += Mathf.Max(bottomMargin, minMarginBottom);
                        rect.anchoredPosition = position;
                    }
                }
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (canvasScaler == null)
            canvasScaler = GetComponent<CanvasScaler>();
        
        SetupCanvasScaler();
    }
#endif
} 