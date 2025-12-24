using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
public class CameraFocusManager : MonoBehaviour
{
    public static CameraFocusManager instance;
    [Header("Focus")]
    private List<Units> focusUnits = new List<Units>();
    [Header("Camera Settings")]
    private float distanceBack = 15f;
    private float heightUp = 30f;
    private float moveDuration = 1f;
    private float focusHoldTime = 0.5f;
    private Renderer lastHitRenderer = null;
    private Color originalColor;
    private int originalRenderQueue;
    private int originalZWrite = 1;
    private float originalSurface = 0f;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    private void Update()
    {
        HandleTransparency();
    }
    public void FocusOnUnit(Units unit)
    {
        focusUnits.Clear();
        if (unit != null)
            focusUnits.Add(unit);
        StopAllCoroutines();
        StartCoroutine(CameraAnimation());
    }
    public void FocusOnUnits(Units a, Units b)
    {
        focusUnits.Clear();
        if (a != null) focusUnits.Add(a);
        if (b != null) focusUnits.Add(b);
        StopAllCoroutines();
        StartCoroutine(CameraAnimation());
    }
    private IEnumerator CameraAnimation()
    {
        CameraManager.instance.GetCanTransposed = false;
        yield return new WaitUntil(() => focusUnits.Count > 0);
        Vector3 initialPosition = transform.position;
        Quaternion initRotation = transform.rotation;
        Vector3 targetPosition;
        Quaternion targetRotation;
        yield return new WaitForSeconds(1f);
        Vector3 center = GetFocusCenter();
        targetPosition = center - Vector3.forward * distanceBack + Vector3.up * heightUp;
        targetRotation = Quaternion.LookRotation(center - targetPosition);
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("CameraZoom"),.3f,false);
        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            transform.position = Vector3.Lerp(initialPosition, targetPosition, elapsed / moveDuration);
            transform.rotation = Quaternion.Slerp(initRotation, targetRotation, elapsed / moveDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPosition;
        transform.rotation = targetRotation;
        yield return new WaitForSeconds(focusHoldTime);
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("CameraZoom"), .3f, false);
        elapsed = 0f;
        while (elapsed < moveDuration)
        {
            transform.position = Vector3.Lerp(targetPosition, initialPosition, elapsed / moveDuration);
            transform.rotation = Quaternion.Slerp(targetRotation, initRotation, elapsed / moveDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = initialPosition;
        transform.rotation = initRotation;
        focusUnits.Clear();
        CameraManager.instance.GetCanTransposed = true;
    }
    private void HandleTransparency()
    {
        if (focusUnits.Count == 0) return;
        Vector3 center = GetFocusCenter();
        Vector3 dir = (center - Camera.main.transform.position).normalized;
        float distance = 15;
        Ray ray = new Ray(Camera.main.transform.position, dir);
        if (Physics.Raycast(ray, out RaycastHit hit, distance))
        {
            Renderer renderer = hit.collider.GetComponent<Renderer>();
            if (renderer == null) return;
            Material material = renderer.material;
            if (renderer != lastHitRenderer)
            {
                if (lastHitRenderer != null)
                    RestoreOriginalMaterial(lastHitRenderer);
                CacheOriginalMaterial(material);
            }
            SetMaterialTransparent(material);
            lastHitRenderer = renderer;
        }
        else
        {
            if (lastHitRenderer != null)
            {
                RestoreOriginalMaterial(lastHitRenderer);
                lastHitRenderer = null;
            }
        }
    }
    private void CacheOriginalMaterial(Material material)
    {
        originalColor = material.color;
        originalRenderQueue = material.renderQueue;
        if (material.HasProperty("_ZWrite"))
            originalZWrite = material.GetInt("_ZWrite");

        if (material.HasProperty("_Surface"))
            originalSurface = material.GetFloat("_Surface");
    }
    private void SetMaterialTransparent(Material material)
    {
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f); // Transparent

        if (material.HasProperty("_ZWrite"))
            material.SetInt("_ZWrite", 0);
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.renderQueue = (int)RenderQueue.Transparent;
        Color c = material.color;
        c.a = 0.2f;
        material.color = c;
    }
    private void RestoreOriginalMaterial(Renderer renderer)
    {
        Material material = renderer.material;
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", originalSurface);
        if (material.HasProperty("_ZWrite"))
            material.SetInt("_ZWrite", originalZWrite);
        material.SetInt("_SrcBlend", (int)BlendMode.One);
        material.SetInt("_DstBlend", (int)BlendMode.Zero);
        material.color = originalColor;
        material.renderQueue = originalRenderQueue;
    }
    private Vector3 GetFocusCenter()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (var unit in focusUnits)
        {
            if (unit != null)
            {
                sum += unit.transform.position;
                count++;
            }
        }
        return count > 0 ? sum / count : Vector3.zero;
    }
}
