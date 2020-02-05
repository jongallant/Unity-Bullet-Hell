namespace BulletHell 
{
    public struct ColorPulse
    {
        private const float PULSE_TIME = 10;
        private float Time;
        private bool PulseDown;

        public float Fraction
        {
            get
            {
                return Time / PULSE_TIME;
            }
        }
        
        public void Update(float tick, float speed)
        {
            if (PulseDown)
            {
                Time -= speed * tick;
                if (Time <= 0)
                {
                    Time = 0;
                    PulseDown = false;
                }
            }
            else
            {
                Time += speed * tick;
                if (Time >= PULSE_TIME)
                {
                    Time = PULSE_TIME;
                    PulseDown = true;
                }
            }
        }

    }
}
