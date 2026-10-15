using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Linq;
using Mindscape.LightSpeed;
using Mindscape.LightSpeed.Linq;

namespace ChromExplorer.DataModel.DAL
{
  [Serializable]
  [GeneratedCode("LightSpeedModelGenerator", "1.0.0.0")]
  [DataObject]
  public partial class SpectrumData : Entity<int>
  {
    #region Fields
  
    private int _spectrumIndex;
    private int _pointIndex;
    private double _mz;
    private double _intensity;

    #endregion
    
    #region Field attribute and view names
    
    /// <summary>Identifies the SpectrumIndex entity attribute.</summary>
    public const string SpectrumIndexField = "SpectrumIndex";
    /// <summary>Identifies the PointIndex entity attribute.</summary>
    public const string PointIndexField = "PointIndex";
    /// <summary>Identifies the Mz entity attribute.</summary>
    public const string MzField = "Mz";
    /// <summary>Identifies the Intensity entity attribute.</summary>
    public const string IntensityField = "Intensity";


    #endregion
    
    #region Properties



    public int SpectrumIndex
    {
      get { return Get(ref _spectrumIndex, "SpectrumIndex"); }
      set { Set(ref _spectrumIndex, value, "SpectrumIndex"); }
    }

    public int PointIndex
    {
      get { return Get(ref _pointIndex, "PointIndex"); }
      set { Set(ref _pointIndex, value, "PointIndex"); }
    }

    public double Mz
    {
      get { return Get(ref _mz, "Mz"); }
      set { Set(ref _mz, value, "Mz"); }
    }

    public double Intensity
    {
      get { return Get(ref _intensity, "Intensity"); }
      set { Set(ref _intensity, value, "Intensity"); }
    }

    #endregion
  }



  /// <summary>
  /// Provides a strong-typed unit of work for working with the ChromExplorerStore model.
  /// </summary>
  [GeneratedCode("LightSpeedModelGenerator", "1.0.0.0")]
  public partial class ChromExplorerStoreUnitOfWork : UnitOfWork
  {

    public IQueryable<SpectrumData> SpectrumDatas
    {
      get { return this.Query<SpectrumData>(); }
    }
    
  }

}
