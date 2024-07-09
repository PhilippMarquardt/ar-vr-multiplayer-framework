using UnityEngine;

namespace TestUtils
{
    public class WaitForFrames : CustomYieldInstruction
    {
        private readonly int targetFrameCount;

        public WaitForFrames(int numberOfFrames)
        {
            targetFrameCount = Time.frameCount + numberOfFrames;
        }

        public override bool keepWaiting => Time.frameCount < targetFrameCount;
    }
}
