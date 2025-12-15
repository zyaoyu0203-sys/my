using UnityEngine;

public class CardClickRaycaster : MonoBehaviour
{
    [SerializeField] private Camera cam;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                var card = hit.collider.GetComponent<CardSelectable3D>();
                if (card != null) card.Toggle();
            }
        }
    }
}