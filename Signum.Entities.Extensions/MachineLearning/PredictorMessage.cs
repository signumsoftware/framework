using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.MachineLearning
{
    public enum PredictorMessage
    {
        Csv,
        Tsv,
        TsvMetadata,
        TensorflowProjector,
        DownloadCsv,
        DownloadTsv,
        DownloadTsvMetadata,
        OpenTensorflowProjector
    }
}
