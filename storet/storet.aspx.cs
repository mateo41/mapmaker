using System;
using System.Collections.Generic;
using System.Collections;
using System.Web.UI.WebControls;

using ESRI.ArcGIS.ADF.Web.DataSources.ArcGISServer;
using ESRI.ArcGIS.ADF.Web.UI.WebControls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Server;




public partial class map : System.Web.UI.Page
{
    
    protected void Page_Load(object sender, EventArgs e)
    {
       
        /*Create the Singleton instance the first time the page is loaded */
        if (Session["Colors"] == null)
        {
            StateSingleton state = StateSingleton.Instance;
            state.setupMap(Map1,6);
            Session.Add("Colors", state.getColors());
        }
        
        if (Session["MemberState"] == null)
        {
            MemberState memberState = new MemberState(this.XmlDataSource1.DataFile, 
                                                      this.XmlDataSource1.TransformFile,
                                                      this.XmlDataSource1.XPath);

            Session.Add("MemberState", memberState);
        }
        if (Session["DatabaseName"] == null)
        {
            string databaseName = "STORET_2009";
            Session.Add("DatabaseName", databaseName);
        }

        if (Session["SiteDim"] == null)
        {
            string siteDim = "Sites";
            Session.Add("SiteDim", siteDim);
        }

        if (Session["RegionIsInt"] == null)
        {
            Session.Add("RegionIsInt", false);
        }

        if (Session["LayerDictionary"] == null)
        {
            Dictionary<int, string> layerDictionary = new Dictionary<int, string>();
            layerDictionary.Add(0, ".[FipsState].[FipsState] ");
            layerDictionary.Add(1, ".[FipsCounty].[FipsCounty] ");
            layerDictionary.Add(2, ".[Huc1].[Huc1] ");
            layerDictionary.Add(3, ".[Huc2].[Huc2] ");
            layerDictionary.Add(4, ".[Huc3].[Huc3] ");
            layerDictionary.Add(5, ".[HUC].[HUC] ");
            Session.Add("LayerDictionary", layerDictionary);
        }
       
        
    }

    
    protected void Page_PreRender(object sender, EventArgs e)
    {
        /*Eliminates the whitespace in the top left corner
         * when the application starts up
         */

        object fixedMap = Session["fixedMap"];
        if (fixedMap == null)
        {
            this.Map1.Extent.XMin = this.Map1.Extent.XMin + 35;
            this.Map1.Extent.YMax = this.Map1.Extent.YMax - 25;
            Session.Add("fixedMap", true);
        }

        
    }
   

    protected QueryTable QueryCube(int layerid)
    {

        string database = (string)Session["DatabaseName"];
        string siteDim = (string)Session["SiteDim"];
        Dictionary<int, string> layerDictionary = (Dictionary<int, string>)Session["LayerDictionary"];
        QueryTable qt = new QueryTable(database, siteDim, layerDictionary); 
        
        MemberState ms = (MemberState)Session["MemberState"];
        bool regionIsInt = (bool)Session["RegionIsInt"];
        qt.createTableFromQuery(layerid, ms, regionIsInt);
        return qt;

    }

    protected int getTopLayer()
    {
        foreach (TreeViewPlusNode resource in this.Toc1.Nodes)
        {
            int i  = 0;
            foreach (TreeViewPlusNode layerNode in resource.Nodes)
            {
                if (layerNode.Checked)
                {
                    return i;
                }
                i++;
            }
        }
        return 0;
    }



    private IColor createRGBColor(IServerContext mapContext, int red, int green, int blue)
    {
        IColor retColor;
        RgbColor pColor;
        pColor = (RgbColor)mapContext.CreateObject("esriDisplay.RgbColor");
        pColor.Red = red;
        pColor.Green = green;
        pColor.Blue = blue;
        retColor = (IColor)pColor;

        return retColor;

    }

