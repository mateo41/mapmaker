using System;
using System.Drawing;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Web;
using System.Data;
using System.Threading;
using System.IO;
using System.Diagnostics;


using ESRI.ArcGIS.ADF.Web.UI.WebControls.Tools;
using ESRI.ArcGIS.ADF.Web.UI.WebControls;
using ESRI.ArcGIS.ADF.Web.Geometry;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.ADF.Web.SpatialReference;


public class PointKey
{
    public ESRI.ArcGIS.ADF.Web.Geometry.Point m_point;
    public int m_gridNum;
    public double m_prevDistance;

    public PointKey(ESRI.ArcGIS.ADF.Web.Geometry.Point point, int gridNum)
    {
        m_point = point;
        m_gridNum = gridNum;
    }
}

/// <summary>
/// Summary description for CrossSectionTool
/// </summary>
public class CrossSectionTool:IMapServerToolAction
//public class CrossSectionTool:ESRI.ArcGIS.ADF.Web.UI.WebControls.WebControl
{
    private int m_IdentifyTolerance;
    private ESRI.ArcGIS.ADF.Web.IdentifyOption m_idOption;

    private string[] m_excludedColumnNames;
    private Map m_map;
    ArrayList m_samplesList;
    private DateTime m_timeBegin;
    private double m_crossSectionDistance;

	public CrossSectionTool()
	{
        m_IdentifyTolerance = 0;
        m_idOption = ESRI.ArcGIS.ADF.Web.IdentifyOption.VisibleLayers;
        m_excludedColumnNames = new string[4];
        m_excludedColumnNames[0] = "OID";
        m_excludedColumnNames[1] = "ObjectID";
        m_excludedColumnNames[2] = "#ID#";
        m_excludedColumnNames[3] = "#SHAPE#";
      
        m_map = null;
        ArrayList a = new ArrayList();
        m_samplesList = ArrayList.Synchronized(a);
        

	}
    /*
    protected override void OnPreRender(EventArgs e)
    {

    }
     * */
    public void ServerAction(ToolEventArgs args)
    {
        LineEventArgs lineArgs = (LineEventArgs)args;
        String outfile = (String)HttpContext.Current.Session["CrossSectionFileName"];
        System.Drawing.Point start = lineArgs.BeginPoint;
        System.Drawing.Point end = lineArgs.EndPoint;
        Map map = (Map)args.Control;
        ArrayList points = new ArrayList();
        int samples = 80;

        TransformationParams transParams = map.GetTransformationParams(TransformationDirection.ToMap);
        SpatialReference spatRef = map.SpatialReference;
        

        for ( int i= 0; i < samples; i++){
                ESRI.ArcGIS.ADF.Web.Geometry.Point p = new ESRI.ArcGIS.ADF.Web.Geometry.Point();
                
                float theta = ((float)i)/((float)samples);
                p.X = (int)(start.X * (1 - theta) + end.X * theta);
                p.Y = (int)(start.Y * (1 - theta) + end.Y * theta);

                ESRI.ArcGIS.ADF.Web.Geometry.Point mapPoint = ESRI.ArcGIS.ADF.Web.Geometry.Point.ToMapPoint(
                Convert.ToInt32(p.X), Convert.ToInt32(p.Y), transParams);
                
                mapPoint.SpatialReference = spatRef;
                points.Add(mapPoint);
                
        }


        m_map = map;
        m_timeBegin = DateTime.Now;

       


        m_crossSectionDistance = calcDistanceMeters((ESRI.ArcGIS.ADF.Web.Geometry.Point)points[points.Count -1], 
                                                    (ESRI.ArcGIS.ADF.Web.Geometry.Point)points[0] );
        ProcessPoints(points);
        DateTime timeEnd = DateTime.Now;
        double datediff = (timeEnd - m_timeBegin).TotalSeconds;
        string filename = "G:\\matt\\temp\\samples.txt";
        serializeSamples(filename);
        //string outfile2 = "G:\\matt\\temp\\crossSection.pdf";
        generateImage(filename, outfile);
        
       

    }
    private void generateImage(string infile, string outfile)
    {
        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = "C:\\Python25\\python.exe";
        psi.Arguments = " G:\\matt\\temp\\cross_section.py -f " + infile + " -o " + outfile;

        Process p = Process.Start(psi);
        p.WaitForExit();

        int k = 3;


    }
    private void serializeSamples(String filename)
    {
        StreamWriter sw = new StreamWriter("G:\\matt\\temp\\samples.txt");

        sw.WriteLine(m_crossSectionDistance);
        foreach (PointKey pk in m_samplesList)
        {
            sw.Write(pk.m_point.X);
            sw.Write(",");
            sw.Write(pk.m_point.Y);
            sw.Write(",");
            sw.Write(pk.m_gridNum);
            sw.Write(",");
            sw.Write(pk.m_prevDistance);
            sw.WriteLine();
        }

        sw.Close();
    }
    private void ProcessPoints(object objPoints)
    {
        DataTable dt;
        ArrayList points = (ArrayList)objPoints;
        int gridNum, index;
        double distance = -1;

        ESRI.ArcGIS.ADF.Web.Geometry.Point prevPoint = null;
        
        
       

        foreach (ESRI.ArcGIS.ADF.Web.Geometry.Point point in points)
        {
            dt = identifyPoint(point);


            
            if (prevPoint != null)
            {
                distance = calcDistanceMeters(point, prevPoint);
                
            }

            
            
            if (dt != null)
            {
                index = dt.Columns.IndexOf("GridNum");
                foreach (DataRow row in dt.Rows)
                {
                    gridNum = Convert.ToInt32(row[index]);
                    PointKey pk = new PointKey(point, gridNum);
                    pk.m_prevDistance = distance;
                    m_samplesList.Add(pk);
                }
            }
            prevPoint = point;
            
            
        }
       
    }

    private double calcDistanceMeters(ESRI.ArcGIS.ADF.Web.Geometry.Point toPoint, ESRI.ArcGIS.ADF.Web.Geometry.Point fromPoint)
    {
        ILine2 line;
        double distance = 0;
        ESRI.ArcGIS.Geometry.Point p1, p2;
        p1 = new PointClass();
        p2 = new PointClass();

        p1.PutCoords(toPoint.X, toPoint.Y);
        p2.PutCoords(fromPoint.X, fromPoint.Y);


        ISpatialReferenceFactory3 spatialReferenceFactory = new SpatialReferenceEnvironmentClass();
        ISpatialReference spatialReference = spatialReferenceFactory.CreateSpatialReference((int)
            esriSRProjCSType.esriSRProjCS_World_Mercator);
        ISpatialReference spatialReferenceOrig = spatialReferenceFactory.CreateSpatialReference((int)esriSRGeoCSType.esriSRGeoCS_WGS1984);
        line = new LineClass();
        line.SpatialReference = spatialReferenceOrig;
        line.PutCoords(p1, p2);
        line.Project(spatialReference);
        distance = line.Length;
        return distance;
    }

    private double calculateDistance(ESRI.ArcGIS.ADF.Web.Geometry.Point fromPoint,  ESRI.ArcGIS.ADF.Web.Geometry.Point toPoint)
    {
        double R = 6371000;
        double delta_lat, delta_long;
        double a, c, distance = 0;
        
        delta_lat = toRadian(fromPoint.X - toPoint.X);
        delta_long = toRadian(fromPoint.Y - toPoint.Y);
        a = Math.Sin(delta_lat / 2) * Math.Sin(delta_lat / 2) +
            toRadian(Math.Cos(fromPoint.X)) * toRadian(Math.Cos(toPoint.X))
            * Math.Sin(delta_long / 2) * Math.Sin(delta_long / 2);
        c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
        distance = R * c;
        return distance;
    }

    private double toRadian(double value)
    {
        return (Math.PI /180) * value;
    }

    

    private DataTable identifyPoint(ESRI.ArcGIS.ADF.Web.Geometry.Point mapPoint)
    {

        #region Variable declarations
        ESRI.ArcGIS.ADF.Web.DataSources.IGISResource resource;
        ESRI.ArcGIS.ADF.Web.DataSources.IQueryFunctionality query;
        System.Data.DataTable[] identifyDataTables = null;
        string tableName;
        System.Data.DataTable identifyTable = null;
        #endregion

        foreach (ESRI.ArcGIS.ADF.Web.DataSources.IMapFunctionality mapFunc in m_map.GetFunctionalities())
        {
            if (mapFunc.DisplaySettings.Visible)
            {
                resource = mapFunc.Resource;
                query = resource.CreateFunctionality(typeof(ESRI.ArcGIS.ADF.Web.DataSources.IQueryFunctionality), "identify_") as ESRI.ArcGIS.ADF.Web.DataSources.IQueryFunctionality;

                if ((query != null) && (query.Supports("identify")))
                {
                    #region Perform the identify query
                    try
                    {
                        identifyDataTables = query.Identify(mapFunc.Name, mapPoint, m_IdentifyTolerance, m_idOption, null);
                    }
                    catch
                    {
                        identifyDataTables = null;
                    }
                    #endregion

                    #region Process result tables
                    if (identifyDataTables != null && identifyDataTables.Length > 0)
                    {
                       
                            identifyTable = identifyDataTables[0];
                            tableName = identifyTable.ExtendedProperties[ESRI.ArcGIS.ADF.Web.Constants.ADFLayerName] as string;
                            if (string.IsNullOrEmpty(tableName))
                                tableName = identifyDataTables[0].TableName;

                            if (tableName != "WQgrid_cells")
                            {
                                return null;
                            }

                            #region Get template for title and contents for this layer from Map Resource Manager

                            #region Get layer format and apply it
                            string layerID = identifyTable.ExtendedProperties[ESRI.ArcGIS.ADF.Web.Constants.ADFLayerID] as string;
                            LayerFormat layerFormat = null;
                            DataTable formattedTable = identifyTable;
                            
                            if (layerID == "0")
                            {
                                layerFormat = LayerFormat.FromMapResourceManager(m_map.MapResourceManagerInstance, mapFunc.Resource.Name, layerID);
                                if (layerFormat != null)
                                {
                                    ESRI.ArcGIS.ADF.Web.Display.Graphics.GraphicsLayer layer = ESRI.ArcGIS.ADF.Web.Converter.ToGraphicsLayer(
                                        identifyTable, System.Drawing.Color.Empty, System.Drawing.Color.Aqua, System.Drawing.Color.Red, true);
                                    if (layer != null)
                                    {
                                        layerFormat.Apply(layer);
                                        formattedTable = layer;
                                    }
                                }
                            }
                            #endregion
                            #endregion

                            return formattedTable;
                            
                            
                        
                    }
                    #endregion
                }
            }
        }
        return null;
    }

}
