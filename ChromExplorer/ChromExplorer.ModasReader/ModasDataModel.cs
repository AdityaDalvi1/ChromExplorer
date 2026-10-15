using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Linq;
using Mindscape.LightSpeed;
using Mindscape.LightSpeed.Linq;
using Mindscape.LightSpeed.Validation;

namespace ChromExplorer.ModasReader
{
  [Serializable]
  [GeneratedCode("LightSpeedModelGenerator", "1.0.0.0")]
  [DataObject]
  public partial class Method : Entity<int>
  {
    #region Fields
  
    private long _methodCrc;
    [ValidatePresence]
    private byte[] _binaryData;
    [ValidatePresence]
    private string _xml;

    #endregion
    
    #region Field attribute and view names
    
    /// <summary>Identifies the MethodCrc entity attribute.</summary>
    public const string MethodCrcField = "MethodCrc";
    /// <summary>Identifies the BinaryData entity attribute.</summary>
    public const string BinaryDataField = "BinaryData";
    /// <summary>Identifies the Xml entity attribute.</summary>
    public const string XmlField = "Xml";


    #endregion
    
    #region Relationships

    [ReverseAssociation("Method")]
    private readonly EntityCollection<Spectrum> _spectrums = new EntityCollection<Spectrum>();


    #endregion
    
    #region Properties

    public EntityCollection<Spectrum> Spectrums
    {
      get { return Get(_spectrums); }
    }


    public long MethodCrc
    {
      get { return Get(ref _methodCrc, "MethodCrc"); }
      set { Set(ref _methodCrc, value, "MethodCrc"); }
    }

    public byte[] BinaryData
    {
      get { return Get(ref _binaryData, "BinaryData"); }
      set { Set(ref _binaryData, value, "BinaryData"); }
    }

    public string Xml
    {
      get { return Get(ref _xml, "Xml"); }
      set { Set(ref _xml, value, "Xml"); }
    }

    #endregion
  }


  [Serializable]
  [GeneratedCode("LightSpeedModelGenerator", "1.0.0.0")]
  [DataObject]
  public partial class Calibrations : Entity<int>
  {
    #region Fields
  
    private Nullable<double> _p0;
    private Nullable<double> _p1;
    private Nullable<double> _p2;
    private Nullable<double> _p3;
    private Nullable<double> _p4;
    private Nullable<double> _p5;
    private Nullable<double> _p6;
    private Nullable<double> _p7;
    private Nullable<double> _p8;
    private Nullable<double> _p9;
    private int _type;
    [ValidatePresence]
    private string _codeName;
    private byte[] _rawData;

    #endregion
    
    #region Field attribute and view names
    
    /// <summary>Identifies the P0 entity attribute.</summary>
    public const string P0Field = "P0";
    /// <summary>Identifies the P1 entity attribute.</summary>
    public const string P1Field = "P1";
    /// <summary>Identifies the P2 entity attribute.</summary>
    public const string P2Field = "P2";
    /// <summary>Identifies the P3 entity attribute.</summary>
    public const string P3Field = "P3";
    /// <summary>Identifies the P4 entity attribute.</summary>
    public const string P4Field = "P4";
    /// <summary>Identifies the P5 entity attribute.</summary>
    public const string P5Field = "P5";
    /// <summary>Identifies the P6 entity attribute.</summary>
    public const string P6Field = "P6";
    /// <summary>Identifies the P7 entity attribute.</summary>
    public const string P7Field = "P7";
    /// <summary>Identifies the P8 entity attribute.</summary>
    public const string P8Field = "P8";
    /// <summary>Identifies the P9 entity attribute.</summary>
    public const string P9Field = "P9";
    /// <summary>Identifies the Type entity attribute.</summary>
    public const string TypeField = "Type";
    /// <summary>Identifies the CodeName entity attribute.</summary>
    public const string CodeNameField = "CodeName";
    /// <summary>Identifies the RawData entity attribute.</summary>
    public const string RawDataField = "RawData";


    #endregion
    
    #region Relationships

    [ReverseAssociation("Calibrations")]
    private readonly EntityCollection<SpetrumCalibrations> _spetrumCalibrations = new EntityCollection<SpetrumCalibrations>();
    [ReverseAssociation("Calibrations")]
    private readonly EntityCollection<Spectrum> _spectrums = new EntityCollection<Spectrum>();


    #endregion
    
    #region Properties

    public EntityCollection<SpetrumCalibrations> SpetrumCalibrations
    {
      get { return Get(_spetrumCalibrations); }
    }

    public EntityCollection<Spectrum> Spectrums
    {
      get { return Get(_spectrums); }
    }


