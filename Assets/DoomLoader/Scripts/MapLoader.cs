using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System;

public static class MapLoader 
{
    public static string CurrentMap;

    public static bool IsSkyTexture(string textureName)
    {
        if (textureName == "F_SKY1")
            return true;

        return false;
    }

    public const int sizeDividor = 32;
    public const int flatUVdividor = 64 / sizeDividor; //all Doom flats are 64x64
    public const float _4units = 4f / sizeDividor;
    public const float _8units = 8f / sizeDividor;
    public const float _16units = 16f / sizeDividor;
    public const float _24units = 24f / sizeDividor;
    public const float _32units = 32f / sizeDividor;
    public const float _64units = 64f / sizeDividor;
    public const float _96units = 96f / sizeDividor;
    public const float _128units = 128f / sizeDividor;

    public static List<Vertex> vertices;
    public static List<Sector> sectors;
    public static List<Linedef> linedefs;
    public static List<Sidedef> sidedefs;
    public static List<Thing> things;

    public static Lump things_lump;
    public static Lump linedefs_lump;
    public static Lump sidedefs_lump;
    public static Lump vertexes_lump;
    public static Lump segs_lump;
    public static Lump ssectors_lump;
    public static Lump nodes_lump;
    public static Lump sectors_lump;
    public static Lump reject_lump;
    public static Lump blockmap_lump;

    public static int minX = int.MaxValue;
    public static int maxX = int.MinValue;
    public static int minY = int.MaxValue;
    public static int maxY = int.MinValue;
    public static int minZ = int.MaxValue;
    public static int maxZ = int.MinValue;

    public static int sizeX = 0;
    public static int sizeY = 0;
    public static int sizeZ = 0;

    public static void Unload()
    {
        if (string.IsNullOrEmpty(CurrentMap))
            return;

        foreach (Vertex v in vertices)
        {
            v.Linedefs.Clear();
        }

        Sector.TaggedSectors = new Dictionary<int, List<Sector>>();
        foreach (Sector s in sectors)
        {
            s.Sidedefs.Clear();
            s.triangles.Clear();
        }

        foreach (Linedef l in linedefs)
        {
            l.start = null;
            l.end = null;
            l.Front = null;
            l.Back = null;
        }

        foreach (Sidedef s in sidedefs)
        {
            s.Line = null;
            s.Sector = null;
        }

        AI.heatmap = new Vector3[0, 0];

        for (int y = 0; y < TheGrid.sizeY; y++)
            for (int x = 0; x < TheGrid.sizeX; x++)
            {
                foreach (Triangle t in TheGrid.triangles[x, y]) t.sector = null;
                TheGrid.triangles[x, y].Clear();
                TheGrid.sectors[x, y].Clear();
                TheGrid.linedefs[x, y].Clear();
                TheGrid.decorThings[x, y].Clear();
                TheGrid.neutralThings[x, y].Clear();
                TheGrid.monsterThings[x, y].Clear();
                TheGrid.itemThings[x, y].Clear();
            }

        TheGrid.triangles = new List<Triangle>[0, 0];
        TheGrid.sizeX = 0;
        TheGrid.sizeY = 0;

        things_lump = null;
        linedefs_lump = null;
        sidedefs_lump = null;
        vertexes_lump = null;
        segs_lump = null;
        ssectors_lump = null;
        nodes_lump = null;
        sectors_lump = null;
        reject_lump = null;
        blockmap_lump = null;

        vertices.Clear();
        sectors.Clear();
        linedefs.Clear();
        sidedefs.Clear();
        things.Clear();

        for (int c = 0; c < GameManager.Instance.transform.childCount; c++)
            GameObject.Destroy(GameManager.Instance.transform.GetChild(c).gameObject);

        for (int c = 0; c < GameManager.Instance.TemporaryObjectsHolder.childCount; c++)
            GameObject.Destroy(GameManager.Instance.TemporaryObjectsHolder.GetChild(c).gameObject);

        GameManager.Instance.Player[0].LastSector = null;
        GameManager.Instance.Player[0].currentSector = null;

        PlayerInfo.Instance.unfoundSecrets = new List<Sector>();
        PlayerInfo.Instance.foundSecrets = new List<Sector>();

        CurrentMap = "";
    }

