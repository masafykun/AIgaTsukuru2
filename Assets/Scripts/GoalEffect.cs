using UnityEngine;
using System.Collections;

public class GoalEffect : MonoBehaviour
{
    public ParticleSystem particles;
    public Light flashLight;

    public void Play()
    {
        if (particles) { particles.Stop(); particles.Clear(); particles.Play(); }
        if (flashLight) StartCoroutine(Flash());
    }

    IEnumerator Flash()
    {
        float duration = 2.5f;
        float elapsed  = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            flashLight.intensity = Mathf.Lerp(10f, 0f, t) * (0.5f + 0.5f * Mathf.Sin(elapsed * 18f));
            yield return null;
        }
        flashLight.intensity = 0f;
    }
}
