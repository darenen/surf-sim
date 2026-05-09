using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class ProceduralWaveMesh : MonoBehaviour
{
    [Header("Wave Moving")]
    public bool waveMoving = false;
    public float waveTravelSpeed = 5f;

    [Header("Grid Dimensions")]
    public int resolution = 50; // High density for the AI to "feel" the slope
    public float size = 20f;     // Total size of the water patch

    [Header("Single Wave Shape")]
    public float waveHeight = 3f;
    public float waveWidth = 0.15f; // Lower number = fatter wave. Higher number = steeper/narrower.
    public float wavePosition = 0f; // Moves the wave left or right on the X-axis

    [Header("Randomization (The Chop)")]
    public float noiseScale = 0.3f;
    public float noiseStrength = 0.4f;
    public float waterFlowSpeed = 2f; // How fast the water rushes over the wave

    private Mesh mesh;
    private MeshCollider meshCollider;
    private Vector3[] baseVertices;
    private Vector3[] workingVertices;
    private int[] triangles;

    void Start()
    {
        meshCollider = GetComponent<MeshCollider>();
        GenerateGrid();
    }

    // This builds the "Fabric" of the mesh
    void GenerateGrid()
    {
        mesh = new Mesh();
        mesh.name = "ProceduralWave";
        GetComponent<MeshFilter>().mesh = mesh;

        // Create vertex positions (Flat initially)
        baseVertices = new Vector3[(resolution + 1) * (resolution + 1)];
        workingVertices = new Vector3[baseVertices.Length];

        int i = 0;
        for (int z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                // We center the mesh so (0,0,0) is in the middle of the grid
                float xPos = (x * (size / resolution)) - (size / 2f);
                float zPos = (z * (size / resolution)) - (size / 2f);
                baseVertices[i] = new Vector3(xPos, 0, zPos);
                i++;
            }
        }

        // Stitch the vertices into triangles
        triangles = new int[resolution * resolution * 6];
        int vert = 0;
        int tris = 0;
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + resolution + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + resolution + 1;
                triangles[tris + 5] = vert + resolution + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }

        mesh.vertices = baseVertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        // Initial collider setup
        meshCollider.sharedMesh = mesh;
    }

    void FixedUpdate()
    {
        AnimateWave();
    }

    // This moves the points every frame
    void AnimateWave()
    {
        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 v = baseVertices[i];

            // 1. The Single Wave (Gaussian Bell Curve)
            // We calculate how far this point is from the center of the wave
            float distance = v.x - wavePosition;

            if (waveMoving)
            {
                // Calculate a moving position that loops from one edge of the mesh to the other
                float currentPosition = Mathf.Repeat(Time.time * waveTravelSpeed, size) - (size / 2f);

                // Calculate distance based on this moving position
                distance = v.x - currentPosition;
            }

            // This math creates one single peak that flattens out on the sides
            float swell = waveHeight * Mathf.Exp(-Mathf.Pow(distance, 2) * waveWidth);

            // 2. The Flowing Water (Perlin Noise)
            // We animate the noise using Time.time so the water looks like it's
            // rushing up and over the stationary wave.
            float chop = Mathf.PerlinNoise(v.x * noiseScale + (Time.time * waterFlowSpeed),
                                           v.z * noiseScale) * noiseStrength;

            // 3. Set the new height
            workingVertices[i] = new Vector3(v.x, swell + chop, v.z);
        }

        // Push the new point positions to the mesh
        mesh.vertices = workingVertices;

        // NEW: Tell the mesh to recalculate which way the slopes are facing!
        mesh.RecalculateNormals();

        // Kick the collider to update the physics
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
    }


}