    public static bool Load(string mapName)
    {
        if (WadLoader.lumps.Count == 0)
        {
            Debug.LogError("MapLoader: Load: WadLoader.lumps == 0");
            return false;
        }

        //lumps
        {
            int i = 0;
            foreach (Lump l in WadLoader.lumps)
            {
                if (l.lumpName.Equals(mapName))
                    goto found;

                i++;
            }

            Debug.LogError("MapLoader: Load: Could not find map \"" + mapName + "\"");
            return false;

        found:
            things_lump = WadLoader.lumps[++i];
            linedefs_lump = WadLoader.lumps[++i];
            sidedefs_lump = WadLoader.lumps[++i];
            vertexes_lump = WadLoader.lumps[++i];
            segs_lump = WadLoader.lumps[++i];
            ssectors_lump = WadLoader.lumps[++i];
            nodes_lump = WadLoader.lumps[++i];
            sectors_lump = WadLoader.lumps[++i];
            reject_lump = WadLoader.lumps[++i];
            blockmap_lump = WadLoader.lumps[++i];
        }

        //fixes a small mishap by original level developer, sector 7 is not closed
        if (mapName == "E1M3")
        {
            //linedef 933 second vertex will be changed to vertex index 764
            linedefs_lump.data[13064] = 252;
            linedefs_lump.data[13065] = 2;
        }

        //things
        {
            int num = things_lump.data.Length / 10;
            things = new List<Thing>(num);

            for (int i = 0, n = 0; i < num; i++)
            {
                short x = (short)(things_lump.data[n++] | (short)things_lump.data[n++] << 8);
                short y = (short)(things_lump.data[n++] | (short)things_lump.data[n++] << 8);
                int facing = (int)(things_lump.data[n++] | (int)things_lump.data[n++] << 8);
                int thingtype = (int)things_lump.data[n++] | ((int)things_lump.data[n++]) << 8;
                int flags = (int)things_lump.data[n++] | ((int)things_lump.data[n++]) << 8;

                things.Add(new Thing(x, y, facing, thingtype, flags));
            }
        }

        //vertices
        {
            int num = vertexes_lump.data.Length / 4;
            vertices = new List<Vertex>(num);

            for (int i = 0, n = 0; i < num; i++)
            {
                short x = (short)(vertexes_lump.data[n++] | (short)vertexes_lump.data[n++] << 8);
                short y = (short)(vertexes_lump.data[n++] | (short)vertexes_lump.data[n++] << 8);

                vertices.Add(new Vertex(x, y));

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        //sectors
        {
            int num = sectors_lump.data.Length / 26;
            sectors = new List<Sector>(num);

            for (int i = 0, n = 0; i < num; i++)
            {
                short hfloor = (short)(sectors_lump.data[n++] | (short)sectors_lump.data[n++] << 8);
                short hceil = (short)(sectors_lump.data[n++] | (short)sectors_lump.data[n++] << 8);

                string tfloor = Encoding.ASCII.GetString(new byte[]
                {
                    sectors_lump.data[n++],
                    sectors_lump.data[n++],
                    sectors_lump.data[n++],
                    sectors_lump.data[n++],
                    sectors_lump.data[n++],
                    sectors_lump.data[n++],
                    sectors_lump.data[n++],
                    sectors_lump.data[n++]
                }).TrimEnd('\0').ToUpper();

                string tceil = Encoding.ASCII.GetString(new byte[]
                {
                    sectors_lump.data[n++],
                    sectors_lump.data[n++],
                    sectors_lump.data[n++],
                    sectors_lump.data[n++],
                    sectors_lump.data[n++],
                    sectors_lump.data[n++],
                    sectors_lump.data[n++],
                    sectors_lump.data[n++]
                }).TrimEnd('\0').ToUpper();

                int bright = sectors_lump.data[n++] | ((int)sectors_lump.data[n++]) << 8;
                int special = sectors_lump.data[n++] | ((int)sectors_lump.data[n++]) << 8;
                int tag = sectors_lump.data[n++] | ((int)sectors_lump.data[n++]) << 8;

                sectors.Add(new Sector(hfloor, hceil, tfloor, tceil, special, tag, bright));

                if (hfloor < minZ) minZ = hfloor;
                if (hceil > maxZ) maxZ = hceil;
            }
        }

        //sidedefs
        {
            int num = sidedefs_lump.data.Length / 30;
            sidedefs = new List<Sidedef>(num);

            for (int i = 0, n = 0; i < num; i++)
            {
                short offsetx = (short)(sidedefs_lump.data[n++] | (short)sidedefs_lump.data[n++] << 8);
                short offsety = (short)(sidedefs_lump.data[n++] | (short)sidedefs_lump.data[n++] << 8);

                string thigh = Encoding.ASCII.GetString(new byte[]
                {
                    sidedefs_lump.data[n++],
                    sidedefs_lump.data[n++],
                    sidedefs_lump.data[n++],
                    sidedefs_lump.data[n++],
                    sidedefs_lump.data[n++],
                    sidedefs_lump.data[n++],
                    sidedefs_lump.data[n++],
                    sidedefs_lump.data[n++]
                }).TrimEnd('\0').ToUpper();

                string tlow = Encoding.ASCII.GetString(new byte[]
                {
                    sidedefs_lump.data[n++],
                    sidedefs_lump.data[n++],
                    sidedefs_lump.data[n++],
                    sidedefs_lump.data[n++],
                    sidedefs_lump.data[n++],
                    sidedefs_lump.data[n++],
                    sidedefs_lump.data[n++],
                    sidedefs_lump.data[n++]
                }).TrimEnd('\0').ToUpper();

                string tmid = Encoding.ASCII.GetString(new byte[]
                {
                    sidedefs_lump.data[n++],
                    sidedefs_lump.data[n++],
                    sidedefs_lump.data[n++],
                    sidedefs_lump.data[n++],
                    sidedefs_lump.data[n++],
                    sidedefs_lump.data[n++],
                    sidedefs_lump.data[n++],
                    sidedefs_lump.data[n++]
                }).TrimEnd('\0').ToUpper();

                int sector = (int)(sidedefs_lump.data[n++] | (int)sidedefs_lump.data[n++] << 8);

                sidedefs.Add(new Sidedef(sectors[sector], offsetx, offsety, thigh, tlow, tmid, i));
            }
        }

        //linedefs
        {
            int num = linedefs_lump.data.Length / 14;
            linedefs = new List<Linedef>(num);

            for (int i = 0, n = 0; i < num; i++)
            {
                int v1 = linedefs_lump.data[n++] | ((int)linedefs_lump.data[n++]) << 8;
                int v2 = linedefs_lump.data[n++] | ((int)linedefs_lump.data[n++]) << 8;
                int flags = linedefs_lump.data[n++] | ((int)linedefs_lump.data[n++]) << 8;
                int action = linedefs_lump.data[n++] | ((int)linedefs_lump.data[n++]) << 8;
                int tag = linedefs_lump.data[n++] | ((int)linedefs_lump.data[n++]) << 8;
                int s1 = linedefs_lump.data[n++] | ((int)linedefs_lump.data[n++]) << 8;
                int s2 = linedefs_lump.data[n++] | ((int)linedefs_lump.data[n++]) << 8;

                Linedef line = new Linedef(vertices[v1], vertices[v2], flags, action, tag);
                linedefs.Add(line);

                if (s1 != ushort.MaxValue)
                    sidedefs[s1].SetLine(line, true);

                if (s2 != ushort.MaxValue)
                    sidedefs[s2].SetLine(line, false);
            }
        }

        //SKY FIX
        {
            foreach (Linedef l in linedefs)
            {
                if (l.Back == null)
                    continue;

                if (IsSkyTexture(l.Front.Sector.ceilingTexture))
                    if (IsSkyTexture(l.Back.Sector.ceilingTexture))
                    {
                        l.Front.tHigh = "F_SKY1";
                        l.Back.tHigh = "F_SKY1";
                    }

                if (IsSkyTexture(l.Front.Sector.floorTexture))
                    if (IsSkyTexture(l.Back.Sector.floorTexture))
                    {
                        l.Front.tLow = "F_SKY1";
                        l.Back.tLow = "F_SKY1";
                    }
            }
        }

        //modify geometry to accomodate expected changes
        foreach (Linedef l in linedefs)
        {
            if (l.lineType == 0)
                continue;

            switch (l.lineType)
            {
                default:
                    break;

                //common doors
                case 1:
                case 26:
                case 27:
                case 28:
                case 31:
                case 46:
                    {
                        if (l.Back != null)
                            if (l.Back.Sector.maximumCeilingHeight == l.Back.Sector.ceilingHeight
                                || l.Front.Sector.ceilingHeight - _4units < l.Back.Sector.maximumCeilingHeight)
                                l.Back.Sector.maximumCeilingHeight = l.Front.Sector.ceilingHeight - _4units;
                    }
                    break;

                //remote doors
                case 2:
                case 63:
                case 90:
                case 103:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                            foreach (Sidedef s in sector.Sidedefs)
                            {
                                if (s.Line.Front.Sector == sector)
                                    continue;

                                if (sector.maximumCeilingHeight == sector.ceilingHeight ||
                                    s.Line.Front.Sector.ceilingHeight - _4units < sector.maximumCeilingHeight)
                                        sector.maximumCeilingHeight = s.Line.Front.Sector.ceilingHeight - _4units;
                            }
                    }
                    break;

                //stairbuilder, 8units
                case 8:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            List<Sector> stairs = new List<Sector>();
                            Sector targetSector = sector;

                            int count = 0;
                            bool failed = false;
                            while (!failed)
                            {
                                count++;
                                stairs.Add(targetSector);
                                targetSector.maximumFloorHeight = sector.floorHeight + _8units * count;

                                failed = true;
                                foreach (Sidedef s in targetSector.Sidedefs)
                                {
                                    if (s.Line.Back == null)
                                        continue;

                                    if (s.Line.Back.Sector == targetSector)
                                        continue;

                                    if (s.Line.Back.Sector.floorTexture != targetSector.floorTexture)
                                        continue;

                                    if (stairs.Contains(s.Line.Back.Sector))
                                        continue;

                                    targetSector = s.Line.Back.Sector;
                                    failed = false;
                                }
                            }
                        }
                    }
                    break;

                //raise floor to next higher
                case 20:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            float targetHeight = float.MaxValue;
                            foreach (Sidedef s in sector.Sidedefs)
                            {
                                if (s.Line.Back == null)
                                    continue;

                                if (s.Line.Back.Sector == s.Line.Front.Sector)
                                    continue;

                                if (s.Line.Front.Sector == sector)
                                    if (s.Line.Back.Sector.floorHeight > sector.floorHeight && s.Line.Back.Sector.floorHeight < targetHeight)
                                        targetHeight = s.Line.Back.Sector.floorHeight;

                                if (s.Line.Back.Sector == sector)
                                    if (s.Line.Front.Sector.floorHeight > sector.floorHeight && s.Line.Front.Sector.floorHeight < targetHeight)
                                        targetHeight = s.Line.Front.Sector.floorHeight;
                            }

                            if (targetHeight < float.MaxValue)
                                sector.maximumFloorHeight = targetHeight;
                        }
                    }
                    break;

