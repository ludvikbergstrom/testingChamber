using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

[RequireComponent(typeof(MeshFilter))]
public class ElastoPlasticBody : MonoBehaviour
{
    [Header("Particles")]
    [Min(0.01f)]
    public float particleSpacing = 0.2f;

    [Tooltip("Visual/collision radius relative to particle spacing.")]
    [Range(0.05f, 0.5f)]
    public float particleRadiusMultiplier = 0.35f;

    [Header("Clusters")]
    [Tooltip("Radius of each shape matching cluster.")]
    public float clusterRadiusMultiplier = 2.5f;

    [Tooltip("Minimum spacing between cluster centres.")]
    public float clusterCenterSpacingMultiplier = 1.5f;

 
    [Header("Plasticity")]

    public bool enablePlasticity = true;

    [Tooltip("Strain required before permanent deformation begins.")]
    [Min(0f)]
    public float plasticYield = 0.05f;

    [Tooltip("How quickly elastic deformation becomes plastic.")]
    [Range(0f, 1f)]
    public float plasticFlowRate = 0.7f;

    [Tooltip("Increases resistance to further plastic deformation.")]
    [Min(0f)]
    public float plasticHardening = 0f;

    [Range(0f, 1f)]
    public float stiffness = 0.5f;

    [Header("Cluster Resampling")]

    public bool enableClusterResampling = true;

    [Tooltip(
        "Replace a cluster when the Frobenius norm " +
        "of its plastic deformation exceeds this value.")]
    [Min(1.74f)]
    public float clusterResampleThreshold = 2.0f;

    public bool logClusterResampling = false;

    [Header("Solver")]
    [Range(1, 20)]
    public int solverIterations = 6;

    [Range(1, 8)]
    public int substeps = 2;

    public Vector3 gravity = new Vector3(0f, -9.81f, 0f);

    [Range(0.9f, 1f)]
    public float velocityDamping = 0.999f;


    [Header("Ground")]
    public float groundHeight = 0f;

    [Range(0f, 1f)]
    public float groundFriction = 0.25f;

    [Header("Visual Mesh")]

    public bool enableSurfaceSkinning = true;

    [Tooltip(
        "Recalculate mesh normals every simulation step. " +
        "Looks better but costs CPU time.")]
    public bool recalculateSurfaceNormals = true;


    [Header("Debug")]
    public bool hideOriginalMesh = true;

    public bool drawParticles = true;
    public bool drawClusters = false;

    private List<ShapeMatchingParticle> particles;
    private List<ShapeMatchingCluster> clusters;

    private float3[] corrections;
    private int[] correctionCounts;

    private Mesh sourceMesh;

    private SurfaceSkinner surfaceSkinner;

    private int totalClusterRebases = 0;
    private int lastClusterRebases = 0;

    public float ParticleRadius =>
        particleSpacing * particleRadiusMultiplier;

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        sourceMesh = meshFilter.sharedMesh;

        if (sourceMesh == null)
        {
            Debug.LogError(
                "ElastoPlasticBody requires a MeshFilter with a mesh.");
            enabled = false;
            return;
        }

        Vector3 scale = transform.lossyScale;

        if (
            Mathf.Abs(scale.x - scale.y) > 0.001f ||
            Mathf.Abs(scale.y - scale.z) > 0.001f)
        {
            Debug.LogWarning(
                "Stage 1 assumes approximately uniform object scale.");
        }

        CreateParticles();
        CreateClusters();

        corrections = new float3[particles.Count];
        correctionCounts = new int[particles.Count];

        MeshRenderer meshRenderer =
            GetComponent<MeshRenderer>();

        if (enableSurfaceSkinning)
        {
            surfaceSkinner =
                new SurfaceSkinner(
                    transform,
                    meshFilter,
                    meshRenderer,
                    particles,
                    clusters,
                    recalculateSurfaceNormals);

            surfaceSkinner.Initialize();
        }
        else if (hideOriginalMesh)
        {
            if (meshRenderer != null)
                meshRenderer.enabled = false;
        }

