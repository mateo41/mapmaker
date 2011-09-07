using System;
using System.IO;
using System.Web;
using System.Net;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;

using ESRI.ArcGIS.ADF.ArcGISServer;
/// <summary>
/// Summary description for MapFrame
/// </summary>
public class MapFrame
{
    private MapServerProxy m_mapServerProxy;
    private MapDescription m_mapDescription;
    private ImageDescription m_imageDescription;
    

    public MapFrame( MapDescription mapDescription, MapServerProxy mapServerProxy, ImageDescription imageDescription)
	{
        m_mapDescription = mapDescription;
        m_mapServerProxy = mapServerProxy;
        m_imageDescription = imageDescription;
        
	}

    public void saveImage(ESRI.ArcGIS.ADF.ArcGISServer.MapImage image, String directory, int frameIndex)
    {

        String filename =  directory + "\\" + frameIndex + ".jpg";

        try
        {
            /*TODO Set timeout parameters */
            WebClient client = new WebClient();
            client.DownloadFile(image.ImageURL, filename);

        }
        catch
        {
            //this.Label2.Text = "Problem downloading image";
        }
        
    }
    public ESRI.ArcGIS.ADF.ArcGISServer.MapImage exportImage()
    //public void exportImage()
    {
        /*
        ESRI.ArcGIS.ADF.ArcGISServer.ImageDescription imageDescription;

        ESRI.ArcGIS.ADF.ArcGISServer.esriImageFormat imageFormat = ESRI.ArcGIS.ADF.ArcGISServer.esriImageFormat.esriImageJPG;
        int imageHeight = 400;
        int imageWidth = 280;
        int imageDpi = 100;
        ESRI.ArcGIS.ADF.ArcGISServer.esriImageReturnType returnType = ESRI.ArcGIS.ADF.ArcGISServer.esriImageReturnType.esriImageReturnURL;
        imageDescription = CreateImageDescription(imageFormat,
        imageHeight, imageWidth, imageDpi, returnType);
         
        */
        
        int i = 0;
        bool tmp1 = m_mapServerProxy.AllowAutoRedirect;
        String tmp2 = m_mapDescription.Name;
        int tmp3 = m_imageDescription.GetHashCode();

        
        
        return m_mapServerProxy.ExportMapImage(m_mapDescription, m_imageDescription);
        
        //m_mapServerProxy.ExportMapImageAsync(m_mapDescription, m_imageDescription);
        
    }

    /*
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
     * */
}
