using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(-25)]
public abstract class WorldBuilder : MonoBehaviour {
    public float bounryAO = 0.62f;
    public Color fog = Color.clear;
    public float fogInvDist = 1.0f;
    public bool lattice3D = false;
    public bool dualLight = false;

    public static float globalBounryAO;

    public struct Tile {
        public Tile(GyroVector _gv,  string _coord, string _tileName) {
            gv = _gv; ho = null; coord = _coord; tileName = _tileName;
        }
        public HyperObject ho;
        public GyroVector gv;
        public string coord;
        public string tileName;
    }

    public struct LoopFlags {
        public bool HasLoop() {
            return R > 0 || L > 0 || D > 0 || U > 0 || B > 0 || F > 0;
        }
        public bool DidShift(LoopFlags f) {
            return (R > 0 && f.R > 0) || (L > 0 && f.L > 0) ||
                   (D > 0 && f.D > 0) || (U > 0 && f.U > 0) ||
                   (B > 0 && f.B > 0) || (F > 0 && f.F > 0);
        }
        public byte R;
        public byte L;
        public byte D;
        public byte U;
        public byte B;
        public byte F;
    }

    private int numExpand = 0;
    private List<Tile> tiles = new List<Tile>();
    private static readonly int fogID = Shader.PropertyToID("_Fog");
    private static readonly int fogInvDistID = Shader.PropertyToID("_FogInvDist");
    private static readonly int warpParamsID = Shader.PropertyToID("_WarpParams");
    private static readonly int dualLightID = Shader.PropertyToID("_DualLight");
    protected bool updateShaders = true;
    protected GyroVector curGV;

    //Override to get a tile from a coordinate (may be null for empty)
    public abstract GameObject GetTile(string coord);
    //Override to specify the geometry of the world and how far to expand it
    public abstract int MaxExpansion();

    public List<Tile> AllTiles() { return tiles; }

    public static void SetFog(Color _fog, float _fogInvDist) {
        Shader.SetGlobalColor(fogID, _fog.linear);
        Shader.SetGlobalFloat(fogInvDistID, _fogInvDist);
    }

    public static void SetWarpParams(Vector4 warpParams) {
        Shader.SetGlobalVector(warpParamsID, warpParams);
    }

    public static void UpdateForwardDrawDist() {
        if (QualitySettings.GetQualityLevel() <= 0) {
            HyperObject.drawDistSqForward = HyperObject.DRAW_DIST_SQ_BACKWARD;
        } else {
            HyperObject.drawDistSqForward = HyperObject.DRAW_DIST_SQ_FORWARD;
        }
    }

    protected virtual void Awake() {
        //Always reset the hyper object static values when first loading a world
        HyperObject.isShaking = false;
        UpdateForwardDrawDist();
        HyperObject.drawDistSqBackward = HyperObject.DRAW_DIST_SQ_BACKWARD;
        HyperObject.colliderDistSqUpdate = HyperObject.COLLIDER_DIST_SQ_UPDATE;

        //Build the level
        CreateLevel();
    }

