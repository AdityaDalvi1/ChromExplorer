using System.Collections.Generic;
using System.Linq;
using System.Xml;
using ChromExplorer.Calibrations;

namespace ChromExplorer.ModasReader
{
    public class MethodFileLite
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public double MFrom { get; set; }
        public double MTo { get; set; }
        public double MainRFSampleRate { get; set; }
        public IList<MethodAction> Actions { get; private set; }
        public IList<double> MassList { get; private set; }
        public int SamplesToSkip = 0;
        public int StartIndex { get; private set; }
        public int ActualSize { get; private set; }
        public bool IsInitialized { get; private set; }
        public string TextOptions { get; private set; }

        public MethodAction DetectionAction
        {
            get { return Actions.FirstOrDefault((a) => a.IsDetection == true); }
        }

        public MethodFileLite(Method method)
        {
            IsInitialized = false;
            ID = method.Id;
            

            var doc = new XmlDocument();
            doc.LoadXml(method.Xml);

            var nav = doc.CreateNavigator();

            var expr = nav.Compile("/MethodFileUI/MassFrom");
            var iterator = nav.Select(expr);

            if(iterator.MoveNext())
            {
                if(iterator.Current != null)
                    MFrom = iterator.Current.ValueAsDouble;
            }

            expr = nav.Compile("/MethodFileUI/MassTo");
            iterator = nav.Select(expr);

            if (iterator.MoveNext())
            {
                if (iterator.Current != null)
                    MTo = iterator.Current.ValueAsDouble;
            }

            expr = nav.Compile("/MethodFileUI/MainRFSampleRate");
            iterator = nav.Select(expr);

            if(iterator.MoveNext())
            {
                if (iterator.Current != null)
                    MainRFSampleRate = iterator.Current.ValueAsDouble;
            }

            expr = nav.Compile("/MethodFileUI/ScanActions/MethodAction");
            iterator = nav.Select(expr);

            Actions = new List<MethodAction>();

            while (iterator.MoveNext())
            {
                if (iterator.Current != null)
                {
                    var action = iterator.Current;
                    Actions.Add(new MethodAction(action));
                }
            }

            expr = nav.Compile("/MethodFileUI/Name");
            iterator = nav.Select(expr);

            if (iterator.MoveNext())
            {
                if (iterator.Current != null)
                    Title = iterator.Current.Value;
            }

            expr = nav.Compile("/MethodFileUI/TextConfig");
            iterator = nav.Select(expr);

            if (iterator.MoveNext())
            {
                if (iterator.Current != null)
                    TextOptions = iterator.Current.Value;
            }


            
        }

        public void BuildMassList(int size, double digitizerSampleRate, ICalibration massCalibration)
        {
            if(size <= 0 /*|| massCalibration == null*/)
                return;

            MassList = new List<double>();

            BuildMainRFRamp();
            
            int lastIndex = 0;
            bool samplesToSkipSet = false;
            for (int i = 0; i < size; i++)
            {
                double time = (double)i / digitizerSampleRate;
                double dac = GetInterpolatedDac(time);

                if (dac > 0)
                {
                    if(!samplesToSkipSet)
                    {
                        samplesToSkipSet = true;
                        SamplesToSkip = i;
                    }
                    if (MassList.Count == 0)
                    {
                        MassList.Add(dac);
                    }
                    else
                    {
                        if (MassList[lastIndex] >= dac)
                        {
                            break;
                        }

                        MassList.Add(dac);
                        lastIndex++;
                    }
                }
            }

            ActualSize = MassList.Count;
            IsInitialized = true;

            if(massCalibration == null)
                return;
            
            for (int i = 0; i < ActualSize; i++)
            {
                MassList[i] = massCalibration.GetCalibratedValue(MassList[i]);
            }
        }

        private List<double> _mainRFTimes;
        private List<double> _mainRFValues;
        private int _triggerIndex;

        private void BuildMainRFRamp()
        {
            var action = DetectionAction;
            if (action == null)
                return;

            var parser = new TextOptionsParser();
            parser.Parse(this.TextOptions);

            double divider = 1 << parser.FracStepBits;


            var rampSize = (int)(action.Duration * MainRFSampleRate);
            
            _mainRFTimes = new List<double>(rampSize);
            _mainRFValues = new List<double>(rampSize);

            double step = 1.0 / MainRFSampleRate;
            double current = 0.0;
            bool firstStep = false;
            int startLevel = 0;

            int currentIndex = 0;
            double currentLevel = 0;
            double currentIncrement = 0;
            bool inTrigger = false;
            int triggerStepCounter = 0;
            int triggerSteps = 0;

            bool triggerIndexSet = false;

            while (current < action.Duration)
            {
                _mainRFTimes.Add(current);

                if (!inTrigger)
                {
                    var trigger = action.GetTrigger(current, current + step);

                    if (trigger != null)
                    {
                        if (!triggerIndexSet)
                        {
                            _triggerIndex = currentIndex + 1;
                            triggerIndexSet = true;
                        }

                        startLevel = (int)trigger.StartLevel;
                        firstStep = true;

                        currentIncrement = trigger.StepSize / divider;
                        triggerSteps = (int)(trigger.Steps);
                        triggerStepCounter = 0;
                        inTrigger = true;
                    }
                }
                else
                {
                    if (triggerStepCounter >= triggerSteps)
                    {
                        inTrigger = false;
                        currentIncrement = 0;
                        triggerStepCounter = 0;

                        firstStep = false;
                        startLevel = 0;
                    }
                    else
                    {
                        triggerStepCounter++;

                        if (firstStep)
                        {
                            currentLevel = startLevel;
                            firstStep = false;
                        }
                        else
                        {
                            currentLevel += currentIncrement;
                        }
                    }
                }

                _mainRFValues.Add(currentLevel);
                currentIndex++;
                current += step;
            }
        }

        private double GetInterpolatedDac(double time)
        {
            int index = GetRightIndex(time);

            if (index > 0 && index > _triggerIndex)
            {
                double leftX = _mainRFTimes[index - 1];
                double rightX = _mainRFTimes[index];

                double leftY = _mainRFValues[index - 1];
                double rightY = _mainRFValues[index];

                return leftY + (time - leftX) * (rightY - leftY) / (rightX - leftX);
            }

            return -1;
        }

        private int GetRightIndex(double x)
        {
            var size = _mainRFTimes.Count;
            for (int i = StartIndex; i < size; i++)
            {
                if (_mainRFTimes[i] >= x)
                {
                    StartIndex = i;
                    return i;
                }
            }

            return -1;
        }

    }
}
