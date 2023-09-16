using System;
using UnityEngine;

public class GraphVisualizationGPU : MonoBehaviour
{

    const int max_resolution = 2000;

    [SerializeField, Range(10, max_resolution)]
    int resolution = 10;

    [SerializeField]
    FunctionLib.FuncType func, shift_func;

    public enum Shifting_Mode { Cycle, Random };

    [SerializeField]
    Shifting_Mode shifting_mod = Shifting_Mode.Cycle;

    [SerializeField, Min(0f)]
    float func_duration = 3f, shifting_duration = 2f;

    [SerializeField]
    ComputeShader computeShader;

    [SerializeField]
    Material mat;

    [SerializeField]
    Mesh mesh;

    float duration = float.Epsilon;

    bool shifting;

    ComputeBuffer positionsBuffer;

// Shader Properties
    static readonly int resolutionId = Shader.PropertyToID("_Resolution");
    static readonly int positionId = Shader.PropertyToID("_Positions");
    static readonly int stepId = Shader.PropertyToID("_Step");
    static readonly int timeId = Shader.PropertyToID("_Time");
    static readonly int shiftingprogressId = Shader.PropertyToID("_ShiftingProgress");

    private void OnEnable() {
        positionsBuffer = new ComputeBuffer(max_resolution * max_resolution, 3 * sizeof(float));
    }

    private void OnDisable() {
        positionsBuffer.Release();
        positionsBuffer = null;
    }

    private void UpdateGPU() {
        float step = 2f / resolution;
        
        computeShader.SetInt(resolutionId, resolution);
        computeShader.SetFloat(stepId, step);
        computeShader.SetFloat(timeId, Time.time);
        if (shifting)
        {
            computeShader.SetFloat(shiftingprogressId, Mathf.SmoothStep(0f, 1f, duration / shifting_duration));
        }

        var kernel_index = (int)func + (int)(shifting ? shift_func : func) * FunctionLib.FuncCount;
        computeShader.SetBuffer(kernel_index, positionId, positionsBuffer);

        int patchs = Mathf.CeilToInt(resolution / 8f);
        computeShader.Dispatch(kernel_index, patchs, patchs, 1);

        mat.SetBuffer(positionId, positionsBuffer);
        mat.SetFloat(stepId, step);

        var bound = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / resolution));
        Graphics.DrawMeshInstancedProcedural(mesh, 0, mat, bound, resolution * resolution);
    }

    private void Update()
    {
        duration += Time.deltaTime;

        if (shifting)
        {
            if (duration > shifting_duration)
            {
                duration -= shifting_duration;
                shifting = false;
            }
        }

        if (duration > func_duration)
        {
            duration -= func_duration;
            shifting = true;

            shift_func = func;

            switch (shifting_mod)
            {
                case Shifting_Mode.Cycle:
                    func = FunctionLib.ServeNextFunc(func);
                    break;
                case Shifting_Mode.Random:
                    func = FunctionLib.ServeRandFuncExcept(func);
                    break;
            }
        }

        UpdateGPU();
    }
}
