#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class TileMapper : MonoBehaviour {
    public const string MAP_DIRECTORY = "Assets/HyperEngine/Resources";

    private struct Tile {
        public Tile(string _coord, GyroVectorD _gv) {
            coord = _coord; gv = _gv;
        }
        public GyroVectorD gv;
        public string coord;
    }

    [MenuItem("HyperEngine/Generate Tile Maps")]
    public static void GenerateTilemaps() {
        Debug.Log("Generating Tile Maps...");
        Thread t = new Thread(new ThreadStart(AsyncGenerateTileMaps));
        t.Start();
    }

    private static void AsyncGenerateTileMaps() {
        GenerateTileMap(3, false, 2);
        GenerateTileMap(4, false, 15);
        GenerateTileMap(5, false, 10);
        GenerateTileMap(6, false, 9);
        GenerateTileMap(999, false, 7);
        GenerateTileMap(3, true, 2);
        GenerateTileMap(4, true, 8);
        GenerateTileMap(5, true, 6);
        GenerateTileMap(6, true, 5);

        Debug.Log("Finished Generating All Tile Maps");
    }

    private static void GenerateTileMap(int type, bool lattice3D, int max_expand) {
        HM.SetTileType(type);
        List<Tile> tiles = new List<Tile>();
        tiles.Add(new Tile("", GyroVectorD.identity));

        if (!Directory.Exists(MAP_DIRECTORY)) {
            Directory.CreateDirectory(MAP_DIRECTORY);
        }

        string mapName = "Map" + type + (lattice3D ? "L" : "") + ".bytes";
        string fname = MAP_DIRECTORY + "/" + mapName;

        using (BinaryWriter writer = new BinaryWriter(File.Open(fname, FileMode.Create))) {
            Debug.Log("Generating " + mapName + "...");
            if (HM.N == 2) {
                tiles.Add(new Tile("R", new GyroVectorD(HM.CELL_WIDTH, 0.0, 0.0)));
            } else if (HM.N == 3) {
                ExpandMap(tiles, 0, lattice3D);
                tiles.Add(new Tile("RR", tiles[1].gv + new Vector3D(HM.CELL_WIDTH, 0.0, 0.0)));
            } else {
                for (int i = 0; i < max_expand; ++i) {
                    ExpandMap(tiles, i, lattice3D);
                }
            }
            Debug.Log("Spawned: " + tiles.Count);
            for (int i = 0; i < tiles.Count; ++i) {
                Add(writer, tiles[i].coord, tiles[i].gv);
            }
        }
    }

    private static void AddGV(BinaryWriter writer, GyroVectorD gv) {
        writer.Write((float)gv.vec.x);
        writer.Write((float)gv.vec.y);
        writer.Write((float)gv.vec.z);
        writer.Write((float)gv.gyr.x);
        writer.Write((float)gv.gyr.y);
        writer.Write((float)gv.gyr.z);
        writer.Write((float)gv.gyr.w);
    }

    private static void Add(BinaryWriter writer, string coord, GyroVectorD gv) {
        writer.Write(coord);
        AddGV(writer, gv);
    }

    private static void ExpandMap(List<Tile> tiles, int len, bool lattice3D) {
        for (int i = 0; i < tiles.Count; ++i) {
            string coord = tiles[i].coord;
            GyroVectorD gv = tiles[i].gv;
            if (coord.Length == len) {
                char last = (coord.Length > 0 ? coord[coord.Length - 1] : '\0');
                if (last != 'L') {
                    TrySpawn(tiles, coord + "R", gv + MakeShift('R'));
                }
                if (last != 'R') {
                    TrySpawn(tiles, coord + "L", gv + MakeShift('L'));
                }
                if (last != 'D') {
                    TrySpawn(tiles, coord + "U", gv + MakeShift('U'));
                }
                if (last != 'U') {
                    TrySpawn(tiles, coord + "D", gv + MakeShift('D'));
                }
                if (lattice3D) {
                    if (last != 'F') {
                        TrySpawn(tiles, coord + "B", gv + MakeShift('B'));
                    }
                    if (last != 'B') {
                        TrySpawn(tiles, coord + "F", gv + MakeShift('F'));
                    }
                }
            }
        }
    }

    private static Vector3D MakeShift(char c) {
        switch (c) {
            case 'L': return new Vector3D(-HM.CELL_WIDTH, 0.0, 0.0);
            case 'R': return new Vector3D(HM.CELL_WIDTH, 0.0, 0.0);
            case 'F': return new Vector3D(0.0, -HM.CELL_WIDTH, 0.0);
            case 'B': return new Vector3D(0.0, HM.CELL_WIDTH, 0.0);
            case 'D': return new Vector3D(0.0, 0.0, -HM.CELL_WIDTH);
            case 'U': return new Vector3D(0.0, 0.0, HM.CELL_WIDTH);
            default: return Vector3D.zero;
        }
    }

    private static bool TrySpawn(List<Tile> tiles, string coord, GyroVectorD gv) {
        for (int i = 0; i < tiles.Count; ++i) {
            GyroVectorD gv2 = tiles[i].gv;
            if ((gv - gv2).vec.sqrMagnitude < 1e-10) {
                //Debug.Log((gv.vec - gv2.vec).sqrMagnitude);
                return false;
            }
        }
        tiles.Add(new Tile(coord, gv));
        return true;
    }

    private static byte NearbyAfterShift(List<Tile> tiles, int ix, char c) {
        GyroVectorD gv = tiles[ix].gv + MakeShift(c);
        for (int i = 0; i < tiles.Count; ++i) {
            GyroVectorD gv2 = tiles[i].gv;
            if ((gv - gv2).vec.sqrMagnitude < 1e-10) {
                return 1;
            }
        }
        return 0;
    }
}
#endif
