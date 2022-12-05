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
            var docPath = @"D:\project\14.ThBim\chart\try.gfc";
            var gfcDoc = ThGFCDocument.Create(docPath);

            if (prj.ProjectSite == null)
            {
                return;
            }

            PrjToGFC(prj, gfcDoc);

            gfcDoc.Close();
        }

        private static void PrjToGFC(THBimProject prj, ThGFCDocument gfcDoc)
        {
            prj.ToGfc(gfcDoc);

            var site = prj.ProjectSite;

            var buildingStoreyGFCDict = new Dictionary<int, List<int>>();//building gfc id，floor gfc ID;
            var floorEntityDict = new Dictionary<int, List<int>>();//floor gfc id，entity gfc ID;

            foreach (var buildingPair in site.SiteBuildings)
            {
                var buildingId = buildingPair.Value.ToGfc(gfcDoc);
                buildingStoreyGFCDict.Add(buildingId, new List<int>());

                foreach (var floorPair in buildingPair.Value.BuildingStoreys)
                {
                    var floorId = floorPair.Value.ToGfc(gfcDoc);
                    buildingStoreyGFCDict[buildingId].Add(floorId);
                    floorEntityDict.Add(floorId, new List<int>());

                    var floorHightMatrix = floorPair.Value.Matrix3D;
                    foreach (var entityRelation in floorPair.Value.FloorEntityRelations)
                    {
                        var rUid = entityRelation.Value.RelationElementUid;
                        prj.PrjAllEntitys.TryGetValue(rUid, out var entity);
                        if (entity is THBimWall wallEntity)
                        {
                            var wid = wallEntity.ToGfc(gfcDoc, floorHightMatrix);
                            floorEntityDict[floorId].Add(wid);
                        }
                    }
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



        }
    }
}
