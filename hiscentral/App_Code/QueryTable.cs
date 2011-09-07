using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Carto;

using Microsoft.AnalysisServices.AdomdClient;


/// <summary>
/// Summary description for QueryTable
/// </summary>
public class QueryTable
{

    private DataTable m_dataTable;
    private ITable m_table;
    private String m_query;
    private string m_databaseName;
    private string m_siteDim;
    private Dictionary<int, string> m_layerDictionary;
    private double[] m_histogramValues;
    private int[] m_histogramCounts;
    private Dictionary<double, int> m_histogramDictionary;
    public QueryTable(string databaseName, string siteDim, Dictionary<int,string> layerDictionary)
    {
        
        m_databaseName = databaseName;
        m_siteDim = siteDim;
        m_layerDictionary = layerDictionary;

        m_histogramDictionary = new Dictionary<double, int>();

    }

    /* This method queries the data cube, puts the results into a DataTable then puts the results
     * into an ArcTable
     * 
     * layerid is an int corresponding to the top layer on the map
     * MemberState is a data structure that contains all of the attribute members that
     * have been selected
     * 
     * 
     */ 
    public void createTableFromQuery(int layerid, MemberState ms, bool regionIsInt)
    {
        String connectionString;
        

        connectionString = "Initial Catalog=" + m_databaseName + "; Data Source=kyle; integrated security=sspi";
        AdomdConnection conn = new AdomdConnection(connectionString);
        conn.Open();

        AdomdCommand cmd = new AdomdCommand();
        CellSet cells;
        cmd.Connection = conn;

        MDXQueryBuilder mdxBuilder = new MDXQueryBuilder(m_layerDictionary,m_siteDim);
        String mdxQuery = mdxBuilder.buildQuery(layerid, ms);
        m_query = mdxQuery;

        cmd.CommandText = mdxQuery;
        cells = cmd.ExecuteCellSet();

        DataTable dt = dataTableFromCells(cells);
        m_dataTable = dt;
      
        ITable table = CreateArcTable(dt, regionIsInt);
        m_table = table;
    }

    public void writeCSV(String filename)
    {
        StreamWriter sw = new StreamWriter(filename);
        sw.Write("Region,Measure");
        sw.WriteLine("");
        foreach (DataRow dr in m_dataTable.Rows)
        {
            for (int i = 0; i < m_dataTable.Columns.Count; i++)
            {
                
                sw.Write(Convert.ToString(dr[i]));
                if (i < m_dataTable.Columns.Count - 1)
                {
                    sw.Write(",");
                }
            }
            sw.WriteLine("");
        }
        sw.Close();
    }

    public void calcFrequencies()
    {
        ArrayList v = new ArrayList(m_histogramDictionary.Keys);
        v.Sort();
       
        double [] values = new double[m_histogramDictionary.Count];
        int[] counts = new int[m_histogramDictionary.Count];
        int i = 0;
        foreach (double d in v){
            values[i] = d;
            counts[i] = m_histogramDictionary[values[i]];
            i++;
        }        

        m_histogramValues = values;
        m_histogramCounts = counts;
    }

    public void calcFrequencies(ITable table)
    {
        IBasicHistogram basicHist = new BasicTableHistogramClass();
        ITableHistogram tableHist = (ITableHistogram)basicHist;
        tableHist.Table = table;
        tableHist.Field = "Measure";

        object uniqueValues;
        object valueCounts;
        
        basicHist.GetHistogram(out uniqueValues, out  valueCounts);
        m_histogramValues = (double[])uniqueValues;
        m_histogramCounts = (int[])valueCounts;

        
    }
    public double[] getValues()
    {
        return m_histogramValues;
    }
    public int[] getCounts()
    {
        return m_histogramCounts;
    }


    
    /* Put the results of the MDX query into a dataTable. 
     Call filterCellSet first if there are cells that do not 
     correspond to a polygon in the shapefile. This method also
     populates the histogram dictionary. This allows us to create 
     the histogram data without using the ESRI API which is too slow.
     */
    
    private DataTable dataTableFromCells(CellSet cells)
    {
        TupleCollection axis1 = cells.Axes[1].Set.Tuples;
        DataTable dt = createDataTable();

        for (int i = 0; i < axis1.Count; i++)
        {
            string region = axis1[i].Members[0].Caption;
            double measure;

            /*Ensure the value is not null and the region is not null or the empty string*/
            if (cells[0, i].Value != null && region != null && region != "")
            {
                measure = Convert.ToDouble(cells[0, i].Value);

                object[] row = new object[2];
                row[0] = region;
                row[1] = measure;
                
                dt.LoadDataRow(row, false);
                if (m_histogramDictionary.ContainsKey(measure))
                {
                    m_histogramDictionary[measure] = m_histogramDictionary[measure] + 1;
                }
                else
                {
                    m_histogramDictionary.Add(measure, 1);
                }
                
            }
        }
        return dt;
    }


    public ITable getTable()
    {
        return m_table;
    }

    public String getQuery()
    {
        return m_query;
    }
 
    private static DataTable createDataTable()
    {
        DataTable dt = new DataTable();

        DataColumn col1 = new DataColumn();
        col1.ColumnName = "Region";
        col1.DataType = System.Type.GetType("System.String");

        DataColumn col2 = new DataColumn();
        col2.ColumnName = "Measure";
        col2.DataType = System.Type.GetType("System.Decimal");

        dt.Columns.Add(col1);
        dt.Columns.Add(col2);
        
        return dt;
    }

    private IField createField(String fieldName, esriFieldType type)
    {
        IFieldEdit fe = new FieldClass();
        fe.Name_2 = fieldName;
        fe.Type_2 = type;

        IField f = (IField)fe;
        return f;
    }

    private ITable CreateArcTable(DataTable dt, bool regionIsInt)
    {
        IWorkspaceFactory workspaceFactory = new InMemoryWorkspaceFactoryClass();
        IWorkspaceName workspaceName = workspaceFactory.Create("", "MyWorkspace",
null, 0);

        IName name = (IName)workspaceName;
        IFeatureWorkspace workspace = (IFeatureWorkspace)name.Open();
        IFieldsEdit fe;

        fe = new FieldsClass();
        IField oid, key, measure;
        oid = createField("ObjectId", esriFieldType.esriFieldTypeOID);
        if (regionIsInt)
        {
            key = createField("Region", esriFieldType.esriFieldTypeInteger);
        }
        else
        {
            key = createField("Region", esriFieldType.esriFieldTypeString);
        }
        
        measure = createField("Measure", esriFieldType.esriFieldTypeDouble);
        fe.AddField(oid);
        fe.AddField(key);
        fe.AddField(measure);

        UID clsid = new UIDClass();
        clsid.Value = "esriGeoDatabase.Object";
        ITable table = workspace.CreateTable("joinedTable", (IFields)fe, clsid, null, null);

        IRowBuffer rowBuffer = table.CreateRowBuffer();
        ICursor cursor = table.Insert(true);

        foreach (DataRow dr in dt.Rows)
        {
            /*Set the Region and the Measure fields 
             The dataTable has 2 columns and the ArcTable has 3 columns.
             The first column in the OID field which is generated internally 
             by the ArcGIS engine.*/
            for (int x = 1; x <= dt.Columns.Count; x++)
            {
                rowBuffer.set_Value(x, dr[x-1]);
            }
            cursor.InsertRow(rowBuffer);
        }
        
        cursor.Flush();



        return table;
    }



}
