using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
public class CameraFocusManager : MonoBehaviour
{
    public List<Transform> points;
    private Renderer lastHitRenderer = null;
    private Color originalColor;
    private int originalMode = 0;
    private int originalRenderQueue = -1;
    IEnumerator CameraAnimation()
    {
        yield return new WaitForSeconds(2);
        float duration = 3.0f;
        float elapsed = 0.0f;
        Vector3 initialPosition = this.transform.position;
        Vector3 targetPosition = Vector3.zero;
        Vector3 promedio = Vector3.zero;
        yield return new WaitUntil(() => points.Count > 0);
        foreach (Transform target in points)
        {
            promedio += target.position;
        }
        promedio /= points.Count;
        yield return new WaitForSeconds(1);
        targetPosition = promedio - Vector3.forward * 5 + Vector3.up * 3;
        while (elapsed < duration)
        {
            this.transform.position = Vector3.Lerp(initialPosition, targetPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        this.transform.position = targetPosition;
        yield return new WaitForSeconds(3);
        elapsed = 0.0f;
        while (elapsed < duration)
        {
            this.transform.position = Vector3.Lerp(targetPosition, initialPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        this.transform.position = initialPosition;
    }
    private void Update()
    {
        TrasnpartentMaterials();
    }
    public void TrasnpartentMaterials()
    {
        print("s");
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        Vector3 promedio = Vector3.zero;
        foreach (Transform target in points)
        {
            promedio += target.position;
        }
        promedio /= points.Count;
        var lenght = 3;
        if (Physics.Raycast(ray, out RaycastHit hit, lenght))
        {
            var renderer = hit.collider.GetComponent<Renderer>();
            if (renderer == null) return;
            var material = renderer.material;
            if (renderer != lastHitRenderer)
            {
                originalColor = material.color;
                originalMode = (int)material.GetFloat("_Mode");
                originalRenderQueue = material.renderQueue;
            }
            material.SetInt("_Mode", 2);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            Color color = material.color;
            color.a = 0.2f;
            material.color = color;
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
    private void RestoreOriginalMaterial(Renderer rendererToRestore)
    {
        var material = rendererToRestore.material;
        material.SetInt("_Mode", originalMode);
        if (originalMode == 0)
        {
            material.SetInt("_ZWrite", 1);
            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.Zero);
            material.DisableKeyword("_ALPHABLEND_ON");
            if (originalMode == 0) // Opaco
            {
                material.DisableKeyword("_ALPHATEST_ON");
            }
        }
        material.color = originalColor;
        material.renderQueue = originalRenderQueue;
    }
}
