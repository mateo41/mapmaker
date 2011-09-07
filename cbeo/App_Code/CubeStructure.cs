using System;
using System.Xml;
using System.IO;
using Microsoft.AnalysisServices.AdomdClient;

using System.Collections.Generic;
using System.Collections;
/// <summary>
/// Summary description for CubeStructure
/// </summary>
public class CubeStructure
{
    private AdomdConnection m_conn;
    private StreamWriter m_output;
    private String m_cube;
    private String m_database;
    private XmlDataDocument m_xmlcubeschema;
    private Dictionary<int, String> m_layerDictionary;
    private ArrayList m_exclusionList;
    private String m_sitesDim;
	public CubeStructure(String database, String cube, String host, Dictionary<int, string> layerDictionary, String sitesDim)
	{
        m_database = database;
        m_cube = cube;
        String connectionString = "Initial Catalog=" +database+"; Data Source="+ host +"; integrated security=sspi;";
        m_conn = new AdomdConnection();
        m_conn = new AdomdConnection(connectionString);
        m_conn.Open();
        m_xmlcubeschema = new XmlDataDocument();
        m_output = new StreamWriter("C:\\inetpub\\wwwroot\\mattsmaps\\App_Code\\App_Data\\" + m_database + "." + m_cube + "." + "Report.txt");
        m_sitesDim = sitesDim;
        m_exclusionList = new ArrayList();
        foreach (String s in layerDictionary.Values){
            String tmpName = s.Substring(1, s.Substring(1, s.Length -1).IndexOf("."));
            String attributeName = tmpName.Replace("]", "").Replace("[", "");
            m_exclusionList.Add(attributeName);
        }

        generateReport();
    }

    public void generateReport()
    {
        XmlElement root = m_xmlcubeschema.CreateElement(m_database);
        m_xmlcubeschema.AppendChild(root);
        
        XmlElement dimElement = m_xmlcubeschema.CreateElement("Dimensions");
        root.AppendChild(dimElement);
        XmlElement cubeElement = m_xmlcubeschema.CreateElement("Cubes");
        root.AppendChild(cubeElement);
        XmlElement measuresElement = m_xmlcubeschema.CreateElement("Measures");
        root.AppendChild(measuresElement);

        foreach (CubeDef cube in m_conn.Cubes)
        {
            


            if (cube.ToString().Equals(m_cube))
            {
                XmlElement cubeNameElement = m_xmlcubeschema.CreateElement("Cube");
                cubeNameElement.SetAttribute("CubeName", cube.ToString());
                cubeElement.AppendChild(cubeNameElement);

                foreach (Dimension dim in cube.Dimensions)
                {

                    
                    if (!dim.Name.Equals("Measures") )
                    {
                        generateDimensionReport(dim, dimElement);
                    }


                        
                    if (dim.Name.Equals("Measures"))
                    {
                        
                        generateDimensionReport(dim, measuresElement);
                    }
                }
            }
        }
        m_xmlcubeschema.Save(m_output);
    }

    private void generateDimensionReport(Dimension dim, XmlElement parent)
    {

        XmlElement dimElement = m_xmlcubeschema.CreateElement(dim.Name.ToString());
        parent.AppendChild(dimElement);
        foreach (Hierarchy h in dim.AttributeHierarchies)
        {
            generateHierarchyReport(h, dimElement);
        }
    }

    private void generateHierarchyReport(Hierarchy h, XmlElement parent)
    {
        XmlElement elem, parentElement, memberElement;
        parentElement = parent;
        foreach (Level l in h.Levels)
        {
            bool exclude = false;
            
                foreach (String attr in m_exclusionList)
                {
                    if (l.Name.Equals(attr))
                    {
                        exclude = true;
                    }
                }
            
            
            if (!l.Name.Equals("(All)") && !exclude)
            {
                try
                {
                    elem = m_xmlcubeschema.CreateElement(l.Name.ToString());
                    parentElement.AppendChild(elem);
                    parentElement = elem;
                }
                catch (System.Xml.XmlException e)
                {
                    throw e;
                }
                MemberCollection mc = l.GetMembers();
                
                foreach (Member m in mc)
                {
                    try
                    {
                        memberElement = m_xmlcubeschema.CreateElement("Member");
                        memberElement.SetAttribute("MemberName", m.Name.ToString());
                        parentElement.AppendChild(memberElement);
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }

                }
                
            }
        }
    }


}
