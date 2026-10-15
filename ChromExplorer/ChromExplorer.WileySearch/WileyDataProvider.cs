using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wiley;

namespace ChromExplorer.WileySearch
{
    public enum IonizationMode
    {
        Positive,
        Negative
    }

    public class WileyDataProvider
    {
        private readonly MSforID m_library;

        public static string WileyLibPath
        {
            get
            {
                string fullPath = System.Reflection.Assembly.GetAssembly(typeof(WileyDataProvider)).Location;
                string theDirectory = Path.GetDirectoryName(fullPath);



                return theDirectory != null ? Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "x86", "MSLibProcessor.dll") : string.Empty;
            }
        }

        public static string WileyPositivePath
        {
            get
            {
                string fullPath = System.Reflection.Assembly.GetAssembly(typeof(WileyDataProvider)).Location;
                string theDirectory = Path.GetDirectoryName(fullPath);

                return theDirectory != null ? Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "positive_mode.sdf.bin") : string.Empty;
            }
        }

        public static string WileyNegativePath
        {
            get
            {
                string fullPath = System.Reflection.Assembly.GetAssembly(typeof(WileyDataProvider)).Location;
                string theDirectory = Path.GetDirectoryName(fullPath);

                return theDirectory != null ? Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "negative_mode.sdf.bin") : string.Empty;
            }
        }


        public WileyDataProvider(IonizationMode mode = IonizationMode.Positive)
        {
            m_library = new MSforID(WileyLibPath);

            var dbPath = mode == IonizationMode.Positive ? WileyPositivePath : WileyNegativePath;

            m_library.InitializeMSLibrary(dbPath);

        }

        public int RecordCount
        {
            get { return m_library.RecordCount; }
        }

        public string GetCompound(int record)
        {
            return m_library.GetStringValue(record, "Compound");
        }

        public string GetFieldAsSting(int record, string fieldName)
        {
            return m_library.GetStringValue(record, fieldName);
        }

        public Record GetRecord(int record, int width = 128, int height = 128)
        {
            var retval = new Record();

            retval.RecordID = record;

            retval.PrecursorIon = m_library.GetDoubleValue(record, "PrecursorIon");
            retval.SpectrumGroup = IntParse(m_library.GetStringValue(record, "SpectrumGroup"));
            retval.SpectrumNo = IntParse(m_library.GetStringValue(record, "SpectrumNo"));
            retval.PrecursorType = m_library.GetStringValue(record, "PrecursorType");
            retval.IonizationType = m_library.GetStringValue(record, "IonizationType");
            retval.IonizationEnergy = IntParse(m_library.GetStringValue(record, "IonizationEnergy"));
            retval.Instrument = m_library.GetStringValue(record, "Instrument");
            retval.PubchemCID = IntParse(m_library.GetStringValue(record, "CID"));
            retval.MSMolWeight = m_library.GetDoubleValue(record, "MSMolWeight");
            retval.MolWeight = m_library.GetDoubleValue(record, "MolWeight");
            retval.MolFormula = m_library.GetStringValue(record, "MolformulaString");
            retval.Compaund = m_library.GetStringValue(record, "Compound");
            retval.CASRegistryNo = m_library.GetStringValue(record, "CASRegistryNo");
            retval.InChi = m_library.GetStringValue(record, "InChi");
            retval.Name = m_library.GetStringValue(record, "Name");
            retval.Structure = m_library.GetStructuralImage(record, width, height);
            retval.Spectrum = m_library.GetMassSpectrum(record);
            /*

            
            
        
          
            Assert.AreEqual("2784-73-8", record.CASRegistryNo);
            Assert.AreEqual("InChI=1S/C19H21NO4/c1-10(21)23-15-6-4-12-13-9-11-3-5-14(22)17-16(11)19(12,18(15)24-17)7-8-20(13)2/h3-6,12-13,15,18,22H,7-9H2,1-2H3/t12-,13+,15-,18-,19-/m0/s1", record.InChi);
            Assert.AreEqual("", record.Name);
             * */


           

            return retval;
        }

        private int IntParse(string value, int dValue = 0)
        {
            int v;
            return int.TryParse(value, out v) ? v : dValue;
        }
    }
}

/*
 * 
            retval.CASRegistryNo = m_library.GetStringValue(record, "CASRegistryNO");
            retval.Compaund = m_library.GetStringValue(record, "Compound");
            retval.Compoundwithalmostidenticalfragmentationpattern = m_library.GetStringValue(record, "Compoundwithalmostidenticalfragmentationpattern");
            retval.PrecursorIon = m_library.GetDoubleValue(record, "PrecursorIon");

 * */

/*
 0:	MassSpectrum	MassSpectrum //Mass-Spectrum
1:	PrecursorIon	Double //Precursor Ion
2:	SpectrumGroup	Longint //Spectrum Group
3:	SpectrumNo	Longint //Spectrum No
4:	PrecursorType	String //Precursor Type
5:	IonizationType	String //Ionization Type
6:	IonizationEnergy	Longint //Ionization Energy (eV)
7:	Instrument	String //Instrument
8:	CID	Longint //Pubchem CID
9:	Structure	ChemicalStructure //Structure
10:	Molformula Binary	MolecularFormula //Structure
11:	Molweight	Floating //Molecular weight
12:	MSMolweight	Double //Monoisotopic Molecular Weight
13:	MolformulaString	String //Molecular Formula
14:	StructureImage	Picture //Structure Image
15:	Compound	String //Compound
16:	CASRegistryNO	String //CAS Registry NO
17:	Name	Text //Name
18:	InChI	Text //InChI
19:	Compoundwithalmostidenticalfragmentationpattern	Text //Compound with almost identical fragmentation pattern
20:	massint	String //mass %int*/