using System;
using ESRI.ArcGIS.ADF.Web.UI.WebControls;


/// <summary>
/// This class is a wrapper layer for 9.2 to 9.3 template compatibility
/// </summary>
public class MapIdentify 
{
	private TaskResults m_resultsDisplay = null;
	private WebMapApp.MapIdentify identify;

    #region Identify Constructors

    public MapIdentify(Map map)
    {
		identify = new WebMapApp.MapIdentify();
		identify.MapBuddyId = map.ID;
		identify.NumberDecimals = 5; // tolerance used in identify request... may need to be adjusted to a specific resource type
        identify.ID = map.ID + "_Identify";
		map.Page.Form.Controls.Add(identify);
		string create = "\nSys.Application.add_init(function() {\n\t MapIdentify = function() { MapIdentifyTool(); }; });\n";
		map.Page.ClientScript.RegisterStartupScript(this.GetType(), "MapIdentify92_startup", create, true);
    }
    #endregion

	public string Identify(System.Collections.Specialized.NameValueCollection query)
	{
		throw new NotImplementedException();
	}

    #region Properties
	public Map Map
	{
		get { throw new NotImplementedException(); }
		set { }
	}
	public TaskResults ResultsDisplay
	{
		get { return m_resultsDisplay; }
		set {
			m_resultsDisplay = value;
			identify.TaskResultsId = value.ID; 
		}
	}
	public int NumberDecimals
	{
		get { return identify.NumberDecimals; }
		set { identify.NumberDecimals = value; }
	}
	#endregion


}
