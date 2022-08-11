using System.Collections.Generic;
using System.Threading.Tasks;
using THBimEngine.Domain;
using Xbim.Common.Geometry;
using Xbim.Common.Step21;

namespace THBimEngine.Geometry.ProjectFactory
{
    public abstract class ConvertFactoryBase
    {
        /// <summary>
        /// Ifc版本信息
        /// </summary>
        protected IfcSchemaVersion schemaVersion;
        protected int globalIndex = 0;
        /// <summary>
        /// 项目信息
        /// </summary>
        protected THBimProject bimProject;
        /// <summary>
        /// 项目中所有的实体信息
        /// </summary>
        protected Dictionary<string,THBimEntity> allEntitys;
        /// <summary>
        /// 项目中非标层，和标准层首层楼层数据
        /// </summary>
        protected Dictionary<string, THBimStorey> prjEntityFloors;
        /// <summary>
        /// 项目中所有楼层信息
        /// </summary>
        protected Dictionary<string, THBimStorey> allStoreys;
        public ConvertFactoryBase(IfcSchemaVersion ifcSchemaVersion) 
        {
            allEntitys = new Dictionary<string,THBimEntity>();
            schemaVersion = ifcSchemaVersion;
            InitOrClearData();
        }
        public abstract ConvertResult ProjectConvert(object prject,bool createSolidMesh);
        public virtual void CreateSolidMesh(List<THBimEntity> meshEntitys)
        {
            //step2 转换每个实体的Solid;
            Parallel.ForEach(meshEntitys, new ParallelOptions(), entity =>
            {
                if (entity == null)
                    return;
                var geometryFactory = new GeometryFactory(schemaVersion);
                if (entity is THBimIFCEntity ifcEntity) 
                {
                    //geometryFactory.GetXBimSolid(ifcEntity.IfcEntity);
                }
                else if (entity is THBimSlab slab)
                {
                    var solids = geometryFactory.GetSlabSolid(entity.GeometryParam, slab.SlabDescendingDatas, XbimVector3D.Zero);
                    if (null != solids && solids.Count > 0)
                        entity.EntitySolids.AddRange(solids);
                }
                else
                {
                    var solids = geometryFactory.GetXBimSolid(entity.GeometryParam, XbimVector3D.Zero);
                    if (null != solids && solids.Count > 0)
                        entity.EntitySolids.AddRange(solids);
                }

            });
            //step3 Solid剪切和Mesh
            Parallel.ForEach(meshEntitys, new ParallelOptions(), entity =>
            {
                if (entity == null)
                    return;
                GeometryFactory geometryFactory = new GeometryFactory(schemaVersion);
                var openingSolds = new List<IXbimSolid>();
                foreach (var opening in entity.Openings)
                {
                    if (opening.EntitySolids.Count < 1)
                    {
                        var opEntity = meshEntitys.Find(c=>c.Uid == opening.Uid);
                        if (null != opEntity) 
                        {
                            foreach (var solid in opEntity.EntitySolids)
                                openingSolds.Add(solid);
                        }
                    }
                    else 
                    {
                        foreach (var solid in opening.EntitySolids)
                            openingSolds.Add(solid);
                    }
                }
                entity.ShapeGeometry = geometryFactory.GetShapeGeometry(entity.EntitySolids, openingSolds);
            });
        }
        protected virtual void InitOrClearData() 
        {
            prjEntityFloors = new Dictionary<string, THBimStorey>();
            allStoreys = new Dictionary<string, THBimStorey>();
            globalIndex = 0;
            allEntitys = new Dictionary<string,THBimEntity>();
            bimProject = null;
        }
        protected int CurrentGIndex()
        {
            var res = globalIndex;
            globalIndex += 1;
            return res;
        }
    }
    
    public class ConvertResult
    {
        public THBimProject BimProject { get; set; }
        public Dictionary<string, THBimEntity> ProjectEntitys { get; }
        public Dictionary<string, THBimStorey> ProjectStoreys { get; }
        public ConvertResult()
        {
            ProjectEntitys = new Dictionary<string, THBimEntity>();
            ProjectStoreys = new Dictionary<string, THBimStorey>();
        }
        public ConvertResult(THBimProject bimProject, Dictionary<string, THBimStorey> storeys, Dictionary<string, THBimEntity> entitys) : this()
        {
            BimProject = bimProject;
            if (null != storeys && storeys.Count > 0)
            {
                foreach (var item in storeys)
                {
                    if (item.Value == null)
                        continue;
                    ProjectStoreys.Add(item.Key, item.Value);
                }
            }
            if (null != entitys && entitys.Count > 0)
            {
                foreach (var item in entitys)
                {
                    if (item.Value == null)
                        continue;
                    ProjectEntitys.Add(item.Key, item.Value);
                }
            }
        }
    }
}
