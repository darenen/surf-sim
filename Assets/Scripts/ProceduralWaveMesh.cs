using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class ProceduralWaveMesh : MonoBehaviour
{
    [Header("Grid Dimensions")]
    public int resolution = 50; // High density for the AI to "feel" the slope
    public float size = 20f;     // Total size of the water patch

    [Header("Wave Shape (The Swell)")]
    public float waveHeight = 1.5f;
    public float waveFrequency = 0.5f;
    public float waveSpeed = 2.0f;

    [Header("Randomization (The Chop)")]
    public float noiseScale = 0.3f;
    public float noiseStrength = 0.4f;

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

            // 1. Math for the rhythmic swell (Sine Wave)
            float swell = Mathf.Sin(v.x * waveFrequency + Time.time * waveSpeed) * waveHeight;

            // 2. Math for the random bumps (Perlin Noise)
            // We use Time.time to make the noise "crawl" across the surface
            float chop = Mathf.PerlinNoise(v.x * noiseScale + (Time.time * 0.5f),
                                           v.z * noiseScale + (Time.time * 0.5f)) * noiseStrength;

            // 3. Set the new height
            workingVertices[i] = new Vector3(v.x, swell + chop, v.z);
        }

        // Push the new point positions to the mesh
        mesh.vertices = workingVertices;

        // This is the most important part for Raycasting:
        // We have to "kick" the collider to notice the change.
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
    }
}