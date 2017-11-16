using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        OpenTensorflowProjector,
        _0IsAlreadyBeingTrained,
        StartingTraining,
        Preview,
        Codifications,
        Progress,
        Results,
        [Description("{0} not supported for {1}")]
        _0NotSuportedFor1,
        [Description("{0} is required for {1}")]
        _0IsRequiredFor1,
        _0ShouldBeDivisibleBy12,
    }
}
