// Master.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class Master : MonoBehaviour
{
    public static Master I;

    [System.Serializable]
    public class GateOption
    {
        public string gateTag;      
        public GameObject hoverBox;  
        public GameObject gatePrefab; 
    }

    [Header("Camera")]
    public Camera mainCam;
    public float rayDistance = 500f;

    [Header("Mouse Raycast Mask (EXCLUDE GateTrigger layer)")]
    public LayerMask mouseMask;

    [Header("Gate Options (3)")]
    public GateOption[] options;

    [Header("Levels")]
    public GameObject level1;
    public GameObject level2;

    [Header("PlaceSpot Hover")]
    public Color placeSpotHoverColor = Color.green;

    // remembers what to enable after reload
    static int nextLevelIndex = 1;

    Transform currentSpot;
    Transform currentTarget;

    // one placed gate per PlaceSpot
    Dictionary<Transform, GameObject> placedGates = new Dictionary<Transform, GameObject>();

    // hover state
    Renderer hoveredR;
    MaterialPropertyBlock mpb;
    Color hoveredDefaultColor;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;

        if (!mainCam) mainCam = Camera.main;
        mpb = new MaterialPropertyBlock();

        HideAll();
        ApplyLevelActiveState();
    }

    void Update()
    {
        UpdatePlaceSpotHover();

        if (Input.GetMouseButtonDown(0))
            HandleClick();
    }

    public void ToggleLevelAndReload()
    {
        nextLevelIndex = (nextLevelIndex == 1) ? 2 : 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void ApplyLevelActiveState()
    {
        if (nextLevelIndex == 1)
        {
            if (level1) level1.SetActive(true);
            if (level2) level2.SetActive(false);
        }
        else
        {
            if (level1) level1.SetActive(false);
            if (level2) level2.SetActive(true);
        }
    }

    void HandleClick()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, mouseMask)) return;

        if (hit.collider.CompareTag("PlaceSpot"))
        {
            currentSpot = hit.collider.transform;
            currentTarget = FindTarget(currentSpot);

            if (placedGates.TryGetValue(currentSpot, out GameObject oldGate))
            {
                Destroy(oldGate);
                placedGates.Remove(currentSpot);
            }

            ShowAll();
            return;
        }

        if (currentSpot != null && currentTarget != null)
        {
            for (int i = 0; i < options.Length; i++)
            {
                GameObject box = options[i].hoverBox;
                if (!box) continue;

                if (hit.collider.gameObject != box &&
                    !hit.collider.transform.IsChildOf(box.transform))
                    continue;

                PlaceGate(options[i]);
                HideAll();
                currentSpot = null;
                currentTarget = null;
                return;
            }
        }

        HideAll();
        currentSpot = null;
        currentTarget = null;
    }

    void PlaceGate(GateOption opt)
    {
        if (opt.gatePrefab == null || currentSpot == null || currentTarget == null) return;

        if (placedGates.TryGetValue(currentSpot, out GameObject oldGate))
        {
            Destroy(oldGate);
            placedGates.Remove(currentSpot);
        }

        Quaternion rot = currentTarget.rotation * Quaternion.Euler(0f, 180f, 0f);
        GameObject newGate = Instantiate(opt.gatePrefab, currentTarget.position, rot);

        placedGates[currentSpot] = newGate;
    }

    Transform FindTarget(Transform spot)
    {
        Transform t = spot.Find("TARGET");
        if (!t) Debug.LogWarning($"PlaceSpot '{spot.name}' has no child named TARGET");
        return t;
    }

    // ---------------- Hover ----------------
    void UpdatePlaceSpotHover()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, rayDistance, mouseMask);
        bool hitPlaceSpot = hitSomething && hit.collider.CompareTag("PlaceSpot");

        if (hitPlaceSpot)
        {
            Renderer r =
                hit.collider.GetComponentInChildren<Renderer>() ??
                hit.collider.GetComponentInParent<Renderer>() ??
                hit.collider.GetComponent<Renderer>();

            if (r != hoveredR)
            {
                ClearHover();

                if (r != null)
                {
                    hoveredR = r;
                    hoveredDefaultColor = GetRendererColor(hoveredR);
                    SetRendererColor(hoveredR, placeSpotHoverColor);
                }
            }
            else if (hoveredR != null)
            {
                SetRendererColor(hoveredR, placeSpotHoverColor);
            }
        }
        else
        {
            ClearHover();
        }
    }

    void ClearHover()
    {
        if (hoveredR != null)
        {
            SetRendererColor(hoveredR, hoveredDefaultColor);
            hoveredR = null;
        }
    }

    // ---------------- UI ----------------
    void ShowAll()
    {
        for (int i = 0; i < options.Length; i++)
            if (options[i].hoverBox) options[i].hoverBox.SetActive(true);
    }

    void HideAll()
    {
        for (int i = 0; i < options.Length; i++)
            if (options[i].hoverBox) options[i].hoverBox.SetActive(false);
    }

    // ---------------- Renderer helpers ----------------
    Color GetRendererColor(Renderer r)
    {
        r.GetPropertyBlock(mpb);

        if (mpb.HasColor("_BaseColor")) return mpb.GetColor("_BaseColor");
        if (mpb.HasColor("_Color")) return mpb.GetColor("_Color");

        if (r.sharedMaterial != null)
        {
            if (r.sharedMaterial.HasProperty("_BaseColor")) return r.sharedMaterial.GetColor("_BaseColor");
            if (r.sharedMaterial.HasProperty("_Color")) return r.sharedMaterial.GetColor("_Color");
        }

        return Color.white;
    }

    void SetRendererColor(Renderer r, Color c)
    {
        r.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", c);
        mpb.SetColor("_Color", c);
        r.SetPropertyBlock(mpb);
    }
}