                //lowering platform
                case 36:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                            foreach (Sidedef s in sector.Sidedefs)
                            {
                                if (s.Line.Front.Sector.floorHeight + _8units < sector.minimumFloorHeight)
                                    sector.minimumFloorHeight = s.Line.Front.Sector.floorHeight + _8units;

                                if (s.Line.Back != null)
                                    if (s.Line.Back.Sector.floorHeight + _8units < sector.minimumFloorHeight)
                                        sector.minimumFloorHeight = s.Line.Back.Sector.floorHeight + _8units;
                            }
                    }
                    break;

                //common lifts
                case 62:
                case 88:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                            foreach (Sidedef s in sector.Sidedefs)
                            {
                                if (s.Line.Front.Sector.floorHeight < sector.minimumFloorHeight)
                                    sector.minimumFloorHeight = s.Line.Front.Sector.floorHeight;

                                if (s.Line.Back != null)
                                    if (s.Line.Back.Sector.floorHeight < sector.minimumFloorHeight)
                                        sector.minimumFloorHeight = s.Line.Back.Sector.floorHeight;
                            }
                    }
                    break;
            }
        }

        sizeX = maxX - minX;
        sizeY = maxY - minY;
        sizeZ = maxZ - minZ;

        CurrentMap = mapName;
        Debug.Log("Loaded map \"" + mapName + "\"");
        return true;
    }

    public static void ApplyLinedefBehavior()
    {
        Transform holder = new GameObject("DynamicMeshes").transform;
        holder.transform.SetParent(GameManager.Instance.transform);

        int index = -1;
        foreach (Linedef l in linedefs)
        {
            index++;

            if (l.lineType == 0)
                continue;

            switch (l.lineType)
            {
                default:
                    Debug.Log("Linedef " + index + " has unknown type (" + l.lineType + ")");
                    break;

                //common door
                case 1:
                case 26: //keycard doors
                case 27:
                case 28:
                    {
                        if (l.TopFrontObject == null)
                            break;

                        if (l.Back == null)
                            break;

                        l.Back.Sector.ceilingObject.transform.SetParent(holder);

                        Door1Controller lc = l.TopFrontObject.AddComponent<Door1Controller>();

                        if (l.lineType == 26) lc.requiresKeycard = 0;
                        if (l.lineType == 27) lc.requiresKeycard = 1;
                        if (l.lineType == 28) lc.requiresKeycard = 2;

                        SlowRepeatableDoorController sc = l.Back.Sector.ceilingObject.GetComponent<SlowRepeatableDoorController>();
                        if (sc == null)
                            sc = l.Back.Sector.ceilingObject.AddComponent<SlowRepeatableDoorController>();

                        sc.Init(l.Back.Sector);
                        lc.sectorController = sc;

                        l.Back.Sector.Dynamic = true;
                    }
                    break;

                //single use door, walk trigger
                case 2:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        List<SlowOneshotDoorController> linked = new List<SlowOneshotDoorController>();
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.ceilingObject.transform.SetParent(holder);

                            SlowOneshotDoorController sc = sector.ceilingObject.GetComponent<SlowOneshotDoorController>();
                            if (sc == null)
                            {
                                sc = sector.ceilingObject.gameObject.AddComponent<SlowOneshotDoorController>();
                                sc.Init(sector);
                            }
                            linked.Add(sc);

                            sector.Dynamic = true;

                            
                        }

                        BoxCollider mc = Mesher.CreateLineTriggerCollider(
                            l,
                            Mathf.Min(l.Front.Sector.minimumFloorHeight, l.Back.Sector.minimumFloorHeight),
                            Mathf.Max(l.Front.Sector.maximumCeilingHeight, l.Back.Sector.maximumCeilingHeight),
                            "Tag_" + l.lineTag + "_trigger",
                            holder
                            );

                        if (mc == null)
                        {
                            Debug.LogError("Linedef " + index + " could not create trigger. Type(" + l.lineType + ")");
                            break;
                        }

                        mc.isTrigger = true;

                        LineTrigger lt = mc.gameObject.AddComponent<LineTrigger>();
                        lt.TriggerAction = (c) =>
                        {
                            PlayerThing player = c.GetComponent<PlayerThing>();

                            if (player == null)
                                return;

                            foreach (SlowOneshotDoorController lc in linked)
                                if (lc.CurrentState == SlowOneshotDoorController.State.Closed)
                                    lc.CurrentState = SlowOneshotDoorController.State.Opening;
                        };
                    }
                    break;

                //stairbuilder, walktrigger
                case 8:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        List<StairbuilderSlow> linked = new List<StairbuilderSlow>();
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.floorObject.transform.SetParent(holder);

                            List<Sector> stairSectors = new List<Sector>();
                            Sector targetSector = sector;

                            int count = 0;
                            bool failed = false;
                            while (!failed)
                            {
                                count++;
                                stairSectors.Add(targetSector);
                                targetSector.Dynamic = true;
                                targetSector.floorObject.transform.SetParent(holder);

                                failed = true;
                                foreach (Sidedef s in targetSector.Sidedefs)
                                {
                                    if (s.Line.Back == null)
                                        continue;

                                    if (s.Line.Back.Sector == targetSector)
                                        continue;

                                    if (s.Line.Back.Sector.floorTexture != targetSector.floorTexture)
                                        continue;

                                    if (stairSectors.Contains(s.Line.Back.Sector))
                                        continue;

                                    targetSector = s.Line.Back.Sector;
                                    failed = false;
                                }
                            }

                            StairbuilderSlow sc = sector.floorObject.GetComponent<StairbuilderSlow>();
                            if (sc == null)
                            {
                                sc = sector.floorObject.gameObject.AddComponent<StairbuilderSlow>();
                                sc.Init(stairSectors);
                            }
                            linked.Add(sc);
                        }

                        BoxCollider mc = Mesher.CreateLineTriggerCollider(
                            l,
                            Mathf.Min(l.Front.Sector.minimumFloorHeight, l.Back.Sector.minimumFloorHeight),
                            Mathf.Max(l.Front.Sector.maximumCeilingHeight, l.Back.Sector.maximumCeilingHeight),
                            "Tag_" + l.lineTag + "_trigger",
                            holder
                            );

                        if (mc == null)
                        {
                            Debug.LogError("Linedef " + index + " could not create trigger. Type(" + l.lineType + ")");
                            break;
                        }

                        mc.isTrigger = true;

                        LineTrigger lt = mc.gameObject.AddComponent<LineTrigger>();
                        lt.TriggerAction = (c) =>
                        {
                            PlayerThing player = c.GetComponent<PlayerThing>();

                            if (player == null)
                                return;

                            foreach (StairbuilderSlow lc in linked)
                                if (lc.CurrentState == StairbuilderSlow.State.Waiting)
                                    lc.CurrentState = StairbuilderSlow.State.Active;
                        };
                    }
                    break;

                //donut, switch
                case 9:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        List<Sector> sectors = Sector.TaggedSectors[l.lineTag];
                        if (sectors.Count == 0)
                            break;

                        sectors[0].floorObject.transform.SetParent(holder);

                        Sector ringSector = null;
                        foreach (Sidedef s in sectors[0].Sidedefs)
                        {
                            if (s.Line.Back == null)
                                continue;

                            if (s.Line.Front.Sector == sectors[0])
                            {
                                ringSector = s.Line.Back.Sector;
                                ringSector.floorObject.transform.SetParent(holder);
                                break;
                            }

                            if (s.Line.Back.Sector == sectors[0])
                            {
                                ringSector = s.Line.Front.Sector;
                                ringSector.floorObject.transform.SetParent(holder);
                                break;
                            }
                        }

                        if (ringSector == null)
                            Debug.LogError("MapLoader: Donut9Controller: No ring sector found!");

                        Donut9SectorController sc = sectors[0].floorObject.gameObject.AddComponent<Donut9SectorController>();

                        Donut9LinedefController script = null;
                        if (l.BotFrontObject != null)
                            script = l.BotFrontObject.AddComponent<Donut9LinedefController>();
                        else if (l.MidFrontObject != null)
                            script = l.MidFrontObject.AddComponent<Donut9LinedefController>();

                        if (script != null)
                        {
                            script.sectorController = sc;
                            sc.Init(sectors[0], ringSector);
                        }

                        sectors[0].Dynamic = true;
                        ringSector.Dynamic = true;
                    }
                    break;

                //level end switch
                case 11:
                    {
                        if (l.BotFrontObject != null)
                        {
                            End11LinedefController lc = l.BotFrontObject.AddComponent<End11LinedefController>();
                            lc.CurrentTexture = l.Front.tLow;
                            l.BotFrontObject.transform.SetParent(holder);
                        }
                        else if (l.MidFrontObject != null)
                        {
                            End11LinedefController lc = l.MidFrontObject.AddComponent<End11LinedefController>();
                            lc.CurrentTexture = l.Front.tMid;
                            l.MidFrontObject.transform.SetParent(holder);
                        }
                    }
                    break;

                //raise floor to next, one use
                case 20:
                    {
                        List<Floor20SectorController> linked = new List<Floor20SectorController>();
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.floorObject.transform.SetParent(holder);

                            Floor20SectorController sc = sector.floorObject.GetComponent<Floor20SectorController>();
                            if (sc == null)
                            {
                                sc = sector.floorObject.gameObject.AddComponent<Floor20SectorController>();
                                sc.Init(sector);
                            }

                            linked.Add(sc);

                            sector.Dynamic = true;
                        }

                        if (l.BotFrontObject != null)
                        {
                            if (index == 1020)
                            {
                                l.BotFrontObject.transform.SetParent(GameManager.Instance.TemporaryObjectsHolder);
                            }

                            Floor20LinedefController lc = l.BotFrontObject.AddComponent<Floor20LinedefController>();
                            lc.sectorControllers = linked;
                            lc.CurrentTexture = l.Front.tLow;
                            l.BotFrontObject.transform.SetParent(holder);
                        }
                        else if (l.MidFrontObject != null)
                        {
                            Floor20LinedefController lc = l.MidFrontObject.AddComponent<Floor20LinedefController>();
                            lc.sectorControllers = linked;
                            lc.CurrentTexture = l.Front.tMid;
                            l.MidFrontObject.transform.SetParent(holder);
                        }
                    }
                    break;

                //single use door, pokeable
                case 31:
                    {
                        if (l.TopFrontObject == null)
                            break;

                        if (l.Back == null)
                            break;

                        l.Back.Sector.ceilingObject.transform.SetParent(holder);

                        Door31Controller lc = l.TopFrontObject.AddComponent<Door31Controller>();
                        SlowOneshotDoorController sc = l.Back.Sector.ceilingObject.GetComponent<SlowOneshotDoorController>();
                        if (sc == null)
                            sc = l.Back.Sector.ceilingObject.AddComponent<SlowOneshotDoorController>();

                        sc.Init(l.Back.Sector);
                        lc.sectorController = sc;

                        l.Back.Sector.Dynamic = true;
                    }
                    break;

                //make sectors dark, walktrigger
                case 35:
                    {

                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        BoxCollider mc = Mesher.CreateLineTriggerCollider(
                            l,
                            Mathf.Min(l.Front.Sector.minimumFloorHeight, l.Back.Sector.minimumFloorHeight),
                            Mathf.Max(l.Front.Sector.maximumCeilingHeight, l.Back.Sector.maximumCeilingHeight),
                            "Tag_" + l.lineTag + "_trigger",
                            holder
                            );

                        if (mc == null)
                        {
                            Debug.LogError("Linedef " + index + " could not create trigger. Type(" + l.lineType + ")");
                            break;
                        }

                        mc.isTrigger = true;

                        LineTrigger lt = mc.gameObject.AddComponent<LineTrigger>();
                        lt.TriggerAction = (c) =>
                        {
                            if (c.GetComponent<PlayerThing>() == null)
                                return;

                            foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                            {
                                sector.brightness = (float)35 / 255f;
                                sector.floorObject.ChangeBrightness(sector.brightness);
                            }
                        };
                    }
                    break;

                //single use floor lower, walktrigger
                case 36:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        List<Floor36Controller> linked = new List<Floor36Controller>();
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.floorObject.transform.SetParent(holder);

                            Floor36Controller sc = sector.floorObject.GetComponent<Floor36Controller>();
                            if (sc == null)
                            {
                                sc = sector.floorObject.gameObject.AddComponent<Floor36Controller>();
                                sc.Init(sector);
                            }
                            linked.Add(sc);

                            sector.Dynamic = true;
                        }

                        BoxCollider mc = Mesher.CreateLineTriggerCollider(
                            l,
                            Mathf.Min(l.Front.Sector.minimumFloorHeight, l.Back.Sector.minimumFloorHeight),
                            Mathf.Max(l.Front.Sector.maximumCeilingHeight, l.Back.Sector.maximumCeilingHeight),
                            "Tag_" + l.lineTag + "_trigger",
                            holder
                            );

                        if (mc == null)
                        {
                            Debug.LogError("Linedef " + index + " could not create trigger. Type(" + l.lineType + ")");
                            break;
                        }

                        mc.isTrigger = true;

                        LineTrigger lt = mc.gameObject.AddComponent<LineTrigger>();
                        lt.TriggerAction = (c) =>
                        {
                            PlayerThing player = c.GetComponent<PlayerThing>();

                            if (player == null)
                                return;

                            foreach (Floor36Controller lc in linked)
                                if (lc.CurrentState == Floor36Controller.State.AtTop)
                                    lc.CurrentState = Floor36Controller.State.Lowering;
                        };
                    }
                    break;

                //single use door, shootable
                case 46:
                    {
                        if (l.TopFrontObject == null)
                            break;

                        if (l.Back == null)
                            break;

                        l.Back.Sector.ceilingObject.transform.SetParent(GameManager.Instance.transform);

                        Door46Controller lc = l.TopFrontObject.AddComponent<Door46Controller>();
                        SlowOneshotDoorController sc = l.Back.Sector.ceilingObject.GetComponent<SlowOneshotDoorController>();
                        if (sc == null)
                            sc = l.Back.Sector.ceilingObject.AddComponent<SlowOneshotDoorController>();

                        sc.Init(l.Back.Sector);
                        lc.sectorController = sc;

                        l.Back.Sector.Dynamic = true;
                    }
                    break;

                //scroll animation, left
                case 48:
                    {
                        foreach (GameObject g in l.gameObjects)
                            if (g != null)
                                g.AddComponent<ScrollLeftAnimation>();
                    }
                    break;

                //secret level end switch
                case 51:
                    {
                        if (l.BotFrontObject != null)
                        {
                            End51LinedefController lc = l.BotFrontObject.AddComponent<End51LinedefController>();
                            lc.CurrentTexture = l.Front.tLow;
                            l.BotFrontObject.transform.SetParent(holder);
                        }
                        else if (l.MidFrontObject != null)
                        {
                            End51LinedefController lc = l.MidFrontObject.AddComponent<End51LinedefController>();
                            lc.CurrentTexture = l.Front.tMid;
                            l.MidFrontObject.transform.SetParent(holder);
                        }
                    }
                    break;

                //common lift, pokeable
                case 62:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        if (l.BotFrontObject == null)
                            break;

                        List<Slow3sLiftController> linked = new List<Slow3sLiftController>();
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.floorObject.transform.SetParent(holder);

                            Slow3sLiftController script = sector.floorObject.GetComponent<Slow3sLiftController>();
                            if (script == null)
                            {
                                script = sector.floorObject.gameObject.AddComponent<Slow3sLiftController>();
                                script.Init(sector);
                            }

                            linked.Add(script);

                            sector.Dynamic = true;
                        }

                        Lift62Controller lc = l.BotFrontObject.AddComponent<Lift62Controller>();
                        lc.liftControllers = linked;
                    }
                    break;

                //repeatable door, switch
                case 63:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        List<SlowRepeatableDoorController> linked = new List<SlowRepeatableDoorController>();
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.ceilingObject.transform.SetParent(holder);

                            SlowRepeatableDoorController sc = sector.ceilingObject.GetComponent<SlowRepeatableDoorController>();
                            if (sc == null)
                            {
                                sc = sector.ceilingObject.AddComponent<SlowRepeatableDoorController>();
                                sc.Init(sector);
                            }

                            linked.Add(sc);

                            sector.Dynamic = true;
                        }

                        if (l.BotFrontObject != null)
                        {
                            Door63Controller lc = l.BotFrontObject.AddComponent<Door63Controller>();
                            lc.sectorControllers = linked;
                            lc.CurrentTexture = l.Front.tLow;
                            l.BotFrontObject.transform.SetParent(holder);
                        }
                        else if (l.MidFrontObject != null)
                        {
                            Door63Controller lc = l.MidFrontObject.AddComponent<Door63Controller>();
                            lc.sectorControllers = linked;
                            lc.CurrentTexture = l.Front.tMid;
                            l.MidFrontObject.transform.SetParent(holder);
                        }
                    }
                    break;

                //common lift, walktrigger
                case 88:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        List<Slow3sLiftController> linked = new List<Slow3sLiftController>(); 
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.floorObject.transform.SetParent(holder);

                            Slow3sLiftController sc = sector.floorObject.GetComponent<Slow3sLiftController>();
                            if (sc == null)
                            {
                                sc = sector.floorObject.gameObject.AddComponent<Slow3sLiftController>();
                                sc.Init(sector);
                            }

                            linked.Add(sc);

                            sector.Dynamic = true;
                        }

                        BoxCollider mc = Mesher.CreateLineTriggerCollider(
                            l,
                            Mathf.Min(l.Front.Sector.minimumFloorHeight, l.Back.Sector.minimumFloorHeight),
                            Mathf.Max(l.Front.Sector.maximumCeilingHeight, l.Back.Sector.maximumCeilingHeight),
                            "Tag_" + l.lineTag + "_trigger",
                            holder
                            );

                        if (mc == null)
                        {
                            Debug.LogError("Linedef " + index + " could not create trigger. Type(" + l.lineType + ")");
                            break;
                        }

                        mc.isTrigger = true;

                        LineTrigger lt = mc.gameObject.AddComponent<LineTrigger>();
                        lt.TriggerAction = (c) => 
                        {
                            foreach (Slow3sLiftController lc in linked)
                                if (lc.CurrentState == Slow3sLiftController.State.AtTop)
                                    lc.CurrentState = Slow3sLiftController.State.Lowering;
                        };
                    }
                    break;

                //repeatable door, walktrigger
                case 90:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        List<SlowRepeatableDoorController> linked = new List<SlowRepeatableDoorController>();
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.ceilingObject.transform.SetParent(holder);

                            SlowRepeatableDoorController sc = sector.ceilingObject.GetComponent<SlowRepeatableDoorController>();
                            if (sc == null)
                            {
                                sc = sector.ceilingObject.gameObject.AddComponent<SlowRepeatableDoorController>();
                                sc.Init(sector);
                            }
                            linked.Add(sc);

                            sector.Dynamic = true;
                        }

                        BoxCollider mc = Mesher.CreateLineTriggerCollider(
                            l,
                            Mathf.Min(l.Front.Sector.minimumFloorHeight, l.Back.Sector.minimumFloorHeight),
                            Mathf.Max(l.Front.Sector.maximumCeilingHeight, l.Back.Sector.maximumCeilingHeight),
                            "Tag_" + l.lineTag + "_trigger",
                            holder
                            );

                        if (mc == null)
                        {
                            Debug.LogError("Linedef " + index + " could not create trigger. Type(" + l.lineType + ")");
                            break;
                        }

                        mc.isTrigger = true;

                        LineTrigger lt = mc.gameObject.AddComponent<LineTrigger>();
                        lt.TriggerAction = (c) =>
                        {
                            PlayerThing player = c.GetComponent<PlayerThing>();

                            if (player == null)
                                return;

                            foreach (SlowRepeatableDoorController lc in linked)
                                if (lc.CurrentState == SlowRepeatableDoorController.State.Closed)
                                    lc.CurrentState = SlowRepeatableDoorController.State.Opening;
                        };
                    }
                    break;

                //single use door, switch
                case 103:
                    {
                        if (!Sector.TaggedSectors.ContainsKey(l.lineTag))
                            break;

                        List<SlowOneshotDoorController> linked = new List<SlowOneshotDoorController>();
                        foreach (Sector sector in Sector.TaggedSectors[l.lineTag])
                        {
                            sector.ceilingObject.transform.SetParent(holder);

                            SlowOneshotDoorController sc = sector.ceilingObject.GetComponent<SlowOneshotDoorController>();
                            if (sc == null)
                            {
                                sc = sector.ceilingObject.AddComponent<SlowOneshotDoorController>();
                                sc.Init(sector);
                            }

                            linked.Add(sc);

                            sector.Dynamic = true;
                        }

                        if (l.BotFrontObject != null)
                        {
                            Door103Controller lc = l.BotFrontObject.AddComponent<Door103Controller>();
                            lc.sectorControllers = linked;
                            lc.CurrentTexture = l.Front.tLow;
                            l.BotFrontObject.transform.SetParent(holder);
                        }
                        else if (l.MidFrontObject != null)
                        {
                            Door103Controller lc = l.MidFrontObject.AddComponent<Door103Controller>();
                            lc.sectorControllers = linked;
                            lc.CurrentTexture = l.Front.tMid;
                            l.MidFrontObject.transform.SetParent(holder);
                        }
                    }
                    break;
            }
        }
    }
}