    private void applyRenderer(IGeoFeatureLayer geoLayer, IServerContext mapContext, string fieldName, Classifier classifier)
    {
        IClassBreaksRenderer cbRenderer = (IClassBreaksRenderer)mapContext.CreateObject("esriCarto.ClassBreaksRenderer");
        cbRenderer.Field = fieldName;

        ILegendInfo legendInfo = (ILegendInfo)cbRenderer;
        ILegendGroup legendGroup = legendInfo.get_LegendGroup(0);
        legendGroup.Heading = "Counts";

        int breaks = classifier.getBreaks();
        IColor [] pColors = new IColor[breaks];
        ISimpleFillSymbol [] symbols = new ISimpleFillSymbol[breaks];


        double breakValue;
        cbRenderer.BreakCount = breaks;
        IAlgorithmicColorRamp colorRamp = (IAlgorithmicColorRamp)mapContext.CreateObject("esriDisplay.AlgorithmicColorRamp");
        IColor startColor, endColor;

        startColor = createRGBColor(mapContext, 255, 255, 0);
        endColor = createRGBColor(mapContext, 255, 0, 0);
        colorRamp.FromColor = startColor;
        colorRamp.ToColor = endColor;
        colorRamp.Algorithm = esriColorRampAlgorithm.esriCIELabAlgorithm;
        colorRamp.Size = breaks;
        bool created;
        try
        {
            colorRamp.CreateRamp(out created);
        }
        catch
        {
            this.Label2.Text = "There is not enough data associated with the current extents and attributes selected. A map was not generated. Please try selecting other attributes.";
            return;
        }
        IEnumColors iterColors = colorRamp.Colors;

        double[] classBreaksArray = (double[])classifier.getClassBreaksArray();
        
        for (int i = 0; i < breaks; i++)
        {
            pColors[i] = iterColors.Next();
            symbols[i] = (ISimpleFillSymbol)mapContext.CreateObject("esriDisplay.SimpleFillSymbol");
            symbols[i].Color = pColors[i];
            cbRenderer.set_Symbol(i, (ISymbol)symbols[i]);

            breakValue = classBreaksArray[i + 1];
            cbRenderer.set_Break(i, breakValue);
            string breakValueString = String.Format("{0:0.00}", breakValue);
            
            if (i == 0)
            {
                cbRenderer.set_Label(i, "<" + breakValueString);
            }
            else
            {
                string prevValueString = String.Format("{0:0.00}", classBreaksArray[i]);
                string label = prevValueString + " < " + breakValueString;
                cbRenderer.set_Label(i, label);
            }
            
        }
      
        geoLayer.Renderer = (IFeatureRenderer)cbRenderer;
        this.Toc1.ExpandDepth = 1;
        this.Toc1.Refresh();
       
        
    }

    public void removeJoins(int layerid)
    {
        
        MapFunctionality mf = (MapFunctionality)Map1.GetFunctionality(0);
        MapResourceLocal mrb = (MapResourceLocal)mf.MapResource;
        IServerContext mapContext = mrb.ServerContextInfo.ServerContext;
        IMapServerObjects mapServerObjects = (IMapServerObjects)mrb.MapServer;
        IMap map = mapServerObjects.get_Map(mrb.DataFrame);
        ILayer layer = map.get_Layer(layerid);
        IFeatureLayer featureLayer = (IFeatureLayer)layer;
  

        IColor[] colors = (IColor[])Session["Colors"];

        /*Fix the renderer*/
        IGeoFeatureLayer geof = (IGeoFeatureLayer)featureLayer;
        ISimpleRenderer renderer = (ISimpleRenderer)mapContext.CreateObject("esriCarto.SimpleRenderer");
        ISimpleFillSymbol symbol = (ISimpleFillSymbol)mapContext.CreateObject("esriDisplay.SimpleFillSymbol");
        symbol.Color = colors[layerid];
        renderer.Symbol = (ISymbol)symbol;
        geof.Renderer = (IFeatureRenderer)renderer;

        esriJoinType joinType = esriJoinType.esriLeftOuterJoin;
        IDisplayRelationshipClass pDispRC = (IDisplayRelationshipClass)featureLayer;
        pDispRC.DisplayRelationshipClass(null, joinType);

        this.Toc1.Refresh();
        
    }


