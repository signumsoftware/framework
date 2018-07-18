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
        [Description("{0} is already being trained")]
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
        [Description("{0} should be divisible by {1} ({2})")]
        _0ShouldBeDivisibleBy12,
        [Description("The type of {0} ({1}) does not match {2} ({3})")]
        TheTypeOf01DoesNotMatch23,
        Predict,
        [Description("There should be {0} columns with {1} {2} (currently {3})")]
        ThereShouldBe0ColumnsWith12Currently3,
        [Description("Should be of type {0}")]
        ShouldBeOfType0,
        TooManyParentKeys,
        [Description("{0} can not be {1} because {2} use {3}")]
        _0CanNotBe1Because2Use3,
        [Description("{0} is not compatible with {1} {2}")]
        _0IsNotCompatibleWith12,
        [Description("No publications for query {0} registered")]
        NoPublicationsForQuery0Registered,
        [Description("No publications process registered for {0}")]
        NoPublicationsProcessRegisteredFor0,
        [Description("Predictor is published. Untrain anyway?")]
        PredictorIsPublishedUntrainAnyway
    }
}
