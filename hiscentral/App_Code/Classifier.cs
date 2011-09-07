using System;
using System.Collections;
using System.Collections.Generic;

using ESRI.ArcGIS.Server;
using ESRI.ArcGIS.esriSystem;

/// <summary>
/// There are 3 types of breaks assignments for the ClassBreaks Renderer.
/// This is a wrapper class to generate the correct ArcGIS classifier.
/// </summary>
public class Classifier
{
    private IClassifyGEN m_classifier;
    private int m_breaks;


    public Classifier(IServerContext mapContext, double [] values, int [] counts, String classifierType, int breaks)
    {

        m_classifier = (IClassifyGEN)mapContext.CreateObject("esriSystem." + classifierType);
        int b = breaks;
  
        m_classifier.Classify(values, counts, ref b);
        m_breaks = b;

    }



    public object getClassBreaksArray()
    {
        return m_classifier.ClassBreaks;
    }

    public int getBreaks()
    {
        return m_breaks;
    }
}
