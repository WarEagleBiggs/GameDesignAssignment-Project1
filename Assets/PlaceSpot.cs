using UnityEngine;

public class PlaceSpot : MonoBehaviour
{
    public enum GateType
    {
        NOT,
        AND,
        OR
    }

    public GateType correctGate;
    public bool supportsAndOr;
    public bool correct;

    public BoxCollider detectBox;
    public LayerMask gateMask = ~0;

    public GameObject[] toColor;
    public Material correctMaterial;

    bool applied;

    void Awake()
    {
        if (!detectBox) detectBox = GetComponent<BoxCollider>();
    }

    void Update()
    {
        if (!detectBox) return;

        GameObject gate = FindGateInside();

        if (gate == null)
        {
            correct = false;
            applied = false;
            return;
        }

        if (supportsAndOr)
        {
            correct = gate.CompareTag("AND") || gate.CompareTag("OR");
        }
        else
        {
            correct = gate.CompareTag(correctGate.ToString());
        }

        if (correct && !applied)
        {
            ApplyMaterial();
            applied = true;
        }

        if (!correct)
            applied = false;
    }

    GameObject FindGateInside()
    {
        Vector3 center = detectBox.bounds.center;
        Vector3 halfExtents = detectBox.bounds.extents;

        Collider[] hits = Physics.OverlapBox(center, halfExtents, detectBox.transform.rotation, gateMask);

        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i]) continue;
            if (hits[i].transform == transform) continue;
            if (hits[i].transform.IsChildOf(transform)) continue;

            if (hits[i].CompareTag("NOT") ||
                hits[i].CompareTag("AND") ||
                hits[i].CompareTag("OR"))
                return hits[i].gameObject;
        }

        return null;
    }

    void ApplyMaterial()
    {
        if (!correctMaterial) return;

        for (int i = 0; i < toColor.Length; i++)
        {
            if (!toColor[i]) continue;

            Renderer r = toColor[i].GetComponent<Renderer>();
            if (r) r.material = correctMaterial;
        }
    }

    void OnDrawGizmosSelected()
    {
        BoxCollider box = detectBox ? detectBox : GetComponent<BoxCollider>();
        if (!box) return;

        Gizmos.matrix = Matrix4x4.TRS(box.bounds.center, box.transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, box.bounds.size);
    }
}
