using Tensorflow;
using static Tensorflow.Binding;

namespace Signum.MachineLearning.TensorFlow;

public static class NetworkBuilder
{
    public static Tensor DenseLayer(Tensor input, int outputDim, NeuralNetworkActivation activation, NeuralNetworkInitializer initializer, int seed, string name)
    {
        //if (input.shape.Rank != 1)
        //{
        //    int newDim = input.Shape.Dimensions.Aggregate((d1, d2) => d1 * d2);
        //    input = CNTKLib.Reshape(input, new int[] { newDim });
        //}

        return tf_with(tf.variable_scope(name), delegate
        {
            Tensor fullyConnected = FullyConnectedLinearLayer(input, outputDim, initializer, seed);
            switch (activation)
            {
                case NeuralNetworkActivation.None: return fullyConnected;
                case NeuralNetworkActivation.ReLU: return tf.nn.relu(fullyConnected, "ReLU");
                case NeuralNetworkActivation.Sigmoid: return tf.nn.sigmoid(fullyConnected, "Sigmoid");
                case NeuralNetworkActivation.Tanh: return tf.nn.tanh(fullyConnected, "Tanh");
                default: throw new InvalidOperationException("Unexpected activation " + activation);
            }
        });
    }

    public static Tensor FullyConnectedLinearLayer(Tensor input, int outputDim, NeuralNetworkInitializer initializer, int seed)
    {
        int inputDim = (int)input.shape[1];
        
        IInitializer? init = GetInitializer(initializer, seed);
        IVariableV1 W = tf.compat.v1.get_variable("W", new int[] { inputDim, outputDim }, tf.float32, init);

        IVariableV1 b = tf.compat.v1.get_variable("b", new int[] { outputDim }, tf.float32, init);
        return tf.matmul(input, W.AsTensor()) + b.AsTensor();
    }

    private static IInitializer GetInitializer(NeuralNetworkInitializer initializer, int seed)
    {
        switch (initializer)
        {
            case NeuralNetworkInitializer.glorot_uniform_initializer: return tf.glorot_uniform_initializer;
            case NeuralNetworkInitializer.ones_initializer: return tf.ones_initializer;
            case NeuralNetworkInitializer.zeros_initializer: return tf.zeros_initializer;
            case NeuralNetworkInitializer.random_uniform_initializer: return tf.random_uniform_initializer;
            case NeuralNetworkInitializer.orthogonal_initializer: return tf.orthogonal_initializer;
            case NeuralNetworkInitializer.random_normal_initializer: return tf.random_normal_initializer(seed: seed);
            case NeuralNetworkInitializer.truncated_normal_initializer: return tf.truncated_normal_initializer(seed: seed);
            case NeuralNetworkInitializer.variance_scaling_initializer: return tf.variance_scaling_initializer(seed: seed);

            default:
                throw new InvalidOperationException("");
        }
    }



    public static Tensor GetEvalFunction(NeuralNetworkEvalFunction lossFunction, Tensor labels, Tensor calculatedOutputs)
    {
        switch (lossFunction)
        {
            case NeuralNetworkEvalFunction.sigmoid_cross_entropy_with_logits: return tf.reduce_mean(tf.nn.sigmoid_cross_entropy_with_logits(labels, calculatedOutputs));
            case NeuralNetworkEvalFunction.softmax_cross_entropy_with_logits: return tf.reduce_mean(tf.nn.softmax_cross_entropy_with_logits(labels, calculatedOutputs));
            case NeuralNetworkEvalFunction.softmax_cross_entropy_with_logits_v2: return tf.reduce_mean(tf.nn.softmax_cross_entropy_with_logits_v2(labels, calculatedOutputs));
            case NeuralNetworkEvalFunction.ClassificationError: return NetworkBuilder.ClassificationError(calculatedOutputs, labels);
            case NeuralNetworkEvalFunction.MeanSquaredError: return MeanSquaredError(labels, calculatedOutputs);
            case NeuralNetworkEvalFunction.MeanAbsoluteError: return NetworkBuilder.MeanAbsoluteError(calculatedOutputs, labels);
            case NeuralNetworkEvalFunction.MeanAbsolutePercentageError: return NetworkBuilder.MeanAbsolutePercentageError(calculatedOutputs, labels);
            default:
                throw new InvalidOperationException("Unexpected " + lossFunction);
        }
    }

    private static Tensor ClassificationError(Tensor calculatedOutputs, Tensor labels)
    {
        var correct_pred = tf.equal(tf.arg_max(calculatedOutputs, 1), tf.arg_max(labels, 1));
        return tf.reduce_mean(tf.cast(correct_pred, tf.float32));
    }

    private static Tensor MeanSquaredError(Tensor labels, Tensor calculatedOutputs)
    {
        return tf.reduce_mean(tf.square(calculatedOutputs - labels));
    }

    public static Tensor MeanAbsoluteError(Tensor labels, Tensor calculatedOutputs)
    {
        return tf.reduce_mean(tf.abs(calculatedOutputs - labels));
    }

    public static Tensor MeanAbsolutePercentageError(Tensor labels, Tensor calculatedOutputs)
    {
        return tf.reduce_mean(tf.abs((calculatedOutputs - labels) / labels));
    }

    internal static Optimizer GetOptimizer(NeuralNetworkSettingsEntity s)
    {
        switch (s.Optimizer)
        {
            case TensorFlowOptimizer.Adam:
                return tf.train.AdamOptimizer((float)s.LearningRate, (float)s.LearningEpsilon);

            case TensorFlowOptimizer.GradientDescentOptimizer:
                return tf.train.GradientDescentOptimizer((float)s.LearningRate);

            default:
                throw new InvalidOperationException("Unexpected Learner");
        }
    }
}