    public bool joinTabletoFeatureLayer(IServerContext mapContext,
        ITable externalTable,
        IFeatureLayer featureLayer,
        string tableJoinField,
        string layerJoinField,
        esriJoinType joinType)
    {
        IDisplayTable pDispTable = featureLayer as IDisplayTable;

        IFeatureClass pFCLayer = pDispTable.DisplayTable as IFeatureClass;
        ITable pTLayer = (ITable)pFCLayer;

        string strJnFieldLayer = layerJoinField;
        string strJnFieldTable = tableJoinField;

        IMemoryRelationshipClassFactory pMemRelFact = (IMemoryRelationshipClassFactory)mapContext.CreateObject("esriGeoDatabase.MemoryRelationshipClassFactory");
        IRelationshipClass pRelClass = (IRelationshipClass)pMemRelFact.Open("Join",
                                                        (IObjectClass)externalTable, strJnFieldTable,
                                                        (IObjectClass)pTLayer, strJnFieldLayer,
                                                        "forward", "backward",
                                                        esriRelCardinality.esriRelCardinalityOneToOne);


        IDisplayRelationshipClass pDispRC = (IDisplayRelationshipClass)featureLayer;
        pDispRC.DisplayRelationshipClass(pRelClass, joinType);   //esriLeftOuterJoin                                                
        IDisplayTable dt = (IDisplayTable)featureLayer;
        ITable jointable = dt.DisplayTable;
        
        bool retval = false;
        if (jointable is IRelQueryTable)
        {
            retval = true;
        }
        return retval;

        


    }

    private void DebugTable(ITable table)
    {
        int count = table.Fields.FieldCount;
        System.Collections.Generic.List<int> constructOIDList = new System.Collections.Generic.List<int>();
        for (int i = 0; i <= 50; i++)
        {
            constructOIDList.Add(i);
        }

        int[] oidList = constructOIDList.ToArray();

        IRow row;
        ICursor cursor = table.GetRows(oidList, false);

        while ((row = cursor.NextRow()) != null)
        {
            for (int i = 0; i < count; i++)
            {
                string name1 = row.Fields.get_Field(i).Name;
                this.Label2.Text += "<br>" + name1;
                this.Label2.Text += "<br>" + row.get_Value(i);
            }
        }
    }



    /*Refresh the map and register the callback
 * which executes javascript on the client */
    private void updateMap()
    {
        this.Map1.Refresh();
        if (ScriptManager1.IsInAsyncPostBack)
        {

            string callback = Map1.CallbackResults.ToString();
            ScriptManager1.RegisterDataItem(this.Map1, callback);

        }
    }

   

