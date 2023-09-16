using UnityEngine;

using TMPro;

public class FPSCounter : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI Display;

    [SerializeField, Range(0f, 4f)]
    float SamplingDuration = 1f;

    public enum DisplayMod { MS, FPS };

    [SerializeField]
    DisplayMod Mod = DisplayMod.FPS;

    int frames = 0;
    float duration = float.Epsilon;
    float best_duration = float.MaxValue;
    float worst_duration = 0f;

    private void Update() {
        float frameduration = Time.unscaledDeltaTime;
        
        ++frames;
        duration += frameduration;

        best_duration = frameduration < best_duration ? frameduration : best_duration;
        worst_duration = frameduration > worst_duration ? frameduration : worst_duration;

        if (duration > SamplingDuration) {
            switch (Mod)
            {
                case DisplayMod.MS:
                    Display.SetText("MS\n{0:1}\n{1:1}\n{2:1}", 1000f * best_duration, 1000f * frameduration, 1000f * worst_duration);
                    break;
                case DisplayMod.FPS:
                    Display.SetText("FPS\n{0:1}\n{1:1}\n{2:1}", 1f / best_duration, frames / duration, 1f / worst_duration);
                    break;
            }
            frames = 0;
            duration -= SamplingDuration;
        }
    }
}