    protected void CreateLevel() {
        //Set the fog parameters in the shader
        SetFog(fog, fogInvDist);
        Shader.SetGlobalVector(warpParamsID, Vector4.zero);
        Shader.SetGlobalFloat(dualLightID, dualLight ? 1.0f : 0.0f);
        Shader.SetGlobalFloat("_Enable", 1.0f);

        //Set the global boundary AO
        globalBounryAO = bounryAO;

        //Load the corresponding resource
        tiles.Clear();
        numExpand = MaxExpansion();
        string fname = "Map" + HM.N + (lattice3D ? "L" : "");
        TextAsset textAsset = Resources.Load(fname) as TextAsset;
        Debug.Assert(textAsset != null, "TileMap not generated yet for tiling " + fname);
        BinaryReader binaryReader = new BinaryReader(new MemoryStream(textAsset.bytes));

        //Read from the resource file
        while (binaryReader.BaseStream.Position != binaryReader.BaseStream.Length) {
            //Get the coordinate
            string coord = binaryReader.ReadString();
            if (coord.Length > numExpand) break;

            //Get the GyroVector
            Vector3 vec = new Vector3(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
            Quaternion gyr = new Quaternion(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
            curGV = new GyroVector(vec, gyr);

            //Create the GameObject for this tile
            GameObject hyperTile = GetTile(coord);
            if (hyperTile == null) {
                continue;
            }

            //Add the tile to the list of all tiles
            Tile tile = new Tile(curGV, coord, hyperTile.name);
            hyperTile.name = "tile_" + coord;
            HyperObject[] hyperObjects = hyperTile.GetComponentsInChildren<HyperObject>();
            Debug.Assert(hyperObjects.Length == 1, "Found a tile with multiple HyperObject components: " + tile.tileName);
            tile.ho = hyperObjects[0];
            tile.ho.localGV = curGV;
            tiles.Add(tile);
        }
        binaryReader.Close();

        //Once all objects are spawned, reset colliders
        WCollider.AllColliders.Clear();

        //Update replacement shaders
        UpdateReplacementShaders(updateShaders);
    }

    public void LoadLoopMap(int expandLevel, Hashtable loopMap) {
        //Load the corresponding resource
        loopMap.Clear();
        string fname = "Loop" + HM.N + (lattice3D ? "Le" : "e") + expandLevel;
        TextAsset textAsset = Resources.Load(fname) as TextAsset;
        Debug.Assert(textAsset != null, "LoopMap not generated yet for tiling " + fname);
        BinaryReader binaryReader = new BinaryReader(new MemoryStream(textAsset.bytes));

        //Read from the resource file
        while (binaryReader.BaseStream.Position != binaryReader.BaseStream.Length) {
            //Get the coordinate
            string coord = binaryReader.ReadString();
            if (coord.Length > numExpand) break;

            //Setup the loop flags
            LoopFlags loopFlags = new LoopFlags();
            loopFlags.R = binaryReader.ReadByte();
            loopFlags.L = binaryReader.ReadByte();
            loopFlags.D = binaryReader.ReadByte();
            loopFlags.U = binaryReader.ReadByte();
            if (lattice3D) {
                loopFlags.B = binaryReader.ReadByte();
                loopFlags.F = binaryReader.ReadByte();
            }

            //Add loop flags to map
            loopMap[coord] = loopFlags;
        }
    }

    public static void UpdateReplacementShaders(bool useK) {
        //Tell the main camera to replace shaders
        Camera cam = Camera.main;
        cam.nearClipPlane = 0.001f;
        cam.ResetReplacementShader();
        if (useK) {
            if (HM.K > 0.0f) {
                cam.SetReplacementShader(Shader.Find("Custom/SphericalShader"), "HyperRenderType");
            } else if (HM.K == 0.0f) {
                cam.SetReplacementShader(Shader.Find("Custom/EuclideanShader"), "HyperRenderType");
            }
        }

#if UNITY_EDITOR
        //Draw the H2xE or S2xE map in the editor while playing to avoid camera issues
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv) {
            if (useK) {
                if (HM.K > 0.0f) {
                    sv.SetSceneViewShaderReplace(Shader.Find("Custom/S2xEShader"), "HyperRenderType");
                } else if (HM.K < 0.0f) {
                    sv.SetSceneViewShaderReplace(Shader.Find("Custom/H2xEShader"), "HyperRenderType");
                } else {
                    sv.SetSceneViewShaderReplace(Shader.Find("Custom/E2xEShader"), "HyperRenderType");
                }
            } else {
                sv.SetSceneViewShaderReplace(null, null);
            }
        }
#endif
    }

    public float NearestTileDistance(GyroVector gv) {
        float minDist = float.MaxValue;
        for (int i = 0; i < tiles.Count; ++i) {
            GyroVector gv2 = tiles[i].gv;
            float dist = (gv - gv2).vec.sqrMagnitude;
            if (dist < minDist) {
                minDist = dist;
            }
        }
        return minDist;
    }

    public Tile NearestTile(GyroVector gv) {
        float minDist = float.MaxValue;
        int bestTileIx = 0;
        for (int i = 0; i < tiles.Count; ++i) {
            GyroVector gv2 = tiles[i].gv;
            float dist = (gv - gv2).vec.sqrMagnitude;
            if (dist < minDist) {
                minDist = dist;
                bestTileIx = i;
            }
        }
        return tiles[bestTileIx];
    }

    public static GameObject MakeTile(string tileStr, Dictionary<string, GameObject> map) {
        if (tileStr == null) { return null; }
        string tile = tileStr.Substring(0, tileStr.Length - 1);
        if (!map.ContainsKey(tile)) {
            Debug.LogWarning("Builder is missing tile: " + tile);
            return null;
        }
        return RotateTile(Instantiate(map[tile]), tileStr[tileStr.Length - 1]);
    }

    public static GameObject RotateTile(GameObject tile, char dir) {
        if (dir == '<') {
            tile.transform.Rotate(0.0f, -90.0f, 0.0f);
        } else if (dir == '>') {
            tile.transform.Rotate(0.0f, 90.0f, 0.0f);
        } else if (dir == 'v') {
            tile.transform.Rotate(0.0f, 180.0f, 0.0f);
        }
        return tile;
    }

    public static GameObject RotateTileRandom(GameObject tile, string coord) {
        return RotateTileRandom(tile, GetDeterministicHash(coord));
    }

    public static GameObject RotateTileRandom(GameObject tile, uint hash) {
        tile.transform.Rotate(0.0f, 90.0f * ((hash / 256) % 4), 0.0f);
        return tile;
    }

    public static GameObject RotateExpansion(GameObject tile, string coord) {
        char dir = coord[0];
        if (dir == 'R') {
            tile.transform.Rotate(0.0f, -90.0f, 0.0f);
        } else if (dir == 'L') {
            tile.transform.Rotate(0.0f, 90.0f, 0.0f);
        } else if (dir == 'U') {
            tile.transform.Rotate(0.0f, 180.0f, 0.0f);
        } else if (dir == 'F') {
            tile.transform.Rotate(-90.0f, 0.0f, 0.0f);
        } else if (dir == 'B') {
            tile.transform.Rotate(90.0f, 0.0f, 0.0f);
        }
        return tile;
    }

    public static GameObject MakeRandomTileAndRotate(GameObject[] tiles, string coord) {
        if (tiles.Length == 0) { return null; }
        uint hash = GetDeterministicHash(coord);
        return RotateTileRandom(Instantiate(tiles[hash % tiles.Length]), hash);
    }
    public static GameObject MakeRandomTileAndRotate(Dictionary<string, GameObject> map, string[] tiles, string coord) {
        if (tiles.Length == 0) { return null; }
        uint hash = GetDeterministicHash(coord);
        return RotateTileRandom(Instantiate(map[tiles[hash % tiles.Length]]), hash);
    }

    public static GameObject MakeRandomTile(GameObject[] tiles, string coord) {
        if (tiles.Length == 0) { return null; }
        uint hash = GetDeterministicHash(coord);
        return Instantiate(tiles[hash % tiles.Length]);
    }
    public static GameObject MakeRandomTile(Dictionary<string, GameObject> map, string[] tiles, string coord) {
        if (tiles.Length == 0) { return null; }
        uint hash = GetDeterministicHash(coord);
        return Instantiate(map[tiles[hash % tiles.Length]]);
    }

    public static void AddNamedTiles(Dictionary<string, GameObject> dict, GameObject[] tiles) {
        foreach (GameObject tile in tiles) {
            if (!dict.ContainsKey(tile.name)) {
                dict.Add(tile.name, tile);
            } else if (dict[tile.name] != tile) {
                Debug.LogError("Found 2 tiles with the same name: " + tile.name);
            }
        }
    }

    public static uint GetDeterministicHash(string str) {
        unchecked {
            uint hash = 5381;
            for (int i = 0; i < str.Length; i++) {
                hash += str[i];
                hash = (hash << 5) ^ (hash >> 3);
            }
            return hash;
        }
    }

    public static Vector3 MakeShift(char c) {
        float cw = (float)HM.CELL_WIDTH;
        switch (c) {
            case 'L': return new Vector3(-cw, 0.0f, 0.0f);
            case 'R': return new Vector3(cw, 0.0f, 0.0f);
            case 'F': return new Vector3(0.0f, -cw, 0.0f);
            case 'B': return new Vector3(0.0f, cw, 0.0f);
            case 'D': return new Vector3(0.0f, 0.0f, -cw);
            case 'U': return new Vector3(0.0f, 0.0f, cw);
            default: return Vector3.zero;
        }
    }

    public void RebuildGVs() {
        Hashtable gvs = new Hashtable();
        gvs[""] = GyroVector.identity;
        for (int i = 1; i < tiles.Count; ++i) {
            Tile tile = tiles[i];
            int endIx = tile.coord.Length - 1;
            char c = tile.coord[endIx];
            tile.ho.localGV = (GyroVector)gvs[tile.coord.Substring(0, endIx)] + MakeShift(c);
            gvs[tile.coord] = tile.ho.localGV;
        }
    }

    public void DestroyAllTiles() {
        for (int i = 0; i < tiles.Count; ++i) {
            Tile tile = tiles[i];
            Destroy(tile.ho.gameObject);
        }
        tiles.Clear();
    }
}
