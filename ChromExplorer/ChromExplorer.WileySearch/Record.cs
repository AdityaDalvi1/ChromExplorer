using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Text;
using ChromExplorer.WileySearch.Annotations;
using Wiley;

namespace ChromExplorer.WileySearch
{
    public class Record : INotifyPropertyChanged
    {
        public int RecordID { get; set; }
        public double PrecursorIon { get; set; }
        public int SpectrumGroup { get; set; }
        public int SpectrumNo { get; set; }
        public string PrecursorType { get; set; }
        public string IonizationType { get; set; }
        public int IonizationEnergy { get; set; }
        public string Instrument { get; set; }
        public int PubchemCID { get; set; }
        public double MolWeight { get; set; }
        public double MSMolWeight { get; set; }
        public string MolFormula { get; set; }
        public string Compaund { get; set; }
        public string CASRegistryNo { get; set; }
        public string Name { get; set; }
        public string InChi { get; set; }
        public string Compoundwithalmostidenticalfragmentationpattern { get; set; }
        public string MassInt { get; set; }
        public Bitmap Structure { get; set; }
        public IList<Peak> Spectrum { get; set; }

        public string SpectrumString
        {
            get
            {
                var sb = new StringBuilder();
                foreach (var peak in Spectrum)
                {
                    sb.AppendFormat("{0:0.000}\t{1:0.000}\n", peak.Mz, peak.Intensity);
                }

                return sb.ToString();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}


/*
 * Precursor Ion
256.054

Spectrum Group
1

Spectrum No
1

Precursor Type
[M+H]+

Ionization Type
ESI+

Ionization Energy (eV)
10

Instrument
QStar XL, AB Sciex

Pubchem CID
6308684

Molecular weight
255.30

Monoisotopic Molecular Weight
255.047

Molecular Formula
C13H9N3OS

Compound
1-(2-thiazolylazo)-2-naphthol

CAS Registry NO
1147-56-4

Name
(1Z)-1-(1,3-thiazol-2-ylhydrazinylidene)naphthalen-2-one

InChI
InChI=1S/C13H9N3OS/c17-11-6-5-9-3-1-2-4-10(9)12(11)15-16-13-14-7-8-18-13/h1-8H,(H,14,16)/b15-12-

Compound with almost identical fragmentation pattern


mass %int



*/