    protected void Button1_Click(object sender, EventArgs e)
    {


        int layerid = getTopLayer();
        if (Session["LayersSymbolized"] != null)
        {
            removeJoins(layerid);
        }
        QueryTable qt = QueryCube(layerid);
        

        this.Label2.Text = "";
        if (this.CheckBox1.Checked)
        {
            String query2 = qt.getQuery();
            this.Label2.Text = this.Label2.Text + query2;
        }


        ITable table = qt.getTable();
        MapFunctionality mf = (MapFunctionality)Map1.GetFunctionality(0);
        MapResourceLocal mrb = (MapResourceLocal)mf.MapResource;
        IServerContext mapContext = mrb.ServerContextInfo.ServerContext;
        IMapServerObjects mapServerObjects = (IMapServerObjects)mrb.MapServer;


        IMap map = mapServerObjects.get_Map(mrb.DataFrame);

        ILayer layer = map.get_Layer(layerid);
        IFeatureLayer featureLayer = (IFeatureLayer)layer;
        String region = table.Fields.get_Field(1).AliasName;

        bool success = joinTabletoFeatureLayer(mapContext, table, featureLayer, region,
                                               featureLayer.DisplayField,
                                               esriJoinType.esriLeftOuterJoin);


        IDisplayTable dt = (IDisplayTable)featureLayer;
        ITable joinTable = dt.DisplayTable;
        

        int breaks = Convert.ToInt32(this.DropDownList2.SelectedValue);
        //qt.calcFrequencies();
        qt.calcFrequencies(joinTable);
        double[] values = qt.getValues();
        int[] counts = qt.getCounts();

        String classifierType = this.DropDownList3.SelectedValue;
        Classifier classifier = new Classifier(mapContext, values,counts, classifierType, breaks);
        applyRenderer((IGeoFeatureLayer)featureLayer, mapContext, "joinedTable.Measure", classifier);
        IGeoFeatureLayer geof = (IGeoFeatureLayer)featureLayer;

        
        if (Session["LayersSymbolized"] == null)
        {

            Dictionary<int, bool> layersDict = new Dictionary<int, bool>();
            layersDict[layerid] = true;
            Session.Add("LayersSymbolized", layersDict);
        }

        updateMap();
        //exportImage();
        
    }
    /*
    public void exportImage()
    {
        ImageDescription imageDescription;
        
        MapFunctionality mapFunctionality = (MapFunctionality)Map1.GetFunctionality(0); ;
        ESRI.ArcGIS.ADF.ArcGISServer.MapDescription mapDescription = mapFunctionality.MapDescription;
        
        ESRI.ArcGIS.ADF.ArcGISServer.Envelope exportedExtent = null;
        //Set the current map extent
        
        mapDescription.MapArea.Extent = (exportedExtent != null)
        ? exportedExtent :
        ESRI.ArcGIS.ADF.Web.DataSources.ArcGISServer.Converter.FromAdfEnvelope(Map1.Extent);

        esriImageFormat imageFormat = esriImageFormat.esriImageJPG;
        int imageHeight = 120;
        int imageWidth = 80;
        int imageDpi = 20;
        esriImageReturnType returnType = esriImageReturnType.esriImageReturnURL;
        imageDescription = CreateImageDescription(imageFormat,
        imageHeight, imageWidth, imageDpi, returnType);

        MapResourceBase mapResource = (MapResourceBase)mapFunctionality.Resource;
        mapResource.Map
        MapServerProxy mapServerProxy = mapResource.MapServerProxy;
        // Return MapImage class
        return mapServerProxy.ExportMapImage(mapDescription, imageDescription);
    }

    private static ESRI.ArcGIS.ADF.ArcGISServer.ImageDescription CreateImageDescription(ESRI.ArcGIS.ADF.ArcGISServer.esriImageFormat
   imageFormat, int imageHeight, int imageWidth, double imageDpi,
   ESRI.ArcGIS.ADF.ArcGISServer.esriImageReturnType returnType)
    {
        ImageDescription imageDescription = new ImageDescription();
        ImageType imageType = new ImageType();
        imageType.ImageFormat = imageFormat;

        // Return url to map image or MIME data
        imageType.ImageReturnType = returnType;
        ImageDisplay imageDisplay = new ImageDisplay();
        imageDisplay.ImageHeight = imageHeight;
        imageDisplay.ImageWidth = imageWidth;
        imageDisplay.ImageDPI = imageDpi;
        imageDescription.ImageDisplay = imageDisplay;
        imageDescription.ImageType = imageType;
        return imageDescription;
    }
     */ 
    protected void Button2_Click(object sender, EventArgs e)
    {

        int layerid = getTopLayer();
        this.Label2.Text = "";
        removeJoins(layerid);

        this.CheckBoxList1.Visible = false;
        Stack checkedNodesStack = new Stack();
        foreach (TreeNode node in this.TreeView2.CheckedNodes)
        {
            checkedNodesStack.Push(node.ValuePath);
        }

        foreach (String ValuePath in checkedNodesStack)
        {
            TreeView2.FindNode(ValuePath).Checked = false;
        }

        Session.Remove("MemberState");
        MemberState memberState = new MemberState(this.XmlDataSource1.DataFile,
                                                      this.XmlDataSource1.TransformFile,
                                                      this.XmlDataSource1.XPath);

        Session.Add("MemberState", memberState);
        this.CheckBoxList1.Visible = false;
        this.TextBox1.Visible = false;
        this.UpdatePanel1.Update();
        updateMap();
    }
    protected void TreeView2_CheckChanged(object sender, System.Web.UI.WebControls.TreeNodeEventArgs e)
    {
        
        String xpath;
        MemberState ms = (MemberState)Session["MemberState"];
        if (e.Node.Checked)
        {
            
            this.TextBox1.Visible = true;
            this.TextBox1.Text = e.Node.Value;
            //String database = ms.getDatabaseString();
            xpath = "//" + e.Node.ValuePath + "//Member";
            this.XmlDataSource2.XPath = xpath;
            ms.pushStack(e.Node.ValuePath);


            this.CheckBoxList1.Visible = true;

        }
        else
        {
            
            if (e.Node.ValuePath == (String)ms.getCurrentAttr())
            {
                ms.popStack();
                if (ms.emptyStack())
                {
                    this.TextBox1.Visible = false;
                    this.CheckBoxList1.Visible = false;
                }
                else
                {
                    xpath = "//" + (string)ms.getCurrentAttr() + "//Member";
                    this.XmlDataSource2.XPath = xpath;
                    string[] pathparts = xpath.Split('/');
                    this.TextBox1.Text = pathparts[pathparts.Length - 3];
                }
            }
            else
            {
                ms.removeFromStack(e.Node.ValuePath);

            }
            
        }
        
    }


