using System.Collections.Generic;
using UnityEngine;

public class CheckpointGuide : MonoBehaviour
{
    private const float BeamHeight = 6f;
    private const float WallBottomAlpha = 0.7f;
    private const float FloorAlpha = 0.35f;
    private const float FloorLift = 0.05f;

    private static readonly Color BeamColor = new Color(0f, 0.55f, 0.8f);

    private MeshRenderer beamRenderer;
    private Mesh beamMesh;
    private Checkpoint shownCheckpoint;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (FindFirstObjectByType<CheckpointGuide>() == null)
        {
            DontDestroyOnLoad(new GameObject("CheckpointGuide", typeof(CheckpointGuide)));
        }
    }

    private void Awake()
    {
        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            enabled = false;
            return;
        }

        GameObject beam = new GameObject("Beam", typeof(MeshFilter), typeof(MeshRenderer));
        beam.transform.SetParent(transform, false);

        beamMesh = new Mesh();
        beamMesh.name = "CheckpointBeam";
        beamMesh.MarkDynamic();
        beam.GetComponent<MeshFilter>().sharedMesh = beamMesh;

        beamRenderer = beam.GetComponent<MeshRenderer>();
        beamRenderer.sharedMaterial = new Material(shader);
        beamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        beamRenderer.receiveShadows = false;
        beamRenderer.enabled = false;
    }

    private void Update()
    {
        Checkpoint target = FindTarget();

        if (target == null)
        {
            beamRenderer.enabled = false;
            shownCheckpoint = null;
            return;
        }

        if (target != shownCheckpoint)
        {
            shownCheckpoint = target;
            BuildMesh(target);
        }

        beamRenderer.enabled = true;
    }

    private static Checkpoint FindTarget()
    {
        RaceManager raceManager = RaceManager.Instance;
        BikeController bike = BikeController.Local;

        if (raceManager == null || bike == null || raceManager.CheckpointCount == 0)
        {
            return null;
        }

        if (raceManager.State != RaceState.Countdown && raceManager.State != RaceState.Racing)
        {
            return null;
        }

        int index = bike.NextCheckpoint;

        if (index < 0 || index >= raceManager.CheckpointCount)
        {
            return null;
        }

        return raceManager.GetCheckpoint(index);
    }

    private void BuildMesh(Checkpoint checkpoint)
    {
        BoxCollider area = checkpoint.Area;
        Transform areaTransform = area.transform;
        Vector3 center = area.center;
        Vector3 half = area.size * 0.5f;
        float bottom = center.y - half.y;

        Vector3[] corners = new Vector3[4];
        corners[0] = areaTransform.TransformPoint(new Vector3(center.x - half.x, bottom, center.z - half.z));
        corners[1] = areaTransform.TransformPoint(new Vector3(center.x + half.x, bottom, center.z - half.z));
        corners[2] = areaTransform.TransformPoint(new Vector3(center.x + half.x, bottom, center.z + half.z));
        corners[3] = areaTransform.TransformPoint(new Vector3(center.x - half.x, bottom, center.z + half.z));

        Color baseColor = QualitySettings.activeColorSpace == ColorSpace.Linear ? BeamColor.linear : BeamColor;
        Color strong = new Color(baseColor.r, baseColor.g, baseColor.b, WallBottomAlpha);
        Color clear = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        Color floor = new Color(baseColor.r, baseColor.g, baseColor.b, FloorAlpha);

        List<Vector3> vertices = new List<Vector3>();
        List<Color> colors = new List<Color>();
        List<int> triangles = new List<int>();
        Vector3 up = Vector3.up * BeamHeight;

        for (int i = 0; i < 4; i++)
        {
            Vector3 from = corners[i];
            Vector3 to = corners[(i + 1) % 4];
            AddQuad(vertices, colors, triangles, from, to, to + up, from + up, strong, strong, clear, clear);
        }

        Vector3 lift = Vector3.up * FloorLift;
        AddQuad(vertices, colors, triangles, corners[0] + lift, corners[1] + lift, corners[2] + lift, corners[3] + lift, floor, floor, floor, floor);

        beamMesh.Clear();
        beamMesh.SetVertices(vertices);
        beamMesh.SetColors(colors);
        beamMesh.SetTriangles(triangles, 0);
        beamMesh.RecalculateBounds();
    }

    private static void AddQuad(List<Vector3> vertices, List<Color> colors, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color colorA, Color colorB, Color colorC, Color colorD)
    {
        int start = vertices.Count;

        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);

        colors.Add(colorA);
        colors.Add(colorB);
        colors.Add(colorC);
        colors.Add(colorD);

        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
        triangles.Add(start);
        triangles.Add(start + 2);
        triangles.Add(start + 3);
    }
}
