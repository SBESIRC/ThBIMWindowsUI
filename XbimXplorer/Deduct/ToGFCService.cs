using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using THBimEngine.Domain;
using THBimEngine.IO.GFC2;

namespace XbimXplorer.Deduct
{
    public class GFCConvertEngine
    {
        public static void ToGFCEngine(THBimProject prj)
        {
            var docPath = @"D:\try.gfc2";
            var gfcDoc = ThGFC2Document.Create(docPath);
            try
            {
                if (prj.ProjectSite == null)
                {
                    return;
                }

                PrjToGFC(prj, gfcDoc);
            }
            catch
            {

            }
            finally
            {
                gfcDoc.Close();
            }
        }

        private static void PrjToGFC(THBimProject prj, ThGFC2Document gfcDoc)
        {
            var globelId = 0;
            prj.ToGfc(gfcDoc, ref globelId);

            var site = prj.ProjectSite;
            var buildingStoreyGFCDict = new Dictionary<int, List<int>>();//building gfc lineNo，floor gfc lineNo;
            var floorEntityDict = new Dictionary<int, List<int>>();//floor gfc lineNo, entity gfc lineNo;
            var entityModel = new Dictionary<int, Tuple<int, List<int>>>();//key：wall width ，item1: wallmodel lineNo, item2: wall entity gfc lineNo;

            var wallCount = 0;

            for (int i = 0; i < site.SiteBuildings.Count; i++)
            {
                //if (i > 0)
                //{
                //    continue;
                //}

                var buildingPair = site.SiteBuildings.ElementAt(i);
                var buildingId = buildingPair.Value.ToGfc(gfcDoc, ref globelId);
                buildingStoreyGFCDict.Add(buildingId, new List<int>());

                for (int j = 0; j < buildingPair.Value.BuildingStoreys.Count; j++)
                {
                    //if (j > 0)
                    //{
                    //    continue;
                    //}

                    var floorPair = buildingPair.Value.BuildingStoreys.ElementAt(j);
                    var floorId = floorPair.Value.ToGfc(gfcDoc, ref globelId);
                    buildingStoreyGFCDict[buildingId].Add(floorId);
                    floorEntityDict.Add(floorId, new List<int>());

                    var floorHightMatrix = floorPair.Value.Matrix3D;

                    foreach (var entityRelation in floorPair.Value.FloorEntityRelations)
                    {
                        //if (wallCount > 0)
                        //{
                        //    continue;
                        //}

                        var rUid = entityRelation.Value.RelationElementUid;
                        prj.PrjAllEntitys.TryGetValue(rUid, out var entity);

                        if (entity is THBimWall wallEntity)
                        {
                            var wid = wallEntity.ToGfc(gfcDoc, floorHightMatrix, ref globelId, ref entityModel);
                            if (wid != -1)
                            {
                                floorEntityDict[floorId].Add(wid);
                                wallCount++;
                            }
                        }
                    }
                    floorEntityDict[floorId].AddRange(entityModel.Select(x => x.Value.Item1));

                }
            }

            foreach (var p in floorEntityDict)
            {
                gfcDoc.AddRelAggregate(p.Key, p.Value);
            }

            foreach (var p in buildingStoreyGFCDict)
            {
                gfcDoc.AddRelAggregate(p.Key, p.Value);
            }

            foreach (var p in entityModel)
            {
                gfcDoc.AddRelDefinesByElement(p.Value.Item1, p.Value.Item2);
            }


        }
    }
}
