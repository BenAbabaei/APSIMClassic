using System;
using System.Collections.Generic;
using System.Text;
using ModelFramework;


abstract public class Phase
{
    [Param]
    public string Start;

    [Param]
    public string End;

    [Link]
    public Component My;

    [Link]
    public Phenology Phenology = null;

    [Link(IsOptional=true)]
    public Function ThermalTime = null;
    
    [Link(IsOptional = true)]
    public Function Stress = null;

    protected double PropOfDayUnused = 0;
    protected double _TTForToday = 0;
    [Output]
    public double TTForToday { get { return _TTForToday; } }
    protected double _TTinPhase = 0;
    [Output]
    public double TTinPhase { get { return _TTinPhase; } }
    
    public string Name { get { return My.Name; } }

    /// <summary>
    /// This function increments thermal time accumulated in each phase 
    /// and returns a non-zero value if the phase target is met today so
    /// the phenology class knows to progress to the next phase and how
    /// much tt to pass it on the first day.
    /// </summary>
    virtual public double DoTimeStep(double PropOfDayToUse)
    {
        // Calculate the TT for today and Accumulate.      
        _TTForToday = ThermalTime.Value * PropOfDayToUse;
        if (Stress != null)
        {
            _TTForToday *= Stress.Value;
        }
        _TTinPhase += _TTForToday;

        return PropOfDayUnused;
    }
    abstract public double FractionComplete { get; }

    [EventHandler]
    public void OnInitialised() { ResetPhase(); }
    public virtual void ResetPhase() 
    { _TTinPhase = 0;
    PropOfDayUnused = 0;
    }
        
}
   
