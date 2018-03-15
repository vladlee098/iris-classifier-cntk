using CNTK;
using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;

namespace IrisClassifier.Cntk
{

    public class ModelInput
    {
        //5.0f, 3.5f, 1.3f, 0.3f, setosa
        public float sepal_length;
        public float sepal_width;
        public float petal_length;
        public float petal_width;

        public float[] ToArray()
        {
            return new float[4] { sepal_length, sepal_width, petal_length, petal_width };
        }
    }

    public class ModelOutput
    {
        //5.0f, 3.5f, 1.3f, 0.3f, setosa
        public string species;
    }

    public static class CntkLib
    {
        private static readonly ILog log = LogManager.GetLogger("cntk");

        private enum Activation
        {
            None,
            ReLU,
            Sigmoid,
            Tanh
        }

        private static void SaveTrainedModel(Function model, string fileName)
        {
            model.Save(fileName);
        }

        private static Function LoadTrainedModel(string fileName, DeviceDescriptor device)
        {
            return Function.Load(fileName, device);
        }

        private static (Function,double) TrainIrisByMinibatchSource(string dataFolder, DeviceDescriptor device)
        {
            var dataPath = Path.Combine(dataFolder, "trainIris_cntk.txt");
            var trainPath = Path.Combine(dataFolder, "testIris_cntk.txt");

            var featureStreamName = "features";
            var labelsStreamName = "label";

            //Network definition
            int inputDim = 4;
            int numOutputClasses = 3;
            int numHiddenLayers = 1;
            int hidenLayerDim = 6;
            uint sampleSize = 130;

            //stream configuration to distinct features and labels in the file
            var streamConfig = new StreamConfiguration[]
               {
                   new StreamConfiguration(featureStreamName, inputDim),
                   new StreamConfiguration(labelsStreamName, numOutputClasses)
               };


            // build a NN model
            //define input and output variable and connecting to the stream configuration
            var feature = Variable.InputVariable(new NDShape(1, inputDim), DataType.Float, featureStreamName);
            var label = Variable.InputVariable(new NDShape(1, numOutputClasses), DataType.Float, labelsStreamName);


            //Build simple Feed Froward Neural Network model
            // var ffnn_model = CreateMLPClassifier(device, numOutputClasses, hidenLayerDim, feature, classifierName);
            var ffnn_model = createFFNN(feature, numHiddenLayers, hidenLayerDim, numOutputClasses, Activation.Tanh, "IrisNNModel", device);

            //Loss and error functions definition
            var trainingLoss = CNTKLib.CrossEntropyWithSoftmax(new Variable(ffnn_model), label, "lossFunction");
            var classError = CNTKLib.ClassificationError(new Variable(ffnn_model), label, "classificationError");


            // prepare the training data
            var minibatchSource = MinibatchSource.TextFormatMinibatchSource(
                dataPath, streamConfig, MinibatchSource.InfinitelyRepeat, true);
            var featureStreamInfo = minibatchSource.StreamInfo(featureStreamName);
            var labelStreamInfo = minibatchSource.StreamInfo(labelsStreamName);

            // set learning rate for the network
            var learningRatePerSample = new TrainingParameterScheduleDouble(0.001125, 1);

            //define learners for the NN model
            var ll = Learner.SGDLearner(ffnn_model.Parameters(), learningRatePerSample);

            //define trainer based on ffnn_model, loss and error functions , and SGD learner 
            var trainer = Trainer.CreateTrainer(ffnn_model, trainingLoss, classError, new Learner[] { ll });

            //Preparation for the iterative learning process
            //used 800 epochs/iterations. Batch size will be the same as sample size since the data set is small
            int epochs = 800;
            int i = 0;
            while (epochs > -1)
            {
                var minibatchData = minibatchSource.GetNextMinibatch(sampleSize, device);
                //pass to the trainer the current batch separated by the features and label.
                var arguments = new Dictionary<Variable, MinibatchData>
                {
                    { feature, minibatchData[featureStreamInfo] },
                    { label, minibatchData[labelStreamInfo] }
                };

                trainer.TrainMinibatch(arguments, device);


                printTrainingProgress(trainer, i++, 50);

                // MinibatchSource is created with MinibatchSource.InfinitelyRepeat.
                // Batching will not end. Each time minibatchSource completes an sweep (epoch),
                // the last minibatch data will be marked as end of a sweep. We use this flag
                // to count number of epochs.
                if (minibatchData.Values.Any(a => a.sweepEnd))
                {
                    epochs--;
                }
            }
            //Summary of training
            double acc = Math.Round((1.0 - trainer.PreviousMinibatchEvaluationAverage()) * 100, 2);
            log.Info($"------TRAINING SUMMARY--------");
            log.Info($"The model trained with the accuracy {acc}%");

            //// validate the model
            // this will be posted as separate blog post
            return (ffnn_model,acc);
        }

