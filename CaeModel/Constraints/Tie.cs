using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;

namespace CaeModel
{
    [Serializable]
    public class Tie : Constraint, IMasterSlaveMultiRegion
    {
        // Variables                                                                                                                
        private static string _positive = "The value must be larger than 0.";
        //
        private double _positionTolerance;
        private bool _adjust;
        //
        private RegionTypeEnum _slaveRegionType;
        private string _slaveSurfaceName;
        private RegionTypeEnum _masterRegionType;
        private string _masterSurfaceName;
        //
        private int[] _slaveCreationIds;
        private Selection _slaveCreationData;
        private int[] _masterCreationIds;
        private Selection _masterCreationData;


        // Properties                                                                                                               
        public double PositionTolerance
        {
            get { return _positionTolerance; }
            set { if (double.IsNaN(value) || value > 0) _positionTolerance = value; else throw new CaeException(_positive); }
        }
        public bool Adjust { get { return _adjust; } set { _adjust = value; } }
        //
        public RegionTypeEnum MasterRegionType { get { return _masterRegionType; } set { _masterRegionType = value; } }
        public string MasterRegionName { get { return _masterSurfaceName; } set { _masterSurfaceName = value; } }
        public RegionTypeEnum SlaveRegionType { get { return _slaveRegionType; } set { _slaveRegionType = value; } }
        public string SlaveRegionName { get { return _slaveSurfaceName; } set { _slaveSurfaceName = value; } }
        //
        public int[] SlaveCreationIds { get { return _slaveCreationIds; } set { _slaveCreationIds = value; } }
        public Selection SlaveCreationData { get { return _slaveCreationData; } set { _slaveCreationData = value; } }
        public int[] MasterCreationIds { get { return _masterCreationIds; } set { _masterCreationIds = value; } }
        public Selection MasterCreationData { get { return _masterCreationData; } set { _masterCreationData = value; } }


        // Constructors                                                                                                             
        public Tie(string name, string masterSurfaceName, RegionTypeEnum masterRegionType,
                   string slaveSurfaceName, RegionTypeEnum slaveRegionType)
           : this(name, double.NaN, true, masterSurfaceName, masterRegionType, slaveSurfaceName, slaveRegionType)
        {
        }
        public Tie(string name, double positionTolerance, bool adjust, string masterSurfaceName, RegionTypeEnum masterRegionType,
                   string slaveSurfaceName, RegionTypeEnum slaveRegionType)
            : base(name)
        {
            if (masterRegionType == RegionTypeEnum.SurfaceName && slaveRegionType == RegionTypeEnum.SurfaceName &&
                slaveSurfaceName == masterSurfaceName) throw new CaeException("The master and slave surface names must be different.");
            //
            PositionTolerance = positionTolerance;
            _adjust = adjust;
            //
            _masterRegionType = masterRegionType;
            _masterSurfaceName = masterSurfaceName;
            _slaveRegionType = slaveRegionType;
            _slaveSurfaceName = slaveSurfaceName;
            //
            _slaveCreationIds = null;
            _slaveCreationData = null;
            _masterCreationIds = null;
            _masterCreationData = null;
        }


        // Methods                                                                                                                  


    }
}
