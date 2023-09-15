using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;

using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;
using Random = UnityEngine.Random;

public class Fractal : MonoBehaviour
{
    [SerializeField, Range(1, 16)]
    int depth = 4;

    [SerializeField]
    Mesh branchmesh, leafmesh;

    [SerializeField]
    Material mat;

    [SerializeField]
    Gradient gradientLevel, gradientBasis;

    [SerializeField]
    Color leafColorA, leafColorB;

    static quaternion[] rotations = {
        quaternion.identity, quaternion.RotateZ(-0.5f * PI), quaternion.RotateZ(0.5f * PI), quaternion.RotateX(0.5f * PI), quaternion.RotateX(-0.5f * PI)
    };

    struct FractalPart
    {
        public float3 worldPos;
        public quaternion rotation, worldRot;
        public float spinangle;
    }

    NativeArray<FractalPart>[] parts;
    NativeArray<float3x4>[] matrices;
    ComputeBuffer[] matricebuffers;
    Vector4[] sequenceNums; // data sent to GPU can't be less than 4 * float

    static readonly int matricesId = Shader.PropertyToID("_Matrices"),
    sequenceNumId = Shader.PropertyToID("_SequenceNumbers"),
    levelColorId = Shader.PropertyToID("_LevelColor"),
    BasisColorId = Shader.PropertyToID("_BasisColor");

