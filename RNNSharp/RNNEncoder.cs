﻿using AdvUtils;
using System.Collections.Generic;

/// <summary>
/// RNNSharp written by Zhongkai Fu (fuzhongkai@gmail.com)
/// </summary>
namespace RNNSharp
{
    public class RNNEncoder
    {
        ModelSetting m_modelSetting;
        public DataSet TrainingSet { get; set; }
        public DataSet ValidationSet { get; set; }

        public RNNEncoder(ModelSetting modelSetting)
        {
            m_modelSetting = modelSetting;
        }

        public void Train()
        {
            RNN rnn;

            if (m_modelSetting.ModelDirection == 0)
            {
                List<SimpleLayer> hiddenLayers = new List<SimpleLayer>();
                for (int i = 0; i < m_modelSetting.NumHidden.Count; i++)
                {
                    SimpleLayer layer = null;
                    if (m_modelSetting.ModelType == 0)
                    {
                        BPTTLayer bpttLayer = new BPTTLayer(m_modelSetting.NumHidden[i], m_modelSetting);
                        layer = bpttLayer;
                    }
                    else
                    {
                        LSTMLayer lstmLayer = new LSTMLayer(m_modelSetting.NumHidden[i], m_modelSetting);
                        layer = lstmLayer;
                    }

                    if (i == 0)
                    {
                        Logger.WriteLine("Create hidden layer {0}: size = {1}, sparse feature size = {2}, dense feature size = {3}",
                            i, m_modelSetting.NumHidden[i], TrainingSet.GetSparseDimension(), TrainingSet.DenseFeatureSize());

                        layer.InitializeWeights(TrainingSet.GetSparseDimension(), TrainingSet.DenseFeatureSize());
                    }
                    else
                    {
                        Logger.WriteLine("Create hidden layer {0}: size = {1}, sparse feature size = {2}, dense feature size = {3}",
                            i, m_modelSetting.NumHidden[i], TrainingSet.GetSparseDimension(), hiddenLayers[i - 1].LayerSize);

                        layer.InitializeWeights(TrainingSet.GetSparseDimension(), hiddenLayers[i - 1].LayerSize);
                    }
                    hiddenLayers.Add(layer);
                }


                if (m_modelSetting.Dropout > 0)
                {
                    Logger.WriteLine("Adding dropout layer");
                    DropoutLayer dropoutLayer = new DropoutLayer(hiddenLayers[hiddenLayers.Count - 1].LayerSize, m_modelSetting);
                    dropoutLayer.InitializeWeights(0, hiddenLayers[hiddenLayers.Count - 1].LayerSize);
                    hiddenLayers.Add(dropoutLayer);
                }

                Logger.WriteLine("Create output layer.");
                SimpleLayer outputLayer = new SimpleLayer(TrainingSet.TagSize);
                outputLayer.InitializeWeights(TrainingSet.GetSparseDimension(), hiddenLayers[hiddenLayers.Count - 1].LayerSize);

                rnn = new ForwardRNN(hiddenLayers, outputLayer);
            }
            else
            {
                List<SimpleLayer> forwardHiddenLayers = new List<SimpleLayer>();
                List<SimpleLayer> backwardHiddenLayers = new List<SimpleLayer>();
                for (int i = 0; i < m_modelSetting.NumHidden.Count; i++)
                {
                    SimpleLayer forwardLayer = null;
                    SimpleLayer backwardLayer = null;
                    if (m_modelSetting.ModelType == 0)
                    {
                        //For BPTT layer
                        BPTTLayer forwardBPTTLayer = new BPTTLayer(m_modelSetting.NumHidden[i], m_modelSetting);
                        forwardLayer = forwardBPTTLayer;

                        BPTTLayer backwardBPTTLayer = new BPTTLayer(m_modelSetting.NumHidden[i], m_modelSetting);
                        backwardLayer = backwardBPTTLayer;
                    }
                    else
                    {
                        //For LSTM layer
                        LSTMLayer forwardLSTMLayer = new LSTMLayer(m_modelSetting.NumHidden[i], m_modelSetting);
                        forwardLayer = forwardLSTMLayer;

                        LSTMLayer backwardLSTMLayer = new LSTMLayer(m_modelSetting.NumHidden[i], m_modelSetting);
                        backwardLayer = backwardLSTMLayer;
                    }

                    if (i == 0)
                    {
                        Logger.WriteLine("Create hidden layer {0}: size = {1}, sparse feature size = {2}, dense feature size = {3}",
                            i, m_modelSetting.NumHidden[i], TrainingSet.GetSparseDimension(), TrainingSet.DenseFeatureSize());

                        forwardLayer.InitializeWeights(TrainingSet.GetSparseDimension(), TrainingSet.DenseFeatureSize());
                        backwardLayer.InitializeWeights(TrainingSet.GetSparseDimension(), TrainingSet.DenseFeatureSize());
                    }
                    else
                    {
                        Logger.WriteLine("Create hidden layer {0}: size = {1}, sparse feature size = {2}, dense feature size = {3}",
                            i, m_modelSetting.NumHidden[i], TrainingSet.GetSparseDimension(), forwardHiddenLayers[i - 1].LayerSize);

                        forwardLayer.InitializeWeights(TrainingSet.GetSparseDimension(), forwardHiddenLayers[i - 1].LayerSize);
                        backwardLayer.InitializeWeights(TrainingSet.GetSparseDimension(), backwardHiddenLayers[i - 1].LayerSize);
                    }

                    forwardHiddenLayers.Add(forwardLayer);
                    backwardHiddenLayers.Add(backwardLayer);
                }

                if (m_modelSetting.Dropout > 0)
                {
                    Logger.WriteLine("Adding dropout layers");
                    DropoutLayer forwardDropoutLayer = new DropoutLayer(forwardHiddenLayers[forwardHiddenLayers.Count - 1].LayerSize, m_modelSetting);
                    DropoutLayer backwardDropoutLayer = new DropoutLayer(backwardHiddenLayers[backwardHiddenLayers.Count - 1].LayerSize, m_modelSetting);

                    forwardDropoutLayer.InitializeWeights(0, forwardHiddenLayers[forwardHiddenLayers.Count - 1].LayerSize);
                    backwardDropoutLayer.InitializeWeights(0, backwardHiddenLayers[backwardHiddenLayers.Count - 1].LayerSize);

                    forwardHiddenLayers.Add(forwardDropoutLayer);
                    backwardHiddenLayers.Add(backwardDropoutLayer);
                }

                Logger.WriteLine("Create output layer.");
                SimpleLayer outputLayer = new SimpleLayer(TrainingSet.TagSize);
                outputLayer.InitializeWeights(TrainingSet.GetSparseDimension(), forwardHiddenLayers[forwardHiddenLayers.Count - 1].LayerSize);

                rnn = new BiRNN(forwardHiddenLayers, backwardHiddenLayers, outputLayer);
            }

            rnn.ModelDirection = (MODELDIRECTION)m_modelSetting.ModelDirection;
            rnn.bVQ = (m_modelSetting.VQ != 0) ? true : false;
            rnn.ModelFile = m_modelSetting.ModelFile;
            rnn.SaveStep = m_modelSetting.SaveStep;
            rnn.MaxIter = m_modelSetting.MaxIteration;
            rnn.IsCRFTraining = m_modelSetting.IsCRFTraining;
            RNNHelper.LearningRate = m_modelSetting.LearningRate;
            RNNHelper.GradientCutoff = m_modelSetting.GradientCutoff;
            
            //Create tag-bigram transition probability matrix only for sequence RNN mode
            if (m_modelSetting.IsCRFTraining)
            {
                rnn.setTagBigramTransition(TrainingSet.CRFLabelBigramTransition);
            }

            Logger.WriteLine("");

            Logger.WriteLine("Iterative training begins ...");
            double lastPPL = double.MaxValue;
            double lastAlpha = RNNHelper.LearningRate;
            int iter = 0;
            while (true)
            {
                Logger.WriteLine("Cleaning training status...");
                rnn.CleanStatus();

                if (rnn.MaxIter > 0 && iter > rnn.MaxIter)
                {
                    Logger.WriteLine("We have trained this model {0} iteration, exit.");
                    break;
                }

                //Start to train model
                double ppl = rnn.TrainNet(TrainingSet, iter);
                if (ppl >= lastPPL && lastAlpha != RNNHelper.LearningRate)
                {
                    //Although we reduce alpha value, we still cannot get better result.
                    Logger.WriteLine("Current perplexity({0}) is larger than the previous one({1}). End training early.", ppl, lastPPL);
                    Logger.WriteLine("Current alpha: {0}, the previous alpha: {1}", RNNHelper.LearningRate, lastAlpha);
                    break;
                }
                lastAlpha = RNNHelper.LearningRate;

                //Validate the model by validated corpus
                if (ValidationSet != null)
                {
                    Logger.WriteLine("Verify model on validated corpus.");
                    if (rnn.ValidateNet(ValidationSet, iter) == true)
                    {
                        //We got better result on validated corpus, save this model
                        Logger.WriteLine("Saving better model into file {0}...", m_modelSetting.ModelFile);
                        rnn.SaveModel(m_modelSetting.ModelFile);
                    }
                }
                else if (ppl < lastPPL)
                {
                    //We don't have validate corpus, but we get a better result on training corpus
                    //We got better result on validated corpus, save this model
                    Logger.WriteLine("Saving better model into file {0}...", m_modelSetting.ModelFile);
                    rnn.SaveModel(m_modelSetting.ModelFile);
                }
                
                if (ppl >= lastPPL)
                {
                    //We cannot get a better result on training corpus, so reduce learning rate
                    RNNHelper.LearningRate = RNNHelper.LearningRate / 2.0f;
                }

                lastPPL = ppl;

                iter++;
            }
        }
    }
}