        private static (float[], float[]) loadIrisDataset(string filePath, int featureDim, int numClasses)
        {
            var rows = File.ReadAllLines(filePath);
            var features = new List<float>();
            var label = new List<float>();
            for (int i = 1; i < rows.Length; i++)
            {
                var row = rows[i].Split(',');
                var input = new float[featureDim];
                for (int j = 0; j < featureDim; j++)
                {
                    input[j] = float.Parse(row[j], CultureInfo.InvariantCulture);
                }
                var output = new float[numClasses];
                for (int k = 0; k < numClasses; k++)
                {
                    int oIndex = featureDim + k;
                    output[k] = float.Parse(row[oIndex], CultureInfo.InvariantCulture);
                }

                features.AddRange(input);
                label.AddRange(output);
            }

            return (features.ToArray(), label.ToArray());
        }

        private static (Function,double) TrainIriswithBatch(string dataFolder, DeviceDescriptor device)
        {
            //data file path
            var iris_data_file = Path.Combine(dataFolder, "iris_with_hot_vector.csv");

            //Network definition
            int inputDim = 4;
            int numOutputClasses = 3;
            int numHiddenLayers = 1;
            int hidenLayerDim = 6;

            //load data in to memory
            var dataSet = loadIrisDataset(iris_data_file, inputDim, numOutputClasses);

            // build a NN model
            //define input and output variable
            var xValues = Value.CreateBatch<float>(new NDShape(1, inputDim), dataSet.Item1, device);
            var yValues = Value.CreateBatch<float>(new NDShape(1, numOutputClasses), dataSet.Item2, device);

            // build a NN model
            //define input and output variable and connecting to the stream configuration
            var feature = Variable.InputVariable(new NDShape(1, inputDim), DataType.Float);
            var label = Variable.InputVariable(new NDShape(1, numOutputClasses), DataType.Float);

            //Combine variables and data in to Dictionary for the training
            var dic = new Dictionary<Variable, Value>();
            dic.Add(feature, xValues);
            dic.Add(label, yValues);

            //Build simple Feed Froward Neural Network model
            // var ffnn_model = CreateMLPClassifier(device, numOutputClasses, hidenLayerDim, feature, classifierName);
            var ffnn_model = createFFNN(feature, numHiddenLayers, hidenLayerDim, numOutputClasses, Activation.Tanh, "IrisNNModel", device);

            //Loss and error functions definition
            var trainingLoss = CNTKLib.CrossEntropyWithSoftmax(new Variable(ffnn_model), label, "lossFunction");
            var classError = CNTKLib.ClassificationError(new Variable(ffnn_model), label, "classificationError");

            // set learning rate for the network
            var learningRatePerSample = new TrainingParameterScheduleDouble(0.001125, 1);

            //define learners for the NN model
            var ll = Learner.SGDLearner(ffnn_model.Parameters(), learningRatePerSample);

            //define trainer based on ffnn_model, loss and error functions , and SGD learner
            var trainer = Trainer.CreateTrainer(ffnn_model, trainingLoss, classError, new Learner[] { ll });

            //Preparation for the iterative learning process
            //used 800 epochs/iterations. Batch size will be the same as sample size since the data set is small
            int epochs = 800;
            int i = 0;
            while (epochs > -1)
            {
                trainer.TrainMinibatch(dic, false, device);

                //print progress
                printTrainingProgress(trainer, i++, 50);

                //
                epochs--;
            }
            //Summary of training
            double acc = Math.Round((1.0 - trainer.PreviousMinibatchEvaluationAverage()) * 100, 2);
            log.Info($"------TRAINING SUMMARY--------");
            log.Info($"The model trained with the accuracy {acc}%");

            return (ffnn_model,acc);
        }

        private static Function createFFNN(Variable input, int hiddenLayerCount, int hiddenDim, int outputDim, Activation activation, string modelName, DeviceDescriptor device)
        {
            //First the parameters initialization must be performed
            var glorotInit = CNTKLib.GlorotUniformInitializer(
                    CNTKLib.DefaultParamInitScale,
                    CNTKLib.SentinelValueForInferParamInitRank,
                    CNTKLib.SentinelValueForInferParamInitRank, 1);

            //hidden layers creation
            //first hidden layer
            Function h = simpleLayer(input, hiddenDim, device);
            h = applyActivationFunction(h, activation);
            for (int i = 1; i < hiddenLayerCount; i++)
            {
                h = simpleLayer(h, hiddenDim, device);
                h = applyActivationFunction(h, activation);
            }
            //the last action is creation of the output layer
            var r = simpleLayer(h, outputDim, device);
            r.SetName(modelName);
            return r;
        }

