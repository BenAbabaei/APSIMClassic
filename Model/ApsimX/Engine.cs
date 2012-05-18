﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Reflection;
using CSGeneral;


public class FinishedException : Exception
   {
   public FinishedException(string msg)
      : base(msg)
      
      {
      }
   }

public class Engine
{
    /// <summary>
    /// Main program entry point.
    /// </summary>
    static int Main(string[] args)
    {
        try
        {
            if (args.Length != 1)
                throw new Exception("Usage: FastApsim .ApsimFileName");

            Engine ApsimEngine = new Engine();
            ModelInstance Simulation = ApsimEngine.Load(args[0]);
            Simulation.Run();
            return 0;
        }
        catch (Exception err)
        {
            if (err.InnerException is FinishedException)
            {
                Console.WriteLine(err.InnerException.Message);
                return 0;
            }
            Console.WriteLine(err.ToString());
            return 1;
        }
    }

    /// <summary>
    /// Load a simulation from the specified file.
    /// </summary>
    private ModelInstance Load(string FileName)
    {
        if (!File.Exists(FileName))
            throw new Exception("Cannot find file: " + FileName);

        StreamReader In = new StreamReader(FileName);

        ModelInstance Simulation = CreateModelInstance(In);
        Simulation.UpdateValues();
        Simulation.PublishToChildren("Initialised");
        Simulation.Run();
        return Simulation;
    }

    /// <summary>
    /// Create instances of all objects specified by the XmlNode. Returns the top 
    /// level instance.
    /// </summary>
    private static ModelInstance CreateModelInstance(TextReader Reader)
    {
        ModelInstance RootInstance = XmlSerialiser.Deserialise(Reader);
        RootInstance.SortChildren();
        RootInstance.Initialise();
        return RootInstance;
    }

}
