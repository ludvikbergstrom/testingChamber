using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class SurfaceSkinner
{
    private readonly Transform bodyTransform;
    private readonly MeshFilter meshFilter;
    private readonly MeshRenderer meshRenderer;

    private readonly List<ShapeMatchingParticle> particles;
    private readonly List<ShapeMatchingCluster> clusters;

    private Mesh runtimeMesh;

    private Vector3[] restVertices;
    private Vector3[] deformedVertices;

    // Which clusters influence each particle?
    private List<int>[] particleClusters;

    // Current averaged orientation for every particle.
    private quaternion[] particleOrientations;

    private VertexSkinData[] skinData;

    private bool recalculateNormals;

    private struct VertexSkinData
    {
        public int4 particleIndices;
        public float4 weights;

        public float3 z0;
        public float3 z1;
        public float3 z2;
        public float3 z3;
    }

    public SurfaceSkinner(
        Transform bodyTransform,
        MeshFilter meshFilter,
        MeshRenderer meshRenderer,
        List<ShapeMatchingParticle> particles,
        List<ShapeMatchingCluster> clusters,
        bool recalculateNormals)
    {
        this.bodyTransform = bodyTransform;
        this.meshFilter = meshFilter;
        this.meshRenderer = meshRenderer;

        this.particles = particles;
        this.clusters = clusters;

        this.recalculateNormals = recalculateNormals;
    }

    public void Initialize()
    {
        if (meshFilter == null ||
            meshFilter.sharedMesh == null)
        {
            Debug.LogError(
                "SurfaceSkinner requires a valid MeshFilter.");

            return;
        }

        // Important:
        // Never modify the imported/shared asset directly.
        runtimeMesh =
            Object.Instantiate(meshFilter.sharedMesh);

        runtimeMesh.name =
            meshFilter.sharedMesh.name +
            "_RuntimeDeformed";

        runtimeMesh.MarkDynamic();

        meshFilter.sharedMesh = runtimeMesh;

        restVertices = runtimeMesh.vertices;

        deformedVertices =
            new Vector3[restVertices.Length];

        System.Array.Copy(
            restVertices,
            deformedVertices,
            restVertices.Length);

        particleOrientations =
            new quaternion[particles.Count];

        BuildParticleClusterMap();

        // At the undeformed state these should mostly be identity,
        // but calculate them properly anyway.
        CalculateParticleOrientations();

        BuildVertexSkinning();

        if (meshRenderer != null)
            meshRenderer.enabled = true;

        Debug.Log(
            $"Surface skinning initialized: " +
            $"{restVertices.Length} vertices.");
    }

    // ============================================================
    // PARTICLE -> CLUSTER CONNECTIONS
    // ============================================================

    private void BuildParticleClusterMap()
    {
        particleClusters =
            new List<int>[particles.Count];

        for (int i = 0; i < particles.Count; i++)
        {
            particleClusters[i] =
                new List<int>();
        }

        for (int clusterIndex = 0;
             clusterIndex < clusters.Count;
             clusterIndex++)
        {
            ShapeMatchingCluster cluster =
                clusters[clusterIndex];

            foreach (
                int particleIndex
                in cluster.particleIndices)
            {
                particleClusters[
                    particleIndex
                ].Add(clusterIndex);
            }
        }
    }

    // ============================================================
    // PARTICLE ORIENTATIONS
    // ============================================================

    private void CalculateParticleOrientations()
    {
        for (int particleIndex = 0;
             particleIndex < particles.Count;
             particleIndex++)
        {
            List<int> influences =
                particleClusters[particleIndex];

            if (influences == null ||
                influences.Count == 0)
            {
                particleOrientations[
                    particleIndex] =
                    quaternion.identity;

                continue;
            }

            float4 sum = float4.zero;

            bool hasReference = false;
            float4 reference = float4.zero;

            for (int i = 0;
                 i < influences.Count;
                 i++)
            {
                ShapeMatchingCluster cluster =
                    clusters[influences[i]];

                quaternion q =
                    new quaternion(
                        cluster.Rotation);

                float4 qValue = q.value;

                // q and -q represent exactly the same rotation.
                // Without this sign correction they could cancel
                // each other when averaged.
                if (!hasReference)
                {
                    reference = qValue;
                    hasReference = true;
                }
                else if (
                    math.dot(
                        reference,
                        qValue) < 0f)
                {
                    qValue = -qValue;
                }

                sum += qValue;
            }

            float magnitudeSquared =
                math.lengthsq(sum);

            if (magnitudeSquared < 1e-10f)
            {
                particleOrientations[
                    particleIndex] =
                    quaternion.identity;
            }
            else
            {
                particleOrientations[
                    particleIndex] =
                    new quaternion(
                        sum *
                        math.rsqrt(
                            magnitudeSquared));
            }
        }
    }

    // ============================================================
    // INITIAL VERTEX BINDING
    // ============================================================

    private void BuildVertexSkinning()
    {
        skinData =
            new VertexSkinData[
                restVertices.Length];

        for (int vertexIndex = 0;
             vertexIndex < restVertices.Length;
             vertexIndex++)
        {
            Vector3 localVertex =
                restVertices[vertexIndex];

            Vector3 worldVertexVector =
                bodyTransform.TransformPoint(
                    localVertex);

            float3 worldVertex =
                new float3(
                    worldVertexVector.x,
                    worldVertexVector.y,
                    worldVertexVector.z);

            FindFourClosestParticles(
                worldVertex,
                out int4 indices,
                out float4 distances);

            float4 weights =
                CalculateWeights(distances);

            VertexSkinData data =
                new VertexSkinData();

            data.particleIndices =
                indices;

            data.weights =
                weights;

            // Paper:
            //
            // za = inverse(Qa) * (y - xa)
            //
            data.z0 =
                CalculateLocalOffset(
                    worldVertex,
                    indices.x);

            data.z1 =
                CalculateLocalOffset(
                    worldVertex,
                    indices.y);

            data.z2 =
                CalculateLocalOffset(
                    worldVertex,
                    indices.z);

            data.z3 =
                CalculateLocalOffset(
                    worldVertex,
                    indices.w);

            skinData[vertexIndex] = data;
        }
    }

    private float3 CalculateLocalOffset(
        float3 vertexWorld,
        int particleIndex)
    {
        ShapeMatchingParticle particle =
            particles[particleIndex];

        quaternion orientation =
            particleOrientations[
                particleIndex];

        quaternion inverse =
            math.inverse(orientation);

        return math.rotate(
            inverse,
            vertexWorld -
            particle.position);
    }

    // ============================================================
    // FOUR NEAREST PARTICLES
    // ============================================================

    private void FindFourClosestParticles(
        float3 position,
        out int4 indices,
        out float4 distances)
    {
        int i0 = 0;
        int i1 = 0;
        int i2 = 0;
        int i3 = 0;

        float d0 = float.PositiveInfinity;
        float d1 = float.PositiveInfinity;
        float d2 = float.PositiveInfinity;
        float d3 = float.PositiveInfinity;

        for (int i = 0;
             i < particles.Count;
             i++)
        {
            float distance =
                math.distance(
                    position,
                    particles[i].position);

            if (distance < d0)
            {
                d3 = d2;
                i3 = i2;

                d2 = d1;
                i2 = i1;

                d1 = d0;
                i1 = i0;

                d0 = distance;
                i0 = i;
            }
            else if (distance < d1)
            {
                d3 = d2;
                i3 = i2;

                d2 = d1;
                i2 = i1;

                d1 = distance;
                i1 = i;
            }
            else if (distance < d2)
            {
                d3 = d2;
                i3 = i2;

                d2 = distance;
                i2 = i;
            }
            else if (distance < d3)
            {
                d3 = distance;
                i3 = i;
            }
        }

        indices =
            new int4(
                i0,
                i1,
                i2,
                i3);

        distances =
            new float4(
                d0,
                d1,
                d2,
                d3);
    }

    private float4 CalculateWeights(
        float4 distances)
    {
        const float epsilon = 0.00001f;

        // If a vertex is essentially exactly on a particle,
        // give that particle full influence.
        if (distances.x < epsilon)
        {
            return new float4(
                1f, 0f, 0f, 0f);
        }

        float4 weights =
            1f /
            math.max(
                distances,
                new float4(epsilon));

        float total =
            math.csum(weights);

        if (total <= epsilon)
        {
            return new float4(
                0.25f,
                0.25f,
                0.25f,
                0.25f);
        }

        return weights / total;
    }

    // ============================================================
    // UPDATE VISUAL MESH
    // ============================================================

    public void UpdateMesh()
    {
        if (runtimeMesh == null)
            return;

        CalculateParticleOrientations();

        Matrix4x4 worldToLocal =
            bodyTransform.worldToLocalMatrix;

        for (int vertexIndex = 0;
             vertexIndex < skinData.Length;
             vertexIndex++)
        {
            VertexSkinData data =
                skinData[vertexIndex];

            float3 y =
                float3.zero;

            y +=
                SkinInfluence(
                    data.particleIndices.x,
                    data.z0)
                * data.weights.x;

            y +=
                SkinInfluence(
                    data.particleIndices.y,
                    data.z1)
                * data.weights.y;

            y +=
                SkinInfluence(
                    data.particleIndices.z,
                    data.z2)
                * data.weights.z;

            y +=
                SkinInfluence(
                    data.particleIndices.w,
                    data.z3)
                * data.weights.w;

            Vector3 localPosition =
                worldToLocal.MultiplyPoint3x4(
                    new Vector3(
                        y.x,
                        y.y,
                        y.z));

            deformedVertices[
                vertexIndex] =
                localPosition;
        }

        runtimeMesh.vertices =
            deformedVertices;

        runtimeMesh.RecalculateBounds();

        if (recalculateNormals)
        {
            runtimeMesh.RecalculateNormals();
        }
    }

    private float3 SkinInfluence(
        int particleIndex,
        float3 localOffset)
    {
        ShapeMatchingParticle particle =
            particles[particleIndex];

        quaternion orientation =
            particleOrientations[
                particleIndex];

        return
            math.rotate(
                orientation,
                localOffset)
            +
            particle.position;
    }
}