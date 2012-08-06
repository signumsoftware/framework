using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Test;
using Signum.Entities;

namespace Signum.Web.Extensions.Sample
{
    public class AlbumFromBandModel : ModelEntity
    {
        public AlbumFromBandModel()
        { 
        }

        public AlbumFromBandModel(Lite<BandDN> band)
        {
            Band = band;
        }

        Lite<BandDN> band;
        [NotNullValidator]
        public Lite<BandDN> Band
        {
            get { return band; }
            set { Set(ref band, value, () => Band); }
        }

        string name;
        [StringLengthValidator(Min=3, AllowNulls=false)]
        public string Name
        {
            get { return name; }
            set { Set(ref name, value, () => Name); }
        }

        int year;
        [NumberBetweenValidatorAttribute(1000,Int32.MaxValue)]
        public int Year
        {
            get { return year; }
            set { Set(ref year, value, () => Year); }
        }

        LabelDN label;
        [NotNullValidator]
        public LabelDN Label
        {
            get { return label; }
            set { Set(ref label, value, () => Label); }
        }
    }
}