        private static Function applyActivationFunction(Function layer, Activation actFun)
        {
            switch (actFun)
            {
                default:
                case Activation.None:
                    return layer;
                case Activation.ReLU:
                    return CNTKLib.ReLU(layer);
                case Activation.Sigmoid:
                    return CNTKLib.Sigmoid(layer);
                case Activation.Tanh:
                    return CNTKLib.Tanh(layer);
            }
        }
        private static Function simpleLayer(Function input, int outputDim, DeviceDescriptor device)
        {
            //prepare default parameters values
            var glorotInit = CNTKLib.GlorotUniformInitializer(
                    CNTKLib.DefaultParamInitScale,
                    CNTKLib.SentinelValueForInferParamInitRank,
                    CNTKLib.SentinelValueForInferParamInitRank, 1);

            //
            var var = (Variable)input;
            var shape = new int[] { outputDim, var.Shape[0] };
            var weightParam = new Parameter(shape, DataType.Float, glorotInit, device, "w");
            var biasParam = new Parameter(new NDShape(1, outputDim), 0, device, "b");


            return CNTKLib.Times(weightParam, input) + biasParam;

        }

        private static void printTrainingProgress(Trainer trainer, int minibatchIdx, int outputFrequencyInMinibatches)
        {
            if ((minibatchIdx % outputFrequencyInMinibatches) == 0 && trainer.PreviousMinibatchSampleCount() != 0)
            {
                float trainLossValue = (float)trainer.PreviousMinibatchLossAverage();
                float evaluationValue = (float)trainer.PreviousMinibatchEvaluationAverage();
                log.Info($"Minibatch: {minibatchIdx} CrossEntropyLoss = {trainLossValue}, EvaluationCriterion = {evaluationValue}");
            }
        }

        private static string EvaluateIrisModel(string modelFileName, DeviceDescriptor device, float[] xVal)
        {
            //calculate Iris flow from those dimensions
            //Example: 5.0f, 3.5f, 1.3f, 0.3f, setosa
            //float[] xVal = new float[4] { 5.0f, 3.5f, 1.3f, 0.3f };
            //load the model from disk
            var ffnn_model = Function.Load(modelFileName, device);

            //extract features and label from the model
            Variable feature = ffnn_model.Arguments[0];
            Variable label = ffnn_model.Output;

            Value xValues = Value.CreateBatch<float>(new int[] { feature.Shape[0] }, xVal, device);
            //Value yValues = - we don't need it, because we are going to calculate it

            //map the variables and values
            var inputDataMap = new Dictionary<Variable, Value>();
            inputDataMap.Add(feature, xValues);
            var outputDataMap = new Dictionary<Variable, Value>();
            outputDataMap.Add(label, null);

            //evaluate the model
            ffnn_model.Evaluate(inputDataMap, outputDataMap, device);
            //extract the result  as one hot vector
            var outputData = outputDataMap[label].GetDenseData<float>(label);

            //transforms into class value
            var actualLabels = outputData.Select(l => l.IndexOf(l.Max())).ToList();
            var flower = actualLabels.FirstOrDefault();
            var strFlower = flower == 0 ? "setosa" : flower == 1 ? "versicolor" : "versicolor";

            log.Info($"Model Prediction: Input({xVal[0]},{xVal[1]},{xVal[2]},{xVal[3]}), Iris Flower={strFlower}");
            log.Info($"Model Expectation: Input({xVal[0]},{xVal[1]},{xVal[2]},{xVal[3]}), Iris Flower= setosa");

            return strFlower;

        }

        public static double CreateAndTrainModel()
        {
            var dataDir = ConfigurationManager.AppSettings["data_dir"];
            if (dataDir == null)
            {
                throw new ArgumentNullException("data_dir is missing in config");
            }
            var modelFileName = Path.Combine(dataDir, @"ffnn_model.cntk");

            var (ffnn_model,accuracy) = TrainIriswithBatch(dataDir, DeviceDescriptor.CPUDevice);
            SaveTrainedModel(ffnn_model, modelFileName);

            log.Info($"Model has been created, file:'{modelFileName}'");
            return accuracy;
        }


        public static double CreateAndTrainModel_Minibatch()
        {
            var dataDir = ConfigurationManager.AppSettings["data_dir"];
            if (dataDir == null)
            {
                throw new ArgumentNullException("data_dir is missing in config");
            }
            var modelFileName = Path.Combine(dataDir, @"ffnn_model.cntk");

            var (ffnn_model,accuracy) = TrainIrisByMinibatchSource(dataDir, DeviceDescriptor.CPUDevice);
            SaveTrainedModel(ffnn_model, modelFileName);

            log.Info($"Model has been created, file:'{modelFileName}'");
            return accuracy;
        }

        public static ModelOutput Evaluate(ModelInput input)
        {
            var dataDir = ConfigurationManager.AppSettings["data_dir"];
            if (dataDir == null)
            {
                throw new ArgumentNullException("data_dir is missing in config");
            }
            var modelFileName = Path.Combine(dataDir, @"ffnn_model.cntk");

            if (File.Exists(modelFileName))
            {
                var result = EvaluateIrisModel(modelFileName, DeviceDescriptor.CPUDevice, input.ToArray() );
                return new ModelOutput() { species = result };
            }
            throw new Exception("Model file not found, please create model first.");
        }
    }
}