        Debug.Log(
            $"Shape Matching initialized: " +
            $"{particles.Count} particles, " +
            $"{clusters.Count} clusters.");
    }

    private void FixedUpdate()
    {
        if (particles == null ||
            particles.Count == 0)
            return;

        float dt =
            Time.fixedDeltaTime /
            substeps;

        for (int i = 0;
             i < substeps;
             i++)
        {
            Simulate(dt);
        }

        // Only update the visual mesh once,
        // after all physics substeps have completed.
        if (enableSurfaceSkinning &&
            surfaceSkinner != null)
        {
            surfaceSkinner.UpdateMesh();
        }
    }

    private void Simulate(float dt)
    {
        // =========================================================
        // 1. EXTERNAL FORCES + PREDICTION
        // =========================================================

        float3 gravityVector =
            new float3(gravity.x, gravity.y, gravity.z);

        for (int i = 0; i < particles.Count; i++)
        {
            ShapeMatchingParticle p = particles[i];

            if (p.inverseMass <= 0f)
                continue;

            p.velocity += gravityVector * dt;

            p.predictedPosition =
                p.position + p.velocity * dt;
        }

        // =========================================================
        // 2. PBD CONSTRAINT SOLVER
        // =========================================================

        for (int iteration = 0;
             iteration < solverIterations;
             iteration++)
        {
            ClearCorrections();

            // All clusters calculate corrections from the
            // same predicted particle state.
            for (int i = 0; i < clusters.Count; i++)
            {
                clusters[i].Project(
                    particles,
                    stiffness,
                    corrections,
                    correctionCounts);
            }

            // Average corrections from overlapping clusters.
            for (int i = 0; i < particles.Count; i++)
            {
                if (correctionCounts[i] > 0)
                {
                    particles[i].predictedPosition +=
                        corrections[i] /
                        correctionCounts[i];
                }
            }

            SolveGroundCollision();
        }
        // =========================================================
        // PLASTIC DEFORMATION
        // =========================================================

        if (enablePlasticity)
        {
            for (int i = 0; i < clusters.Count; i++)
            {
                clusters[i].UpdatePlasticity(
                    particles,
                    plasticFlowRate,
                    plasticHardening,
                    dt);
            }
        }
        if (enableClusterResampling)
        {
            ResampleOverDeformedClusters();
        }


        // =========================================================
        // 3. UPDATE VELOCITIES AND POSITIONS
        // =========================================================

        float minimumY =
            groundHeight + ParticleRadius;

        for (int i = 0; i < particles.Count; i++)
        {
            ShapeMatchingParticle p = particles[i];

            p.velocity =
                (p.predictedPosition - p.position) / dt;

            p.velocity *= velocityDamping;

            // Simple contact friction.
            if (p.predictedPosition.y <= minimumY + 0.0001f)
            {
                p.velocity.x *= 1f - groundFriction;
                p.velocity.z *= 1f - groundFriction;

                if (p.velocity.y < 0f)
                    p.velocity.y = 0f;
            }

            p.position = p.predictedPosition;
        }
        DebugPlasticity();
    }

    private void ClearCorrections()
    {
        for (int i = 0; i < particles.Count; i++)
        {
            corrections[i] = float3.zero;
            correctionCounts[i] = 0;
        }
    }

    private void SolveGroundCollision()
    {
        float minimumY =
            groundHeight + ParticleRadius;

        for (int i = 0; i < particles.Count; i++)
        {
            ShapeMatchingParticle p = particles[i];

            if (p.predictedPosition.y < minimumY)
            {
                p.predictedPosition =
                    new float3(
                        p.predictedPosition.x,
                        minimumY,
                        p.predictedPosition.z);
            }
        }
    }

    // =============================================================
    // PARTICLE GENERATION
    // =============================================================

    private void CreateParticles()
    {
        particles =
            new List<ShapeMatchingParticle>();

        Bounds bounds = sourceMesh.bounds;

        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        Vector3[] vertices = sourceMesh.vertices;
        int[] triangles = sourceMesh.triangles;

        float halfSpacing =
            particleSpacing * 0.5f;

        for (
            float x = min.x + halfSpacing;
            x <= max.x;
            x += particleSpacing)
        {
            for (
                float y = min.y + halfSpacing;
                y <= max.y;
                y += particleSpacing)
            {
                for (
                    float z = min.z + halfSpacing;
                    z <= max.z;
                    z += particleSpacing)
                {
                    Vector3 localPoint =
                        new Vector3(x, y, z);

                    if (!PointInsideMesh(
                            localPoint,
                            vertices,
                            triangles))
                    {
                        continue;
                    }

                    Vector3 worldPoint =
                        transform.TransformPoint(localPoint);

                    particles.Add(
                        new ShapeMatchingParticle(
                            new float3(
                                worldPoint.x,
                                worldPoint.y,
                                worldPoint.z),
                            1f));
                }
            }
        }

        Debug.Log(
            $"Generated {particles.Count} particles.");
    }

    // =============================================================
    // CLUSTER GENERATION
    // =============================================================

    private void CreateClusters()
    {
        clusters =
            new List<ShapeMatchingCluster>();

        float clusterRadius =
            particleSpacing *
            clusterRadiusMultiplier;

        float centerSpacing =
            particleSpacing *
            clusterCenterSpacingMultiplier;

        List<int> clusterCenters =
            new List<int>();

        // Greedily choose cluster centres.
        for (int i = 0; i < particles.Count; i++)
        {
            bool tooClose = false;

            for (int c = 0; c < clusterCenters.Count; c++)
            {
                float distance =
                    math.distance(
                        particles[i].position,
                        particles[
                            clusterCenters[c]
                        ].position);

                if (distance < centerSpacing)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
                clusterCenters.Add(i);
        }

        // Create overlapping clusters.
        foreach (int centerIndex in clusterCenters)
        {
            List<int> members =
                FindParticlesInRadius(
                    particles[centerIndex].position,
                    clusterRadius);

            if (members.Count < 4)
                continue;

            ShapeMatchingCluster cluster =
                new ShapeMatchingCluster(
                    particles,
                    members,
                    plasticYield);

            if (cluster.Valid)
                clusters.Add(cluster);
        }

        Debug.Log(
            $"Generated {clusters.Count} clusters.");
    }

    private List<int> FindParticlesInRadius(
        float3 center,
        float radius)
    {
        List<int> result = new List<int>();

        float radiusSquared =
            radius * radius;

        for (int i = 0; i < particles.Count; i++)
        {
            float3 difference =
                particles[i].position - center;

            if (math.lengthsq(difference)
                <= radiusSquared)
            {
                result.Add(i);
            }
        }

        return result;
    }

    // =============================================================
    // POINT-IN-MESH TEST
    // =============================================================

    private bool PointInsideMesh(
        Vector3 point,
        Vector3[] vertices,
        int[] triangles)
    {
        // Deliberately non-axis-aligned to reduce the chance
        // of hitting triangle edges exactly.
        Vector3 direction =
            new Vector3(
                1f,
                0.1234567f,
                0.3713907f
            ).normalized;

        int intersections = 0;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a =
                vertices[triangles[i]];

            Vector3 b =
                vertices[triangles[i + 1]];

            Vector3 c =
                vertices[triangles[i + 2]];

            if (RayTriangleIntersection(
                    point,
                    direction,
                    a,
                    b,
                    c,
                    out float distance))
            {
                if (distance > 0.00001f)
                    intersections++;
            }
        }

        return (intersections % 2) == 1;
    }

    private bool RayTriangleIntersection(
        Vector3 origin,
        Vector3 direction,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        out float distance)
    {
        const float epsilon = 0.000001f;

        Vector3 edge1 = b - a;
        Vector3 edge2 = c - a;

        Vector3 h =
            Vector3.Cross(direction, edge2);

        float determinant =
            Vector3.Dot(edge1, h);

        if (Mathf.Abs(determinant) < epsilon)
        {
            distance = 0f;
            return false;
        }

        float inverseDeterminant =
            1f / determinant;

        Vector3 s =
            origin - a;

        float u =
            inverseDeterminant *
            Vector3.Dot(s, h);

        if (u < 0f || u > 1f)
        {
            distance = 0f;
            return false;
        }

        Vector3 q =
            Vector3.Cross(s, edge1);

        float v =
            inverseDeterminant *
            Vector3.Dot(direction, q);

        if (v < 0f || u + v > 1f)
        {
            distance = 0f;
            return false;
        }

        distance =
            inverseDeterminant *
            Vector3.Dot(edge2, q);

        return distance > epsilon;
    }

    private void ResampleOverDeformedClusters()
    {
        lastClusterRebases = 0;

        for (int i = 0;
             i < clusters.Count;
             i++)
        {
            ShapeMatchingCluster cluster =
                clusters[i];

            if (!cluster.Valid)
                continue;

            if (cluster.PlasticDeformationNorm <
                clusterResampleThreshold)
            {
                continue;
            }

            float oldNorm =
                cluster.PlasticDeformationNorm;

            bool success =
                cluster.RebaseToCurrentConfiguration(
                    particles);

            if (!success)
                continue;

            lastClusterRebases++;
            totalClusterRebases++;

            if (logClusterResampling)
            {
                Debug.Log(
                    $"Rebased cluster {i}. " +
                    $"Old FP norm: {oldNorm:F3}");
            }
        }
    }

    private void DebugPlasticity()
    {
        if (Time.frameCount % 30 != 0)
            return;

        float maxNorm = 0f;
        float minDeterminant = float.PositiveInfinity;
        float maxDeterminant = float.NegativeInfinity;

        foreach (ShapeMatchingCluster cluster in clusters)
        {
            float norm = cluster.PlasticDeformationNorm;

            float determinant =
                math.determinant(
                    cluster.PlasticDeformation);

            maxNorm = math.max(maxNorm, norm);

            minDeterminant =
                math.min(minDeterminant, determinant);

            maxDeterminant =
                math.max(maxDeterminant, determinant);
        }

        Debug.Log(
     $"FP: max norm = {maxNorm:F3}, " +
     $"det range = " +
     $"[{minDeterminant:F3}, {maxDeterminant:F3}], " +
     $"cluster rebases = {lastClusterRebases}, " +
     $"total = {totalClusterRebases}");
    }

    // =============================================================
    // DEBUG DRAWING
    // =============================================================

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        if (particles == null)
            return;

        if (drawParticles)
        {
            Gizmos.color = Color.yellow;

            float radius =
                ParticleRadius * 0.45f;

            foreach (
                ShapeMatchingParticle particle
                in particles)
            {
                Gizmos.DrawSphere(
                    (Vector3)particle.position,
                    radius);
            }
        }

        if (drawClusters && clusters != null)
        {
            Gizmos.color =
                new Color(0f, 1f, 1f, 0.2f);

            float clusterRadius =
                particleSpacing *
                clusterRadiusMultiplier;

            foreach (
                ShapeMatchingCluster cluster
                in clusters)
            {
                Gizmos.DrawWireSphere(
                    (Vector3)cluster.RestCenter,
                    clusterRadius);
            }
        }
    }
}