    public Nullable<double> P0
    {
      get { return Get(ref _p0, "P0"); }
      set { Set(ref _p0, value, "P0"); }
    }

    public Nullable<double> P1
    {
      get { return Get(ref _p1, "P1"); }
      set { Set(ref _p1, value, "P1"); }
    }

    public Nullable<double> P2
    {
      get { return Get(ref _p2, "P2"); }
      set { Set(ref _p2, value, "P2"); }
    }

    public Nullable<double> P3
    {
      get { return Get(ref _p3, "P3"); }
      set { Set(ref _p3, value, "P3"); }
    }

    public Nullable<double> P4
    {
      get { return Get(ref _p4, "P4"); }
      set { Set(ref _p4, value, "P4"); }
    }

    public Nullable<double> P5
    {
      get { return Get(ref _p5, "P5"); }
      set { Set(ref _p5, value, "P5"); }
    }

    public Nullable<double> P6
    {
      get { return Get(ref _p6, "P6"); }
      set { Set(ref _p6, value, "P6"); }
    }

    public Nullable<double> P7
    {
      get { return Get(ref _p7, "P7"); }
      set { Set(ref _p7, value, "P7"); }
    }

    public Nullable<double> P8
    {
      get { return Get(ref _p8, "P8"); }
      set { Set(ref _p8, value, "P8"); }
    }

    public Nullable<double> P9
    {
      get { return Get(ref _p9, "P9"); }
      set { Set(ref _p9, value, "P9"); }
    }

    public int Type
    {
      get { return Get(ref _type, "Type"); }
      set { Set(ref _type, value, "Type"); }
    }

    public string CodeName
    {
      get { return Get(ref _codeName, "CodeName"); }
      set { Set(ref _codeName, value, "CodeName"); }
    }

    public byte[] RawData
    {
      get { return Get(ref _rawData, "RawData"); }
      set { Set(ref _rawData, value, "RawData"); }
    }

    #endregion
  }


  [Serializable]
  [GeneratedCode("LightSpeedModelGenerator", "1.0.0.0")]
  [DataObject]
  public partial class Spectrum : Entity<int>
  {
    #region Fields
  
    private DateTime _timestamp;
    private int _dataSize;
    private long _dataCrc;
    private double _tic;
    private int _basePeakIndex;
    private double _elapsedMilliseconds;
    private double _digitizerSampleRate;
    private double _basePeakIntensity;
    private int _msPower;
    private double _parentIonMass;
    private double _injectionTime;
    private Nullable<int> _methodId;
    private Nullable<int> _calibrationsId;

    #endregion
    
    #region Field attribute and view names
    
    /// <summary>Identifies the Timestamp entity attribute.</summary>
    public const string TimestampField = "Timestamp";
    /// <summary>Identifies the DataSize entity attribute.</summary>
    public const string DataSizeField = "DataSize";
    /// <summary>Identifies the DataCrc entity attribute.</summary>
    public const string DataCrcField = "DataCrc";
    /// <summary>Identifies the Tic entity attribute.</summary>
    public const string TicField = "Tic";
    /// <summary>Identifies the BasePeakIndex entity attribute.</summary>
    public const string BasePeakIndexField = "BasePeakIndex";
    /// <summary>Identifies the ElapsedMilliseconds entity attribute.</summary>
    public const string ElapsedMillisecondsField = "ElapsedMilliseconds";
    /// <summary>Identifies the DigitizerSampleRate entity attribute.</summary>
    public const string DigitizerSampleRateField = "DigitizerSampleRate";
    /// <summary>Identifies the BasePeakIntensity entity attribute.</summary>
    public const string BasePeakIntensityField = "BasePeakIntensity";
    /// <summary>Identifies the MsPower entity attribute.</summary>
    public const string MsPowerField = "MsPower";
    /// <summary>Identifies the ParentIonMass entity attribute.</summary>
    public const string ParentIonMassField = "ParentIonMass";
    /// <summary>Identifies the InjectionTime entity attribute.</summary>
    public const string InjectionTimeField = "InjectionTime";
    /// <summary>Identifies the MethodId entity attribute.</summary>
    public const string MethodIdField = "MethodId";
    /// <summary>Identifies the CalibrationsId entity attribute.</summary>
    public const string CalibrationsIdField = "CalibrationsId";


    #endregion
    
    #region Relationships

