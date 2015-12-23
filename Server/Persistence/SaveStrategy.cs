// **********
// RpiUO - SaveStrategy.cs
// Last Edit: 2015/12/23
// Look for Rpi comment
// **********

namespace Server
{
    public abstract class SaveStrategy
    {
        public abstract string Name { get; }
        public static SaveStrategy Acquire()
        {
            //Rpi - Removed Conditional pre-processor clausule - pi got only 4 processors and the DynamicSaveStrategy does not work well on there...
            //Since pi is multicore, there is no need to another save strategy than the best available.
            /*

            if (Core.MultiProcessor)
            {
                int processorCount = Core.ProcessorCount;

                
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
            */
            return new DualSaveStrategy();
        }

        public abstract void Save(SaveMetrics metrics, bool permitBackgroundWrite);

        public abstract void ProcessDecay();
    }
}