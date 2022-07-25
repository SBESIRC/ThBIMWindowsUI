using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using System;

namespace THBimEngine.Geometry.NTS
{
    public class EngineNTSService
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly EngineNTSService instance = new EngineNTSService() { PrecisionReduce = false };
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static EngineNTSService() { }
        internal EngineNTSService() { }
        public static EngineNTSService Instance { get { return instance; } }
        //-------------SINGLETON-----------------
        public bool PrecisionReduce { get; set; }

        public double AcadGlobalTolerance = 1e-8;
        public double ChordHeightTolerance = 50.0;
        public double ArcTessellationLength { get; set; } = 1000.0;

        private NetTopologySuite.Geometries.GeometryFactory geometryFactory;
        private NetTopologySuite.Geometries.GeometryFactory defaultGeometryFactory;
        public NetTopologySuite.Geometries.GeometryFactory GeometryFactory
        {
            get
            {
                if (PrecisionReduce)
                {
                    if (geometryFactory == null)
                    {
                        geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(PrecisionModel);
                    }
                    return geometryFactory;

                }
                else
                {
                    if (defaultGeometryFactory == null)
                    {
                        defaultGeometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory();
                    }
                    return defaultGeometryFactory;
                }
            }
        }

        private PreparedGeometryFactory preparedGeometryFactory;
        public PreparedGeometryFactory PreparedGeometryFactory
        {
            get
            {
                if (preparedGeometryFactory == null)
                {
                    preparedGeometryFactory = new PreparedGeometryFactory();
                }
                return preparedGeometryFactory;
            }
        }

        private Lazy<PrecisionModel> precisionModel;
        public PrecisionModel PrecisionModel
        {
            get
            {
                if (PrecisionReduce)
                {
                    if (precisionModel == null)
                    {
                        precisionModel = PrecisionModel.Fixed;
                    }
                    return precisionModel.Value;
                }
                else
                {
                    return NtsGeometryServices.Instance.DefaultPrecisionModel;
                }
            }
        }
    }
}
