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



            foreach (var buildingPair in site.SiteBuildings)
            {
                buildingPair.Value.ToGfc(gfcDoc);
                var storeyDict = new Dictionary<string, int>();//storey uid，gfc ID;

                foreach (var floorPair in buildingPair.Value.BuildingStoreys)
                {
                    var floorId = floorPair.Value.ToGfc(gfcDoc);
                    storeyDict.Add(floorPair.Value.Uid, floorId);
                    var entityDict = new Dictionary<string, int>();

                    foreach (var entityRelation in floorPair.Value.FloorEntityRelations)
                    {
                        var rUid = entityRelation.Value.RelationElementUid;
                        prj.PrjAllEntitys.TryGetValue(rUid, out var entity);
                        if (entity is THBimWall wallEntity)
                        {
                            var wid = wallEntity.ToGfc(gfcDoc);
                            entityDict.Add(entity.Uid, wid);
                        }
                    }



                }
            }


        }
    }
}
