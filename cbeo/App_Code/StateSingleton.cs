
using ESRI.ArcGIS.ADF.Web.DataSources.ArcGISServer;

using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Server;
using ESRI.ArcGIS.Display;



/// <summary>
/// Summary description for StateSingleton
/// </summary>
/* This retrieves the colors from the layers when the page is 
 * first loaded. The page is only loaded once per session because
 * the application events use AJAX partial postbacks instead of
 * full postbacks. The object is instatiated, the colors are retrieved
 * from the layers in the setupMap method. The getColors method is used
 * to retrieve the array of IColors. These colors are used to return
 * the layer to its original color when the symbolization is removed.
 * */
public class StateSingleton
{
    private static StateSingleton instance;
    private static IColor[] m_colors;
    private static ILineSymbol[] m_lines;
    
    

    private StateSingleton()
    {
        
    }

    public static StateSingleton Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new StateSingleton();
            }
            return instance;
        }
    }
    public void setupMap(ESRI.ArcGIS.ADF.Web.UI.WebControls.Map map1, int numLayers)
    {
        MapFunctionality mf = (MapFunctionality)map1.GetFunctionality(0);
        MapResourceLocal mrb = (MapResourceLocal)mf.MapResource;
        IServerContext mapContext = mrb.ServerContextInfo.ServerContext;
        IMapServerObjects mapServerObjects = (IMapServerObjects)mrb.MapServer;
        IMap map = mapServerObjects.get_Map(mrb.DataFrame);

        
        IEnumLayer layers = map.get_Layers(null, true);
        ILayer layer;
        StateSingleton.m_colors = new IColor[numLayers];
        StateSingleton.m_lines = new ILineSymbol[numLayers];
        IGeoFeatureLayer geoLayer;
        ISimpleRenderer renderer;
        IFillSymbol symbol;
        int i = 0;


        layer = layers.Next();
        while (i < numLayers){
                geoLayer = (IGeoFeatureLayer)layer;
                renderer = (ISimpleRenderer)geoLayer.Renderer;
                symbol = (IFillSymbol)renderer.Symbol;
                m_lines[i] = symbol.Outline;
                m_colors[i] = symbol.Color;
                i++;
                layer = layers.Next();
        }
        

    }
    
    public IColor[] getColors()
    {
        return m_colors;
    }

    public ILineSymbol[] getLines()
    {
        return m_lines;
    }

}