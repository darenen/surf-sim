using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class ProceduralWaveMesh : MonoBehaviour
{
    [Header("Grid Dimensions")]
    public int resolutionX = 100;
    public int resolutionZ = 50;
    public float length = 40f;
    public float width = 24f;

    [Header("Wave Shape")]
    public float waveHeight = 2.5f;
    public float waveWidth = 0.12f;
    public float waveCenterLocalX = 0f;

    [Header("Chop")]
    public float noiseScale = 0.25f;
    public float noiseStrength = 0.15f;

    [Header("Movement")]
    public bool moveWave = true;
    public Vector3 moveDirection = Vector3.right;
    public float moveSpeed = 1.5f;
    public float startX = -20f;
    public float resetX = 20f;

    [Header("Collider")]
    public bool makeColliderTrigger = true;

    Mesh mesh;
    MeshCollider meshCollider;

    Vector3[] vertices;
    int[] triangles;

    void Start()
    {
        meshCollider = GetComponent<MeshCollider>();
        meshCollider.isTrigger = makeColliderTrigger;

        GenerateWaveMesh();
    }

    void FixedUpdate()
    {
        if (!moveWave)
            return;

        Vector3 dir = moveDirection.sqrMagnitude > 0.001f ? moveDirection.normalized : Vector3.right;
        transform.position += dir * moveSpeed * Time.fixedDeltaTime;

        if (dir.x > 0f && transform.position.x > resetX)
        {
            transform.position = new Vector3(startX, transform.position.y, transform.position.z);
        }
        else if (dir.x < 0f && transform.position.x < startX)
        {
            transform.position = new Vector3(resetX, transform.position.y, transform.position.z);
        }
    }

    void GenerateWaveMesh()
    {
        mesh = new Mesh();
        mesh.name = "MovingProceduralWave";
        mesh.MarkDynamic();

        GetComponent<MeshFilter>().mesh = mesh;

        vertices = new Vector3[(resolutionX + 1) * (resolutionZ + 1)];
        triangles = new int[resolutionX * resolutionZ * 6];

        int i = 0;

        for (int z = 0; z <= resolutionZ; z++)
        {
            for (int x = 0; x <= resolutionX; x++)
            {
                float x01 = x / (float)resolutionX;
                float z01 = z / (float)resolutionZ;

                float localX = x01 * length - length * 0.5f;
                float localZ = z01 * width - width * 0.5f;

                float height = GetLocalWaveHeight(localX, localZ);

                vertices[i] = new Vector3(localX, height, localZ);
                i++;
            }
        }

        int vert = 0;
        int tris = 0;

        for (int z = 0; z < resolutionZ; z++)
        {
            for (int x = 0; x < resolutionX; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + resolutionX + 1;
                triangles[tris + 2] = vert + 1;

                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + resolutionX + 1;
                triangles[tris + 5] = vert + resolutionX + 2;

                vert++;
                tris += 6;
            }

            vert++;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
    }

    float GetLocalWaveHeight(float localX, float localZ)
    {
        float distance = localX - waveCenterLocalX;
        float swell = waveHeight * Mathf.Exp(-(distance * distance) * waveWidth);

        float noise = Mathf.PerlinNoise(
            localX * noiseScale,
            localZ * noiseScale
        );

        float chop = (noise - 0.5f) * noiseStrength;

        return swell + chop;
    }
}