    [ReverseAssociation("Spectrum")]
    private readonly EntityCollection<SpectrumData> _spectrumData = new EntityCollection<SpectrumData>();
    [ReverseAssociation("Spectrum")]
    private readonly EntityCollection<SpetrumCalibrations> _spetrumCalibrations = new EntityCollection<SpetrumCalibrations>();
    [ReverseAssociation("Spectrums")]
    private readonly EntityHolder<Method> _method = new EntityHolder<Method>();
    [ReverseAssociation("Spectrums")]
    private readonly EntityHolder<Calibrations> _calibrations = new EntityHolder<Calibrations>();


    #endregion
    
    #region Properties

    public EntityCollection<SpectrumData> SpectrumData
    {
      get { return Get(_spectrumData); }
    }

    public EntityCollection<SpetrumCalibrations> SpetrumCalibrations
    {
      get { return Get(_spetrumCalibrations); }
    }

    public Method Method
    {
      get { return Get(_method); }
      set { Set(_method, value); }
    }

    public Calibrations Calibrations
    {
      get { return Get(_calibrations); }
      set { Set(_calibrations, value); }
    }


    public DateTime Timestamp
    {
      get { return Get(ref _timestamp, "Timestamp"); }
      set { Set(ref _timestamp, value, "Timestamp"); }
    }

    public int DataSize
    {
      get { return Get(ref _dataSize, "DataSize"); }
      set { Set(ref _dataSize, value, "DataSize"); }
    }

    public long DataCrc
    {
      get { return Get(ref _dataCrc, "DataCrc"); }
      set { Set(ref _dataCrc, value, "DataCrc"); }
    }

    public double Tic
    {
      get { return Get(ref _tic, "Tic"); }
      set { Set(ref _tic, value, "Tic"); }
    }

    public int BasePeakIndex
    {
      get { return Get(ref _basePeakIndex, "BasePeakIndex"); }
      set { Set(ref _basePeakIndex, value, "BasePeakIndex"); }
    }

    public double ElapsedMilliseconds
    {
      get { return Get(ref _elapsedMilliseconds, "ElapsedMilliseconds"); }
      set { Set(ref _elapsedMilliseconds, value, "ElapsedMilliseconds"); }
    }

    public double DigitizerSampleRate
    {
      get { return Get(ref _digitizerSampleRate, "DigitizerSampleRate"); }
      set { Set(ref _digitizerSampleRate, value, "DigitizerSampleRate"); }
    }

    public double BasePeakIntensity
    {
      get { return Get(ref _basePeakIntensity, "BasePeakIntensity"); }
      set { Set(ref _basePeakIntensity, value, "BasePeakIntensity"); }
    }

    public int MsPower
    {
      get { return Get(ref _msPower, "MsPower"); }
      set { Set(ref _msPower, value, "MsPower"); }
    }

    public double ParentIonMass
    {
      get { return Get(ref _parentIonMass, "ParentIonMass"); }
      set { Set(ref _parentIonMass, value, "ParentIonMass"); }
    }

    public double InjectionTime
    {
      get { return Get(ref _injectionTime, "InjectionTime"); }
      set { Set(ref _injectionTime, value, "InjectionTime"); }
    }

    /// <summary>Gets or sets the ID for the <see cref="Method" /> property.</summary>
    public Nullable<int> MethodId
    {
      get { return Get(ref _methodId, "MethodId"); }
      set { Set(ref _methodId, value, "MethodId"); }
    }

    /// <summary>Gets or sets the ID for the <see cref="Calibrations" /> property.</summary>
    public Nullable<int> CalibrationsId
    {
      get { return Get(ref _calibrationsId, "CalibrationsId"); }
      set { Set(ref _calibrationsId, value, "CalibrationsId"); }
    }

    #endregion
  }


  [Serializable]
  [GeneratedCode("LightSpeedModelGenerator", "1.0.0.0")]
  [DataObject]
  public partial class SpectrumData : Entity<int>
  {
    #region Fields
  
    private byte[] _data;
    private int _spectrumId;

    #endregion
    
    #region Field attribute and view names
    
    /// <summary>Identifies the Data entity attribute.</summary>
    public const string DataField = "Data";
    /// <summary>Identifies the SpectrumId entity attribute.</summary>
    public const string SpectrumIdField = "SpectrumId";


    #endregion
    
    #region Relationships

    [ReverseAssociation("SpectrumData")]
    private readonly EntityHolder<Spectrum> _spectrum = new EntityHolder<Spectrum>();


    #endregion
    
    #region Properties

    public Spectrum Spectrum
    {
      get { return Get(_spectrum); }
      set { Set(_spectrum, value); }
    }


    public byte[] Data
    {
      get { return Get(ref _data, "Data"); }
      set { Set(ref _data, value, "Data"); }
    }