    static MaterialPropertyBlock propertyBlock;

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)] // CompileSynchronously tells unity to always use BurstCompiler in editor mode
    // FloatPresion.Standard and FloatMode.Fast used for better optimizing the caculations, usually there is no reason not to do so
    struct UpdateFracLevelPartsJob : IJobFor
    {
        public float scale;
        public float spinAngleDelta;

        [ReadOnly]
        public NativeArray<FractalPart> parents;
        public NativeArray<FractalPart> current;

        [WriteOnly]
        public NativeArray<float3x4> currentmatrices;
        public void Execute(int loop)
        {

            FractalPart parentpart = parents[loop / 5];
            FractalPart part = current[loop];
            part.spinangle += spinAngleDelta;

            float3 localUp = mul(mul(parentpart.worldRot, part.rotation), up());
            float3 sagAxis = cross(up(), localUp);

            float sagMagnitude = length(sagAxis);
            quaternion baseRotation;
            if (sagMagnitude > 0f) // when local up is (0,1,0) cross produce a zero
            {
                sagAxis /= sagMagnitude;
                quaternion sagRotation = quaternion.AxisAngle(sagAxis, PI * 0.25f * sagMagnitude);
                baseRotation = mul(sagRotation, parentpart.worldRot);
            }
            else
            {
                baseRotation = parentpart.worldRot;
            }

            part.worldRot = mul(baseRotation, mul(part.rotation, quaternion.RotateY(part.spinangle)));
            part.worldPos = parentpart.worldPos + mul(part.worldRot, float3(0f, 1.5f * scale, 0f));
            // Funcs of matrices operations in Unity.Mathmatics are similar to the ones in GLSL (personaly speaking)
            current[loop] = part;
            float3x3 r = float3x3(part.worldRot) * scale;
            currentmatrices[loop] = float3x4(r.c0, r.c1, r.c2, part.worldPos); // building transaltion and rotation matrix
        }
    }

    private void OnEnable()
    {
        parts = new NativeArray<FractalPart>[depth];
        matrices = new NativeArray<float3x4>[depth];
        matricebuffers = new ComputeBuffer[depth];
        sequenceNums = new Vector4[depth];
        // int stride = 4 * 16; // float3x4 need 4 * 16 byte and its also the size of each storing unit in GPU
        int stride = 4 * 12; // the last row of our Matrices should always be 0,0,0,1 which means there is no necessary to send them to GPU

        for (int i = 0, len = 1; i < parts.Length; ++i, len *= 5)
        {
            parts[i] = new NativeArray<FractalPart>(len, Allocator.Persistent);
            matrices[i] = new NativeArray<float3x4>(len, Allocator.Persistent);
            matricebuffers[i] = new ComputeBuffer(len, stride);
            sequenceNums[i] = new Vector4(Random.value, Random.value, Random.value, Random.value);
        }

        parts[0][0] = CreatePart(0);
        for (int i = 1; i < parts.Length; ++i)
        {
            NativeArray<FractalPart> levelParts = parts[i];
            for (int j = 0; j < levelParts.Length; j += 5)
                for (int ci = 0; ci < 5; ++ci)
                    levelParts[j + ci] = CreatePart(ci);
        }

        propertyBlock ??= new MaterialPropertyBlock();
    }

    private void OnDisable()
    {
        for (int i = 0; i < matricebuffers.Length; ++i)
        {
            matricebuffers[i].Release();
            parts[i].Dispose();
            matrices[i].Dispose();
        }
        parts = null;
        matrices = null;
        matricebuffers = null;
        sequenceNums = null;
    }

    private void OnValidate() // invoke after changes have been made in inspector
    {
        if (parts != null && enabled)
        {
            OnDisable();
            OnEnable();
        }
    }

    private void Update()
    {
        float spinangledelta = 0.125f * PI * Time.deltaTime;
        FractalPart root = parts[0][0];
        root.spinangle += spinangledelta;
        // GameObject Bindings
        root.worldPos = transform.position;
        root.worldRot = mul(transform.rotation, mul(root.rotation, quaternion.RotateY(root.spinangle)));

        float objectScale = transform.lossyScale.x;
        parts[0][0] = root;

        float3x3 r = float3x3(root.worldRot) * objectScale;
        matrices[0][0] = float3x4(r.c0, r.c1, r.c2, root.worldPos);

        float scale = transform.lossyScale.x; // GPUInstancing requires a uniform scale

        JobHandle jobHandle = default;
        for (int i = 1; i < parts.Length; ++i)
        {
            scale *= 0.5f;

            // Job Schedule in a Sequence
            jobHandle = new UpdateFracLevelPartsJob
            {
                scale = scale,
                spinAngleDelta = spinangledelta,

                parents = parts[i - 1],
                current = parts[i],
                currentmatrices = matrices[i]
            }.ScheduleParallel(parts[i].Length, 5/*This is batch count, basicly if Execute() do a lot, make it small. otherwise, make it big*/, jobHandle);
            // This will put parts[i].Length times of Execute() function in schedule and in sequence by levels of the fractal
        }
        // Executing all scheduled jobs
        jobHandle.Complete();

        // drawing
        var bounds = new Bounds(root.worldPos, 3f * scale * Vector3.one);
        int leafLevelIndex = matricebuffers.Length - 1;

        for (int i = 0; i < matricebuffers.Length; ++i)
        {
            ComputeBuffer buffer = matricebuffers[i];
            buffer.SetData(matrices[i]); // now transfering into GPU

            if (!mat.enableInstancing)
            {
                mat.enableInstancing = true;
            }

            propertyBlock.SetBuffer(matricesId, buffer);
            propertyBlock.SetVector(sequenceNumId, sequenceNums[i]);

            Color colorA, colorB;
            Mesh instanceMesh;
            if (i == leafLevelIndex)
            {
                colorA = leafColorA;
                colorB = leafColorB;
                instanceMesh = leafmesh;
            }
            else
            {
                float gradientInterpolator = i / (matricebuffers.Length - 2f + math.EPSILON); // except the leaf level
                colorA = gradientLevel.Evaluate(gradientInterpolator);
                colorB = gradientBasis.Evaluate(gradientInterpolator);
                instanceMesh = branchmesh;
            }
            propertyBlock.SetColor(levelColorId, colorA);
            propertyBlock.SetColor(BasisColorId, colorB);


            mat.SetBuffer(matricesId, buffer); // binding buffer (computebuffer for storing matrices)
            Graphics.DrawMeshInstancedProcedural(instanceMesh, 0, mat, bounds, buffer.count, propertyBlock);
        }
    }

    FractalPart CreatePart(int childindex) => new FractalPart
    {
        rotation = rotations[childindex]
    };
}
