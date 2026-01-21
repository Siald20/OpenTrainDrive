
public enum SignalAspect_l
{
    Magenta,
    H,
    NH,
    F1,
    F2,
    F2_BES,
    F3,
    F5, 
    F6,


}

public class image_Signal_l
{
    public bool PowerOn { get; set; } = true;
    public bool Error { get; set; }
    public bool ManualStop { get; set; }
    public bool RouteSet { get; set; }
    public bool OccupiedEntrance { get; set; }
    public bool ShortEntrance { get; set; }
    public int VelocityLimit { get; set; } = 0;


    public SignalAspect_l Aspect { get; private set; } = SignalAspect_l.Magenta;
    public bool LampRed { get; private set; }
    public bool LampEmergencyRed { get; private set; }
    public bool LampOrange1 { get; private set; }
    public bool LampOrange2 { get; private set; }
    public bool LampGreen1 { get; private set; }
    public bool LampGreen2 { get; private set; }
    public bool LampGreen3 { get; private set; }

    public void SignalSystemL()
    {
        LampRed = false;
        LampOrange1 = false;
        LampOrange2 = false;
        LampGreen1 = false;
        LampGreen2 = false;
        LampGreen3 = false;

        if (!PowerOn || Error)
        {
            Aspect = SignalAspect_l.Magenta;
            return;
        }

        if (PowerOn && (ManualStop || !RouteSet))
        {
            Aspect = SignalAspect_l.H;
            LampRed = true;
            return;
        }

        if (PowerOn && Error)
        {
            Aspect = SignalAspect_l.NH;
            LampEmergencyRed = true;
            return;
        }

        if (PowerOn && RouteSet && VelocityLimit == 0)
        {
            Aspect = SignalAspect_l.F1;
            LampGreen1 = true;
            return;
        }
        if (PowerOn && RouteSet && VelocityLimit == 40)
        {
            Aspect = SignalAspect_l.F2;
            LampGreen1 = true;
            LampOrange2 = true;
            return;
        }
        if (PowerOn && RouteSet && VelocityLimit == 60)
        {
            Aspect = SignalAspect_l.F3;
            LampGreen1 = true;
            LampGreen2 = true;
            return;
        }
        if (PowerOn && RouteSet && VelocityLimit == 90)
        {
            Aspect = SignalAspect_l.F5;
            LampGreen1 = true;
            LampGreen2 = true;
            LampGreen3 = true;
            return;
        }
        if (PowerOn && RouteSet && ShortEntrance)
        {
            Aspect = SignalAspect_l.F6;
            LampOrange1 = true;
            LampOrange2 = true;
            return;
        }
        if (PowerOn && RouteSet && OccupiedEntrance)
        {
            Aspect = SignalAspect_l.F2_BES;
            LampGreen1 = true;
            LampOrange2 = true;
            return;
        }
        else
        {
            
        }
    
    }
}

