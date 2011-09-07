using System;
using System.Collections.Generic;
using System.Collections;
using System.Web.UI.WebControls;
using System.Xml;
using System.IO;
using System.Diagnostics;
using System.Threading;

using ESRI.ArcGIS.ADF.Web.DataSources.ArcGISServer;
using ESRI.ArcGIS.ADF.Web.UI.WebControls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Server;


public partial class map : System.Web.UI.Page
{
    /*To use with a different Map Service and data cube
     * Change the DatabaseName, SiteDim and LayerDictionary Session Variables
     * 
     * DatabaseName is the name of the Analysis Services database
     * SiteDim is the name of the dimension in the cube that has the spatial attributes
     * LayerDictionary contains the mapping between layerids in the Map to the attributes 
     * in the site dimension.
     */


    protected void Page_Load(object sender, EventArgs e)
    {
     
        /*Create the Singleton instance the first time the page is loaded */
        if (Session["Colors"] == null)
        {
            StateSingleton state = StateSingleton.Instance;
            int numLayers = 1;
            state.setupMap(Map1, numLayers);
            Session.Add("Colors", state.getColors());
            Session.Add("Lines", state.getLines());
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
            string databaseName = "CBEO_MAP";
            Session.Add("DatabaseName", databaseName);
        }

        if (Session["SiteDim"] == null)
        {
            string siteDim = "Boxes";
            Session.Add("SiteDim", siteDim);
        }
        /*The region could be a string or an int*/
        if (Session["RegionIsInt"] == null)
        {
            Session.Add("RegionIsInt", true);
        }
        
        /*LayerDictionary maps a layerid to an MDX expression that specifies the
         * attributes used when the MDX queries are generated.
         */ 
        if (Session["LayerDictionary"] == null)
        {
            Dictionary<int, string> layerDictionary = new Dictionary<int, string>();
            layerDictionary.Add(0, ".[Surfbox].[Surfbox] ");
            Session.Add("LayerDictionary", layerDictionary);
        }

        if (Session["movieid"] == null)
        {
            Session.Add("movieid", 0);
        }

        if (Session["CrossSectionFileName"] == null){

            Session.Add("CrossSectionFileName", "G:\\matt\\temp\\crossSection.pdf");
        }

    }

    protected QueryTable QueryCube(int layerid)
    {

        string  database = (string)Session["DatabaseName"];
        string  siteDim  = (string)Session["SiteDim"];
        Dictionary<int,string> layerDictionary = (Dictionary<int,string>)Session["LayerDictionary"];
        QueryTable qt = new QueryTable(database, siteDim , layerDictionary );  
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
        colorRamp.CreateRamp(out created);
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
       
        
    }

