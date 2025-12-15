using UnityEngine;

public class CardSelectable3D : MonoBehaviour
{
    [SerializeField] private bool isSelected;
    [SerializeField] private bool isHovering;

    [Header("Move")]
    [SerializeField] private Transform visual;
    [SerializeField] private float liftDistance = 0.2f;
    [SerializeField] private float moveSpeed = 12f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip selectSfx;
    [SerializeField] private AudioClip deselectSfx;

    private Vector3 startPos;
    private Vector3 targetPos;
    private Camera mainCamera;

    void Awake()
    {
        if (visual == null) visual = transform;
        startPos = visual.localPosition;
        targetPos = startPos;
        mainCamera = Camera.main;
    }

    void Update()
    {
        // 檢測滑鼠點擊
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    SetSelected(!isSelected);
                }
            }
        }

        visual.localPosition = Vector3.Lerp(visual.localPosition, targetPos, Time.deltaTime * moveSpeed);
    }

    public void Toggle()
    {
        SetSelected(!isSelected);
    }

    public void SetSelected(bool selected)
    {
        if (isSelected == selected) return;
        isSelected = selected;

        targetPos = isSelected ? startPos + Vector3.up * liftDistance : startPos;
        PlaySfx(isSelected ? selectSfx : deselectSfx);
    }

    void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        audioSource.PlayOneShot(clip);
    }
}
