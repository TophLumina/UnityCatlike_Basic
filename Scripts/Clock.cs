using System;
using UnityEngine;

public class Clock : MonoBehaviour
{
    const float hours2deg = -360 / 12;
    const float minutes2deg = -360 / 60;
    const float second2deg = -360 / 60;

    [SerializeField]
    private Transform hoursPivot, minutesPivot, secondsPivot;

    private void Update() {
        TimeSpan time = DateTime.Now.TimeOfDay;
        hoursPivot.localRotation = Quaternion.Euler(0, 0, (float)time.TotalHours * hours2deg);
        minutesPivot.localRotation = Quaternion.Euler(0, 0, (float)time.TotalMinutes * minutes2deg);
        secondsPivot.localRotation = Quaternion.Euler(0, 0, (float)time.TotalSeconds * second2deg);
    }
}