    public void removeJoins(int layerid)
    {

        IFeatureLayer featureLayer = getFeatureLayer(layerid);
        IServerContext mapContext = getMapContext();

        IColor[] colors = (IColor[])Session["Colors"];
        ILineSymbol[] lines = (ILineSymbol[])Session["Lines"];

        /*Fix the renderer*/
        IGeoFeatureLayer geof = (IGeoFeatureLayer)featureLayer;
        ISimpleRenderer renderer = (ISimpleRenderer)mapContext.CreateObject("esriCarto.SimpleRenderer");
        IFillSymbol symbol = (ISimpleFillSymbol)mapContext.CreateObject("esriDisplay.SimpleFillSymbol");
        
        symbol.Color = colors[layerid];
        symbol.Outline = lines[layerid];
        renderer.Symbol = (ISymbol)symbol;
        geof.Renderer = (IFeatureRenderer)renderer;

        esriJoinType joinType = esriJoinType.esriLeftOuterJoin;
        IDisplayRelationshipClass pDispRC = (IDisplayRelationshipClass)featureLayer;
        pDispRC.DisplayRelationshipClass(null, joinType);     

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

    protected ArrayList generateAnimationQueries(int layerid)
    {
        ArrayList queryTables = new ArrayList();
        MemberState ms = (MemberState)Session["MemberState"];
        String attr = '/' + ms.getCurrentAttr();
        ArrayList attrMembers = ms.getAttributeMembers(attr);
        foreach (String member in attrMembers){
            this.Label2.Text += "\r\n";
            this.Label2.Text += member;
        }
        ArrayList queries = new ArrayList();
        QueryTable qt;
        String query;
        foreach (String member in attrMembers)
        {
            ms.setCheckBoxState(attr, member, true);
            qt = QueryCube(layerid);
            queryTables.Add(qt);
            query = qt.getQuery();
            ms.setCheckBoxState(attr, member, false);
        }
        int i = 0;
        foreach (QueryTable queryTable in queryTables)
        {
            String num = Convert.ToString(i);
            String filename = "data" + num + ".csv";
            queryTable.writeCSV("G:\\CBEO\\" + filename);
            i++;
        }
        return queryTables;
    }

    protected void Button1_Click(object sender, EventArgs e)
    {

        MemberState ms = (MemberState)Session["MemberState"];



        int layerid = getTopLayer();
        if (Session["LayersSymbolized"] != null)
        {
            removeJoins(layerid);
            this.Toc1.Refresh();
        }
        
        if (ms.getAnimateAttribute())
        {            
            ArrayList queryTables = generateAnimationQueries(layerid);
            Session.Add("queryTables", queryTables);
            String parentDirectoryName = System.Web.HttpContext.Current.Server.MapPath("Videos");
            Session.Add("parentDirectoryName", parentDirectoryName);
            this.Label2.Text = "Processing Animation ... this may take a while. \n";
            this.Label2.Text += "There are " + queryTables.Count + " frames \n";
            this.UpdatePanel2.Update();
            
            Session.Add("runAnimation", true);
            runAnimation();
            
        }
        else
        {
            createMap(layerid);
            this.Toc1.Refresh();
        }
        
    }

    
    private void runAnimation(){
        
        ArrayList queryTables = (ArrayList)Session["queryTables"];
        int movieid = (int)Session["movieid"];
        String parentDirectoryName = (String)Session["parentDirectoryName"];
        String directoryName = parentDirectoryName + "\\" + Session.GetHashCode() + '-' + movieid;
        DirectoryInfo dirInfo = Directory.CreateDirectory(directoryName);
        Session.Add("directoryName", directoryName);

        /*This statement caches the MapResourceLocal object in the Session variable
        * It has to be here */
        MapResourceLocal mrl = getMapResource();
        ESRI.ArcGIS.ADF.ArcGISServer.MapServerProxy msp = getMapServerProxy();
        ESRI.ArcGIS.ADF.ArcGISServer.MapDescription mapDescription = getMapDescription();
        ESRI.ArcGIS.ADF.ArcGISServer.ImageDescription imageDescription = getImageDescription();

        Thread thread = new Thread(launchThreads);
        thread.IsBackground = true;
        thread.Start(queryTables);
        thread.Join();
        //createLink(movieid);
    }
    
    private void launchThreads(object tables)
    {
        ArrayList queryTables = (ArrayList)tables;
        ArrayList tlist = new ArrayList();

        int layerid = getTopLayer();
        IServerContext mapContext = getMapContext();
        IFeatureLayer featureLayer = getFeatureLayer(layerid);
        Classifier classifier = createClassifier((QueryTable)queryTables[0], mapContext, featureLayer);
        Session.Add("classifier", classifier);

        for (int i = 0; i < queryTables.Count; i++)
        {
            Thread thread = new Thread(createFrame);
            thread.IsBackground = true;
            thread.Start(i);
            
            tlist.Add(thread);
        }
        for (int i = 0; i < queryTables.Count; i++)
        {
            Thread thread = (Thread)tlist[i];
            thread.Join();
        }
        String directoryName = (String)Session["DirectoryName"];
        createMovie(directoryName);
        Session.Remove("runAnimation");
    }
    private void createFrame(object indexObject)
    {
        int layerid = getTopLayer();
        int frameIndex = Convert.ToInt32(indexObject);
        
        ArrayList queryTables = (ArrayList)Session["queryTables"];

        
        IServerContext mapContext = getMapContext();
        IFeatureLayer featureLayer = getFeatureLayer(layerid);

        Classifier classifier = (Classifier)Session["classifier"];
       
        String directoryName = (String)Session["directoryName"];
        QueryTable queryTable = (QueryTable)queryTables[frameIndex];
        joinTable(queryTable.getTable(), mapContext, featureLayer);
        renderMap(layerid, mapContext, featureLayer, classifier);
        generateFrame(frameIndex, directoryName);
        removeJoins(layerid);
        this.Label2.Text += "Rendered frame: " + frameIndex + "\n";
        //this.UpdatePanel2.Update();

    }

    private void createMovie(String directoryName)
    {
        this.Label2.Text += "Animation complete";
        String argument = " -i " + directoryName + "\\%d.jpg " + directoryName + "\\" + Session.GetHashCode() + ".wmv";
        ProcessStartInfo psi = new ProcessStartInfo();
        String parentDirectoryName = (String)Session["parentDirectoryName"];

        psi.FileName = parentDirectoryName + "\\ffmpeg.exe";
        psi.Arguments = argument;

        Process p = Process.Start(psi);
        p.WaitForExit();
    }

    /*
    private void createLink(int movieid)
    {
        String url = System.Web.HttpContext.Current.Request.Url.PathAndQuery;
        String[] tokens = url.Split('/');
        this.HyperLink1.NavigateUrl = "/" + tokens[1] + "/Videos/" + Session.GetHashCode() + '-' + movieid + "/" + Session.GetHashCode() + ".wmv";
        this.HyperLink1.Visible = true;
        movieid++;
        Session["movieid"] = movieid;
    }
    */
    private void createMap(int layerid)
    {
        QueryTable queryTable = QueryCube(layerid);


        this.Label2.Text = "";
        if (this.CheckBox1.Checked)
        {
            String query2 = queryTable.getQuery();
            this.Label2.Text = this.Label2.Text + query2;
        }


        ITable table = queryTable.getTable();


        IServerContext mapContext = getMapContext();
        IFeatureLayer featureLayer = getFeatureLayer(layerid);


        Classifier classifier = createClassifier(queryTable, mapContext, featureLayer);
        joinTable(table, mapContext, featureLayer);

        renderMap(layerid, mapContext, featureLayer, classifier);


        updateMap();
        
    }

    private MapResourceLocal getMapResource()
    {
        MapResourceLocal mrl;
        if (Session["mapResourceLocal"] == null)
        {
            MapFunctionality mf = (MapFunctionality)Map1.GetFunctionality(0);
            mrl = (MapResourceLocal)mf.MapResource;
            Session.Add("mapResourceLocal", mrl);

        }
        else
        {
            mrl = (MapResourceLocal)Session["mapResourceLocal"];
        }
        return mrl;
    }

    private ESRI.ArcGIS.ADF.ArcGISServer.ImageDescription getImageDescription()
    {

        ESRI.ArcGIS.ADF.ArcGISServer.ImageDescription imageDescription;
        if (Session["imageDescription"] == null) {
            ESRI.ArcGIS.ADF.ArcGISServer.esriImageFormat imageFormat = ESRI.ArcGIS.ADF.ArcGISServer.esriImageFormat.esriImageJPG;
            int imageHeight = 400;
            int imageWidth = 280;
            int imageDpi = 100;
            ESRI.ArcGIS.ADF.ArcGISServer.esriImageReturnType returnType = ESRI.ArcGIS.ADF.ArcGISServer.esriImageReturnType.esriImageReturnURL;
            imageDescription = CreateImageDescription(imageFormat, imageHeight, 
                                                      imageWidth, imageDpi, returnType);
        }
        else 
        {
            imageDescription = (ESRI.ArcGIS.ADF.ArcGISServer.ImageDescription)Session["imageDescription"];        
        }
        return imageDescription;
    }

    private static ESRI.ArcGIS.ADF.ArcGISServer.ImageDescription CreateImageDescription(ESRI.ArcGIS.ADF.ArcGISServer.esriImageFormat
imageFormat, int imageHeight, int imageWidth, double imageDpi,
ESRI.ArcGIS.ADF.ArcGISServer.esriImageReturnType returnType)
    {

        ESRI.ArcGIS.ADF.ArcGISServer.ImageDescription imageDescription = new ESRI.ArcGIS.ADF.ArcGISServer.ImageDescription();
        ESRI.ArcGIS.ADF.ArcGISServer.ImageType imageType = new ESRI.ArcGIS.ADF.ArcGISServer.ImageType();
        imageType.ImageFormat = imageFormat;

        // Return url to map image or MIME data
        imageType.ImageReturnType = returnType;
        ESRI.ArcGIS.ADF.ArcGISServer.ImageDisplay imageDisplay = new ESRI.ArcGIS.ADF.ArcGISServer.ImageDisplay();
        imageDisplay.ImageHeight = imageHeight;
        imageDisplay.ImageWidth = imageWidth;
        imageDisplay.ImageDPI = imageDpi;
        imageDescription.ImageDisplay = imageDisplay;
        imageDescription.ImageType = imageType;
        return imageDescription;
    }

    private ESRI.ArcGIS.ADF.ArcGISServer.MapDescription getMapDescription()
    {

        ESRI.ArcGIS.ADF.ArcGISServer.MapDescription md;
        
        if (Session["mapDescription"] == null)
        {
            MapFunctionality mapFunctionality = (MapFunctionality)Map1.GetFunctionality(0);
            md = mapFunctionality.MapDescription;
            
            Session.Add("mapDescription", md);
        }
        else
        {
            md = (ESRI.ArcGIS.ADF.ArcGISServer.MapDescription)Session["mapDescription"];
        }
        return md;

    }

    private ESRI.ArcGIS.ADF.ArcGISServer.MapServerProxy getMapServerProxy()
    {

        ESRI.ArcGIS.ADF.ArcGISServer.MapServerProxy msp;
        if (Session["mapServerProxy"] == null)
        {
            MapFunctionality mapFunctionality = (MapFunctionality)Map1.GetFunctionality(0);
            MapResourceBase mapResource = (MapResourceBase)mapFunctionality.Resource;
            msp = mapResource.MapServerProxy;
            Session.Add("mapServerProxy", msp);
        }
        else
        {
            msp = (ESRI.ArcGIS.ADF.ArcGISServer.MapServerProxy)Session["mapServerProxy"];
        }
        
        return msp;
    }
    private IServerContext getMapContext(){
        MapResourceLocal mrl = getMapResource();
        IServerContext mapContext = mrl.ServerContextInfo.ServerContext;
        
        return mapContext;
    }


    private IFeatureLayer getFeatureLayer(int layerid){
        MapResourceLocal mrb = getMapResource();
        IMapServerObjects mapServerObjects = (IMapServerObjects)mrb.MapServer;
        IMap map = mapServerObjects.get_Map(mrb.DataFrame);
        ILayer layer = map.get_Layer(layerid);
        IFeatureLayer featureLayer = (IFeatureLayer)layer;
        
        return featureLayer;
    }

    private void joinTable(ITable table, IServerContext mapContext, IFeatureLayer featureLayer)
    {
        String region = table.Fields.get_Field(1).AliasName;
        ITable layerTable = (ITable)featureLayer;
        string layerField = layerTable.Fields.get_Field(4).AliasName;

        bool success = joinTabletoFeatureLayer(mapContext, table, featureLayer, region,
                                              layerField,
                                              esriJoinType.esriLeftOuterJoin);
    }

    private void renderMap(int layerid, IServerContext mapContext, IFeatureLayer featureLayer, Classifier classifier)
    {
        applyRenderer((IGeoFeatureLayer)featureLayer, mapContext, "joinedTable.Measure", classifier);

        if (Session["LayersSymbolized"] == null)
        {

            Dictionary<int, bool> layersDict = new Dictionary<int, bool>();
            layersDict[layerid] = true;
            Session.Add("LayersSymbolized", layersDict);
        }
    }

    private Classifier createClassifier(QueryTable queryTable, IServerContext mapContext, IFeatureLayer featureLayer)
    {
        IDisplayTable dt = (IDisplayTable)featureLayer;
        ITable joinTable = dt.DisplayTable;


        int breaks = Convert.ToInt32(this.DropDownList2.SelectedValue);
        String classifierType = this.DropDownList3.SelectedValue;
        queryTable.calcFrequencies();
        //queryTable.calcFrequencies(joinTable);
        double[] values = queryTable.getValues();
        int[] counts = queryTable.getCounts();


        Classifier classifier = new Classifier(mapContext, values, counts, classifierType, breaks);
        return classifier;
    }

    protected void generateFrame(int frameIndex, String directory)
    {
        //MapFunctionality mapFunctionality = (MapFunctionality)Map1.GetFunctionality(0);
        //mapDescription = mapFunctionality.MapDescription;

        ESRI.ArcGIS.ADF.ArcGISServer.MapDescription mapDescription = getMapDescription();
        ESRI.ArcGIS.ADF.ArcGISServer.ImageDescription imageDescription = getImageDescription();
        ESRI.ArcGIS.ADF.ArcGISServer.Envelope exportedExtent = null;
        //Set the current map extent

        mapDescription.MapArea.Extent = (exportedExtent != null)
        ? exportedExtent :
        ESRI.ArcGIS.ADF.Web.DataSources.ArcGISServer.Converter.FromAdfEnvelope(Map1.Extent);
        //MapResourceBase mapResource = (MapResourceBase)mapFunctionality.Resource;
        //ESRI.ArcGIS.ADF.ArcGISServer.MapServerProxy mapServerProxy = mapResource.MapServerProxy;
        ESRI.ArcGIS.ADF.ArcGISServer.MapServerProxy mapServerProxy = getMapServerProxy();
        MapFrame frame = new MapFrame(mapDescription, mapServerProxy, imageDescription);
        //frame.exportImage();
        ESRI.ArcGIS.ADF.ArcGISServer.MapImage mapImage = frame.exportImage();
        
        frame.saveImage(mapImage, directory, frameIndex);
    }


    protected void Button2_Click(object sender, EventArgs e)
    {

        clearState();
    }

    private void clearState()
    {
        int layerid = getTopLayer();
        this.Label2.Text = "";
        removeJoins(layerid);
        this.Toc1.Refresh();
        this.CheckBoxList1.Visible = false;
        this.CheckBox2.Checked = false;
        //this.HyperLink1.Visible = false;
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

        this.UpdatePanel2.Update();
        if (e.Node.Checked)
        {

            if (ms.getAnimate())
            {
                if (ms.getAnimateAttribute())
                {
                    this.Label2.Text = "Animation Attribute already set\n";
                    this.UpdatePanel2.Update();
                    e.Node.Checked = false;
                    return;
                }
                else
                {
                    ms.setAnimateAttribute(true);
                }
            }
            this.TextBox1.Visible = true;
            this.TextBox1.Text = e.Node.Value;
            xpath = "//" + e.Node.ValuePath + "//Member";
            this.XmlDataSource2.XPath = xpath;
            ms.pushStack(e.Node.ValuePath);
            this.CheckBoxList1.Visible = true;

        }
        else
        {
            if (ms.getAnimate())
            {
                ms.setAnimateAttribute(false);
            }
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

        if (ms.getAnimate())
        {
            foreach (ListItem item in this.CheckBoxList1.Items)
            {
                item.Selected = true;
            }
        }
        else
        {
            foreach (ListItem item in this.CheckBoxList1.Items)
            {
                item.Selected = ms.getCheckBoxState(currentAttrPath, item.Value);
            }
        }
       
    }
    protected void Button6_Click(object sender, EventArgs e)
    {
        String database, cube, host, sitesDim;
        database = "CBEO_MAP";
        cube = "CBEO_MAP";
        host = "kyle";
        sitesDim = (String)Session["SitesDim"];
        Dictionary<int, String> layerDictionary = (Dictionary<int, String>)Session["LayerDictionary"];
       
        CubeStructure cs = new CubeStructure(database, cube, host, layerDictionary, sitesDim);
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
                    this.Toc1.Refresh();
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
   
    protected void AnimateCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        MemberState ms = (MemberState)Session["MemberState"];
        this.Label2.Text += "Animate checked \n";
       
        ms.setAnimate(CheckBox2.Checked);
        if (CheckBox2.Checked == false)
        {
            clearState();
        }
    }


    protected void Panel1_Disposed(object sender, EventArgs e)
    {
        int i = 3;
    }
    protected void Toolbar1_CommandClick(object sender, ToolbarCommandClickEventArgs args)
    {
        int i = 3;
    }
}