    protected void CheckBoxList1_SelectedIndexChanged(object sender, EventArgs e)
    {
        MemberState ms = (MemberState)Session["MemberState"];
        String currentAttr = ms.getCurrentAttr();
        String currentAttrPath = "/" + currentAttr;
        this.Label2.Text += this.CheckBoxList1.SelectedIndex.ToString();

        foreach (ListItem item in this.CheckBoxList1.Items)
        {
            ms.setCheckBoxState(currentAttrPath, item.Value, item.Selected);
        }    
    }

    protected void CheckBoxList1_PreRender(object sender, EventArgs e)
    {
        
        this.restoreCheckboxList1();
    }
    
    private void restoreCheckboxList1()
    {
        
        MemberState ms = (MemberState)Session["MemberState"];
        String currentAttr = ms.getCurrentAttr();
        String currentAttrPath = "/" + currentAttr;
       
        foreach (ListItem item in this.CheckBoxList1.Items)
        {
            item.Selected = ms.getCheckBoxState(currentAttrPath, item.Value);
        }
       
    }

    
    protected void Toc1_NodeChecked(object sender, TreeViewPlusNodeEventArgs args)
    {
        if (Session["LayersSymbolized"] != null)
        {
            Dictionary<int, bool> layersDict = (Dictionary<int, bool>)Session["LayersSymbolized"];
            foreach (int layerid in layersDict.Keys)
            {
                if (layersDict[layerid])
                {
                    removeJoins(layerid);
                }
                Session.Remove("LayersSymbolized");
            }
        }

    }

    protected void DropDownList1_PreRender(object sender, EventArgs e)
    {
        MemberState ms = (MemberState)Session["MemberState"];
        ms.setMeasureString(this.DropDownList1.SelectedValue);
    }

    protected void Button3_Click(object sender, EventArgs e)
    {
        String database, cube, host, sitesDim;
        database = "STORET_2009";
        cube = "STORET_2009";
        host = "kyle";
        sitesDim = (String)Session["SitesDim"];
        Dictionary<int, String> layerDictionary = (Dictionary<int, String>)Session["LayerDictionary"];
        CubeStructure cs = new CubeStructure(database, cube, host, layerDictionary, sitesDim);
    }

}
