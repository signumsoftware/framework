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

        [NotNullValidator]
        public Lite<BandDN> Band { get; set; }

        [StringLengthValidator(Min=3, AllowNulls=false)]
        public string Name { get; set; }

        [NumberBetweenValidatorAttribute(1000,Int32.MaxValue)]
        public int Year { get; set; }

        [NotNullValidator]
        public LabelDN Label { get; set; }
    }
}