    /// <summary>Gets or sets the ID for the <see cref="Spectrum" /> property.</summary>
    public int SpectrumId
    {
      get { return Get(ref _spectrumId, "SpectrumId"); }
      set { Set(ref _spectrumId, value, "SpectrumId"); }
    }

    #endregion
  }


  [Serializable]
  [GeneratedCode("LightSpeedModelGenerator", "1.0.0.0")]
  [DataObject]
  public partial class SpetrumCalibrations : Entity<int>
  {
    #region Fields
  
    private int _calibrationsId;
    private int _spectrumId;

    #endregion
    
    #region Field attribute and view names
    
    /// <summary>Identifies the CalibrationsId entity attribute.</summary>
    public const string CalibrationsIdField = "CalibrationsId";
    /// <summary>Identifies the SpectrumId entity attribute.</summary>
    public const string SpectrumIdField = "SpectrumId";


    #endregion
    
    #region Relationships

    [ReverseAssociation("SpetrumCalibrations")]
    private readonly EntityHolder<Calibrations> _calibrations = new EntityHolder<Calibrations>();
    [ReverseAssociation("SpetrumCalibrations")]
    private readonly EntityHolder<Spectrum> _spectrum = new EntityHolder<Spectrum>();


    #endregion
    
    #region Properties

    public Calibrations Calibrations
    {
      get { return Get(_calibrations); }
      set { Set(_calibrations, value); }
    }

    public Spectrum Spectrum
    {
      get { return Get(_spectrum); }
      set { Set(_spectrum, value); }
    }


    /// <summary>Gets or sets the ID for the <see cref="Calibrations" /> property.</summary>
    public int CalibrationsId
    {
      get { return Get(ref _calibrationsId, "CalibrationsId"); }
      set { Set(ref _calibrationsId, value, "CalibrationsId"); }
    }

    /// <summary>Gets or sets the ID for the <see cref="Spectrum" /> property.</summary>
    public int SpectrumId
    {
      get { return Get(ref _spectrumId, "SpectrumId"); }
      set { Set(ref _spectrumId, value, "SpectrumId"); }
    }

    #endregion
  }


  [Serializable]
  [GeneratedCode("LightSpeedModelGenerator", "1.0.0.0")]
  [DataObject]
  public partial class ModasVersion : Entity<int>
  {
    #region Fields
  
    private string _name;
    private Nullable<int> _major;
    private Nullable<int> _minor;
    private Nullable<int> _revision;

    #endregion
    
    #region Field attribute and view names
    
    /// <summary>Identifies the Name entity attribute.</summary>
    public const string NameField = "Name";
    /// <summary>Identifies the Major entity attribute.</summary>
    public const string MajorField = "Major";
    /// <summary>Identifies the Minor entity attribute.</summary>
    public const string MinorField = "Minor";
    /// <summary>Identifies the Revision entity attribute.</summary>
    public const string RevisionField = "Revision";


    #endregion
    
    #region Properties



    public string Name
    {
      get { return Get(ref _name, "Name"); }
      set { Set(ref _name, value, "Name"); }
    }

    public Nullable<int> Major
    {
      get { return Get(ref _major, "Major"); }
      set { Set(ref _major, value, "Major"); }
    }

    public Nullable<int> Minor
    {
      get { return Get(ref _minor, "Minor"); }
      set { Set(ref _minor, value, "Minor"); }
    }

    public Nullable<int> Revision
    {
      get { return Get(ref _revision, "Revision"); }
      set { Set(ref _revision, value, "Revision"); }
    }

    #endregion
  }



  /// <summary>
  /// Provides a strong-typed unit of work for working with the ModasDataModel model.
  /// </summary>
  [GeneratedCode("LightSpeedModelGenerator", "1.0.0.0")]
  public partial class ModasDataModelUnitOfWork : UnitOfWork
  {

    public IQueryable<Method> Methods
    {
      get { return this.Query<Method>(); }
    }
    
    public IQueryable<Calibrations> Calibrations
    {
      get { return this.Query<Calibrations>(); }
    }
    
    public IQueryable<Spectrum> Spectrums
    {
      get { return this.Query<Spectrum>(); }
    }
    
    public IQueryable<SpectrumData> SpectrumDatas
    {
      get { return this.Query<SpectrumData>(); }
    }
    
    public IQueryable<SpetrumCalibrations> SpetrumCalibrations
    {
      get { return this.Query<SpetrumCalibrations>(); }
    }
    
    public IQueryable<ModasVersion> ModasVersions
    {
      get { return this.Query<ModasVersion>(); }
    }
    
  }

}
