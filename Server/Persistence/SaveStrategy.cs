// **********
// RpiUO - SaveStrategy.cs
// Last Edit: 2015/12/20
// Look for Rpi comment
// **********


using System;

namespace Server
{
    public abstract class SaveStrategy
    {
        public abstract string Name { get; }
        public static SaveStrategy Acquire()
        {
            if (Core.MultiProcessor)
            {
                int processorCount = Core.ProcessorCount;

                //Rpi - Removed Conditional pre-processor clausule - pi got only 4 processors and the DynamicSaveStrategy does not work well on there...
                //#if DynamicSaveStrategy
                if (processorCount > 4)
                {
                    return new DynamicSaveStrategy();
                }
                //Rpi - Removed Conditional pre-processor clausule
                //#else
                if (processorCount > 16)
                {
                    return new ParallelSaveStrategy(processorCount);
                }
                //Rpi - Removed Conditional pre-processor clausule
                //#endif
                else
                {
                    return new DualSaveStrategy();
                }
            }
            else
            {
                return new StandardSaveStrategy();
            }
        }

        public abstract void Save(SaveMetrics metrics, bool permitBackgroundWrite);

        public abstract void ProcessDecay();
    }
}